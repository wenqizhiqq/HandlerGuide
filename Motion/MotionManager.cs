// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/MotionManager.cs — 运动控制总控（单例）
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：
//    1. 管理控制卡连接：优先尝试雷赛 LTDMC.dll，失败则自动降级到内置仿真卡；
//    2. 轴表 = 交接资料实测的 45 根轴（HandoverTables.Axes：轴号 / 轴名 / 工站都是真机的），
//       资料里没有软限位 / 回零位 / 速度，由 DefaultsFor(AxisRule 规则表) 按
//       Kind + Power + 电机机座号 + 名称关键词推导，规则表在 AxisRules 里显式列出；
//    3. 具名 IO 表 = 交接资料实测的 303 入 / 123 出（输入全局位号 = 块序×32 + 块内位号，
//       输出 = (块序-20)×32 + 块内位号，正好 352 / 160 位地址空间），
//       旧仿真表的 32+32 个名字：真表有同义点的直接命中真表，其余追加为兼容点
//       （输入 352~383 / 输出 160~191），实测兼容点 17 入 + 19 出，合计 320 入 / 142 出；
//    4. 作为「防撞裁决中心」：所有运动请求都必须先通过 SafetyCheck()，
//       包含 急停 / 安全门 / 气压 / 伺服报警 / 软限位 / Z 轴未抬起 /
//       共享导轨扫掠区间重叠 等互锁，任一不满足即拒绝并记入信息记录，
//       从而在软件层面保证「不撞机」；
//    5. 每 tick 把轴状态回写到 MachineModel.Motors，供既有诊断页复用；
//    6. 提供一条安全的自演示节拍（AutoCycle），用于展示取放料流程。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MotionManager）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_MotionManager
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · MotionManager。</summary>
        internal const string WenQiZhiFile = "MotionManager";
    }

    /// <summary>一条安全互锁规则（可在界面上实时观察其满足状态）。</summary>
    public sealed class SafetyRule
    {
        /// <summary>规则编号，如 SR-05。</summary>
        public string Id { get; set; } = "";

        /// <summary>规则名。</summary>
        public string Name { get; set; } = "";

        /// <summary>判定依据说明。</summary>
        public string Detail { get; set; } = "";

        /// <summary>true = 条件满足（安全）。</summary>
        public Func<bool> Satisfied { get; set; }

        /// <summary>不满足时是否禁止运动。</summary>
        public bool Blocking { get; set; } = true;

        /// <summary>当前是否满足（异常时按不满足处理）。</summary>
        public bool IsOk()
        {
            try { return Satisfied == null || Satisfied(); }
            catch { return false; }
        }
    }

    /// <summary>运动控制总控（单例）。</summary>
    public sealed class MotionManager
    {
        /// <summary>
        /// 单例（延迟创建）。
        /// 关键：不能用 `public static readonly MotionManager Instance = new MotionManager();` 这种写法 ——
        /// C# 的静态字段初始化按**文本顺序**执行，而这个类下面的静态表
        /// （AxisRules / AxisFallback / AxisAlias / LegacyIn* / LegacyOut*）都声明在本行之后，
        /// 直接在静态初始化里跑构造函数就会读到 null 表（实测 TypeInitializationException + NRE）。
        /// 用 Lazy 之后，构造发生在「静态初始化全部跑完」之后的第一次访问。
        /// </summary>
        private static readonly Lazy<MotionManager> _instance = new Lazy<MotionManager>(() => new MotionManager());

        /// <summary>单例。</summary>
        public static MotionManager Instance => _instance.Value;

        // ---------------- IO 地址空间（与交接资料一致） ----------------
        /// <summary>输入真表地址空间：U0 ~ U10 共 11 块 × 32 位 = 352 位。</summary>
        public const int RealInputBits = 352;
        /// <summary>输出真表地址空间：U20 ~ U24 共 5 块 × 32 位 = 160 位。</summary>
        public const int RealOutputBits = 160;
        /// <summary>兼容段位宽：旧仿真表 32 个点里真表没有同名的，追加登记在这段里。</summary>
        public const int LegacySpan = 32;
        /// <summary>兼容点段基址（输入）：全局位号 = 352 + 老位号，一眼能看出是哪一路老信号。</summary>
        public const int CompatInBase = RealInputBits;
        /// <summary>兼容点段基址（输出）：全局位号 = 160 + 老位号。</summary>
        public const int CompatOutBase = RealOutputBits;
        /// <summary>控制卡输入容量 = 真表空间 + 兼容段 = 384。</summary>
        public const int CardInputBits = RealInputBits + LegacySpan;
        /// <summary>控制卡输出容量 = 真表空间 + 兼容段 = 192。</summary>
        public const int CardOutputBits = RealOutputBits + LegacySpan;

        // ---------------- 关键输入 / 输出位定义 ----------------
        // 这些位号是「全局位号」，BuildIo 末尾按点位语义解析出来（真表点直接命中，
        // 真表没有的语义点命中兼容点），所以不再是编译期常量。

        /// <summary>输入：急停按钮（真表 U0_X1.0，true = 已按下）。</summary>
        public int InEStop { get; private set; }
        /// <summary>输入：安全门关闭（真表里对应门禁到位类信号）。</summary>
        public int InSafeDoor { get; private set; }
        /// <summary>输入：气压正常（真表里对应进气正压表）。</summary>
        public int InAirOk { get; private set; }
        /// <summary>输入：主电源正常（兼容点）。</summary>
        public int InPowerOk { get; private set; }
        /// <summary>输入：前梭车到位（兼容点，由轴位置推导）。</summary>
        public int InShuttleA { get; private set; }
        /// <summary>输入：后梭车到位（兼容点，由轴位置推导）。</summary>
        public int InShuttleB { get; private set; }
        /// <summary>输入：测试座下压到位（兼容点，由压头 Z 位置推导）。</summary>
        public int InPressDownOk { get; private set; }
        /// <summary>输入：料盘传送臂原点（兼容点，由搬运 X 轴位置推导）。</summary>
        public int InTrayArmHome { get; private set; }
        /// <summary>输入：伺服总报警（兼容点，由轴报警汇总推导）。</summary>
        public int InServoAlarm { get; private set; }

        /// <summary>输出：红色指示灯（真表 U20_Y0.0 红灯）。</summary>
        public int OutLampRed { get; private set; }
        /// <summary>输出：黄色指示灯（真表 U20_Y0.1 黄灯）。</summary>
        public int OutLampYellow { get; private set; }
        /// <summary>输出：绿色指示灯（真表 U20_Y0.2 绿灯）。</summary>
        public int OutLampGreen { get; private set; }
        /// <summary>输出：蜂鸣器（真表 U20_Y0.3）。</summary>
        public int OutBuzzer { get; private set; }

        // ---------------- 轴 / 点查找表 ----------------
        /// <summary>轴号 → 轴配置（真机轴号不连续：分支4 缺 17、分支2 缺 38，不能用列表下标当轴号）。</summary>
        private readonly Dictionary<int, AxisConfig> _byIndex = new Dictionary<int, AxisConfig>();
        /// <summary>轴名 → 轴配置。</summary>
        private readonly Dictionary<string, AxisConfig> _byName = new Dictionary<string, AxisConfig>();
        /// <summary>输入点名 → 点（含兼容点）。</summary>
        private readonly Dictionary<string, IoPoint> _byInName = new Dictionary<string, IoPoint>();
        /// <summary>输出点名 → 点（含兼容点）。</summary>
        private readonly Dictionary<string, IoPoint> _byOutName = new Dictionary<string, IoPoint>();
        /// <summary>老位号（0~31）→ 全局位号；真表已有同名的直接指向真表点。</summary>
        private readonly int[] _legacyIn = new int[LegacySpan];
        /// <summary>老位号（0~31）→ 全局位号（输出）。</summary>
        private readonly int[] _legacyOut = new int[LegacySpan];

        // 几根在节拍 / 状态推导里反复用到的轴号（BuildAxes 末尾按真名解析）
        private int _axPressFZ = -1, _axPressBZ = -1;   // 前 / 后压头 Z
        private int _axShuttleF = -1, _axShuttleB = -1; // 前 / 后梭车 X
        private int _axTrayArm = -1;                    // 搬运 X（旧名：料盘传送臂）

        // ---------------- 状态 ----------------

        private IMotionCard _card;
        private readonly List<DemoStep> _demo = new List<DemoStep>();
        private int _demoStep;
        private double _demoDwell;
        private double _demoTimeout;

        /// <summary>当前控制卡（真卡或仿真卡）。</summary>
        public IMotionCard Card => _card;

        /// <summary>卡型名称。</summary>
        public string CardName => _card?.CardName ?? "未初始化";

        /// <summary>是否运行在仿真模式。</summary>
        public bool IsSim => _card == null || !_card.IsReal;

        /// <summary>是否已连接。</summary>
        public bool Connected => _card != null && _card.IsConnected;

        /// <summary>状态描述。</summary>
        public string Info => _card == null ? "未初始化" : _card.Info;

        /// <summary>轴配置表。</summary>
        public List<AxisConfig> Axes { get; } = new List<AxisConfig>();

        /// <summary>输入点表。</summary>
        public List<IoPoint> Inputs { get; } = new List<IoPoint>();

        /// <summary>输出点表。</summary>
        public List<IoPoint> Outputs { get; } = new List<IoPoint>();

        /// <summary>安全互锁规则表。</summary>
        public List<SafetyRule> Rules { get; } = new List<SafetyRule>();

        /// <summary>急停是否已按下。</summary>
        public bool EStop { get; private set; }

        /// <summary>最近一次被拒绝的运动原因。</summary>
        public string LastReject { get; private set; } = "";

        /// <summary>
        /// 最近一次「真卡探测」的说明文字。
        /// 没连上真卡时这里写的是具体原因（驱动缺失 / 未插卡 / 位数不匹配），
        /// 现场排查时先看这一条。
        /// </summary>
        public string LastProbeInfo { get; private set; } = "尚未探测";

        /// <summary>被安全策略拒绝的累计次数。</summary>
        public int RejectCount { get; private set; }

        /// <summary>累计运动指令条数。</summary>
        public int MoveCount { get; private set; }

        /// <summary>
        /// 仿真时钟：所有 Tick(dt) 传入的 dt 累计得到（秒）。
        /// 与墙钟对比就能看出「界面卡顿有没有把机器时间拖慢」——
        /// 真机上这两个数应该一直是同步的。
        /// </summary>
        public double SimClockSeconds { get; private set; }

        /// <summary>
        /// 单步积分上限（秒）。梯形速度曲线是离散积分，一步跨度太大就会
        /// 一步冲过目标、或者直接触发 SimMotionCard 内部的 0.2s 截断而
        /// 悄悄丢掉时间。所以一条 Tick 里按这个上限拆成多步。
        /// </summary>
        private const double SimStepMax = 0.02;

        /// <summary>最近一帧里最多补多少秒（防长时间挂起后一次补太多）。</summary>
        private const double SimCatchUpMax = 5.0;

        /// <summary>自演示节拍是否运行中。</summary>
        public bool AutoCycle { get; private set; }

        /// <summary>
        /// true = 轴交给 Lua 流程驱动：22 步自演示节拍让位（AutoCycle 仍为真，
        /// 只当画布的动画时钟用）。需求 60：脚本要影响仿真，仿真不再自己跑一套。
        /// </summary>
        public bool ScriptDriven { get; set; }

        /// <summary>节拍是否处于「暂停」（保持 AutoCycle，但不推进步骤）。</summary>
        public bool Paused { get; private set; }

        /// <summary>当前节拍步骤描述。</summary>
        public string DemoStepText { get; private set; } = "未运行";

        /// <summary>节拍完成的循环数。</summary>
        public int DemoCycles { get; private set; }

        /// <summary>演示节拍因轴名解析失败而没有建立（第一次 Tick 时提示一次）。</summary>
        public bool DemoSkipped { get; private set; }

        /// <summary>
        /// 一个完整取放料循环结束。产量计数 / Bin 分类 / UPH 由订阅方（Ops）负责，
        /// 这样运控层不需要知道业务统计，避免 Motion ↔ ViewModels 互相引用。
        /// </summary>
        public event Action CycleCompleted;

        private MotionManager()
        {
            BuildAxes();
            BuildIo();
            BuildRules();
            BuildDemo();
            // 默认先接仿真卡，Connect() 会尝试升级到真卡。
            UseSim();
        }

        // ==================================================================
        //  轴拓扑
        // ==================================================================

        /// <summary>
        /// 一条「名称关键词 → 运动参数」推导规则。
        /// 交接资料只给了轴号 / 轴名 / 电机 / 驱动器，软限位、回零位、速度都是这里给的默认值；
        /// 出厂调试以《运动参数设置 → 应用轴参数》实测填写为准（ApplyAxisParams 会覆盖本表）。
        /// </summary>
        private struct AxisRule
        {
            /// <summary>轴名包含该关键字即命中（规则表按顺序匹配，先具体后通用）。</summary>
            public string Key;
            /// <summary>负向软限位。</summary>
            public double Min;
            /// <summary>正向软限位。</summary>
            public double Max;
            /// <summary>回零位 = 停放位 = 上电初始位置（也是「安全位」）。</summary>
            public double Home;
            /// <summary>最大速度（单位/秒）。</summary>
            public double Vel;
            /// <summary>加速度（单位/秒²）。</summary>
            public double Acc;
            /// <summary>是否垂直轴。</summary>
            public bool Vertical;
            /// <summary>垂直轴抬起阈值（位置 ≥ 该值视为已抬起）。</summary>
            public double ZUp;
            /// <summary>共享导轨标识。</summary>
            public string Rail;
            /// <summary>共享导轨安全间隙。</summary>
            public double Clearance;
            /// <summary>工程单位（mm / deg）。</summary>
            public string Unit;
        }

        /// <summary>按名称关键词推导运动参数的规则表（顺序 = 优先级，先具体后通用）。</summary>
        private static readonly AxisRule[] AxisRules =
        {
            // ---- 压头测试工位：Y 走位 0~320，Z 下压 0~25（数值越大越靠上，回零位 = 完全抬起）----
            new AxisRule { Key = "前压头Y轴", Min = 0, Max = 320, Home = 160, Vel = 600, Acc = 4000, Unit = "mm", Clearance = 40 },
            new AxisRule { Key = "后压头Y轴", Min = 0, Max = 320, Home = 160, Vel = 600, Acc = 4000, Unit = "mm", Clearance = 40 },
            new AxisRule { Key = "前压头Z轴", Min = 0, Max = 25, Home = 25, Vel = 200, Acc = 3000, Vertical = true, ZUp = 24, Unit = "mm", Clearance = 40 },
            new AxisRule { Key = "后压头Z轴", Min = 0, Max = 25, Home = 25, Vel = 200, Acc = 3000, Vertical = true, ZUp = 24, Unit = "mm", Clearance = 40 },

            // ---- 梭车：共享同一条导轨，安全间隙 120 ----
            new AxisRule { Key = "前梭车X", Min = 0, Max = 1200, Home = 0, Vel = 1500, Acc = 8000, Rail = "SHUTTLE", Clearance = 120, Unit = "mm" },
            new AxisRule { Key = "后梭车X", Min = 0, Max = 1200, Home = 1200, Vel = 1500, Acc = 8000, Rail = "SHUTTLE", Clearance = 120, Unit = "mm" },

            // ---- 上 / 下料臂 X：**两条独立导轨**（需求 71 更正）----
            // 交接资料 HandoverTables：上料X轴(26) 在 **分支2**、下料X轴(10) 在 **分支4**，
            // 是两个独立的 EtherCAT 分支 / 独立龙门，机械上不共轨。旧版把它们挂在同一条
            // 虚构的 "ARM_X" 导轨上（那是 31 轴旧仿真表的遗留），于是 SR-08/SR-11 把它们
            // 锁成「同一时刻只有一根能离开停放位」——上料、测试、下料只能串行，一圈 8.46s，
            // UPH 3.4K，永远追不上样本手册的 11K。真机（含资料里的平移式机型）是
            // 上下料并行 + 双梭车双压头流水线，这里按真表把共轨约束拆掉：
            //   · 上料臂 分支2 / 下料臂 分支4 → 互不干涉，可同时动；
            //   · 画布上的包络也不重叠（上料臂 X 674.1~930.16px，下料臂 X 72~475.15px）。
            // 真正需要互斥的是「上料臂 ↔ 同分支2 的料盘搬运臂」（都要进取料区），见 CorridorX。
            new AxisRule { Key = "上料X轴", Min = 0, Max = 900, Home = 0, Vel = 1500, Acc = 6000, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "下料X轴", Min = 0, Max = 900, Home = 900, Vel = 1500, Acc = 6000, Clearance = 40, Unit = "mm" },
            // 需求 66：搬运X 的停放位不是 0。需求 40 定的停放位是画布 530.2px
            // （= 照片原位 694.4 靠左 2 个料盘宽），按画布几何 0..700mm ↔ 120..769.2px
            // 折过来正好 442.3mm。Home 取 0 会有两个后果：①〔全部回零〕把手臂停到
            // 最左端（画布 120px，正压在自动盘3 上）；② 脚本一启动画布就瞬移 410px。
            // X 走廊互斥判据用的就是 HomeMm±40，所以停放位必须写在这里。
            new AxisRule { Key = "搬运X轴", Min = 0, Max = 700, Home = 442.3, Vel = 800, Acc = 4000, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "上料Y轴", Min = 0, Max = 300, Home = 0, Vel = 800, Acc = 4000, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "下料Y轴", Min = 0, Max = 300, Home = 0, Vel = 800, Acc = 4000, Clearance = 40, Unit = "mm" },

            // ---- 变距轴：只走 ±20 ----
            new AxisRule { Key = "变距X", Min = -20, Max = 20, Home = 0, Vel = 50, Acc = 500, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "变距Y", Min = -20, Max = 20, Home = 0, Vel = 50, Acc = 500, Clearance = 40, Unit = "mm" },

            // ---- 旋转 U：整圈旋转，单位是度 ----
            new AxisRule { Key = "旋转U", Min = -180, Max = 180, Home = 0, Vel = 120, Acc = 600, Clearance = 40, Unit = "deg" },

            // ---- 吸嘴 Z：1~8 根独立真空吸嘴的升降，行程 30，抬起阈值 29 ----
            new AxisRule { Key = "吸嘴", Min = 0, Max = 30, Home = 30, Vel = 300, Acc = 3000, Vertical = true, ZUp = 29, Clearance = 40, Unit = "mm" },

            // ---- 通用兜底：仓类升降 Z（进料仓 / 出盘仓 / 空盘仓 / Auto1~3）、分度 Y、走位 X ----
            new AxisRule { Key = "Z轴", Min = 0, Max = 500, Home = 500, Vel = 150, Acc = 1500, Vertical = true, ZUp = 470, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "Y轴", Min = 0, Max = 240, Home = 0, Vel = 120, Acc = 1200, Clearance = 40, Unit = "mm" },
            new AxisRule { Key = "X轴", Min = 0, Max = 700, Home = 0, Vel = 800, Acc = 4000, Clearance = 40, Unit = "mm" },
        };

        /// <summary>规则表全落空时的最终兜底参数。</summary>
        private static readonly AxisRule AxisFallback =
            new AxisRule { Key = "", Min = 0, Max = 100, Home = 0, Vel = 100, Acc = 1000, Clearance = 40, Unit = "mm" };

        /// <summary>旧仿真表里的 31 个轴名 → 真表轴名（界面 / 自检 / 工作基点都还在按老名字找轴）。</summary>
        private static readonly Dictionary<string, string> AxisAlias = BuildAxisAlias();

        private static Dictionary<string, string> BuildAxisAlias()
        {
            var d = new Dictionary<string, string>
            {
                // 测试区域（真表工站名：压头测试工位）
                { "测试位移动Y1", "前压头Y轴" },
                { "测试位移动Y2", "后压头Y轴" },
                { "测试位下压Z1", "前压头Z轴" },
                { "测试位下压Z2", "后压头Z轴" },
                // 上料取放臂
                { "上料取放臂X轴", "上料X轴" },
                { "上料取放臂Y轴", "上料Y轴" },
                { "上料取放臂PX轴", "上料变距X" },
                { "上料取放臂PY轴", "上料变距Y" },
                // 下料取放臂
                { "下料取放臂X轴", "下料X轴" },
                { "下料取放臂Y轴", "下料Y轴" },
                { "下料取放臂PX轴", "下料变距X轴" },
                { "下料取放臂PY轴", "下料变距Y轴" },
                // 运料小车 / 料盘传送臂
                { "测试运料小车A", "前梭车X轴" },
                { "测试运料小车B", "后梭车X轴" },
                { "料盘传送臂", "搬运X轴" },
            };
            for (int i = 1; i <= 8; i++)
            {
                d["上料取放臂Z" + i + "轴"] = "上料吸嘴" + i + "Z轴";
                d["下料取放臂Z" + i + "轴"] = "下料吸嘴" + i + "Z轴";
            }
            return d;
        }

        /// <summary>交接资料实测的 45 根轴：轴号 / 轴名 / 工站是真机的，运动参数按规则表推导。</summary>
        private void BuildAxes()
        {
            foreach (var h in HandoverTables.Axes)
            {
                var r = DefaultsFor(h);

                Axes.Add(new AxisConfig
                {
                    Index = h.Index,
                    Name = h.Name,
                    Group = string.IsNullOrEmpty(h.Station) ? h.Branch : h.Station,
                    Unit = string.IsNullOrEmpty(r.Unit) ? "mm" : r.Unit,
                    PulseEquivalent = 1000,
                    MinMm = r.Min,
                    MaxMm = r.Max,
                    HomeMm = r.Home,
                    MaxVel = r.Vel,
                    Acc = r.Acc,
                    Dec = r.Acc,
                    IsVertical = r.Vertical,
                    ZUpMm = r.ZUp,
                    RailId = r.Rail ?? "",
                    ClearanceMm = r.Clearance
                });
            }

            foreach (var a in Axes)
            {
                _byIndex[a.Index] = a;
                _byName[a.Name] = a;
            }

            // 几根反复用到的轴（按真名解析；解析不到保持 -1，调用方会跳过）
            _axPressFZ = AxisIndex("前压头Z轴");
            _axPressBZ = AxisIndex("后压头Z轴");
            _axShuttleF = AxisIndex("前梭车X轴");
            _axShuttleB = AxisIndex("后梭车X轴");
            _axTrayArm = AxisIndex("搬运X轴");
            BuildCorridor();
        }

        /// <summary>
        /// 名称关键词命中规则，再按 Kind（伺服 / 步进）+ Power + 电机机座号兜底收紧速度 / 加速度。
        /// 步进（雷赛 28 / 42 / 57 机座）绝不允许跑到伺服的速度档，避免仿真给出物理上不可能的节拍。
        /// </summary>
        private static AxisRule DefaultsFor(HandoverAxis h)
        {
            var r = AxisFallback;
            foreach (var q in AxisRules)
                if (q.Key.Length > 0 && h.Name.Contains(q.Key)) { r = q; break; }

            double vCap = 1500, aCap = 8000;
            if (h.Kind == "步进")
            {
                vCap = 300; aCap = 3000;
                if (h.Motor.Contains("42CM")) { vCap = 200; aCap = 2000; }
                if (h.Motor.Contains("57CM")) { vCap = 120; aCap = 1200; }
            }
            else if (h.Kind == "伺服")
            {
                if (h.Power.Contains("100W")) { vCap = 300; aCap = 3000; }
                else if (h.Power.Contains("1000W")) { vCap = 1200; aCap = 6000; }
            }
            if (r.Vel > vCap) r.Vel = vCap;
            if (r.Acc > aCap) r.Acc = aCap;
            return r;
        }

        // ==================================================================
        //  IO 表
        // ==================================================================

        /// <summary>旧仿真表的 32 个输入点名（下标 = 老位号）。</summary>
        private static readonly string[] LegacyInNames =
        {
            "急停按钮", "安全门关闭", "气压正常", "主电源正常", "上料盘到位", "下料盘到位", "空盘到位",
            "小车A到位", "小车B到位", "料盘传送臂原点",
            "真空反馈1", "真空反馈2", "真空反馈3", "真空反馈4",
            "真空反馈5", "真空反馈6", "真空反馈7", "真空反馈8",
            "测试座下压到位", "加热器就绪", "测试机 Ready", "测试机 Result OK",
            "视觉相机就绪", "清洁盘在位", "上料臂原点", "下料臂原点",
            "门锁反馈", "真空压力开关", "伺服使能反馈", "温度超限", "伺服总报警", "安全继电器 OK",
        };

        /// <summary>旧输入点的分组（兼容点沿用旧分组；真表命中时点在真表工站名下）。</summary>
        private static readonly string[] LegacyInGroups =
        {
            "安全", "安全", "安全", "安全", "进出盘", "进出盘", "进出盘",
            "测试区域", "测试区域", "传送臂",
            "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂",
            "测试区域", "温控", "测试系统", "测试系统", "视觉", "清洁盘", "上料臂", "下料臂",
            "安全", "安全", "安全", "温控", "安全", "安全",
        };

        /// <summary>旧输入点 → 真表同义点名（空串 = 真表里没有，需要追加兼容点）。</summary>
        private static readonly string[] LegacyInAlias =
        {
            "", "左门禁1", "进气正压表", "", "进料仓料盘有无感应", "出空盘仓料盘有无感应", "进空盘仓料盘有无感应",
            "", "", "",
            "左上料真空1", "左上料真空2", "左上料真空3", "左上料真空4",
            "左上料真空5", "左上料真空6", "左上料真空7", "左上料真空8",
            "", "", "", "", "", "", "", "",
            "门禁气缸1+", "", "", "", "", "",
        };

        /// <summary>旧输入点是否低电平有效（急停 / 门禁类现场接常闭触点）。</summary>
        private static readonly bool[] LegacyInActiveLow =
        {
            true, true, true, true, false, false, false,
            false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            true, false, false, false, false, true,
        };

        /// <summary>旧仿真表的 32 个输出点名（下标 = 老位号）。</summary>
        private static readonly string[] LegacyOutNames =
        {
            "红色指示灯", "黄色指示灯", "绿色指示灯", "蜂鸣器",
            "真空电磁阀1", "真空电磁阀2", "真空电磁阀3", "真空电磁阀4",
            "真空电磁阀5", "真空电磁阀6", "真空电磁阀7", "真空电磁阀8",
            "上料臂气缸", "下料臂气缸", "测试臂下压气缸", "加热使能",
            "冷却风扇", "吹气", "料盘传送臂气缸", "小车夹紧", "真空破坏阀", "测试机启动",
            "视觉触发", "清洁盘动作", "照明灯", "冷却水阀",
            "备用输出1", "备用输出2", "备用输出3", "备用输出4", "备用输出5", "备用输出6",
        };

        /// <summary>旧输出点的分组。</summary>
        private static readonly string[] LegacyOutGroups =
        {
            "信号灯", "信号灯", "信号灯", "信号灯",
            "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂", "上料臂",
            "上料臂", "下料臂", "测试区域", "温控", "温控", "上料臂", "传送臂", "测试区域",
            "上料臂", "测试系统", "视觉", "清洁盘", "视觉", "温控",
            "备用", "备用", "备用", "备用", "备用", "备用",
        };

        /// <summary>
        /// 旧输出点 → 真表同义点名（下标必须与 LegacyOutNames 一一对应；
        /// 空串 = 先按老名字本身去真表找，再找不到才追加兼容点）。
        /// </summary>
        private static readonly string[] LegacyOutAlias =
        {
            "红灯", "黄灯", "绿灯", "",                                    // 0~3  信号灯（真表 U20_Y0.0~0.3）
            "左上料吸真空1", "左上料吸真空2", "左上料吸真空3", "左上料吸真空4",  // 4~7
            "左上料吸真空5", "左上料吸真空6", "左上料吸真空7", "左上料吸真空8",  // 8~11
            "", "", "", "", "", "",                                       // 12~17 气缸 / 加热 / 风扇 / 吹气
            "", "", "", "", "", "",                                       // 18~23 传送臂 / 夹紧 / 破真空 / SOT / 视觉 / 清洁
            "电箱照明", "",                                               // 24 照明灯 / 25 冷却水阀
            "", "", "", "", "", "",                                       // 26~31 备用输出1~6
        };

        /// <summary>旧输出点联动的旧输入位号（-1 = 无反馈联动）。</summary>
        private static readonly int[] LegacyOutFeedback =
        {
            -1, -1, -1, -1,                                   // 0~3  灯 / 蜂鸣
            10, 11, 12, 13, 14, 15, 16, 17,                   // 4~11 真空电磁阀1~8 → 真空反馈1~8
            -1, -1, 18, 19, -1, -1,                           // 12~17 气缸 / 下压到位 / 加热器就绪
            -1, -1, -1, 20, 22, 23,                           // 18~23 …测试机Ready / 视觉相机就绪 / 清洁盘在位
            -1, -1,                                           // 24~25
            -1, -1, -1, -1, -1, -1,                           // 26~31 备用输出
        };

        /// <summary>建立具名 IO 表：真表 303 入 / 123 出 + 旧 32 点表的别名与兼容点。</summary>
        private void BuildIo()
        {
            // ---- 输入：交接资料真表（全局位号 = 块序×32 + 块内位号）----
            foreach (var h in HandoverTables.Inputs)
            {
                var p = new IoPoint
                {
                    Bit = GlobalInBit(h),
                    Name = h.Name,
                    Group = h.Station,
                    IsOutput = false,
                    ActiveLow = InputActiveLow(h.Name),
                    Block = h.Block,
                    Address = HandoverAddr.Address(h),
                    Sensor = h.Sensor,
                    FromHandover = true
                };
                Inputs.Add(p);
                _byInName[h.Name] = p;
            }

            // ---- 输出：交接资料真表（块序按 U20 起算，5 块正好落在 0~159）----
            foreach (var h in HandoverTables.Outputs)
            {
                var p = new IoPoint
                {
                    Bit = GlobalOutBit(h),
                    Name = h.Name,
                    Group = h.Station,
                    IsOutput = true,
                    Block = h.Block,
                    Address = HandoverAddr.Address(h),
                    Sensor = h.Sensor,
                    FromHandover = true
                };
                Outputs.Add(p);
                _byOutName[h.Name] = p;
            }

            // ---- 旧 32 输入：真表有同义点的直接命中真表，没有的追加登记为兼容点 ----
            for (int i = 0; i < LegacySpan; i++)
            {
                string name = LegacyInNames[i];
                // 先按同义点名的真表点，其次按老名字本身在真表里找（急停按钮就是这样）
                var real = FindInExact(LegacyInAlias[i]) ?? FindInExact(name);
                if (real == null)
                {
                    var p = new IoPoint
                    {
                        Bit = CompatInBase + i,
                        Name = name,
                        Group = LegacyInGroups[i],
                        IsOutput = false,
                        ActiveLow = LegacyInActiveLow[i],
                        Block = "兼容",
                        Address = "IN" + i,
                        Sensor = "兼容点（交接资料未列名，保留旧仿真语义）",
                        LegacyBit = i
                    };
                    Inputs.Add(p);
                    _byInName[name] = p;
                    _legacyIn[i] = p.Bit;
                }
                else
                {
                    _byInName[name] = real;          // 老名字同样能查到这条真表点
                    _legacyIn[i] = real.Bit;
                }
            }

            // ---- 旧 32 输出：同上，并把「输出 → 反馈输入」的老联动关系搬到全局位号上 ----
            for (int i = 0; i < LegacySpan; i++)
            {
                string name = LegacyOutNames[i];
                IoPoint p;
                // 同上：同义点名 → 老名字本身 → 都没有才建兼容点
                var real = FindOutExact(LegacyOutAlias[i]) ?? FindOutExact(name);
                if (real == null)
                {
                    p = new IoPoint
                    {
                        Bit = CompatOutBase + i,
                        Name = name,
                        Group = LegacyOutGroups[i],
                        IsOutput = true,
                        Block = "兼容",
                        Address = "OUT" + i,
                        Sensor = "兼容点（交接资料未列名，保留旧仿真语义）",
                        LegacyBit = i
                    };
                    Outputs.Add(p);
                    _byOutName[name] = p;
                }
                else
                {
                    p = real;
                    _byOutName[name] = real;
                    if (string.IsNullOrEmpty(real.Sensor)) real.Sensor = "旧仿真表：" + name;
                }
                _legacyOut[i] = p.Bit;

                int fb = LegacyOutFeedback[i];
                if (fb >= 0 && fb < LegacySpan)
                {
                    p.FeedbackInputBit = _legacyIn[fb];
                    p.FeedbackInputName = LegacyInNames[fb];
                }
            }

            // ---- 语义常量：SafetyCheck / 规则表 / 信号灯用的关键位号（都是全局位号）----
            InEStop = _legacyIn[0];
            InSafeDoor = _legacyIn[1];
            InAirOk = _legacyIn[2];
            InPowerOk = _legacyIn[3];
            InShuttleA = _legacyIn[7];
            InShuttleB = _legacyIn[8];
            InPressDownOk = _legacyIn[18];
            InTrayArmHome = _legacyIn[9];
            InServoAlarm = _legacyIn[30];

            OutLampRed = _legacyOut[0];
            OutLampYellow = _legacyOut[1];
            OutLampGreen = _legacyOut[2];
            OutBuzzer = _legacyOut[3];
        }

        /// <summary>资料块号（"U7"→7、"U23"→23）。</summary>
        private static int BlockNo(string block)
        {
            int v = 0;
            if (block != null)
                foreach (var ch in block)
                    if (ch >= '0' && ch <= '9') v = v * 10 + (ch - '0');
            return v;
        }

        /// <summary>输入全局位号 = 块序（U0~U10）×32 + 块内位号 → 0~351。</summary>
        private static int GlobalInBit(HandoverIo h) => BlockNo(h.Block) * 32 + h.Bit;

        /// <summary>输出全局位号 = （块序 - 20）×32 + 块内位号 → 0~159（真表地址串仍保存在 Address 里）。</summary>
        private static int GlobalOutBit(HandoverIo h)
        {
            int blk = BlockNo(h.Block);
            return (blk >= 20 ? blk - 20 : blk) * 32 + h.Bit;
        }

        /// <summary>输入点是否低电平有效：急停类 / 门禁类（按钮除外）现场接常闭触点。</summary>
        private static bool InputActiveLow(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Contains("急停")) return true;
            if (name.Contains("按钮")) return false;
            if (name.Contains("门禁")) return true;
            return false;
        }

        /// <summary>按真表点名精确查输入点（找不到返回 null）。</summary>
        private IoPoint FindInExact(string name)
            => string.IsNullOrEmpty(name) ? null
               : Inputs.FirstOrDefault(x => x.Name == name && x.FromHandover);

        /// <summary>按真表点名精确查输出点（找不到返回 null）。</summary>
        private IoPoint FindOutExact(string name)
            => string.IsNullOrEmpty(name) ? null
               : Outputs.FirstOrDefault(x => x.Name == name && x.FromHandover);

        /// <summary>按点名查输入点：真名 → 旧名别名 → 「包含」模糊匹配。</summary>
        public IoPoint FindInput(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            IoPoint p;
            if (_byInName.TryGetValue(name, out p)) return p;
            return Inputs.FirstOrDefault(x => x.Name.Contains(name));
        }

        /// <summary>按点名查输出点：真名 → 旧名别名 → 「包含」模糊匹配。</summary>
        public IoPoint FindOutput(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            IoPoint p;
            if (_byOutName.TryGetValue(name, out p)) return p;
            return Outputs.FirstOrDefault(x => x.Name.Contains(name));
        }

        /// <summary>按点名取输入全局位号（找不到返回 -1）。</summary>
        public int InBitOf(string name) { var p = FindInput(name); return p == null ? -1 : p.Bit; }

        /// <summary>按点名取输出全局位号（找不到返回 -1）。</summary>
        public int OutBitOf(string name) { var p = FindOutput(name); return p == null ? -1 : p.Bit; }

        /// <summary>老位号（0~31）→ 输入全局位号；不在老表范围内的原样返回。</summary>
        public int LegacyInBit(int legacyBit)
            => legacyBit >= 0 && legacyBit < LegacySpan ? _legacyIn[legacyBit] : legacyBit;

        /// <summary>老位号（0~31）→ 输出全局位号；不在老表范围内的原样返回。</summary>
        public int LegacyOutBit(int legacyBit)
            => legacyBit >= 0 && legacyBit < LegacySpan ? _legacyOut[legacyBit] : legacyBit;

        // ==================================================================
        //  安全互锁规则
        // ==================================================================

        /// <summary>建立安全规则表（与 SafetyCheck 中的判定一一对应）。</summary>
        private void BuildRules()
        {
            Rules.Add(new SafetyRule
            {
                Id = "SR-01", Name = "急停按钮未按下", Detail = "急停输入（真表 U0_X1.0）触发时禁止一切运动", Blocking = true,
                Satisfied = () => !EStop && !ReadIn(InEStop)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-02", Name = "安全门已关闭", Detail = "门禁到位输入（真表 U0_X2.0 左门禁1）闭合后才允许运动", Blocking = true,
                Satisfied = () => ReadIn(InSafeDoor)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-03", Name = "气压正常", Detail = "气压输入（真表 U1_X4.4 进气正压表）正常后才允许运动", Blocking = true,
                Satisfied = () => ReadIn(InAirOk)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-04", Name = "无伺服报警", Detail = "任一轴 ALM 置位时禁止运动", Blocking = true,
                Satisfied = () => !AnyAlarm()
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-05", Name = "上料臂 Z 轴全部抬起",
                Detail = "上料吸嘴 1~8 Z 均在抬起位（≥" + ZUpOf("上料臂").ToString("0") + "mm）才允许水平运动",
                Blocking = true, Satisfied = () => AllVerticalUp("上料臂")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-06", Name = "下料臂 Z 轴全部抬起",
                Detail = "下料吸嘴 1~8 Z 均在抬起位（≥" + ZUpOf("下料臂").ToString("0") + "mm）才允许水平运动",
                Blocking = true, Satisfied = () => AllVerticalUp("下料臂")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-07", Name = "压头下压轴抬起",
                Detail = "前 / 后压头 Z 抬起后才允许压头 Y 走位", Blocking = true,
                Satisfied = () => AllVerticalUp("压头测试工位")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-08", Name = "前梭车 / 前压头联动互锁",
                Detail = "前梭车 X 走位前 前压头Z 必须抬到 " + ZUpOf("压头测试工位").ToString("0")
                         + "mm 以上（压头没抬就送料 = 撞压头）",
                Blocking = true,
                Satisfied = () => ShuttlePressClear(_axShuttleF, _axPressFZ)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-09", Name = "小车导轨互斥",
                Detail = "前 / 后梭车 X 共享导轨，间距不得小于 " + RailClearance("SHUTTLE").ToString("0") + "mm",
                Blocking = true,
                Satisfied = () => RailClear("SHUTTLE")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-11", Name = "后梭车 / 后压头联动互锁",
                Detail = "后梭车 X 走位前 后压头Z 必须抬到 " + ZUpOf("压头测试工位").ToString("0")
                         + "mm 以上；前 / 后梭车共轨，间距 ≥ " + RailClearance("SHUTTLE").ToString("0") + "mm",
                Blocking = true,
                Satisfied = () => ShuttlePressClear(_axShuttleB, _axPressBZ) && RailClear("SHUTTLE")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-12", Name = "上料臂 / 料盘搬运臂 X 走廊互斥",
                Detail = "上料X轴(分支2) / 搬运X轴(分支2) 共享取放料区，同一时刻只允许一根离开停放位"
                         + "（回停放位永远放行，否则会死锁）｜下料X轴为分支4独立导轨，不参与",
                Blocking = true,
                Satisfied = () =>
                {
                    int off = 0;
                    foreach (var idx in CorridorX)
                    {
                        var o = Cfg(idx);
                        if (Math.Abs(Position(idx) - o.HomeMm) > o.ClearanceMm) off++;
                    }
                    return off <= 1;
                }
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-10", Name = "全部轴已回零", Detail = "所有轴回零完成（非阻塞，仅提示）", Blocking = false,
                Satisfied = () => Axes.All(a => State(a.Index).Homed)
            });
        }

        // ==================================================================
        //  连接管理
        // ==================================================================

        /// <summary>尝试连接雷赛真卡；失败自动降级为仿真卡。</summary>
        public bool Connect()
        {
            var mm = MachineModel.Instance;
            if (_card != null && _card.IsReal && _card.IsConnected) return true;

            // 1) 先探测 LTDMC.dll + 板卡（DMC-E 总线卡 / DMC 脉冲卡都能识别）
            bool found = DmceApi.ProbeEx(out ushort cardId, out bool isBus, out string desc);
            LastProbeInfo = desc;
            if (found)
            {
                var real = new DmceCard(Axes.Count, CardInputBits, CardOutputBits);
                if (real.Open())
                {
                    _card = real;
                    ApplyAllConfig();
                    mm.Log("信息", "已连接雷赛运动控制卡：" + real.CardName + " · " + desc + "（"
                                   + Axes.Count + " 轴 / " + CardInputBits + " 入 / " + CardOutputBits + " 出）");
                    return true;
                }
            }

            // 2) 降级到仿真卡
            UseSim();
            mm.Log("信息", "未检测到雷赛控制卡（LTDMC.dll / DMC 硬件），已切换到内置仿真模式");
            return true;
        }

        /// <summary>强制使用仿真卡。</summary>
        public void UseSim()
        {
            if (_card is SimMotionCard s && s.IsConnected) return;
            var sim = new SimMotionCard(Axes.Count, CardInputBits, CardOutputBits);
            sim.Open();
            _card = sim;
            ApplyAllConfig();
            // 仿真卡上电位置 = 各自回零位置（Z 轴处于抬起位，天然安全）
            // 注意：这里必须用轴号（真机轴号不连续，列表下标 ≠ 轴号）
            foreach (var a in Axes) sim.SetPosition(a.Index, a.HomeMm);
            SeedSimInputs(sim);
        }

        /// <summary>
        /// 仿真卡上电后的 IO 默认值：安全链路按「可以动」初始化，其余全 0。
        /// （老版本把这些硬编码在 SimMotionCard 里，现在拓扑知识只留在 MotionManager。）
        /// </summary>
        private void SeedSimInputs(SimMotionCard sim)
        {
            if (sim == null) return;
            sim.SetInput(InEStop, false);
            sim.SetInput(InSafeDoor, true);
            sim.SetInput(InAirOk, true);
            sim.SetInput(InPowerOk, true);
            // 真表里的门禁 / 离子风类安全输入：仿真下默认「门已关、无异常」
            foreach (var p in Inputs)
            {
                if (!p.FromHandover) continue;
                if (p.Name.Contains("门禁") && !p.Name.Contains("按钮")) sim.SetInput(p.Bit, true);
                else if (p.Name.Contains("离子风")) sim.SetInput(p.Bit, false);
            }
        }

        /// <summary>断开控制卡。</summary>
        public void Disconnect()
        {
            _card?.Close();
            MachineModel.Instance.Log("信息", "运动控制卡已断开");
        }

        /// <summary>把所有轴配置下发到当前卡。</summary>
        private void ApplyAllConfig()
        {
            if (_card == null) return;
            foreach (var a in Axes) _card.Configure(a);
        }

        // ==================================================================
        //  位置 / 状态读取
        // ==================================================================

        /// <summary>读当前位置（轴号不存在返回 0，避免界面按老轴号取到幻影轴）。</summary>
        public double Position(int axis)
            => axis >= 0 && HasAxis(axis) && _card != null ? _card.GetPosition(axis) : 0;

        /// <summary>读轴状态（轴号不存在返回空状态对象）。</summary>
        public AxisState State(int axis)
            => axis >= 0 && HasAxis(axis) && _card != null ? _card.GetState(axis) : new AxisState();

        /// <summary>读状态位。</summary>
        public AxisStatusBits Bits(int axis) => State(axis).Bits();

        /// <summary>按名字取轴号（真名 → 旧名别名 → 包含匹配；找不到返回 -1）。</summary>
        public int AxisIndex(string name)
        {
            var a = FindByName(name);
            return a == null ? -1 : a.Index;
        }

        /// <summary>
        /// 按轴名取轴配置：先查真表名，再查「旧名 → 真名」别名表，最后才做包含匹配。
        /// 界面（DiagPages / maint_base 工作基点）、自检（App.xaml.cs）、Ops 的按名运动
        /// 全都还写着旧名（测试位下压Z1 / 上料取放臂X轴 / 料盘传送臂 …），别名表就是为了让它们不改字面量也能命中真机轴。
        /// </summary>
        public AxisConfig FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            AxisConfig a;
            if (_byName.TryGetValue(name, out a)) return a;
            string real;
            if (AxisAlias.TryGetValue(name, out real) && _byName.TryGetValue(real, out a)) return a;
            return Axes.FirstOrDefault(x => x.Name.Contains(name));
        }

        /// <summary>按轴号取轴配置（真机轴号不连续，绝不能用 Axes 列表下标当代轴号）。</summary>
        public AxisConfig Cfg(int axis)
        {
            AxisConfig a;
            if (_byIndex.TryGetValue(axis, out a)) return a;
            return new AxisConfig { Index = axis, Name = "轴" + axis };
        }

        /// <summary>
        /// 把某根轴的参数恢复成「轴表推导值」（HandoverTables + AxisRules）。
        /// 用途：旧参数存档迁移 —— 轴表改过之后，按轴号存下来的限位会张冠李戴，
        /// 例如 下料吸嘴1Z轴 被写成变距轴的 -20..20，而 ZUp=29 ⇒ 互锁永远判
        /// 「同组垂直轴未抬起」，同组水平轴被永久拒绝，整机一动就被拦。
        /// </summary>
        public void ResetAxisDefaults(int axis)
        {
            AxisConfig a;
            if (!_byIndex.TryGetValue(axis, out a)) return;
            var h = HandoverTables.Axes.FirstOrDefault(x => x.Index == axis);
            if (h == null) return;
            var r = DefaultsFor(h);
            a.MinMm = r.Min;
            a.MaxMm = r.Max;
            a.HomeMm = r.Home;
            a.MaxVel = r.Vel;
            a.Acc = r.Acc;
            a.Dec = r.Acc;
            a.IsVertical = r.Vertical;
            a.ZUpMm = r.ZUp;
            a.RailId = r.Rail ?? "";
            a.ClearanceMm = r.Clearance;
            a.Unit = string.IsNullOrEmpty(r.Unit) ? "mm" : r.Unit;
        }

        /// <summary>该轴参数是否自相矛盾（限位容不下回零位 / 抬起阈值）。</summary>
        public static bool AxisParamSane(AxisConfig a)
        {
            if (a == null) return true;
            if (a.MinMm > a.MaxMm) return false;
            if (a.HomeMm < a.MinMm - 1e-9 || a.HomeMm > a.MaxMm + 1e-9) return false;
            // 垂直轴：正向限位必须容得下 ZUp，否则「抬起」这个状态永远达不到
            double floor = a.IsVertical ? Math.Max(a.HomeMm, a.ZUpMm) : a.HomeMm;
            return a.MaxMm >= floor - 1e-9;
        }

        /// <summary>轴号是否存在于本表。</summary>
        public bool HasAxis(int axis) => axis >= 0 && _byIndex.ContainsKey(axis);

        /// <summary>取某区域的轴。</summary>
        public List<AxisConfig> GroupAxes(string group)
            => Axes.Where(a => a.Group == group).ToList();

        /// <summary>区域名列表（去重，按出现顺序）。</summary>
        public List<string> Groups()
        {
            var list = new List<string>();
            foreach (var a in Axes)
                if (!list.Contains(a.Group)) list.Add(a.Group);
            return list;
        }

        /// <summary>是否存在伺服报警。</summary>
        public bool AnyAlarm() => Axes.Any(a => State(a.Index).Alarm);

        /// <summary>是否有轴在运动。</summary>
        public bool AnyBusy() => Axes.Any(a => State(a.Index).Busy);

        /// <summary>
        /// 某区域的垂直轴是否全部抬起。
        /// 约定：垂直轴数值越大 = 越靠上（0 = 下压到底，Home = 完全抬起），
        /// 因此「抬起」判据是 位置 ≥ ZUpMm。
        /// </summary>
        private bool AllVerticalUp(string group)
        {
            foreach (var a in Axes.Where(x => x.Group == group && x.IsVertical))
                if (Position(a.Index) < a.ZUpMm) return false;
            return true;
        }

        /// <summary>取某区域垂直轴的抬起阈值。</summary>
        private double ZUpOf(string group)
        {
            var a = Axes.FirstOrDefault(x => x.Group == group && x.IsVertical);
            return a?.ZUpMm ?? 0;
        }

        /// <summary>
        /// X 走廊互斥轴对 —— 需求 71 更正为「上料X轴(26) / 搬运X轴(27)」。
        /// 这两根都在 **分支2**，都要把手臂伸到进料盘/空盘仓那一片取放料区，机械上是
        /// 真会撞的；下料X轴(10) 在 **分支4**，与它们不同分支、不同龙门，需求 71 起
        /// **不再参与**这条互斥（否则上下料无法并行，UPH 只能停在 3.4K）。
        /// 语义仍是「分时轮流」：同一时刻只允许一根离开停放位（HomeMm ± clearance），
        /// 且**回停放位永远放行**（否则走廊清空只能靠回停放位，会把自己锁死）。
        /// 注意 搬运X轴 不挂 RailId —— 它与 上料X轴 的停放位不同（442.3 / 0），
        /// 挂同一条导轨会被 RailClear 判成「间距 442 < 40 以外的乱账」；分时互斥才是正确语义。
        /// 轴号在 BuildAxes 末尾按真名解析（真机轴号不连续，不能再写死下标）。
        /// </summary>
        private int[] CorridorX = new int[0];

        /// <summary>填 X 走廊的轴号（BuildAxes 末尾调用）。</summary>
        private void BuildCorridor()
        {
            var list = new List<int>();
            foreach (var n in new[] { "上料X轴", "搬运X轴" })
            {
                int ix = AxisIndex(n);
                if (ix >= 0 && !list.Contains(ix)) list.Add(ix);
            }
            CorridorX = list.ToArray();
        }

        /// <summary>
        /// 需求 38 判据：X 走廊上除 movingAxis 之外，是否已有别的轴离开停放位。
        /// 返回 null = 走廊空闲、可以动；否则返回可直接用于拒绝的原因文本。
        /// </summary>
        private string CorridorBusyBy(int movingAxis, double clearance)
        {
            foreach (var idx in CorridorX)
            {
                if (idx == movingAxis) continue;
                var o = Cfg(idx);
                double op = Position(idx);
                if (Math.Abs(op - o.HomeMm) > clearance)
                    return "互锁：X 走廊上 " + o.Name + " 已离开停放位（" + F(op) + "，停放 " + F(o.HomeMm)
                         + "），走廊分时互斥，已拒绝（防撞）";
            }
            return null;
        }

        /// <summary>取某条共享导轨上实际生效的安全间隙（同轨各轴取最大值，与 RailClear 一致）。</summary>
        private double RailClearance(string railId)
            => Axes.Where(a => a.RailId == railId).Select(a => a.ClearanceMm).DefaultIfEmpty(0).Max();

        /// <summary>共享导轨上任意两轴间距是否都满足安全间隙。</summary>
        private bool RailClear(string railId)
        {
            var list = Axes.Where(a => a.RailId == railId).ToList();
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    double need = Math.Max(list[i].ClearanceMm, list[j].ClearanceMm);
                    if (Math.Abs(Position(list[i].Index) - Position(list[j].Index)) < need)
                        return false;
                }
            return true;
        }

        // ------------------------------------------------------------------
        //  需求 71：梭车 ↔ 压头 联动互锁
        //
        //  真机的送料顺序是「压头抬起 → 梭车把料座送进压头下方 → 压头下压」。
        //  时序错了就是撞压头，所以这条必须在安全层（而不是脚本里）兜住：
        //    · 梭车走位前，对应压头 Z 必须已抬到抬起阈值（ZUp）以上；
        //    · 压头在下位时，梭车只允许待在「不在压头正下方」的位置。
        //  正压测位来自 FlowPoints：前 500 / 后 700（两梭车共轨、间距 200 > 120）。
        //  注意：不做「压头下压必须先看到梭车到位」这一条 —— 那是工艺互锁，
        //  11_前压头 这类单独调机的流程要在空座上点动下压，放在安全层会把它锁死。
        // ------------------------------------------------------------------

        /// <summary>压头是否已抬起（轴不存在 = 视为允许）。</summary>
        private bool PressUp(int pressAxis)
            => pressAxis < 0 || Position(pressAxis) >= Cfg(pressAxis).ZUpMm - 0.001;

        /// <summary>梭车正压测区（+/-60mm）。</summary>
        private bool InPressZone(int shuttleAxis, double pos)
        {
            if (shuttleAxis == _axShuttleF) return Math.Abs(pos - FlowPoints.ShFTest) <= 60;
            if (shuttleAxis == _axShuttleB) return Math.Abs(pos - FlowPoints.ShBTest) <= 60;
            return false;
        }

        /// <summary>梭车 / 压头联动规则判据（SR-08 / SR-11）。</summary>
        private bool ShuttlePressClear(int shuttleAxis, int pressAxis)
        {
            if (shuttleAxis < 0 || pressAxis < 0) return true;
            if (PressUp(pressAxis)) return true;                       // 压头抬着，梭车随便走
            return !InPressZone(shuttleAxis, Position(shuttleAxis));    // 压头在下：梭车不许在正下方
        }

        // ==================================================================
        //  防撞裁决
        // ==================================================================

        /// <summary>
        /// 运动前的安全裁决。所有 Move* / Jog / Home 都必须先通过这里。
        /// 返回 Accepted=false 时调用方必须放弃本次运动（绝不带着风险下发指令）。
        /// </summary>
        public MotionResult SafetyCheck(int axis, double target)
        {
            if (!Connected) return MotionResult.Reject("控制卡未连接");
            if (!HasAxis(axis)) return MotionResult.Reject("轴号 " + axis + " 不在轴表内（本表 0~46，缺 17 / 38）");
            var c = _byIndex[axis];

            if (EStop) return MotionResult.Reject("急停已按下：所有运动被禁止");
            if (ReadIn(InEStop)) return MotionResult.Reject("急停按钮处于按下状态：所有运动被禁止");
            if (!ReadIn(InSafeDoor)) return MotionResult.Reject("安全门未关闭（IN1）：禁止运动");
            if (!ReadIn(InAirOk)) return MotionResult.Reject("气压异常（IN2）：禁止运动");
            if (AnyAlarm()) return MotionResult.Reject("存在伺服报警：请先清除报警");

            double lo = Math.Min(c.MinMm, c.MaxMm);
            double hi = Math.Max(c.MinMm, c.MaxMm);
            if (target < lo || target > hi)
                return MotionResult.Reject(c.Name + " 目标 " + F(target) + " 超出软限位 [" + F(lo) + ", " + F(hi)
                                          + "]，已拒绝（防撞）");

            if (!c.IsVertical)
            {
                // 1) 水平运动前：同组垂直轴必须全部抬起（位置 ≥ ZUpMm）
                foreach (var z in Axes.Where(x => x.Group == c.Group && x.IsVertical))
                {
                    double zp = Position(z.Index);
                    if (zp < z.ZUpMm)
                        return MotionResult.Reject("互锁：同组垂直轴 " + z.Name + " 未抬起（" + F(zp) + " < " + F(z.ZUpMm)
                                                  + "），禁止 " + c.Name + " 水平运动（防撞）");
                }

                // 2) 共享导轨：本次扫掠区间（含安全间隙）不得覆盖同轨其它轴
                if (!string.IsNullOrEmpty(c.RailId))
                {
                    double from = Position(axis);
                    double slo = Math.Min(from, target) - c.ClearanceMm;
                    double shi = Math.Max(from, target) + c.ClearanceMm;
                    foreach (var o in Axes)
                    {
                        if (o.Index == axis || o.RailId != c.RailId) continue;
                        double op = Position(o.Index);
                        if (op >= slo && op <= shi)
                            return MotionResult.Reject("互锁：导轨 " + c.RailId + " 上 " + o.Name + " 位于 " + F(op)
                                                      + "，落在 " + c.Name + " 本次扫掠区间 [" + F(slo) + ", " + F(shi)
                                                      + "] 内，已拒绝（防撞）");
                    }
                }

                // 3) 需求 71：梭车走位前，对应压头必须已抬起（压头没抬就送料 = 撞压头）
                int pressPair = axis == _axShuttleF ? _axPressFZ : (axis == _axShuttleB ? _axPressBZ : -1);
                if (pressPair >= 0 && !PressUp(pressPair))
                    return MotionResult.Reject("互锁：梭车走位前 " + Cfg(pressPair).Name + " 未抬起（"
                                              + F(Position(pressPair)) + " < " + F(Cfg(pressPair).ZUpMm)
                                              + "），已拒绝（防撞）");

                // 4) 需求 38 / 71：上料臂 ↔ 料盘搬运臂 X 走廊分时互斥。
                //    同一时刻只允许一根离开停放位；别的臂还在走廊里时，本次运动直接拒绝。
                //    **回停放位永远放行** —— 否则走廊清空只能靠回停放位，会把自己锁死。
                if (Array.IndexOf(CorridorX, axis) >= 0
                    && Math.Abs(target - c.HomeMm) > c.ClearanceMm)
                {
                    string busy = CorridorBusyBy(axis, c.ClearanceMm);
                    if (busy != null) return MotionResult.Reject(busy);
                }
            }
            else
            {
                // 3) 垂直轴下行前：同组水平轴必须已停止到位（避免边走边压）
                if (target < Position(axis) - 0.01)
                {
                    foreach (var h in Axes.Where(x => x.Group == c.Group && !x.IsVertical))
                    {
                        if (State(h.Index).Busy)
                            return MotionResult.Reject("互锁：同组水平轴 " + h.Name + " 正在运动，禁止 " + c.Name
                                                      + " 下压（防撞）");
                    }
                }
            }

            return MotionResult.Ok();
        }

        /// <summary>最近被拒绝的运动原因（最多保留 40 条，供界面与自检查看）。</summary>
        public List<string> RecentRejects { get; } = new List<string>();

        /// <summary>记录一次被拒绝的运动。</summary>
        private void NoteReject(string reason)
        {
            LastReject = reason;
            RejectCount++;
            RecentRejects.Insert(0, reason);
            if (RecentRejects.Count > 40) RecentRejects.RemoveAt(RecentRejects.Count - 1);
            MachineModel.Instance.Log("警告", "运动被拒绝：" + reason);
        }

        // ==================================================================
        //  运动指令（全部经安全裁决）
        // ==================================================================

        /// <summary>绝对定位。</summary>
        public MotionResult MoveAbs(int axis, double target)
        {
            var chk = SafetyCheck(axis, target);
            if (!chk.Accepted) { NoteReject(chk.Reason); return chk; }
            var r = _card.MoveAbs(axis, target);
            if (r.Accepted) MoveCount++;
            else NoteReject(r.Reason);
            return r;
        }

        /// <summary>相对定位。</summary>
        public MotionResult MoveRel(int axis, double delta)
            => MoveAbs(axis, Position(axis) + delta);

        /// <summary>点动（与 MoveRel 同语义，便于界面语义化）。</summary>
        public MotionResult Jog(int axis, double delta) => MoveRel(axis, delta);

        /// <summary>停止单轴。</summary>
        public void Stop(int axis)
        {
            _card?.Stop(axis, MotionStopMode.Deceleration);
            MachineModel.Instance.Log("信息", Cfg(axis).Name + " 已停止");
        }

        /// <summary>停止全部轴。</summary>
        public void StopAll()
        {
            AutoCycle = false;
            DemoStepText = "已停止";
            _card?.StopAll(MotionStopMode.Deceleration);
            MachineModel.Instance.Log("信息", "已停止全部轴");
        }

        /// <summary>单轴回零。</summary>
        public MotionResult Home(int axis)
        {
            var c = Cfg(axis);
            var chk = SafetyCheck(axis, c.HomeMm);
            if (!chk.Accepted) { NoteReject(chk.Reason); return chk; }
            var r = _card.Home(axis);
            if (r.Accepted) MachineModel.Instance.Log("信息", c.Name + " 开始回零");
            else NoteReject(r.Reason);
            return r;
        }

        /// <summary>全部轴回零（按区域顺序，先 Z 抬起再水平轴，避免互相干涉）。</summary>
        public void HomeAll()
        {
            // 先让所有垂直轴回到抬起位
            foreach (var a in Axes.Where(x => x.IsVertical).OrderByDescending(x => x.HomeMm))
                MoveAbs(a.Index, a.HomeMm);
            // 再回水平轴
            foreach (var a in Axes.Where(x => !x.IsVertical))
                MoveAbs(a.Index, a.HomeMm);
            MachineModel.Instance.Log("信息", "已下发全部轴回零指令（" + Axes.Count + " 轴）");
        }

        /// <summary>伺服使能 / 断使能。</summary>
        public void SetServo(int axis, bool on)
        {
            _card?.SetServo(axis, on);
            MachineModel.Instance.Log("信息", Cfg(axis).Name + (on ? " 伺服使能" : " 伺服断使能"));
        }

        /// <summary>全部伺服使能 / 断使能。</summary>
        public void SetServoAll(bool on)
        {
            foreach (var a in Axes) _card?.SetServo(a.Index, on);
            MachineModel.Instance.Log("信息", on ? "全部轴伺服使能" : "全部轴伺服断使能");
        }

        /// <summary>清除全部报警。</summary>
        public void ClearAlarm()
        {
            foreach (var a in Axes) _card?.ClearAlarm(a.Index);
            LastReject = "";
            MachineModel.Instance.Log("信息", "已清除全部轴报警");
        }

        // ==================================================================
        //  急停
        // ==================================================================

        /// <summary>触发急停：立即断使能并停止全部轴。</summary>
        public void TriggerEStop()
        {
            EStop = true;
            AutoCycle = false;
            DemoStepText = "急停";
            _card?.SetInput(InEStop, true);
            _card?.StopAll(MotionStopMode.Emergency);
            _card?.WriteOut(OutLampRed, true);
            _card?.WriteOut(OutLampGreen, false);
            _card?.WriteOut(OutBuzzer, true);
            MachineModel.Instance.Log("警告", "急停触发：全部轴已断使能并停止");
        }

        /// <summary>复位急停（需先松开急停按钮）。</summary>
        public void ResetEStop()
        {
            EStop = false;
            _card?.SetInput(InEStop, false);
            _card?.WriteOut(OutLampRed, false);
            _card?.WriteOut(OutBuzzer, false);
            _card?.WriteOut(OutLampGreen, true);
            LastReject = "";
            MachineModel.Instance.Log("信息", "急停已复位，请重新使能伺服");
        }

        // ==================================================================
        //  IO
        // ==================================================================

        /// <summary>读输入。</summary>
        public bool ReadIn(int bit) => _card != null && _card.ReadIn(bit);

        /// <summary>读输出。</summary>
        public bool ReadOut(int bit) => _card != null && _card.ReadOut(bit);

        /// <summary>写输出（自动联动反馈输入）。</summary>
        public void WriteOut(int bit, bool on)
        {
            _card?.WriteOut(bit, on);
            var p = Outputs.FirstOrDefault(x => x.Bit == bit);
            if (p != null && p.FeedbackInputBit >= 0) _card?.SetInput(p.FeedbackInputBit, on);
            if (p != null)
                MachineModel.Instance.Log("信息", "输出 " + p.Name + (on ? " ON" : " OFF"));
        }

        /// <summary>翻转一个输出（仿真页点击）。</summary>
        public void ToggleOut(int bit) => WriteOut(bit, !ReadOut(bit));

        /// <summary>全部输出复位。</summary>
        public void ResetAllOutputs()
        {
            foreach (var p in Outputs) _card?.WriteOut(p.Bit, false);
            MachineModel.Instance.Log("信息", "已复位全部输出点");
        }

        // ==================================================================
        //  自演示节拍
        // ==================================================================

        private sealed class DemoStep
        {
            public string Label = "";
            public Action Act;
            public double Dwell;
        }

        /// <summary>
        /// 一条完整、安全（全部经 SafetyCheck）的取放料演示节拍：
        /// 上料臂取料 → 放到小车 A → 小车送料 → 下料臂取料 → 放到出料位 → 复位。
        /// </summary>
        private void BuildDemo()
        {
            // 全部按真机轴名解析（旧写法是硬编码轴号 4 / 8 / 16 / 20 / 28）
            int LX = AxisIndex("上料X轴"), LZ = AxisIndex("上料吸嘴1Z轴");
            int UX = AxisIndex("下料X轴"), UZ = AxisIndex("下料吸嘴1Z轴");
            int SA = AxisIndex("前梭车X轴");
            // 上料臂 1 号吸嘴的吸真空（真表 U21_Y2.0）
            int VAC = OutBitOf("真空电磁阀1");
            // 解析失败就整条节拍不建立（注意：构造函数链上不能碰 MachineModel.Instance，
            // 否则 MachineModel.InitMotors → MotionManager 构造 → 这里再取 MachineModel 会自递归）。
            if (LX < 0 || LZ < 0 || UX < 0 || UZ < 0 || SA < 0) { DemoSkipped = true; return; }

            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 取料位 200mm", Act = () => MoveAbs(LX, 200) });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 下降取料", Act = () => MoveAbs(LZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "打开吸嘴真空（左上料吸真空1）", Act = () => WriteOut(VAC, true), Dwell = 0.4 });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "小车 A → 接料位 300mm", Act = () => MoveAbs(SA, 300) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 放料位 620mm", Act = () => MoveAbs(LX, 620) });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 下降放料", Act = () => MoveAbs(LZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "关闭吸嘴真空", Act = () => WriteOut(VAC, false), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 停放位 0mm", Act = () => MoveAbs(LX, 0) });
            _demo.Add(new DemoStep { Label = "小车 A → 测试位 900mm", Act = () => MoveAbs(SA, 900) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 取料位 620mm", Act = () => MoveAbs(UX, 620) });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 下降取料", Act = () => MoveAbs(UZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂真空开（右下料吸真空1）",
                                     Act = () => WriteOut(OutBitOf("右下料吸真空1"), true), Dwell = 0.4 });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 抬起", Act = () => MoveAbs(UZ, 30) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 出料位 880mm", Act = () => MoveAbs(UX, 880) });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 下降放料", Act = () => MoveAbs(UZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂真空关",
                                     Act = () => WriteOut(OutBitOf("右下料吸真空1"), false), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 抬起", Act = () => MoveAbs(UZ, 30) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 停放位 900mm", Act = () => MoveAbs(UX, 900) });
            _demo.Add(new DemoStep { Label = "小车 A → 回位 0mm", Act = () => MoveAbs(SA, 0) });
        }

        /// <summary>启动自演示节拍。</summary>
        public void StartDemo()
        {
            if (!Connected) { MachineModel.Instance.Log("警告", "控制卡未连接，无法启动演示"); return; }
            AutoCycle = true;
            VarBox.SetD("仿真.演示", 1);   // 需求 55：Lua 流程与仿真联动（脚本的让行判据之一）
            Paused = false;
            _demoStep = 0;
            _demoDwell = 0;
            _demoTimeout = 0;
            DemoCycles = 0;
            DemoStepText = "准备中";
            MachineModel.Instance.Log("信息", "启动运动演示节拍（全程经安全互锁裁决）");
        }

        /// <summary>暂停节拍：轴保持当前位置，步骤不再推进。</summary>
        public void PauseDemo()
        {
            if (!AutoCycle) return;
            Paused = true;
            DemoStepText = "已暂停：" + DemoStepText;
            MachineModel.Instance.Log("信息", "节拍已暂停（保持当前位置）");
        }

        /// <summary>从暂停处继续。</summary>
        public void ResumeDemo()
        {
            if (!AutoCycle) return;
            Paused = false;
            DemoStepText = DemoStepText.Replace("已暂停：", "");
            MachineModel.Instance.Log("信息", "节拍已继续");
        }

        /// <summary>单步执行一次节拍（暂停状态下使用）。</summary>
        public void StepOnce()
        {
            if (!Connected) { MachineModel.Instance.Log("警告", "控制卡未连接"); return; }
            if (!AutoCycle) { StartDemo(); Paused = true; }
            Paused = false;
            _demoDwell = 0;
            _demoTimeout = 0;
            AdvanceDemo();
            Paused = true;
        }

        /// <summary>停止自演示节拍。</summary>
        public void StopDemo()
        {
            AutoCycle = false;
            ScriptDriven = false;
            VarBox.SetD("仿真.演示", 0);
            Paused = false;
            DemoStepText = "已停止";
            StopAll();
        }

        /// <summary>推进演示节拍：仅在所有轴静止且驻留结束时进入下一步。</summary>
        private void StepDemo(double dt)
        {
            if (!AutoCycle || Paused || ScriptDriven) return;

            _demoDwell -= dt;
            if (AnyBusy() || _demoDwell > 0)
            {
                // 安全兜底：某一步卡住超过 20s 就跳过，避免演示僵死
                _demoTimeout += dt;
                if (_demoTimeout > 20)
                {
                    _demoTimeout = 0;
                    MachineModel.Instance.Log("警告", "演示步骤超时，自动跳过：" + DemoStepText);
                    AdvanceDemo();
                }
                return;
            }

            _demoTimeout = 0;
            AdvanceDemo();
        }

        /// <summary>进入下一步。</summary>
        private void AdvanceDemo()
        {
            if (_demoStep >= _demo.Count)
            {
                _demoStep = 0;
                DemoCycles++;
                // 通知订阅方（Ops）做产量计数 —— 运控层不碰业务统计。
                try { CycleCompleted?.Invoke(); }
                catch (Exception ex) { MachineModel.Instance.Log("警告", "循环完成回调异常：" + ex.Message); }
            }
            var st = _demo[_demoStep];
            DemoStepText = "第 " + (_demoStep + 1) + "/" + _demo.Count + " 步：" + st.Label;
            int before = RejectCount;
            try { st.Act?.Invoke(); }
            catch (Exception ex) { MachineModel.Instance.Log("警告", "演示步骤异常：" + ex.Message); }
            // 被互锁拒绝时不停留，直接进入下一步（等待机会重试）
            _demoDwell = RejectCount > before ? 0.6 : Math.Max(0.15, st.Dwell);
            _demoStep++;
        }

        // ==================================================================
        //  周期推进
        // ==================================================================

        /// <summary>
        /// 由界面定时器驱动。dt = 距离上一帧的**真实墙钟时间**（不要压缩它，
        /// 压缩就等于让机器跑得比真机慢；压缩后的节拍没法跟样本手册的
        /// Index Time / UPH 对照）。
        /// </summary>
        public void Tick(double dt)
        {
            if (dt <= 0) return;
            if (dt > SimCatchUpMax) dt = SimCatchUpMax;
            // 时钟先记账（即使仿真卡还没连上也照记），这样「仿真时钟 vs 墙钟」
            // 的偏差可以当健康指示看：一直同步才说明界面没在拖慢机器时间。
            SimClockSeconds += dt;

            if (_card == null) return;
            if (DemoSkipped)
            {
                DemoSkipped = false;
                MachineModel.Instance.Log("警告", "演示节拍未能解析到全部所需轴名，已跳过建立（检查轴表）");
            }

            // 1) 仿真卡推进物理积分。必须分步：SimMotionCard.Step 内部把 dt 截到
            //    0.2s，一步跨过去的话超出部分的时间会被静默丢弃 —— 那正是
            //    「脚本一圈 8.5s、界面却要跑好几分钟」的根因。
            double left = dt;
            while (left > 1e-9)
            {
                double s = Math.Min(left, SimStepMax);
                _card.Step(s);
                left -= s;
            }

            // 2) 由轴状态推导部分输入点，让 IO 面板是"活"的
            SyncDerivedInputs();
            SyncVacuumFeedback();

            // 3) 回写 MachineModel.Motors，供既有《诊断 / 电机状态》等页面复用
            var mm = MachineModel.Instance;
            // Motors 由 InitMotors 按轴表顺序建立（顺序 = 轴号顺序），所以逐行对应 Axes
            int n = Math.Min(mm.Motors.Count, Axes.Count);
            for (int i = 0; i < n; i++)
            {
                var mo = mm.Motors[i];
                var s = State(Axes[i].Index);
                mo.Position = Math.Round(s.PositionMm, 3);
                mo.Torque = Math.Round(s.Torque / 20.0, 2);
                mo.Busy = s.Busy;
                mo.Homed = s.Homed;
                mo.ALM = s.Alarm;
                mo.ORG = s.Org;
                mo.INP = s.InPosition;
                mo.Code = s.LastReject;
            }

            // 4) 信号灯跟随状态
            if (!EStop)
            {
                bool alarm = AnyAlarm();
                _card.WriteOut(OutLampGreen, !alarm && Connected);
                _card.WriteOut(OutLampYellow, alarm);
            }

            // 5) 演示节拍
            StepDemo(dt);
        }

        /// <summary>
        /// 把轴状态映射到相关输入点（真表里没有「小车到位 / 下压到位」这类硬点，
        /// 全部落在兼容点上，界面与自检仍按语义读）。
        /// </summary>
        private void SyncDerivedInputs()
        {
            if (_card == null) return;
            SetInputAt(InShuttleA, _axShuttleF, 3);
            SetInputAt(InShuttleB, _axShuttleB, 3);
            SetInputAt(InTrayArmHome, _axTrayArm, 3);
            if (_card != null && InPressDownOk >= 0 && HasAxis(_axPressFZ) && HasAxis(_axPressBZ))
                _card.SetInput(InPressDownOk, Position(_axPressFZ) < 1 && Position(_axPressBZ) < 1);
            _card.SetInput(InServoAlarm, AnyAlarm());
        }

        /// <summary>
        /// 真空反馈回显：把「吸真空n」输出映射到同名的「真空n」输入。
        /// 真机是阀一开、真空开关在几十毫秒内闭合；仿真里没人去闭合它，于是
        /// 流程线程等「8 路真空反馈齐了才记投料」永远等到超时 —— 投料恒 0 颗。
        /// 这里按输出状态回显（1 帧滞后 ≈ 20~80ms，与真机的建立时间同量级）。
        /// 只动「吸真空 → 真空」这一组点，IO 面板手动置位其它点不受影响。
        /// </summary>
        private void SyncVacuumFeedback()
        {
            if (_card == null) return;
            if (_vacMap == null)
            {
                _vacMap = new List<int[]>();
                var re = new System.Text.RegularExpressions.Regex(@"^(.*)吸真空(\d+)$");
                foreach (var o in Outputs)
                {
                    var mt = re.Match(o.Name);
                    if (!mt.Success) continue;
                    var inPt = FindInput(mt.Groups[1].Value + "真空" + mt.Groups[2].Value);
                    if (inPt != null) _vacMap.Add(new[] { o.Bit, inPt.Bit });
                }
            }
            for (int k = 0; k < _vacMap.Count; k++)
                _card.SetInput(_vacMap[k][1], _card.ReadOut(_vacMap[k][0]));
        }

        /// <summary>真空阀 → 真空开关的位号对（懒构建）。</summary>
        private List<int[]> _vacMap;

        /// <summary>「轴在停放位附近」→ 兼容输入点（轴号无效时不动这一路）。</summary>
        private void SetInputAt(int bit, int axis, double tol)
        {
            if (_card == null || bit < 0 || !HasAxis(axis)) return;
            _card.SetInput(bit, Math.Abs(Position(axis) - Cfg(axis).HomeMm) < tol);
        }

        /// <summary>数字格式化（界面与日志统一）。</summary>
        private static string F(double v) => v.ToString("0.###");
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
