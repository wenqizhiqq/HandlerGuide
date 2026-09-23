// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/MotionManager.cs — 运动控制总控（单例）
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：
//    1. 管理控制卡连接：优先尝试雷赛 LTDMC.dll，失败则自动降级到内置仿真卡；
//    2. 定义 31 根轴的拓扑（区域 / 软限位 / 垂直轴 / 共享导轨 / 安全间隙）；
//    3. 定义具名 IO 表（32 入 / 32 出）并模拟输出→输入反馈；
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
        /// <summary>单例。</summary>
        public static MotionManager Instance { get; } = new MotionManager();

        // ---------------- 关键输入 / 输出位定义 ----------------
        /// <summary>输入：急停按钮（true = 已按下）。</summary>
        public const int InEStop = 0;
        /// <summary>输入：安全门关闭。</summary>
        public const int InSafeDoor = 1;
        /// <summary>输入：气压正常。</summary>
        public const int InAirOk = 2;
        /// <summary>输入：主电源正常。</summary>
        public const int InPowerOk = 3;
        /// <summary>输入：小车 A 到位。</summary>
        public const int InShuttleA = 7;
        /// <summary>输入：小车 B 到位。</summary>
        public const int InShuttleB = 8;
        /// <summary>输入：伺服总报警。</summary>
        public const int InServoAlarm = 30;

        /// <summary>输出：红色指示灯。</summary>
        public const int OutLampRed = 0;
        /// <summary>输出：黄色指示灯。</summary>
        public const int OutLampYellow = 1;
        /// <summary>输出：绿色指示灯。</summary>
        public const int OutLampGreen = 2;
        /// <summary>输出：蜂鸣器。</summary>
        public const int OutBuzzer = 3;

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

        /// <summary>自演示节拍是否运行中。</summary>
        public bool AutoCycle { get; private set; }

        /// <summary>当前节拍步骤描述。</summary>
        public string DemoStepText { get; private set; } = "未运行";

        /// <summary>节拍完成的循环数。</summary>
        public int DemoCycles { get; private set; }

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

        /// <summary>按手册《诊断 / 电机状态》的 31 轴建立拓扑与软限位。</summary>
        private void BuildAxes()
        {
            // 测试区域：0 测试位移动Y1 / 1 Y2 / 2 下压Z1 / 3 Z2
            Add(0, "测试位移动Y1", "测试区域", 0, 320, 160, 600, 4000, false, 0, "");
            Add(1, "测试位移动Y2", "测试区域", 0, 320, 160, 600, 4000, false, 0, "");
            Add(2, "测试位下压Z1", "测试区域", 0, 25, 25, 200, 3000, true, 24, "");
            Add(3, "测试位下压Z2", "测试区域", 0, 25, 25, 200, 3000, true, 24, "");

            // 上料取放臂：4 X / 5 Y / 6 PX / 7 PY / 8..15 Z1..Z8
            Add(4, "上料取放臂X轴", "上料臂", 0, 900, 0, 1200, 6000, false, 0, "ARM_X");
            Add(5, "上料取放臂Y轴", "上料臂", 0, 300, 0, 800, 4000, false, 0, "");
            Add(6, "上料取放臂PX轴", "上料臂", -20, 20, 0, 50, 500, false, 0, "");
            Add(7, "上料取放臂PY轴", "上料臂", -20, 20, 0, 50, 500, false, 0, "");
            for (int i = 0; i < 8; i++)
                Add(8 + i, "上料取放臂Z" + (i + 1) + "轴", "上料臂", 0, 30, 30, 300, 3000, true, 29, "");

            // 下料取放臂：16 X / 17 Y / 18 PX / 19 PY / 20..27 Z1..Z8
            Add(16, "下料取放臂X轴", "下料臂", 0, 900, 900, 1200, 6000, false, 0, "ARM_X");
            Add(17, "下料取放臂Y轴", "下料臂", 0, 300, 0, 800, 4000, false, 0, "");
            Add(18, "下料取放臂PX轴", "下料臂", -20, 20, 0, 50, 500, false, 0, "");
            Add(19, "下料取放臂PY轴", "下料臂", -20, 20, 0, 50, 500, false, 0, "");
            for (int i = 0; i < 8; i++)
                Add(20 + i, "下料取放臂Z" + (i + 1) + "轴", "下料臂", 0, 30, 30, 300, 3000, true, 29, "");

            // 小车与传送臂
            Add(28, "测试运料小车A", "小车", 0, 1200, 0, 1500, 8000, false, 0, "SHUTTLE", 120);
            Add(29, "测试运料小车B", "小车", 0, 1200, 1200, 1500, 8000, false, 0, "SHUTTLE", 120);
            Add(30, "料盘传送臂", "传送臂", 0, 700, 0, 800, 4000, false, 0, "");
        }

        /// <summary>追加一根轴。</summary>
        private void Add(int index, string name, string group, double min, double max, double home,
                         double maxVel, double acc, bool vertical, double zUp, string rail,
                         double clearance = 40)
        {
            Axes.Add(new AxisConfig
            {
                Index = index,
                Name = name,
                Group = group,
                Unit = "mm",
                PulseEquivalent = 1000,
                MinMm = min,
                MaxMm = max,
                HomeMm = home,
                MaxVel = maxVel,
                Acc = acc,
                Dec = acc,
                IsVertical = vertical,
                ZUpMm = zUp,
                RailId = rail,
                ClearanceMm = clearance
            });
        }

        // ==================================================================
        //  IO 表
        // ==================================================================

        /// <summary>建立具名 IO 表（与手册《面板IO / 测试系统IO》语义对齐）。</summary>
        private void BuildIo()
        {
            // ---- 输入 ----
            In(0, "急停按钮", "安全", true);
            In(1, "安全门关闭", "安全", true);
            In(2, "气压正常", "安全", true);
            In(3, "主电源正常", "安全", true);
            In(4, "上料盘到位", "进出盘");
            In(5, "下料盘到位", "进出盘");
            In(6, "空盘到位", "进出盘");
            In(7, "小车A到位", "测试区域");
            In(8, "小车B到位", "测试区域");
            In(9, "料盘传送臂原点", "传送臂");
            for (int i = 0; i < 8; i++) In(10 + i, "真空反馈" + (i + 1), "上料臂");
            In(18, "测试座下压到位", "测试区域");
            In(19, "加热器就绪", "温控");
            In(20, "测试机 Ready", "测试系统");
            In(21, "测试机 Result OK", "测试系统");
            In(22, "视觉相机就绪", "视觉");
            In(23, "清洁盘在位", "清洁盘");
            In(24, "上料臂原点", "上料臂");
            In(25, "下料臂原点", "下料臂");
            In(26, "门锁反馈", "安全");
            In(27, "真空压力开关", "安全");
            In(28, "伺服使能反馈", "安全");
            In(29, "温度超限", "温控");
            In(30, "伺服总报警", "安全");
            In(31, "安全继电器 OK", "安全");

            // ---- 输出 ----
            Out(0, "红色指示灯", "信号灯");
            Out(1, "黄色指示灯", "信号灯");
            Out(2, "绿色指示灯", "信号灯");
            Out(3, "蜂鸣器", "信号灯");
            for (int i = 0; i < 8; i++) Out(4 + i, "真空电磁阀" + (i + 1), "上料臂", 10 + i, "真空反馈" + (i + 1));
            Out(12, "上料臂气缸", "上料臂");
            Out(13, "下料臂气缸", "下料臂");
            Out(14, "测试臂下压气缸", "测试区域", 18, "测试座下压到位");
            Out(15, "加热使能", "温控", 19, "加热器就绪");
            Out(16, "冷却风扇", "温控");
            Out(17, "吹气", "上料臂");
            Out(18, "料盘传送臂气缸", "传送臂");
            Out(19, "小车夹紧", "测试区域");
            Out(20, "真空破坏阀", "上料臂");
            Out(21, "测试机启动", "测试系统", 20, "测试机 Ready");
            Out(22, "视觉触发", "视觉", 22, "视觉相机就绪");
            Out(23, "清洁盘动作", "清洁盘", 23, "清洁盘在位");
            Out(24, "照明灯", "视觉");
            Out(25, "冷却水阀", "温控");
            for (int i = 26; i < 32; i++) Out(i, "备用输出" + (i - 25), "备用");
        }

        /// <summary>加一个输入点。</summary>
        private void In(int bit, string name, string group, bool activeLow = false)
            => Inputs.Add(new IoPoint { Bit = bit, Name = name, Group = group, IsOutput = false, ActiveLow = activeLow });

        /// <summary>加一个输出点（可选联动输入）。</summary>
        private void Out(int bit, string name, string group, int feedbackBit = -1, string feedbackName = "")
            => Outputs.Add(new IoPoint
            {
                Bit = bit,
                Name = name,
                Group = group,
                IsOutput = true,
                FeedbackInputBit = feedbackBit,
                FeedbackInputName = feedbackName
            });

        // ==================================================================
        //  安全互锁规则
        // ==================================================================

        /// <summary>建立安全规则表（与 SafetyCheck 中的判定一一对应）。</summary>
        private void BuildRules()
        {
            Rules.Add(new SafetyRule
            {
                Id = "SR-01", Name = "急停按钮未按下", Detail = "急停输入 IN0 触发时禁止一切运动", Blocking = true,
                Satisfied = () => !EStop && !ReadIn(InEStop)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-02", Name = "安全门已关闭", Detail = "安全门输入 IN1 闭合后才允许运动", Blocking = true,
                Satisfied = () => ReadIn(InSafeDoor)
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-03", Name = "气压正常", Detail = "气压输入 IN2 正常后才允许运动", Blocking = true,
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
                Detail = "上料取放臂 Z1~Z8 均在抬起位（≥" + ZUpOf("上料臂").ToString("0") + "mm）才允许水平运动",
                Blocking = true, Satisfied = () => AllVerticalUp("上料臂")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-06", Name = "下料臂 Z 轴全部抬起",
                Detail = "下料取放臂 Z1~Z8 均在抬起位（≥" + ZUpOf("下料臂").ToString("0") + "mm）才允许水平运动",
                Blocking = true, Satisfied = () => AllVerticalUp("下料臂")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-07", Name = "测试位下压轴抬起",
                Detail = "测试位下压 Z1/Z2 抬起后才允许测试位移移动", Blocking = true,
                Satisfied = () => AllVerticalUp("测试区域")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-08", Name = "双臂走廊互斥",
                Detail = "上/下料取放臂 X 轴共享走廊，间距不得小于 80mm", Blocking = true,
                Satisfied = () => RailClear("ARM_X")
            });
            Rules.Add(new SafetyRule
            {
                Id = "SR-09", Name = "小车导轨互斥",
                Detail = "运料小车 A/B 共享导轨，间距不得小于 120mm", Blocking = true,
                Satisfied = () => RailClear("SHUTTLE")
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
                var real = new DmceCard(Axes.Count);
                if (real.Open())
                {
                    _card = real;
                    ApplyAllConfig();
                    mm.Log("信息", "已连接雷赛运动控制卡：" + real.CardName + " · " + desc + "（" + Axes.Count + " 轴）");
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
            var sim = new SimMotionCard(Axes.Count);
            sim.Open();
            _card = sim;
            ApplyAllConfig();
            // 仿真卡上电位置 = 各自回零位置（Z 轴处于抬起位，天然安全）
            for (int i = 0; i < Axes.Count; i++) sim.SetPosition(i, Axes[i].HomeMm);
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

        /// <summary>读当前位置。</summary>
        public double Position(int axis) => _card?.GetPosition(axis) ?? 0;

        /// <summary>读轴状态。</summary>
        public AxisState State(int axis) => _card?.GetState(axis) ?? new AxisState();

        /// <summary>读状态位。</summary>
        public AxisStatusBits Bits(int axis) => State(axis).Bits();

        /// <summary>按名字取轴号（找不到返回 -1）。</summary>
        public int AxisIndex(string name)
        {
            for (int i = 0; i < Axes.Count; i++)
                if (Axes[i].Name == name) return Axes[i].Index;
            return -1;
        }

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
            if (axis < 0 || axis >= Axes.Count) return MotionResult.Reject("轴号 " + axis + " 越界");
            var c = Axes[axis];

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
            MachineModel.Instance.Log("信息", Axes[axis].Name + " 已停止");
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
            var c = Axes[axis];
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
            MachineModel.Instance.Log("信息", Axes[axis].Name + (on ? " 伺服使能" : " 伺服断使能"));
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
            int LX = 4, LZ = 8;       // 上料臂 X / Z1
            int UX = 16, UZ = 20;     // 下料臂 X / Z1
            int SA = 28;              // 小车 A

            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 取料位 200mm", Act = () => MoveAbs(LX, 200) });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 下降取料", Act = () => MoveAbs(LZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "打开吸嘴真空（OUT4）", Act = () => WriteOut(4, true), Dwell = 0.4 });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "小车 A → 接料位 300mm", Act = () => MoveAbs(SA, 300) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 放料位 620mm", Act = () => MoveAbs(LX, 620) });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 下降放料", Act = () => MoveAbs(LZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "关闭吸嘴真空", Act = () => WriteOut(4, false), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "上料臂 Z1 抬起", Act = () => MoveAbs(LZ, 30) });
            _demo.Add(new DemoStep { Label = "上料臂 X → 停放位 0mm", Act = () => MoveAbs(LX, 0) });
            _demo.Add(new DemoStep { Label = "小车 A → 测试位 900mm", Act = () => MoveAbs(SA, 900) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 取料位 620mm", Act = () => MoveAbs(UX, 620) });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 下降取料", Act = () => MoveAbs(UZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂真空开（OUT5）", Act = () => WriteOut(5, true), Dwell = 0.4 });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 抬起", Act = () => MoveAbs(UZ, 30) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 出料位 880mm", Act = () => MoveAbs(UX, 880) });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 下降放料", Act = () => MoveAbs(UZ, 5), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂真空关", Act = () => WriteOut(5, false), Dwell = 0.3 });
            _demo.Add(new DemoStep { Label = "下料臂 Z1 抬起", Act = () => MoveAbs(UZ, 30) });
            _demo.Add(new DemoStep { Label = "下料臂 X → 停放位 900mm", Act = () => MoveAbs(UX, 900) });
            _demo.Add(new DemoStep { Label = "小车 A → 回位 0mm", Act = () => MoveAbs(SA, 0) });
        }

        /// <summary>启动自演示节拍。</summary>
        public void StartDemo()
        {
            if (!Connected) { MachineModel.Instance.Log("警告", "控制卡未连接，无法启动演示"); return; }
            AutoCycle = true;
            _demoStep = 0;
            _demoDwell = 0;
            _demoTimeout = 0;
            DemoCycles = 0;
            DemoStepText = "准备中";
            MachineModel.Instance.Log("信息", "启动运动演示节拍（全程经安全互锁裁决）");
        }

        /// <summary>停止自演示节拍。</summary>
        public void StopDemo()
        {
            AutoCycle = false;
            DemoStepText = "已停止";
            StopAll();
        }

        /// <summary>推进演示节拍：仅在所有轴静止且驻留结束时进入下一步。</summary>
        private void StepDemo(double dt)
        {
            if (!AutoCycle) return;

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

        /// <summary>由界面定时器驱动。</summary>
        public void Tick(double dt)
        {
            if (_card == null) return;

            // 1) 仿真卡推进物理积分
            _card.Step(dt);

            // 2) 由轴状态推导部分输入点，让 IO 面板是"活"的
            SyncDerivedInputs();

            // 3) 回写 MachineModel.Motors，供既有《诊断 / 电机状态》等页面复用
            var mm = MachineModel.Instance;
            int n = Math.Min(mm.Motors.Count, Axes.Count);
            for (int i = 0; i < n; i++)
            {
                var mo = mm.Motors[i];
                var s = State(i);
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

        /// <summary>把轴状态映射到相关输入点。</summary>
        private void SyncDerivedInputs()
        {
            if (_card == null) return;
            _card.SetInput(InShuttleA, Math.Abs(Position(28) - Axes[28].HomeMm) < 3);
            _card.SetInput(InShuttleB, Math.Abs(Position(29) - Axes[29].HomeMm) < 3);
            _card.SetInput(18, Position(2) < 1 && Position(3) < 1);   // 测试座下压到位
            _card.SetInput(InServoAlarm, AnyAlarm());
            _card.SetInput(9, Math.Abs(Position(30) - Axes[30].HomeMm) < 3);
        }

        /// <summary>数字格式化（界面与日志统一）。</summary>
        private static string F(double v) => v.ToString("0.###");
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
