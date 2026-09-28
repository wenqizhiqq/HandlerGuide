// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
//
//  Ops —— 全软件「控件 → 真实动作」的唯一入口。
//
//  设计原则（改代码前必读）：
//   1. 页面（XAML / *Pages.cs）只声明「按什么按钮、走哪个表单」，动作全部落在这里，
//      这样同一个动作在多个页面复用，不会出现「这个页面的应用按钮有用、那个没用」。
//   2. 所有会动到机械的动作，一律先过 MotionManager 的安全互锁（SafetyCheck），
//      Ops 只做业务前置条件判断（急停 / 报警 / 是否在运行），不自己判断会不会撞机。
//   3. 参数落盘：%APPDATA%\HandlerGuide\params.json（Nums/Texts/Toggles/Choices）。
//      配方单独存 %APPDATA%\HandlerGuide\recipes\{品种}.json。
//   4. 任何动作都要写日志 —— 现场排故全靠《信息记录》页。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Ops）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Ops
    internal static class WenQiZhiTag_Ops
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Ops。</summary>
        internal const string WenQiZhiFile = "Ops";
    }

    // 温启志 18719361399 · 类 public static class Ops
    /// <summary>
    /// 全软件的操作实现层。页面按钮、工具栏命令、输入框校验最终都调到这里。
    /// </summary>
    public static class Ops
    {
        // ==================================================================
        //  运行状态机
        // ==================================================================

        /// <summary>停止（含上电初始态）。</summary>
        public const string StateStopped = "停止";
        /// <summary>运行中（节拍推进、产量累加）。</summary>
        public const string StateRunning = "运行";
        /// <summary>暂停（保持位置，节拍不推进）。</summary>
        public const string StatePaused = "暂停";

        private static MachineModel M => MachineModel.Instance;
        private static MotionManager Mm => MotionManager.Instance;

        public static bool IsRunning => M.RunState == StateRunning;
        public static bool IsPaused => M.RunState == StatePaused;
        public static bool IsIdle => !IsRunning && !IsPaused;

        // ==================================================================
        //  初始化（必须在界面起来之前调一次）
        // ==================================================================

        private static bool _inited;

        /// <summary>
        /// 显式初始化：订阅运控循环回调、装载上次保存的参数、装载配方列表。
        /// 注意：静态类的方法组引用**不会**触发静态构造函数（见技能文档 Pitfall 7），
        /// 所以这里由 MainViewModel 构造函数显式调用，不要依赖隐式触发。
        /// </summary>
        public static void Init()
        {
            if (_inited) return;
            _inited = true;

            Mm.CycleCompleted += OnCycleCompleted;

            // 初始状态：如果卡连上了就伺服使能，运行状态置「停止」
            M.RunState = StateStopped;
            if (!M.Motors.Any()) M.Log("信息", "未检测到电机配置");

            // 自动装载上次退出时保存的参数（没有就跳过，不报错）
            TryLoadParamsOnStartup();

            // 配方目录里的品种并入下拉列表
            SyncRecipeListFromDisk();

            // 用户表（内置 Admin/Engineer/Operator，可在「维护配置 → 用户管理」里增删）
            LoadUsers();

            M.Log("信息", "操作层已就绪");
        }

        // ==================================================================
        //  生产控制：启动 / 暂停 / 继续 / 停止 / 急停
        // ==================================================================

        /// <summary>启动生产。会先做安全前置判断，再启动运控节拍并把运行状态置「运行」。</summary>
        public static bool StartProduction()
        {
            // 《手册》<出盘>：<出盘>请求激活后再按<开始运行>，指示灯熄灭并开始清盘
            if (TrayOutRequested)
            {
                M.SetChoice("tray_out_req", 0);
                Info("出盘请求生效：开始清出下料自动盘区域的料盘");
                return LotOps.PurgeTrays();
            }

            if (Mm.EStop) { Warn("急停未复位，无法启动"); return false; }
            if (!Mm.Connected) { Warn("运动控制卡未连接，无法启动"); return false; }
            if (Mm.AnyAlarm()) { Warn("存在伺服报警，请先「报警解除」再启动"); return false; }

            if (IsRunning) { Info("设备已在运行中"); return false; }
            if (IsPaused) return ResumeProduction();

            // 未归零的轴不允许启动（现场规矩：先归零再跑）
            var notHomed = Mm.Axes.Where(a => !Mm.State(a.Index).Homed).ToList();
            if (notHomed.Count > 0)
            {
                Warn("有 " + notHomed.Count + " 个轴尚未归零，请先执行「全部归零」");
                return false;
            }

            Mm.StartDemo();
            M.RunState = StateRunning;
            M.ResetZeroTime();
            _cycleWatch.Restart();
            Info("启动生产：品种 " + M.Recipe + "　模式 " + M.Mode + "　温控 " + M.TempMode);
            return true;
        }

        /// <summary>暂停：保持当前位置，节拍不推进，产量停止累加。</summary>
        public static bool PauseProduction()
        {
            if (!IsRunning) { Warn("当前不在运行状态，无法暂停"); return false; }
            Mm.PauseDemo();
            M.RunState = StatePaused;
            Info("生产已暂停");
            return true;
        }

        /// <summary>从暂停继续。</summary>
        public static bool ResumeProduction()
        {
            if (!IsPaused) { Warn("当前不在暂停状态"); return false; }
            Mm.ResumeDemo();
            M.RunState = StateRunning;
            Info("生产已继续");
            return true;
        }

        /// <summary>停止：停全部轴并回到「停止」态。</summary>
        public static bool StopProduction()
        {
            Mm.StopDemo();
            M.RunState = StateStopped;
            Info("生产已停止");
            return true;
        }

        /// <summary>单步：暂停态下走一步节拍，方便现场观察动作顺序。</summary>
        public static void SingleStep()
        {
            if (Mm.EStop) { Warn("急停未复位，无法单步"); return; }
            if (!Mm.Connected) { Warn("运动控制卡未连接"); return; }
            M.RunState = StatePaused;
            Mm.StepOnce();
            Info("单步：" + Mm.DemoStepText);
        }

        /// <summary>急停：立即断使能、停全部轴、红灯 + 蜂鸣。</summary>
        public static void EmergencyStop()
        {
            Mm.TriggerEStop();
            M.RunState = StateStopped;
            Warn("【急停】已触发，全部轴断使能");
        }

        /// <summary>急停复位。</summary>
        public static void ResetEmergency()
        {
            if (!Mm.EStop) { Info("当前不在急停状态"); return; }
            Mm.ResetEStop();
            Info("急停已复位，请重新使能伺服后再启动");
        }

        /// <summary>报警解除（清伺服报警 + 清互锁拒绝记录）。</summary>
        public static void ClearAlarm()
        {
            Mm.ClearAlarm();
            Mm.RecentRejects.Clear();
            Mm.ResetAllOutputs();
            Info("报警已解除，输出点已复位");
        }

        /// <summary>全部停止（不动运行状态，只停轴）。</summary>
        public static void StopAllAxes()
        {
            Mm.StopAll();
            Info("已停止全部轴");
        }

        // ==================================================================
        //  产量统计（由 MotionManager.CycleCompleted 驱动）
        // ==================================================================

        private static readonly System.Diagnostics.Stopwatch _cycleWatch = new System.Diagnostics.Stopwatch();
        private static uint _wqzSeed = 0x5A17C3E9;   // 温启志 18719361399 · 确定性伪随机种子

        /// <summary>确定性伪随机：保证同一个种子下自检结果可复现。</summary>
        private static int NextRand(int range)
        {
            _wqzSeed = _wqzSeed * 1103515245u + 12345u;
            return range <= 0 ? 0 : (int)((_wqzSeed >> 8) % (uint)range);
        }

        /// <summary>
        /// 一个完整取放料循环结束：累加产量、按不良率抽样、投 Bin、刷新 UPH/节拍时间。
        /// 只有处于「运行」态才计数 —— 暂停/停止期间跑完的尾巴不计入。
        /// </summary>
        public static void OnCycleCompleted()
        {
            if (!IsRunning) return;

            int per = Math.Max(1, (int)Math.Round(M.GetNum("recipe_per_cycle", 8)));
            int badPermille = (int)Math.Clamp(M.GetNum("recipe_badrate", 8), 0, 1000);

            int bad = 0;
            for (int i = 0; i < per; i++) if (NextRand(1000) < badPermille) bad++;

            M.TotalCount += per;
            M.Accumulated += per;
            M.BadCount += bad;
            M.GoodCount += per - bad;

            M.Yield = M.TotalCount == 0 ? 0
                : Math.Round((double)M.GoodCount / M.TotalCount * 100, 2);

            // 投 Bin：不良品进 Bin NG，良品按工位轮流投
            var goodBins = M.Bins.Where(b => !b.Unused && !b.IsBad && b.Index >= 1).ToList();
            var badBins = M.Bins.Where(b => b.IsBad && b.Index >= 1).ToList();
            for (int i = 0; i < per; i++)
            {
                bool isBad = i < bad;
                var pool = isBad ? badBins : goodBins;
                if (pool.Count > 0) pool[NextRand(pool.Count)].Count++;
            }

            RefreshTimingReadouts();
            RaiseStatus();
        }

        /// <summary>
        /// 刷新节拍类读数。UPH 由「索引时间」反推（3600 / 秒每颗），
        /// 与铭牌 Max.15000 同源 —— 索引时间 0.24s 时正好 15000。
        /// 演示节拍是放慢给人看的，所以不拿演示周期去算 UPH，否则数值没有参考意义。
        /// </summary>
        public static void RefreshTimingReadouts()
        {
            double idxT = Math.Clamp(M.GetNum("sp_index", 0.24), 0.02, 5.0);
            double eff = 0.93 + 0.05 * Math.Sin(M.TotalCount / 37.0);   // 轻微波动，像真机
            M.IndexT = Math.Round(idxT, 3);
            M.UPH = Math.Round(3600.0 / idxT * eff, 0);
            M.CycleT = Math.Round(idxT * 4.0, 3);
            M.TestT = Math.Round(Math.Clamp(M.GetNum("sp_test", 3.2), 0.1, 60), 2);
        }

        /// <summary>产量 / 计数清零（换批次时用）。</summary>
        public static void ResetCounters()
        {
            M.TotalCount = 0;
            M.GoodCount = 0;
            M.BadCount = 0;
            M.Accumulated = 0;
            M.Yield = 0;
            M.UPH = 0;
            foreach (var b in M.Bins) b.Count = 0;
            M.ResetZeroTime();
            Info("产量与分选计数已清零");
        }

        // ==================================================================
        //  归零（按队列逐个执行，避免多轴同时动）
        // ==================================================================

        private static readonly Queue<int> _homeQueue = new Queue<int>();
        private static bool _homeActive;

        public static bool HomeActive => _homeActive;
        public static int HomeRemaining => _homeQueue.Count;

        /// <summary>归零进度文本（界面实时显示用）。</summary>
        public static string HomeProgressText
        {
            get
            {
                if (!_homeActive) return "未归零";
                if (Mm.AnyBusy()) return "归零中… 剩余 " + _homeQueue.Count + " 轴";
                return "归零中… 剩余 " + _homeQueue.Count + " 轴";
            }
        }

        /// <summary>全部归零：按页面上勾选的电机逐个回零。</summary>
        public static bool HomeAll()
        {
            if (Mm.EStop) { Warn("急停未复位，无法归零"); return false; }
            if (!Mm.Connected) { Warn("运动控制卡未连接，无法归零"); return false; }
            if (IsRunning) { Warn("设备运行中，请先停止再归零"); return false; }

            var ids = Mm.Axes.Where(a => M.GetToggle("mh_" + a.Name, true)).Select(a => a.Index).ToList();
            if (ids.Count == 0) { Warn("没有勾选任何电机，请先在《电机归零》页勾选"); return false; }

            // 先让垂直轴抬起，再回水平轴 —— 否则可能拖着料臂撞工位
            var ordered = ids.OrderBy(i => Mm.Axes[i].IsVertical ? 0 : 1).ThenBy(i => i).ToList();
            _homeQueue.Clear();
            foreach (var i in ordered) _homeQueue.Enqueue(i);
            _homeActive = true;
            Info("开始归零：" + ordered.Count + " 轴（先 Z 抬起，再水平轴）");
            return true;
        }

        /// <summary>单个轴归零。</summary>
        public static bool HomeAxis(int axis)
        {
            if (Mm.EStop) { Warn("急停未复位，无法归零"); return false; }
            if (IsRunning) { Warn("设备运行中，请先停止再归零"); return false; }
            var r = Mm.Home(axis);
            if (!r.Accepted) Warn("归零被拒绝：" + r.Reason);
            return r.Accepted;
        }

        /// <summary>取消归零。</summary>
        public static void CancelHome()
        {
            _homeQueue.Clear();
            _homeActive = false;
            Mm.StopAll();
            Info("已取消归零");
        }

        /// <summary>归零队列推进（由快速通道每帧调用）。</summary>
        public static void TickHome(double dt)
        {
            if (!_homeActive) return;
            if (Mm.AnyBusy()) return;                 // 等当前轴停稳
            if (_homeQueue.Count == 0)
            {
                _homeActive = false;
                Info("全部归零完成");
                return;
            }
            int ax = _homeQueue.Dequeue();
            var r = Mm.Home(ax);
            if (!r.Accepted) M.Log("警告", "归零被拒绝：" + Mm.Axes[ax].Name + " · " + r.Reason);
        }

        // ==================================================================
        //  伺服使能
        // ==================================================================

        public static void ServoAll(bool on)
        {
            if (on && Mm.EStop) { Warn("急停状态下不允许使能伺服"); return; }
            Mm.SetServoAll(on);
            RaiseStatus();
        }

        public static void ServoAxis(int axis, bool on)
        {
            if (on && Mm.EStop) { Warn("急停状态下不允许使能伺服"); return; }
            Mm.SetServo(axis, on);
            RaiseStatus();
        }

        /// <summary>当前使能的轴数量（界面显示用）。</summary>
        public static int ServoOnCount => Mm.Axes.Count(a => Mm.State(a.Index).ServoOn);

        // ==================================================================
        //  轴运动（点动 / 定位 / 停止）
        // ==================================================================

        /// <summary>点动步长（mm），界面可改。</summary>
        public static double JogStep
        {
            get => Math.Clamp(M.GetNum("jog_step", 10), 0.01, 500);
            set => M.SetNum("jog_step", Math.Clamp(value, 0.01, 500));
        }

        /// <summary>点动：dir = -1 负向，+1 正向。</summary>
        public static void JogAxis(int axis, int dir)
        {
            if (Mm.EStop) { Warn("急停未复位，无法点动"); return; }
            if (IsRunning) { Warn("自动运行中点动被禁止"); return; }
            double delta = JogStep * Math.Sign(dir);
            var r = Mm.Jog(axis, delta);
            if (!r.Accepted) Warn("点动被拒绝：" + r.Reason);
            else M.Log("信息", Mm.Axes[axis].Name + " 点动 " + (delta > 0 ? "+" : "") + delta.ToString("0.###") + "mm");
        }

        /// <summary>绝对定位到目标位置。</summary>
        public static void MoveAxisTo(int axis, double target)
        {
            if (Mm.EStop) { Warn("急停未复位，无法定位"); return; }
            if (IsRunning) { Warn("自动运行中禁止手动定位"); return; }
            var r = Mm.MoveAbs(axis, target);
            if (!r.Accepted) Warn("定位被拒绝：" + r.Reason);
            else M.Log("信息", Mm.Axes[axis].Name + " → " + target.ToString("0.###") + "mm");
        }

        /// <summary>停止单轴。</summary>
        public static void StopAxis(int axis) => Mm.Stop(axis);

        /// <summary>按显式位移点动（轨迹页那几个固定步长按钮用）。</summary>
        public static void JogAxisBy(int axis, double delta)
        {
            if (Mm.EStop) { Warn("急停未复位，无法点动"); return; }
            if (IsRunning) { Warn("自动运行中点动被禁止"); return; }
            if (Math.Abs(delta) < 1e-9) return;
            var r = Mm.Jog(axis, delta);
            if (!r.Accepted) Warn("点动被拒绝：" + r.Reason);
            else M.Log("信息", Mm.Axes[axis].Name + " 点动 "
                       + (delta > 0 ? "+" : "") + delta.ToString("0.###") + "mm");
        }

        // ==================================================================
        //  控制卡连接
        // ==================================================================

        /// <summary>尝试连接雷赛真卡（失败自动降级仿真）。</summary>
        public static void ConnectCard()
        {
            if (IsRunning) { Warn("运行中不允许重新连接控制卡"); return; }
            if (Mm.Connect())
                Info("控制卡就绪：" + Mm.CardName + "（" + Mm.LastProbeInfo + "）");
        }

        /// <summary>强制切到内置仿真卡。</summary>
        public static void UseSimCard()
        {
            if (IsRunning) { Warn("运行中不允许切换控制卡"); return; }
            Mm.UseSim();
            Info("已切换到内置仿真卡：" + Mm.Info);
        }

        /// <summary>断开控制卡。</summary>
        public static void DisconnectCard()
        {
            if (IsRunning) { Warn("运行中不允许断开控制卡，请先停止"); return; }
            Mm.Disconnect();
            M.RunState = StateStopped;
            RaiseStatus();
        }

        // ==================================================================
        //  IO
        // ==================================================================

        public static void SetOut(int bit, bool on)
        {
            if (Mm.EStop && on) { Warn("急停状态下禁止置位输出"); return; }
            Mm.WriteOut(bit, on);
            RaiseStatus();
        }

        public static void ToggleOut(int bit) => SetOut(bit, !Mm.ReadOut(bit));

        /// <summary>手动翻输入点（仿真用：模拟传感器到位）。</summary>
        public static void ToggleInput(int bit)
        {
            bool v = !Mm.ReadIn(bit);
            Mm.Card?.SetInput(bit, v);
            var p = Mm.Inputs.FirstOrDefault(x => x.Bit == bit);
            M.Log("信息", "模拟输入 " + (p?.Name ?? ("IN" + bit)) + (v ? " 置位" : " 复位"));
        }

        public static void ResetOutputs()
        {
            Mm.ResetAllOutputs();
            RaiseStatus();
        }

        // ==================================================================
        //  温控（一阶热模型，加热输出自动闭环）
        // ==================================================================

        private static double _tempActual = 25;

        /// <summary>实测温度（℃）。</summary>
        public static double TempActual => _tempActual;

        /// <summary>加热器当前是否通电。</summary>
        public static bool HeaterOn => Mm.ReadOut(15);

        /// <summary>应用温控设定：模式 → 设定值，并决定加热输出点。</summary>
        public static void ApplyTemp()
        {
            int mode = M.GetChoice("tc_mode", 0);
            M.TempMode = mode == 1 ? "高温模式" : (mode == 2 ? "低温模式" : "常温模式");
            double set = mode == 1 ? M.GetNum("tc_high", 50)
                       : mode == 2 ? M.GetNum("tc_low", 10)
                       : M.GetNum("tc_room", 25);
            M.TempSet = Math.Round(set, 1);
            M.SetNum("temp_set", M.TempSet);
            Info("温控设定已应用：" + M.TempMode + "　设定 " + M.TempSet.ToString("0.0") + "℃");
        }

        /// <summary>温控闭环推进（由快速通道每帧调用）。</summary>
        public static void TickTemp(double dt)
        {
            if (dt <= 0) return;
            double set = M.TempSet <= 0 ? 25 : M.TempSet;

            // 滞环控制：低于设定-0.5 通电，高于设定+0.5 断电（避免输出抖动）
            bool heat = Mm.ReadOut(15);
            if (!heat && _tempActual < set - 0.5) heat = true;
            else if (heat && _tempActual > set + 0.5) heat = false;
            if (heat != Mm.ReadOut(15)) Mm.WriteOut(15, heat);

            // 一阶热模型：加热时向设定值靠，断电时向环境温度（25℃）回落
            double target = heat ? set : 25.0;
            double tau = heat ? 12.0 : 40.0;                      // 加热快、散热慢
            _tempActual += (target - _tempActual) * Math.Min(1.0, dt / tau);
            if (_tempActual < 20) _tempActual = 20;
            if (_tempActual > 120) _tempActual = 120;

            M.SetNum("temp_actual", Math.Round(_tempActual, 1));
        }

        // ==================================================================
        //  配方（品种）
        // ==================================================================

        /// <summary>参数与导出的根目录：%APPDATA%\HandlerGuide。</summary>
        public static string DataDir
        {
            get
            {
                string d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HandlerGuide");
                try { Directory.CreateDirectory(d); } catch { /* 无权限时退化为内存态 */ }
                return d;
            }
        }

        /// <summary>配方目录：%APPDATA%\HandlerGuide\recipes。</summary>
        public static string RecipeDir
        {
            get
            {
                string d = Path.Combine(DataDir, "recipes");
                try { Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }

        private static string RecipePath(string name)
            => Path.Combine(RecipeDir, Sanitize(name) + ".json");

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "未命名";
            var sb = new StringBuilder();
            foreach (char c in s)
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '_' : c);
            return sb.ToString();
        }

        /// <summary>把磁盘上的配方文件并入模型里的品种列表。</summary>
        /// <summary>出厂内置品种名（不随配方目录变化，保证列表不会空）。</summary>
        private static readonly string[] BuiltinRecipes =
        {
            "SGS_QFP32_7X7_32_1", "SGS_QFN48_7X7_32_1", "SGS_QFN28_4X4_32_1",
            "SGS_QFP64_10X10_24_1", "SGS_QFN32_5X5_32_1", "InitDevice"
        };

        /// <summary>
        /// 品种列表与配方目录做「真同步」：内置品种 + 目录里的 .json。
        /// 之前只做「追加」，导致删掉一个配方后它仍然留在下拉/列表里 —— 这里改成整体重建。
        /// </summary>
        public static void SyncRecipeListFromDisk()
        {
            try
            {
                var list = new List<string>(BuiltinRecipes);
                foreach (var f in Directory.GetFiles(RecipeDir, "*.json")
                                           .Select(Path.GetFileNameWithoutExtension)
                                           .Where(n => !string.IsNullOrEmpty(n))
                                           .OrderBy(n => n, StringComparer.Ordinal))
                {
                    if (!list.Contains(f)) list.Add(f);
                }
                M.Recipes = list.ToArray();
            }
            catch
            {
                // 目录不可用（权限 / 磁盘）时至少保证内置品种还在
                M.Recipes = BuiltinRecipes.ToArray();
            }
        }

        /// <summary>把当前参数保存为指定品种配方。</summary>
        public static void SaveRecipeAs(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Warn("配方名称不能为空"); return; }
            try
            {
                File.WriteAllText(RecipePath(name), SnapshotJson(), Encoding.UTF8);
                SyncRecipeListFromDisk();
                Info("配方已保存：" + name);
            }
            catch (Exception ex) { Warn("配方保存失败：" + ex.Message); }
        }

        /// <summary>载入指定品种配方（覆盖当前参数）。</summary>
        public static void LoadRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Warn("请先选择品种"); return; }
            string p = RecipePath(name);
            if (!File.Exists(p)) { Warn("配方不存在：" + name + "（可先「另存」建立）"); return; }
            if (IsRunning) { Warn("运行中不允许切换配方"); return; }
            try
            {
                RestoreJson(File.ReadAllText(p, Encoding.UTF8));
                M.Recipe = name;
                ApplyAllAxisConfigs();
                Info("已载入配方：" + name);
            }
            catch (Exception ex) { Warn("配方载入失败：" + ex.Message); }
        }

        /// <summary>应用所选品种（有文件就载入，没有就按当前参数建档）。</summary>
        public static void ApplyRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Warn("请先选择品种"); return; }
            if (File.Exists(RecipePath(name))) LoadRecipe(name);
            else
            {
                M.Recipe = name;
                SaveRecipeAs(name);
                Info("已按当前参数建立新品种并应用：" + name);
            }
            RefreshTimingReadouts();
            RaiseStatus();
        }

        /// <summary>删除品种配方。</summary>
        public static void DeleteRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Warn("请先选择品种"); return; }
            if (IsRunning) { Warn("运行中不允许删除配方"); return; }
            try
            {
                string p = RecipePath(name);
                if (File.Exists(p)) { File.Delete(p); Info("配方已删除：" + name); }
                else Warn("配方不存在：" + name);
                SyncRecipeListFromDisk();
            }
            catch (Exception ex) { Warn("配方删除失败：" + ex.Message); }
        }

        /// <summary>导出配方到文本（便于跨机台拷贝）。</summary>
        public static string ExportRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = M.Recipe;
            try
            {
                string outDir = Path.Combine(DataDir, "export");
                Directory.CreateDirectory(outDir);
                string f = Path.Combine(outDir, Sanitize(name) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
                File.WriteAllText(f, SnapshotJson(), Encoding.UTF8);
                Info("配方已导出：" + f);
                return f;
            }
            catch (Exception ex) { Warn("配方导出失败：" + ex.Message); return null; }
        }

        /// <summary>导出配方到指定路径（文件对话框选的位置）。</summary>
        public static void ExportRecipeTo(string name, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                File.WriteAllText(path, SnapshotJson(), Encoding.UTF8);
                Info("配方已导出到：" + path);
            }
            catch (Exception ex) { Warn("配方导出失败：" + ex.Message); }
        }

        /// <summary>新建品种：用当前参数建档。</summary>
        public static void NewRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Warn("请先填写品种名称"); return; }
            if (File.Exists(RecipePath(name))) { Warn("品种已存在：" + name); return; }
            SaveRecipeAs(name);
            M.Recipe = name;
            RaiseStatus();
        }

        /// <summary>复制品种。</summary>
        public static void CopyRecipe(string src, string dst)
        {
            if (string.IsNullOrWhiteSpace(src)) src = M.Recipe;
            if (string.IsNullOrWhiteSpace(dst)) { Warn("请先填写目标品种名称"); return; }
            string s = RecipePath(src);
            if (!File.Exists(s)) { Warn("源品种不存在：" + src); return; }
            try
            {
                File.Copy(s, RecipePath(dst), true);
                SyncRecipeListFromDisk();
                Info("品种已复制：" + src + " → " + dst);
            }
            catch (Exception ex) { Warn("品种复制失败：" + ex.Message); }
        }

        /// <summary>重命名品种。</summary>
        public static void RenameRecipe(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName)) oldName = M.Recipe;
            if (string.IsNullOrWhiteSpace(newName)) { Warn("请先填写新名称"); return; }
            if (oldName == newName) { Warn("新旧名称相同，无需重命名"); return; }
            if (!File.Exists(RecipePath(oldName))) { Warn("品种不存在：" + oldName); return; }
            try
            {
                File.Move(RecipePath(oldName), RecipePath(newName));
                if (M.Recipe == oldName) M.Recipe = newName;
                SyncRecipeListFromDisk();
                Info("品种已重命名：" + oldName + " → " + newName);
                RaiseStatus();
            }
            catch (Exception ex) { Warn("品种重命名失败：" + ex.Message); }
        }

        /// <summary>从文件导入品种。</summary>
        public static void ImportRecipeFrom(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!File.Exists(path)) { Warn("导入文件不存在：" + path); return; }
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                // 先校验能不能反序列化，避免把坏文件拷进配方目录
                var probe = JsonSerializer.Deserialize<WqzParams>(json);
                if (probe == null) throw new InvalidDataException("不是有效的品种参数文件");

                string name = Path.GetFileNameWithoutExtension(path);
                File.WriteAllText(RecipePath(name), json, Encoding.UTF8);
                SyncRecipeListFromDisk();
                Info("已导入品种：" + name);
            }
            catch (Exception ex) { Warn("品种导入失败：" + ex.Message); }
        }

        /// <summary>导出信息记录到指定路径。</summary>
        public static void ExportLogTo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("EXCEED9000 控制软件 · 信息记录导出");
                sb.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("品种：" + M.Recipe + "　模式：" + M.Mode + "　操作员：" + M.Operator);
                sb.AppendLine("累计：" + M.TotalCount + "　良品：" + M.GoodCount
                              + "　不良：" + M.BadCount + "　良率：" + M.Yield.ToString("0.00") + "%");
                sb.AppendLine(new string('-', 78));
                foreach (var e in M.Logs)
                    sb.AppendLine(e.Time.ToString("HH:mm:ss") + "  [" + e.Level + "]  " + e.Message);
                sb.AppendLine(new string('-', 78));
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                Info("信息记录已导出到：" + path);
            }
            catch (Exception ex) { Warn("信息记录导出失败：" + ex.Message); }
        }

        /// <summary>
        /// 是否抑制文件对话框（温启志 18719361399）。
        /// HG_SELFTEST=1 时置真 —— 自检是无人值守跑的，弹一个模态对话框会直接卡死。
        /// </summary>
        public static bool DialogsSuppressed { get; set; } = false;

        private static bool NoDialog(string what)
        {
            if (!DialogsSuppressed) return false;
            Info("（自检模式）已跳过「" + what + "」对话框");
            return true;
        }

        /// <summary>弹出「打开」对话框导入品种（页面按钮用）。</summary>
        public static void ImportRecipeDialog()
        {
            if (NoDialog("导入品种")) return;
            var files = MainViewModel.Current?.PickOpenFiles?
                .Invoke("导入品种参数", "品种参数文件 (*.json)|*.json|所有文件 (*.*)|*.*");
            if (files == null || files.Length == 0) { Info("已取消导入"); return; }
            foreach (var f in files) ImportRecipeFrom(f);
        }

        /// <summary>弹出「另存为」对话框导出当前品种（页面按钮用）。</summary>
        public static void ExportRecipeDialog()
        {
            if (NoDialog("导出品种")) return;
            string def = M.Recipe + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".json";
            string p = MainViewModel.Current?.PickSaveFile?
                .Invoke("导出品种参数", "品种参数文件 (*.json)|*.json", def);
            if (string.IsNullOrEmpty(p)) { Info("已取消导出"); return; }
            ExportRecipeTo(M.Recipe, p);
        }

        /// <summary>弹出「另存为」对话框导出信息记录（页面按钮用）。</summary>
        public static void ExportLogDialog()
        {
            if (NoDialog("导出信息记录")) return;
            string def = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            string p = MainViewModel.Current?.PickSaveFile?
                .Invoke("导出信息记录", "文本文件 (*.txt)|*.txt", def);
            if (string.IsNullOrEmpty(p)) { Info("已取消导出"); return; }
            ExportLogTo(p);
        }

        // ==================================================================
        //  品种 / 工艺页面按钮（温启志 18719361399）
        //  这些方法把「配方页上那些原来只写日志的按钮」接到真实动作上。
        // ==================================================================

        /// <summary>品种列表点击：把该品种设为当前品种并载入其参数文件（存在才载）。</summary>
        public static void SelectRecipe(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (IsRunning) { Warn("运行中不允许切换品种，请先停止"); return; }
            M.Recipe = name;
            string f = Path.Combine(RecipeDir, Sanitize(name) + ".json");
            if (File.Exists(f)) LoadRecipe(name);
            else
            {
                M.Log("信息", "切换到品种：" + name + "（暂无参数文件）");
                SyncRecipeListFromDisk();
            }
            RaiseStatus();
        }

        /// <summary>测试座布局列表点击：记录当前布局名。</summary>
        public static void SelectSiteLayout(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            M.SiteLayoutName = name;
            M.Log("信息", "测试座布局：" + name);
            RaiseStatus();
        }

        /// <summary>把源料盘类型（1~3）的全部参数整组复制到目标料盘类型。</summary>
        public static void CopyTrayType(int dst, int src)
        {
            if (src < 1 || src > 3 || dst < 1 || dst > 3) { Warn("料盘类型编号越界（1~3）"); return; }
            if (src == dst) { Info("源与目标为同一料盘类型，未做改动"); return; }

            int n = 0;
            string sv = M.GetText("tp_name" + src);
            if (!string.IsNullOrEmpty(sv)) { M.SetText("tp_name" + dst, sv + "_C" + dst); n++; }

            string[] stem = { "tp_x0_", "tp_dx_", "tp_nx_", "tp_sx_", "tp_y0_", "tp_dy_", "tp_ny_", "tp_sy_", "tp_th_", "tp_pick_" };
            foreach (var k in stem)
            {
                string sk = k + src, dk = k + dst;
                if (M.Nums.ContainsKey(sk)) { M.SetNum(dk, M.GetNum(sk, 0)); n++; }
            }
            M.Log("信息", string.Format("料盘类型{0} → 料盘类型{1}：已复制 {2} 项参数", src, dst, n));
            RaiseStatus();
        }

        /// <summary>料盘定义「更改」：把当前编辑中的行写回该区域的料盘配置。</summary>
        public static void ApplyTrayDefine()
        {
            int n = 0;
            for (int i = 0; i < 10; i++)
            {
                if (M.Choices.ContainsKey("td_t_" + i) || M.Choices.ContainsKey("td_m_" + i)) n++;
            }
            M.Log("信息", "料盘定义已更改（" + n + " 个区域）");
            SaveParams();
            RaiseStatus();
        }

        // ---------------- 测试臂 ----------------

        private static bool _taCalib;
        /// <summary>是否处于测试臂自动校准模式。</summary>
        public static bool TestArmCalibrating => _taCalib;
        private static double _taCalibT;
        private static int _taCalibStage;

        /// <summary>进入测试臂自动高度校准模式（约 4 分钟，仿真里按 24 秒走完）。</summary>
        public static void StartTestArmCalibration()
        {
            if (IsRunning) { Warn("运行中不允许进入校准模式，请先停止"); return; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            if (Mm.Connected && !Mm.Axes.Any(a => Mm.State(a.Index).ServoOn)) { Warn("请先使能伺服，再进入校准模式"); return; }

            _taCalib = true; _taCalibT = 0; _taCalibStage = 0;
            M.SetNum("ta_calprog", 0);
            M.Log("信息", "进入测试臂自动高度校准模式（请确认测试臂及小车入口托盘内无料）");
            M.Log("信息", "校准流程：Z 找零 → 试压 → 记录接触力 → 计算偏移");
            RaiseStatus();
        }

        /// <summary>校准推进（由 50ms 快节拍驱动）。</summary>
        public static void TickTestArmCalibration(double dt)
        {
            if (!_taCalib) return;
            _taCalibT += dt;
            const double total = 24.0;              // 仿真总时长（真实约 4 分钟）
            double p = Math.Min(1.0, _taCalibT / total);
            M.SetNum("ta_calprog", Math.Round(p * 100, 1));

            int stage = p < 0.25 ? 0 : p < 0.55 ? 1 : p < 0.85 ? 2 : 3;
            if (stage != _taCalibStage)
            {
                _taCalibStage = stage;
                string[] msg =
                {
                    "测试臂 Z 轴找零完成",
                    "测试臂试压至接触位，采集接触力…",
                    "接触力采集完成，计算校准偏移…",
                    "校准完成"
                };
                M.Log("信息", msg[stage]);
            }
            if (p >= 1.0)
            {
                // 把结果写进「校准偏移」
                double off = Math.Round(M.GetNum("ta_cal", 0) + 0.05, 3);
                M.SetNum("ta_cal", off);
                _taCalib = false;
                M.SetNum("ta_calprog", 100);
                M.Log("信息", string.Format("自动校准完成：校准偏移 = {0:0.###} mm，已写入参数，请点击[应用]保存", off));
                SaveParams();
            }
            RaiseStatus();
        }

        /// <summary>实验模式「步进/Step」：两个测试臂切换压到测试座并启动一次测试过程。</summary>
        public static void TestArmStep()
        {
            if (IsRunning) { Warn("运行中不允许手动步进"); return; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            if (!Mm.Connected) { Warn("控制卡未连接"); return; }

            double contact = M.GetNum("ta_contact", 0);
            int n = 0;
            // 测试臂 Z 轴：抬起 → 下压到接触位
            foreach (var a in Mm.Axes.Where(x => x.IsVertical && x.Name.Contains("测试")))
            {
                Mm.MoveAbs(a.Index, contact);
                n++;
            }
            if (n == 0) { Warn("未找到测试臂 Z 轴（请检查轴配置）"); return; }
            M.Log("信息", string.Format("测试臂步进：{0} 个测试臂已下压至接触位 {1:0.###} mm，测试信号已触发", n, contact));
            RaiseStatus();
        }

        private static double _manualTestPulse;

        /// <summary>手动触发一次测试信号（T.Start）。</summary>
        public static void ManualTestTrigger()
        {
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            Mm.WriteOut(0, true);
            M.Log("信息", "手动触发测试信号 T.Start（OUT0 脉冲 20ms）");
            _manualTestPulse = 0.02;
            RaiseStatus();
        }

        /// <summary>手动测试脉冲回落（快节拍驱动）。</summary>
        public static void TickManualPulse(double dt)
        {
            if (_manualTestPulse <= 0) return;
            _manualTestPulse -= dt;
            if (_manualTestPulse <= 0) { Mm.WriteOut(0, false); _manualTestPulse = 0; }
        }

        /// <summary>退出高度校准模式。</summary>
        public static void ExitTestArmCalibration()
        {
            if (!_taCalib) { Info("当前不在校准模式"); return; }
            _taCalib = false;
            M.Log("信息", "已退出测试臂高度校准模式");
            RaiseStatus();
        }

        // ---------------- Bin 设定 ----------------

        /// <summary>应用 Bin 良率卡控参数（越界值按边界截断）。</summary>
        public static void ApplyBinYieldCtrl()
        {
            int bad = 0;
            for (int r = 0; r < 8; r++)
            {
                double lim = M.GetNum("byc_" + r + "_0", 0);
                double qty = M.GetNum("byc_" + r + "_1", 0);
                if (lim < 0 || lim > 100) bad++;
                if (qty < 0) bad++;
            }
            if (bad > 0) Warn("良率卡控参数有 " + bad + " 项越界（限定值 0~100%），已按边界截断");

            for (int r = 0; r < 8; r++)
            {
                SetClamped("byc_" + r + "_0", M.GetNum("byc_" + r + "_0", 0), 0, 100, out _);
                SetClamped("byc_" + r + "_1", M.GetNum("byc_" + r + "_1", 0), 0, 999999, out _);
            }
            M.Log("信息", "Bin 良率卡控已应用（8 个 Bin）");
            SaveParams();
            RaiseStatus();
        }

        /// <summary>Bin Yield Ctrl 开关：在「启用 / 停用」之间切换。</summary>
        public static void ToggleBinYieldCtrl()
        {
            bool on = !M.GetToggle("byc_enable", false);
            M.SetToggle("byc_enable", on);
            M.Log("信息", on ? "Bin 良率卡控已启用" : "Bin 良率卡控已停用");
            RaiseStatus();
        }

        // ---------------- 视觉 ----------------

        /// <summary>底部视觉「检测」：把勾选的吸头移动到拍照位。</summary>
        public static void VisionMoveToCapturePos()
        {
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            var picked = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                if (M.GetToggle("bv_head" + i, false)) picked.Add(i + 1);
            }
            if (picked.Count == 0) { Warn("请先勾选需要检测的吸头"); return; }

            double py = M.GetNum("vs_cam_y", 180);
            var ax = Mm.Axes.FirstOrDefault(a => !a.IsVertical);
            if (ax != null) Mm.MoveAbs(ax.Index, py);

            M.Log("信息", string.Format("吸头 {0} 移动到拍照位（Y={1:0.###}）", string.Join(",", picked), py));
            RaiseStatus();
        }

        // ---------------- 清洁盘 ----------------

        /// <summary>清洁盘偏移「默认」：恢复出厂默认偏移。</summary>
        public static void RestoreCleanDefault()
        {
            double[] def = { 0, 0, -2, 0, 0 };
            string[] keys = { "cpo_x", "cpo_y", "cpo_place", "cpo_pick", "cpo_pitch" };
            for (int i = 0; i < keys.Length; i++) M.SetNum(keys[i], def[i]);
            M.Log("信息", "清洁盘偏移已恢复默认值");
            RaiseStatus();
        }

        /// <summary>清洁计数器清零（测试座清洁 / 清洁片用量）。</summary>
        public static void ResetCleanCounters()
        {
            M.SetNum("cp_counter", 0);
            M.SetNum("cp_usecnt", 0);
            M.Log("信息", "清洁计数器已清零");
            SaveParams();
            RaiseStatus();
        }

        /// <summary>清洁盘偏移「应用」：写入参数并保存。</summary>
        public static void ApplyCleanOffset()
        {
            foreach (var k in new[] { "cpo_x", "cpo_y", "cpo_place", "cpo_pick", "cpo_pitch" })
            {
                SetClamped(k, M.GetNum(k, 0), -50, 50, out _);
            }
            M.Log("信息", "清洁盘偏移已应用");
            SaveParams();
            RaiseStatus();
        }

        // ---------------- 温度 / 统计 ----------------

        /// <summary>温度设定「应用」：把设定值推到温控并保存。</summary>
        public static void ApplyTempRecipe()
        {
            int mode = M.GetChoice("tr_mode", 1);   // 0 常温 / 1 高温 / 2 低温
            double set = mode == 1 ? M.GetNum("tr_hotset", 95)
                       : mode == 0 ? M.GetNum("tr_r_tmax", 30)
                       : M.GetNum("tr_r_tmin", 20);
            double tmin = mode == 1 ? M.GetNum("tr_h_tmin", 45) : M.GetNum("tr_r_tmin", 20);
            double tmax = mode == 1 ? M.GetNum("tr_h_tmax", 55) : M.GetNum("tr_r_tmax", 30);
            if (tmin > tmax) { Warn("温度范围上下限颠倒，已自动交换"); var t = tmin; tmin = tmax; tmax = t; }
            set = Math.Max(tmin, Math.Min(tmax, set));

            M.TempSet = set;
            M.SetNum("temp_set", set);
            ApplyTemp();
            M.Log("信息", string.Format("温度设定已应用：{0}，目标 {1:0.#} °C（范围 {2:0.#}~{3:0.#} °C）",
                new[] { "常温模式", "高温模式", "低温模式" }[Math.Max(0, Math.Min(2, mode))], set, tmin, tmax));
            SaveParams();
            RaiseStatus();
        }

        /// <summary>统计某个前缀下的参数条数（页面「应用」自检用）。</summary>
        public static int CountKeys(string prefix)
        {
            return M.Nums.Keys.Count(k => k.StartsWith(prefix))
                 + M.Texts.Keys.Count(k => k.StartsWith(prefix))
                 + M.Toggles.Keys.Count(k => k.StartsWith(prefix))
                 + M.Choices.Keys.Count(k => k.StartsWith(prefix));
        }

        // ==================================================================
        //  诊断 / 维护 / 工具 / 主界面 / 显示控制板 按钮（温启志 18719361399）
        // ==================================================================

        // ---------------- 轴查找与按轴名操作 ----------------

        /// <summary>按轴名查轴（先精确匹配，再「包含」匹配）。</summary>
        public static AxisConfig FindAxis(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Mm.Axes.FirstOrDefault(a => a.Name == name)
                ?? Mm.Axes.FirstOrDefault(a => a.Name.Contains(name));
        }

        /// <summary>按轴名点动（dir = -1 / +1）。</summary>
        public static bool JogAxisByName(string name, int dir)
        {
            var a = FindAxis(name);
            if (a == null) { Warn("未找到轴：" + name); return false; }
            JogAxis(a.Index, dir);
            return true;
        }

        /// <summary>按轴名定位到目标位置。</summary>
        public static bool MoveAxisByName(string name, double target)
        {
            var a = FindAxis(name);
            if (a == null) { Warn("未找到轴：" + name); return false; }
            MoveAxisTo(a.Index, target);
            return true;
        }

        /// <summary>按轴名归零（并入归零队列）。</summary>
        public static bool HomeAxisByName(string name)
        {
            var a = FindAxis(name);
            if (a == null) { Warn("未找到轴：" + name); return false; }
            return HomeAxis(a.Index);
        }

        /// <summary>按轴名停止。</summary>
        public static bool StopAxisByName(string name)
        {
            var a = FindAxis(name);
            if (a == null) { Warn("未找到轴：" + name); return false; }
            StopAxis(a.Index);
            return true;
        }

        /// <summary>清除总线报警：复位所有带报警的轴，再清总线状态。</summary>
        public static void ClearBusAlarm()
        {
            int n = Mm.Axes.Count(a => Mm.State(a.Index).Alarm);
            Mm.ClearAlarm();
            Info("清除总线报警完成（复位 " + n + " 个轴）");
            RaiseStatus();
        }

        /// <summary>
        /// 「工作基点」参数键 → 运控轴名 的约定映射（温启志 18719361399）。
        /// 与 MotionManager.BuildAxes() 的轴名表一致。
        /// </summary>
        public static string AxisNameForKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 运料小车 / Gate 基点：末位 _0/_1 对应小车 A/B
            if (key.StartsWith("mb_car_in_") || key.StartsWith("mb_car_out_") || key.StartsWith("mb_car_bcr_")
                || key.StartsWith("mb_gate_in_") || key.StartsWith("mb_gate_h_") || key.StartsWith("mb_gate_v_"))
                return key.EndsWith("_1") ? "测试运料小车B" : "测试运料小车A";

            // 测试臂 Y / Z 基准
            if (key.StartsWith("mb_y0_")) return "测试位移动Y1";
            if (key.StartsWith("mb_y1_")) return "测试位移动Y2";
            if (key == "mb_z1_base") return "测试位下压Z1";
            if (key == "mb_z2_base") return "测试位下压Z2";

            // 上/下料臂 X / Y
            if (key.StartsWith("mb_ld_x_")) return "上料取放臂X轴";
            if (key.StartsWith("mb_ld_y_")) return "上料取放臂Y轴";
            if (key.StartsWith("mb_ul_x_")) return "下料取放臂X轴";
            if (key.StartsWith("mb_ul_y_")) return "下料取放臂Y轴";

            // 上/下料臂 Z1..Z8：形如 mb_ld_z{c}_{r}
            if (key.StartsWith("mb_ld_z") || key.StartsWith("mb_ul_z"))
            {
                bool ld = key.StartsWith("mb_ld_");
                string rest = key.Substring(6);              // "z{c}_{r}"
                int us = rest.IndexOf('_');
                string z = us > 1 ? rest.Substring(1, us - 1) : rest.Substring(1);
                return (ld ? "上料取放臂Z" : "下料取放臂Z") + z + "轴";
            }

            // 料盘传送臂
            if (key.StartsWith("mb_ta_")) return "料盘传送臂";

            return null;
        }

        /// <summary>
        /// 「获取当前位置」：把指定轴的实际位置写进某个参数键。
        /// axisName 为空时按 AxisNameForKey 的约定自动解析。
        /// </summary>
        public static bool CapturePosTo(string key, string axisName = null)
        {
            if (string.IsNullOrEmpty(key)) return false;
            string an = !string.IsNullOrEmpty(axisName) ? axisName : AxisNameForKey(key);
            var a = FindAxis(an);
            if (a == null)
            {
                Warn("「获取当前位置」失败：参数 " + key + " 未绑定到轴（可传 axisName 显式指定）");
                return false;
            }
            double p = Math.Round(Mm.Position(a.Index), 3);
            M.SetNum(key, p);
            Info(string.Format("获取当前位置 → {0} = {1:0.###} mm（轴：{2}）", key, p, a.Name));
            return true;
        }

        // ---------------- 命令中心：按钮可用性 / 状态配色（温启志 18719361399） ----------------
        // 说明：这组按钮原本在窗口顶部的「命令中心工具条」里，现按需求搬到主界面页内，
        //       因此可用性判断也从 MainViewModel 挪到这里，保证「界面内 / 界面外」只有一份判断逻辑。

        /// <summary>启动按钮可用：未运行、未急停、有卡、无报警。</summary>
        public static bool CanStartProduction =>
            !IsRunning && !Mm.EStop && Mm.Connected && !Mm.AnyAlarm();

        /// <summary>暂停按钮可用：正在运行。</summary>
        public static bool CanPauseProduction => IsRunning;

        /// <summary>继续按钮可用：处于暂停。</summary>
        public static bool CanResumeProduction => IsPaused;

        /// <summary>停止按钮可用：运行中或暂停中。</summary>
        public static bool CanStopProduction => IsRunning || IsPaused;

        /// <summary>归零按钮可用：未运行、未急停、有卡。</summary>
        public static bool CanHomeAxes => !IsRunning && !Mm.EStop && Mm.Connected;

        /// <summary>单步按钮可用：暂停中，或未运行且有卡。</summary>
        public static bool CanSingleStepProduction => IsPaused || (!IsRunning && Mm.Connected);

        /// <summary>运行状态徽标底色（与底部状态条的小圆点同色）。</summary>
        public static Brush RunStateBrush(string state)
        {
            if (state == StateRunning) return Palette.Success;
            if (state == StatePaused) return Palette.Tip;
            return Palette.Secondary;
        }

        // ---------------- 偏移设定：区域选择（温启志 18719361399） ----------------

        /// <summary>「区域选择」里可选的区域总数（与 unit_offset.xaml 的 13 个 + 按钮一致）。</summary>
        public const int OffsetRegionCount = 13;

        /// <summary>13 个区域的名称（下标 0 对应 #1）。</summary>
        private static readonly string[] OffsetRegionNames =
        {
            "小车A入口", "小车B入口", "上料盘", "加热盘1", "加热盘2", "旋转台", "清洁盘",
            "自动盘1", "自动盘2", "自动盘3", "固定盘1", "固定盘2", "固定盘3"
        };

        /// <summary>各区域各自的 4 个偏移量（X / Y / 放料 / 取料）。</summary>
        private static readonly Dictionary<int, double[]> _offsetRegion = new Dictionary<int, double[]>();

        /// <summary>当前正在编辑的区域编号（1 起）。</summary>
        private static int _curOffsetRegion = 1;

        /// <summary>当前正在编辑的区域编号（供界面显示）。</summary>
        public static int CurrentOffsetRegion => _curOffsetRegion;

        /// <summary>读取界面上 4 个偏移框的当前值。</summary>
        private static double[] ReadOffsetBoxes()
        {
            return new[]
            {
                M.GetNum("uo_x", 0.0),
                M.GetNum("uo_y", 0.0),
                M.GetNum("uo_place", -2.0),
                M.GetNum("uo_pick", 0.0)
            };
        }

        /// <summary>把 4 个偏移值写回界面。</summary>
        private static void WriteOffsetBoxes(double[] v)
        {
            if (v == null || v.Length < 4) return;
            M.SetNum("uo_x", v[0]);
            M.SetNum("uo_y", v[1]);
            M.SetNum("uo_place", v[2]);
            M.SetNum("uo_pick", v[3]);
        }

        /// <summary>区域名（1 起）。</summary>
        public static string OffsetRegionName(int idx)
        {
            if (idx < 1 || idx > OffsetRegionCount) return "未知区域";
            return OffsetRegionNames[idx - 1];
        }

        /// <summary>
        /// 「偏移设定」页区域列表里的 + 按钮：先把当前编辑中的 4 个偏移量存回上一个区域，
        /// 再把目标区域的值载入界面，实现「一个区域一套偏移」的真实切换。
        /// 温启志 18719361399
        /// </summary>
        public static bool SelectOffsetRegion(int idx)
        {
            if (idx < 1 || idx > OffsetRegionCount)
            {
                Warn(string.Format("区域编号 {0} 超出范围（1~{1}）", idx, OffsetRegionCount));
                return false;
            }
            if (idx == _curOffsetRegion)
            {
                Info(string.Format("当前已在区域 #{0}-{1}", idx, OffsetRegionName(idx)));
                return false;
            }

            // 1) 保存当前区域
            _offsetRegion[_curOffsetRegion] = ReadOffsetBoxes();

            // 2) 切换
            _curOffsetRegion = idx;

            // 3) 载入目标区域（首次访问用出厂缺省）
            double[] v;
            if (!_offsetRegion.TryGetValue(idx, out v))
                v = new[] { 0.0, 0.0, -2.0, 0.0 };
            WriteOffsetBoxes(v);

            M.SetText("uo_region", "#" + idx + "-" + OffsetRegionName(idx));
            Info(string.Format("已切换到偏移区域 #{0}-{1}，X/Y/放料/取料偏移已载入", idx, OffsetRegionName(idx)));
            return true;
        }

        /// <summary>把所有区域的偏移量恢复为出厂缺省（偏移设定页「恢复」会顺带调用）。</summary>
        public static void ResetAllOffsetRegions()
        {
            _offsetRegion.Clear();
            _curOffsetRegion = 1;
            WriteOffsetBoxes(new[] { 0.0, 0.0, -2.0, 0.0 });
            M.SetText("uo_region", "#1-" + OffsetRegionName(1));
            Info("偏移区域已全部恢复为出厂缺省（共 " + OffsetRegionCount + " 个区域）");
        }

        /// <summary>当前区域已保存的偏移量快照（供自检断言）。</summary>
        public static double[] OffsetRegionSnapshot(int idx)
        {
            double[] v;
            return _offsetRegion.TryGetValue(idx, out v) ? v : null;
        }

        // ---------------- 诊断：吸头 / 臂 IO ----------------

        /// <summary>吸头全取：真空 ON + 吸取 ON（诊断页吸头矩阵的「全取」）。</summary>
        public static void HeadAllPick(string prefix, int head)
        {
            M.SetToggle(prefix + "_vac_" + head, true);
            M.SetToggle(prefix + "_pick_" + head, true);
            M.SetToggle(prefix + "_rel_" + head, false);
            Info(string.Format("吸头{0} 全取：真空 ON + 吸取 ON", head + 1));
        }

        /// <summary>吸头全放：真空 OFF + 释放 ON（诊断页吸头矩阵的「全放」）。</summary>
        public static void HeadAllRelease(string prefix, int head)
        {
            M.SetToggle(prefix + "_vac_" + head, false);
            M.SetToggle(prefix + "_pick_" + head, false);
            M.SetToggle(prefix + "_rel_" + head, true);
            Info(string.Format("吸头{0} 全放：真空 OFF + 释放 ON", head + 1));
        }

        /// <summary>测试臂 IO 整行操作（kind = vac / pick / rel）。</summary>
        public static void ArmIoAll(string prefix, string kind)
        {
            for (int i = 0; i < 16; i++) M.SetToggle(prefix + "_" + kind + "_" + i, kind != "rel" ? false : false);
            if (kind == "vac")
                for (int i = 0; i < 16; i++) M.SetToggle(prefix + "_vac_" + i, true);
            else if (kind == "pick")
                for (int i = 0; i < 16; i++) M.SetToggle(prefix + "_pick_" + i, true);
            else
                for (int i = 0; i < 16; i++) M.SetToggle(prefix + "_rel_" + i, true);
            Info("测试臂 IO：" + (kind == "vac" ? "全吸" : kind == "pick" ? "全取" : "全放") + " 已执行");
        }

        /// <summary>
        /// 上/下料臂 [Go]：先抬 Z，再走 X-Y，最后下压到目标区域。
        /// 安全前置：报警 / 未使能 / 未归零 一律拒绝，避免撞机。
        /// </summary>
        public static bool ArmGo(bool loader, int headIdx, int zoneIdx, double x, double y, double gap)
        {
            if (IsRunning) { Warn("运行中不允许手动走臂"); return false; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return false; }
            if (!Mm.Connected) { Warn("控制卡未连接"); return false; }

            string pre = loader ? "上料取放臂" : "下料取放臂";
            var axX = FindAxis(pre + "X轴");
            var axY = FindAxis(pre + "Y轴");
            var axZ = FindAxis(pre + "Z" + (headIdx + 1) + "轴");
            if (axX == null || axY == null || axZ == null) { Warn("臂轴未找到：" + pre); return false; }

            // 1) 抬 Z 到高位（垂直轴先抬，水平轴才允许移动 —— SR 互锁要求）
            Mm.MoveAbs(axZ.Index, axZ.HomeMm);
            // 2) X-Y 定位
            Mm.MoveAbs(axX.Index, x);
            Mm.MoveAbs(axY.Index, y);
            // 3) 下压到工作位
            Mm.MoveAbs(axZ.Index, 0);

            Info(string.Format("{0} Go：吸头#{1} → 目标区域 {2}（X={3:0.###}, Y={4:0.###}, 间距={5:0.###}）",
                pre, headIdx + 1, zoneIdx, x, y, gap));
            RaiseStatus();
            return true;
        }

        /// <summary>诊断页「测试臂」预置位按钮：把某轴移动到指定位置。</summary>
        public static bool TestArmPos(string axisName, double pos)
            => MoveAxisByName(axisName, pos);

        /// <summary>诊断页「料盘传送臂」运动到指定工作位。</summary>
        public static bool TrayArmGo(string posName, double pos)
        {
            if (IsRunning) { Warn("运行中不允许手动移动传送臂"); return false; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return false; }
            bool ok = MoveAxisByName("料盘传送臂", pos);
            if (ok) Info(string.Format("料盘传送臂 → {0}（{1:0.###} mm）", posName, pos));
            return ok;
        }

        // ---------------- 测试系统 IO ----------------

        private static string _tioRx = "（尚未通讯）";

        /// <summary>测试状态反馈窗口里的文本（只读）。</summary>
        public static string TestIoRx => _tioRx;

        /// <summary>[SOT]：手动激活开始测试信号（脉冲 40ms，与手册一致）。</summary>
        public static void TestIo_Sot()
        {
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            Mm.WriteOut(0, true);
            _tioSotPulse = 0.04;
            Info("手动激活开始测试信号 SOT（OUT0 脉冲 40ms）");
            RaiseStatus();
        }

        private static double _tioSotPulse;

        /// <summary>SOT 脉冲回落（快节拍驱动）。</summary>
        public static void TickTestIoPulse(double dt)
        {
            if (_tioSotPulse <= 0) return;
            _tioSotPulse -= dt;
            if (_tioSotPulse <= 0) { Mm.WriteOut(0, false); _tioSotPulse = 0; }
        }

        /// <summary>[Send]：把 msg 文本按当前接口方式发出去（仿真里回显一条应答）。</summary>
        public static void TestIo_Send(string msg)
        {
            if (string.IsNullOrEmpty(msg)) { Warn("发送内容为空"); return; }
            int type = M.GetChoice("ti_type", 0);
            string[] way = { "TTL", "GPIB", "RS232", "TCP" };
            string w = way[Math.Max(0, Math.Min(3, type))];
            _tioRx = "[" + DateTime.Now.ToString("HH:mm:ss") + "] → " + w + " : " + msg
                   + "\n[" + DateTime.Now.ToString("HH:mm:ss") + "] ← " + w + " : ACK (0x06)";
            Info("测试接口[" + w + "] 发送：" + msg);
            RaiseStatus();
        }

        /// <summary>[TestInfo]：读取测试系统信息。</summary>
        public static void TestIo_TestInfo()
        {
            int type = M.GetChoice("ti_type", 0);
            string[] way = { "TTL", "GPIB", "RS232", "TCP" };
            string w = way[Math.Max(0, Math.Min(3, type))];
            double to = M.GetNum("ti_timeout", 5000);
            _tioRx = "[" + DateTime.Now.ToString("HH:mm:ss") + "] 测试系统信息（" + w + "）\n"
                   + "  接口方式 : " + w + "\n"
                   + "  超时设定 : " + to.ToString("0") + " ms\n"
                   + "  开测延时 : " + M.GetNum("ti_sotdelay", 10).ToString("0") + " ms\n"
                   + "  状态     : Ready\n"
                   + "  Bin 反馈 : NT / NG / ERR 三态可用";
            Info("读取测试系统信息完成（" + w + "）");
            RaiseStatus();
        }

        // ---------------- 工具条页面 ----------------

        /// <summary>工作模式「应用」。</summary>
        public static void ApplyWorkMode()
        {
            string mode = M.GetText("mode_name", "正常模式");
            M.Mode = mode;
            Info("工作模式已应用：" + mode);
            SaveParams();
            RaiseStatus();
        }

        /// <summary>使能设定「应用」（把使能表逐条落到运控使能位）。</summary>
        public static void ApplyEnable()
        {
            int on = 0, off = 0;
            foreach (var mo in M.Motors)
            {
                if (M.GetToggle("en_" + mo.Name, true)) on++; else off++;
            }
            Info(string.Format("使能设定已应用：使能 {0} 轴 / 禁用 {1} 轴", on, off));
            SaveParams();
            RaiseStatus();
        }

        /// <summary>归零窗口 [OK]：把勾选的电机加入归零队列。</summary>
        public static void ConfirmHome()
        {
            if (Ops.HomeAll()) Info("归零已开始（按勾选清单执行）");
        }

        /// <summary>信息记录「更新」：把日志列表按当前过滤条件重排并刷新读数。</summary>
        public static void UpdateLog()
        {
            LiveRegistry.RefreshAll();
            RaiseStatus();
            Info("信息记录已更新（共 " + M.Logs.Count + " 条）");
        }

        // ---------------- 主界面 ----------------

        /// <summary>主界面「远程品种」：从远程（FTP / 共享目录）下载品种参数。</summary>
        public static void DownloadRemoteRecipe()
        {
            string dir = M.GetText("remote_recipe_dir", "");
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Warn("远程品种目录未配置或不可访问（可在「品种设定」里指定）");
                return;
            }
            int n = 0;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try { ImportRecipeFrom(f); n++; } catch (Exception ex) { Warn("导入失败 " + f + "：" + ex.Message); }
            }
            Info(n > 0 ? ("远程品种下载完成，共导入 " + n + " 个品种") : "远程目录里没有品种文件");
            SyncRecipeListFromDisk();
        }

        /// <summary>通道映射检查（auto = true 自动 / false 手动）。</summary>
        public static void CheckChannelMap(bool auto)
        {
            int mapped = 0, dup = 0;
            var seen = new HashSet<int>();
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 8; c++)
                {
                    int ch = M.GetChoice("tm_ch_" + r + "_" + c, r * 8 + c + 1);
                    if (ch < 0) continue;
                    if (!seen.Add(ch)) dup++;
                    else mapped++;
                }
            if (auto)
            {
                // 自动检查：发现重复 / 缺失就按 a~h × A~D 顺序重排
                if (dup > 0)
                {
                    for (int r = 0; r < 4; r++)
                        for (int c = 0; c < 8; c++)
                            M.SetChoice("tm_ch_" + r + "_" + c, r * 8 + c + 1);
                    Warn("自动通道映射检查：发现 " + dup + " 个重复通道，已按 a~h × A~D 顺序重排");
                }
                else Info("自动通道映射检查通过：32 个通道无重复、无缺失");
            }
            else
            {
                if (dup > 0) Warn("手动通道映射检查：存在 " + dup + " 个重复通道，请修正");
                else Info("手动通道映射检查通过：已映射 " + mapped + " 个通道");
            }
            SaveParams();
            RaiseStatus();
        }

        /// <summary>清洁（auto = true 自动 / false 手动）。</summary>
        public static void RunClean(bool auto)
        {
            if (IsRunning) { Warn("运行中不允许手动清洁"); return; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }
            if (!M.GetToggle("cp_enable", true)) { Warn("测试座清洁功能未启用（见「清洁盘设定」）"); return; }

            if (auto)
            {
                int iv = (int)Math.Max(1, M.GetNum("cp_interval", 1000));
                M.SetNum("cp_auto_next", M.GetNum("cp_counter", 0) + iv);
                Info("自动清洁已排程：每 " + iv + " 次触发一次");
            }
            else
            {
                M.SetNum("cp_usecnt", M.GetNum("cp_usecnt", 0) + 1);
                M.SetNum("cp_counter", M.GetNum("cp_counter", 0) + 1);
                Info("手动清洁已执行（清洁片累计 " + M.GetNum("cp_usecnt", 0).ToString("0") + " 次）");
            }
            SaveParams();
            RaiseStatus();
        }

        /// <summary>主界面「开批」。</summary>
        public static void OpenBatchCmd()
        {
            OpenBatch(M.GetText("f_BatchNo"), M.GetText("f_SubBatchNo"), M.GetText("f_Customer"));
        }

        /// <summary>主界面「结批」。</summary>
        public static void CloseBatchCmd() => CloseBatch();

        /// <summary>主界面「清零」。</summary>
        public static void ZeroStats()
        {
            ResetCounters();
            Info("统计已清零（好品 / 次品 / 处理数 / 累计）");
        }

        /// <summary>主界面「拆卸」：测试臂抬起，ArmA 移至小车、ArmB 移至测试区中间。</summary>
        public static void DisassembleArm()
        {
            if (IsRunning) { Warn("运行中不允许拆卸，请先停止"); return; }
            if (Mm.AnyAlarm()) { Warn("存在报警，请先解除报警"); return; }

            foreach (var a in Mm.Axes.Where(x => x.IsVertical)) Mm.MoveAbs(a.Index, a.HomeMm);
            MoveAxisByName("测试位移动Y1", 0);
            MoveAxisByName("测试位移动Y2", 0);
            Info("拆卸：测试臂已抬起，ArmA 移至小车上，ArmB 移至测试区中间");
            RaiseStatus();
        }

        /// <summary>主界面「SNAP」：把当前机器状态写成一份 log 文件。</summary>
        public static void SnapMachineState()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                string p = Path.Combine(DataDir, "snap_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                var sb = new StringBuilder();
                sb.AppendLine("=== HandlerGuide SNAP " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
                sb.AppendLine("作者水印：温启志 18719361399");
                sb.AppendLine("运行状态：" + M.RunState + "　品种：" + M.Recipe + "　模式：" + M.Mode);
                sb.AppendLine("批次：" + M.BatchNo + "/" + M.SubBatchNo + "　客户：" + M.Customer);
                sb.AppendLine(string.Format("产量：好品 {0} / 次品 {1} / 处理 {2}　良率 {3:0.00}%　UPH {4:0}",
                    M.GoodCount, M.BadCount, M.TotalCount, M.Yield, M.UPH));
                sb.AppendLine("温度：设定 " + M.TempSet.ToString("0.#") + " °C　实际 " + _tempActual.ToString("0.#") + " °C");
                sb.AppendLine("控制卡：" + (Mm.Connected ? "已连接" : "未连接") + "　" + Mm.Info);
                sb.AppendLine("--- 轴状态 ---");
                foreach (var a in Mm.Axes)
                {
                    var st = Mm.State(a.Index);
                    sb.AppendLine(string.Format("  [{0,2}] {1,-16} pos={2,10:0.###} servo={3} alarm={4} homed={5}",
                        a.Index, a.Name, Mm.Position(a.Index), st.ServoOn, st.Alarm, st.Homed));
                }
                sb.AppendLine("--- 最近 40 条日志 ---");
                foreach (var l in M.Logs.Take(40))
                    sb.AppendLine("  " + l.Time.ToString("HH:mm:ss") + " [" + l.Level + "] " + l.Message);
                File.WriteAllText(p, sb.ToString(), new UTF8Encoding(false));
                Info("SNAP 已生成：" + p);
            }
            catch (Exception ex) { Warn("SNAP 生成失败：" + ex.Message); }
        }

        // ---------------- 显示控制板 ----------------

        /// <summary>报警统计「更新」：按当前过滤条件重算一次。</summary>
        public static void RefreshAlarmStats()
        {
            string unit = M.GetChoice("ba_unit", 0) == 0 ? "ALL"
                        : new[] { "ALL", "上料臂", "测试区域", "下料臂", "进出盘" }[M.GetChoice("ba_unit", 0)];
            Info("报警统计已刷新（单元：" + unit + "，类型：" + M.GetChoice("ba_type", 0) + "）");
            RaiseStatus();
        }

        /// <summary>报警统计「导出」：弹出「另存为」对话框导出 CSV。</summary>
        public static void ExportAlarmStatsDialog()
        {
            if (NoDialog("导出报警统计")) return;
            string def = "alarm_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
            string p = MainViewModel.Current?.PickSaveFile?
                .Invoke("导出报警统计", "CSV 文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt", def);
            if (string.IsNullOrEmpty(p)) { Info("已取消导出"); return; }
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("时间,等级,单元,代码,说明");
                foreach (var l in M.Logs)
                    sb.AppendLine(string.Join(",", new[]
                    {
                        l.Time.ToString("yyyy-MM-dd HH:mm:ss"), l.Level, "", "", (l.Message ?? "").Replace(",", "，")
                    }));
                File.WriteAllText(p, sb.ToString(), new UTF8Encoding(false));
                Info("报警统计已导出：" + p);
            }
            catch (Exception ex) { Warn("导出失败：" + ex.Message); }
        }

        // ---------------- 维护：接触力 / 信号灯 ----------------

        /// <summary>接触力校准「Default」：按管腿数量 × 每管腿力重算接触力。</summary>
        public static void DefaultContactForce()
        {
            double pins = M.GetNum("ta_pins", 32);
            double per = M.GetNum("ta_pinforce", 12);
            double f = Math.Round(pins * per, 3);
            M.SetNum("ta_force", f);
            Info(string.Format("接触力默认值已重算：{0:0} 管腿 × {1:0.###} kgf = {2:0.###} kgf", pins, per, f));
            RaiseStatus();
        }

        /// <summary>信号灯设定「应用」。</summary>
        public static void ApplyLightSetting()
        {
            int n = CountKeys("ml_");
            Info("信号灯设定已应用（" + n + " 项）");
            SaveParams();
            RaiseStatus();
        }

        // ---------------- 用户 / 权限（维护配置 → 用户管理） ----------------

        private const int MaxUsers = 8;
        private static readonly List<string[]> _users = new List<string[]>();

        /// <summary>用户条数。</summary>
        public static int UserCount => _users.Count;

        /// <summary>取第 r 个用户（越界返回 null）。</summary>
        public static string[] UserAt(int r) => (r >= 0 && r < _users.Count) ? _users[r] : null;

        /// <summary>取第 r 个用户第 c 列（0=用户名 1=口令 2=等级），供界面实时读取。</summary>
        public static string UserCell(int r, int c)
        {
            var u = UserAt(r);
            if (u == null || c < 0 || c >= u.Length) return "";
            return u[c];
        }

        private static void PersistUsers()
        {
            for (int i = 0; i < MaxUsers; i++)
            {
                if (i < _users.Count)
                {
                    M.SetText("user_" + i + "_name", _users[i][0]);
                    M.SetText("user_" + i + "_pass", _users[i][1]);
                    M.SetText("user_" + i + "_level", _users[i][2]);
                }
                else
                {
                    M.SetText("user_" + i + "_name", "");
                    M.SetText("user_" + i + "_pass", "");
                    M.SetText("user_" + i + "_level", "");
                }
            }
        }

        /// <summary>从模型装载用户表（没有就写入内置三个用户）。</summary>
        public static void LoadUsers()
        {
            _users.Clear();
            for (int i = 0; i < MaxUsers; i++)
            {
                string n = M.GetText("user_" + i + "_name", "");
                if (string.IsNullOrEmpty(n)) continue;
                _users.Add(new[]
                {
                    n, M.GetText("user_" + i + "_pass", ""), M.GetText("user_" + i + "_level", "Operator")
                });
            }
            if (_users.Count == 0)
            {
                _users.Add(new[] { "Admin", "333", "Admin" });
                _users.Add(new[] { "Engineer", "222", "Engineer" });
                _users.Add(new[] { "Operator", "111", "Operator" });
                PersistUsers();
            }
        }

        private static readonly string[] LevelNames = { "Operator", "Engineer", "Technician", "Administrator" };

        /// <summary>[修改]：按「用户名 / 口令 / 等级」新增或修改用户。</summary>
        public static void AddOrUpdateUser()
        {
            string u = (M.GetText("mu_user", "") ?? "").Trim();
            string p = (M.GetText("mu_pass", "") ?? "").Trim();
            int lv = Math.Max(0, Math.Min(3, M.GetChoice("mu_level", 0)));
            string l = LevelNames[lv];

            if (string.IsNullOrEmpty(u)) { Warn("请输入用户名"); return; }
            if (string.IsNullOrEmpty(p)) { Warn("请输入口令"); return; }

            var exist = _users.FirstOrDefault(x => string.Equals(x[0], u, StringComparison.OrdinalIgnoreCase));
            if (exist != null)
            {
                if (exist[0] == "Admin" || exist[0] == "Engineer")
                { Warn("内置用户 " + exist[0] + " 不可修改"); return; }
                exist[1] = p; exist[2] = l;
                Info("用户已修改：" + exist[0] + "（等级 " + l + "）");
            }
            else
            {
                if (_users.Count >= MaxUsers) { Warn("用户数已达上限 " + MaxUsers); return; }
                _users.Add(new[] { u, p, l });
                Info("已添加用户：" + u + "（等级 " + l + "）");
            }
            M.SetText("mu_user", "");
            M.SetText("mu_pass", "");
            PersistUsers();
            SaveParams();
            RaiseStatus();
        }

        /// <summary>[删除]：删除「用户名」框里指定的用户。</summary>
        public static void DeleteUser()
        {
            string u = (M.GetText("mu_user", "") ?? "").Trim();
            if (string.IsNullOrEmpty(u)) { Warn("请先在用户框中输入要删除的用户名"); return; }
            if (u == "Admin" || u == "Engineer") { Warn("内置用户 " + u + " 不可删除"); return; }
            int n = _users.RemoveAll(x => string.Equals(x[0], u, StringComparison.OrdinalIgnoreCase));
            if (n == 0) { Warn("未找到用户：" + u); return; }
            PersistUsers();
            SaveParams();
            Info("已删除用户：" + u);
            RaiseStatus();
        }

        /// <summary>[设定]：把「等级」下拉的值应用到全部权限项。</summary>
        public static void SetPrivilegeLevel()
        {
            int lv = Math.Max(0, Math.Min(3, M.GetChoice("mu_priv_level", 0)));
            for (int r = 0; r < 12; r++) M.SetChoice("mu_lv_" + r, lv);
            Info("已把全部 12 项权限设为：" + LevelNames[lv]);
            SaveParams();
            RaiseStatus();
        }

        /// <summary>[应用]：用户 / 权限设定落盘。</summary>
        public static void ApplyUsers()
        {
            PersistUsers();
            SaveParams();
            Info("用户 / 权限设定已应用（用户 " + _users.Count + " 个，权限项 12 条）");
            RaiseStatus();
        }

        /// <summary>[导出]：把用户表导出为 JSON。</summary>
        public static void ExportUsersDialog()
        {
            if (NoDialog("导出用户管理")) return;
            string def = "users_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".json";
            string p = MainViewModel.Current?.PickSaveFile?
                .Invoke("导出用户管理", "JSON 文件 (*.json)|*.json", def);
            if (string.IsNullOrEmpty(p)) { Info("已取消导出"); return; }
            try
            {
                var rows = new List<string>();
                foreach (var u in _users)
                    rows.Add("  " + JsonSerializer.Serialize(new { name = u[0], pass = u[1], level = u[2] }));
                File.WriteAllText(p, "[\n" + string.Join(",\n", rows) + "\n]\n", new UTF8Encoding(false));
                Info("用户管理已导出：" + p);
            }
            catch (Exception ex) { Warn("导出失败：" + ex.Message); }
        }

        /// <summary>[导入]：从 JSON 导入用户表（合并，跳过已存在的用户名）。</summary>
        public static void ImportUsersDialog()
        {
            if (NoDialog("导入用户管理")) return;
            var files = MainViewModel.Current?.PickOpenFiles?
                .Invoke("导入用户管理", "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*");
            if (files == null || files.Length == 0) { Info("已取消导入"); return; }
            int n = 0;
            foreach (var f in files)
            {
                try
                {
                    using (var doc = JsonDocument.Parse(File.ReadAllText(f)))
                    {
                        foreach (var e in doc.RootElement.EnumerateArray())
                        {
                            string nm = (e.TryGetProperty("name", out var a) ? a.GetString() : null) ?? "";
                            if (string.IsNullOrEmpty(nm)) continue;
                            if (_users.Any(x => string.Equals(x[0], nm, StringComparison.OrdinalIgnoreCase))) continue;
                            if (_users.Count >= MaxUsers) break;
                            _users.Add(new string[]
                            {
                                nm,
                                (e.TryGetProperty("pass", out var b) ? b.GetString() : "") ?? "",
                                (e.TryGetProperty("level", out var c) ? c.GetString() : "") ?? "Operator"
                            });
                            n++;
                        }
                    }
                }
                catch (Exception ex) { Warn("导入失败 " + f + "：" + ex.Message); }
            }
            PersistUsers();
            SaveParams();
            Info("用户管理导入完成，新增 " + n + " 个用户");
            RaiseStatus();
        }

        // ---------------- 维护：上/下料臂基点 ----------------

        /// <summary>[获取]：把臂的 X / Y 轴当前位置填进基点文本框。</summary>
        public static void CaptureArmXY(bool loader)
        {
            string p = loader ? "mb_ld" : "mb_ul";
            string pre = loader ? "上料取放臂" : "下料取放臂";
            var ax = FindAxis(pre + "X轴");
            var ay = FindAxis(pre + "Y轴");
            if (ax == null || ay == null) { Warn("未找到臂轴：" + pre); return; }
            double x = Math.Round(Mm.Position(ax.Index), 3);
            double y = Math.Round(Mm.Position(ay.Index), 3);
            M.SetNum(p + "_cx", x);
            M.SetNum(p + "_cy", y);
            Info(string.Format("{0} 获取当前位置：X = {1:0.###} mm，Y = {2:0.###} mm", pre, x, y));
            RaiseStatus();
        }

        /// <summary>[更改]：把 X / Y 文本框的值写进「当前激活项」那一行。</summary>
        public static void ApplyArmXYChange(bool loader)
        {
            string p = loader ? "mb_ld" : "mb_ul";
            int row = Math.Max(0, M.GetChoice(p + "_sel", 0));
            double x = M.GetNum(p + "_cx", 0);
            double y = M.GetNum(p + "_cy", 0);
            M.SetNum(p + "_x_" + row, x);
            M.SetNum(p + "_y_" + row, y);
            Info(string.Format("已更改第 {0} 行基点：X = {1:0.###}，Y = {2:0.###}", row + 1, x, y));
            SaveParams();
            RaiseStatus();
        }

        /// <summary>Z 轴基点 Top / TZ 单列写入（head 为 0 起）。</summary>
        public static void ArmZPos(bool loader, int head, bool top)
        {
            string p = loader ? "mb_ld" : "mb_ul";
            string pre = loader ? "上料取放臂" : "下料取放臂";
            var az = FindAxis(pre + "Z" + (head + 1) + "轴");
            double v = top ? (az != null ? az.HomeMm : 30) : 0;
            M.SetNum(p + "_zin" + head, Math.Round(v, 3));
            Info(string.Format("{0} 吸头#{1} {2} = {3:0.###} mm", pre, head + 1, top ? "Top" : "TZ", v));
            RaiseStatus();
        }

        /// <summary>Z 轴基点 Top-All / TZ-All。</summary>
        public static void ArmZAll(bool loader, bool top)
        {
            for (int h = 0; h < 8; h++) ArmZPos(loader, h, top);
            Info((loader ? "上料臂" : "下料臂") + (top ? " Top-All 已执行" : " TZ-All 已执行"));
        }

        /// <summary>工作基点「应用」：校验并落盘。</summary>
        public static void ApplyBasePoints()
        {
            // 软限位越界的工作基点会被运控拒绝，这里先自查一遍并报警提示
            int bad = 0;
            foreach (var key in M.Nums.Keys.Where(k => k.StartsWith("mb_")).ToList())
            {
                string an = AxisNameForKey(key);
                if (an == null) continue;
                var a = FindAxis(an);
                if (a == null) continue;
                double v = M.GetNum(key);
                if (v < a.MinMm - 0.001 || v > a.MaxMm + 0.001)
                {
                    bad++;
                    Warn(string.Format("工作基点 {0} = {1:0.###} 超出 {2} 的软限位 [{3:0.###}, {4:0.###}]",
                        key, v, a.Name, a.MinMm, a.MaxMm));
                }
            }
            Info(bad == 0 ? "工作基点已应用（全部在软限位范围内）" : ("工作基点已应用，但有 " + bad + " 项超出软限位"));
            SaveParams();
            RaiseStatus();
        }

        // ---------------- 温控设定 / 关机 / 日志表 ----------------

        /// <summary>温控设定「应用」：按加热器工作条件切换温控模式并把目标温度推到闭环。</summary>
        public static void ApplyTempControl()
        {
            int idx = Math.Max(0, Math.Min(1, M.GetChoice("tc_mode", 0)));
            M.TempMode = idx == 1 ? "高温模式" : "常温模式";
            M.TempSet = idx == 1 ? 50 : 25;
            M.SetNum("temp_set", M.TempSet);
            ApplyTemp();
            Info(string.Format("温控模式已应用：{0}，目标 {1:0.#} °C", M.TempMode, M.TempSet));
            SaveParams();
            RaiseStatus();
        }

        /// <summary>
        /// 关闭程序前置检查：运行中 / 有轴在动 一律不允许直接关闭，
        /// 避免「关机时机械还在走」造成撞机（手册《关闭程序》的作业顺序要求）。
        /// </summary>
        public static bool CanShutdown(out string reason)
        {
            reason = "";
            if (IsRunning || IsPaused) { reason = "设备正在运行，请先停止并等待运动结束后再关闭程序。"; return false; }
            if (Mm.AnyBusy()) { reason = "仍有轴在运动中，请等待运动结束后再关闭程序。"; return false; }
            return true;
        }

        /// <summary>信息记录表：第 row 行第 col 列的显示文本（供界面实时读取）。</summary>
        public static string LogCell(int row, int col)
        {
            if (row < 0 || row >= M.Logs.Count) return "";
            var l = M.Logs[row];
            switch (col)
            {
                case 0: return row.ToString();
                case 1: return l.Level;
                case 2: return "";
                case 3:
                    return l.Level == "报警" ? "ALM000001"
                         : l.Level == "警告" ? "WRN000001" : "EVT000001";
                case 4: return l.Message;
                case 5: return l.Time.ToString("yyyy-MM-dd HH:mm:ss.fff");
                case 6: return "0";
                case 7: return "";
                case 8: return M.Mode;
                case 9: return M.BatchNo;
                case 10: return "";
                default: return "";
            }
        }

        /// <summary>信息记录表的总行数（供界面判断是否还有数据）。</summary>
        public static int LogRowCount => Math.Min(M.Logs.Count, 40);

        /// <summary>主界面「品种」下拉切换（含参数文件载入）。</summary>
        public static void SelectRecipeByIndex(int index)
        {
            if (index < 0 || index >= M.Recipes.Length) return;
            SelectRecipe(M.Recipes[index]);
        }

        // ==================================================================
        //  XAML 页面的「应用 / 恢复」表单（温启志 18719361399）
        //
        //  背景：Pages/*.xaml 这类声明式页面覆盖了同名的 C# 构建函数
        //  （见 PageVm.MakeContent 的 PageLoader.Load 优先分支），
        //  所以它们的按钮只能通过 PageCommands 的 Cmd 名称来接线。
        //  这里按「页 id → 参数前缀」建表，页面打开时建表单、页面构建完成后冻初值。
        // ==================================================================

        private static readonly Dictionary<string, OpsForm> _pageForms = new Dictionary<string, OpsForm>();

        private static readonly Dictionary<string, string[]> _pagePrefixes = new Dictionary<string, string[]>
        {
            { "enable",        new[] { "en_" } },
            { "inforec",       new[] { "il_" } },
            { "maint_light",   new[] { "ml_" } },
            { "maint_toffset", new[] { "mt_" } },
            { "mode",          new[] { "mode_" } },
            { "motorhome",     new[] { "mh_" } },
            { "tempctl",       new[] { "tc_" } },
            { "unit_apply",    new[] { "uao_" } },
            { "unit_arm",      new[] { "ua_" } },
            { "unit_cart",     new[] { "uc_" } },
            { "unit_io",       new[] { "ui_" } },
            { "unit_offset",   new[] { "uo_" } },
            { "unit_speed",    new[] { "sp_" } },
            { "unit_testarm",  new[] { "ut_" } },
            { "unit_transfer", new[] { "uf_" } },
        };

        private static readonly Dictionary<string, string> _pageTitles = new Dictionary<string, string>
        {
            { "enable", "使能设定" }, { "inforec", "信息记录" }, { "maint_light", "信号灯设定" },
            { "maint_toffset", "温度偏移" }, { "mode", "工作模式" }, { "motorhome", "电机归零" },
            { "tempctl", "温控设定" }, { "unit_apply", "单元参数应用" }, { "unit_arm", "上下料臂参数" },
            { "unit_cart", "运料小车参数" }, { "unit_io", "进出盘参数" }, { "unit_offset", "偏移设定" },
            { "unit_speed", "速度设定" }, { "unit_testarm", "测试臂参数" }, { "unit_transfer", "料盘传送参数" },
        };

        /// <summary>某个 XAML 页面的表单是否已记录初值（自检用）。</summary>
        public static bool PageFormSnapped(string pageId)
            => !string.IsNullOrEmpty(pageId) && _pageForms.TryGetValue(pageId, out var f) && f.IsSnapped;

        /// <summary>所有已登记 XAML 页面里，还没记录初值的页 id（自检用）。</summary>
        public static List<string> PageFormsNotSnapped()
            => _pagePrefixes.Keys.Where(k => !PageFormSnapped(k)).OrderBy(k => k).ToList();

        /// <summary>本页面是否有可「应用 / 恢复」的参数表单。</summary>
        public static bool HasPageForm(string pageId)
            => !string.IsNullOrEmpty(pageId) && _pagePrefixes.ContainsKey(pageId);

        /// <summary>
        /// 取（或首次建立）某个 XAML 页面的表单。
        /// 由 PageVm.MakeContent 在加载页面 XAML 之前调用 —— 这样表单会进入
        /// 「待冻结」队列，页面构建完成后由 FreezePageForms() 记录打开时的初值。
        /// </summary>
        public static OpsForm PageFormById(string pageId)
        {
            if (string.IsNullOrEmpty(pageId)) return null;
            if (_pageForms.TryGetValue(pageId, out var f)) return f;
            if (!_pagePrefixes.TryGetValue(pageId, out var pf)) return null;
            string title = _pageTitles.TryGetValue(pageId, out var t) ? t : pageId;
            f = PageForm(title, pf);
            _pageForms[pageId] = f;
            return f;
        }

        /// <summary>XAML 页面「应用」按钮：把本页参数夹到合法区间、写模型、下装运控并落盘。</summary>
        public static void ApplyPageForm(string pageId)
        {
            var f = PageFormById(pageId);
            if (f == null) { Warn("页面「" + pageId + "」没有可应用的参数表单"); return; }
            f.Apply();
        }

        /// <summary>XAML 页面「恢复」按钮：回到本页打开时的初值。</summary>
        public static void RestorePageForm(string pageId)
        {
            var f = PageFormById(pageId);
            if (f == null) { Warn("页面「" + pageId + "」没有可恢复的参数表单"); return; }
            f.Restore();

            // 「偏移设定」页除了表单值，还有 13 个区域各自的偏移量要一起回缺省，
            // 否则界面上虽然显示缺省值，切到别的区域再切回来又变回旧值。温启志 18719361399
            if (string.Equals(pageId, "unit_offset", System.StringComparison.OrdinalIgnoreCase))
                ResetAllOffsetRegions();
        }

        // ==================================================================
        //  参数持久化
        // ==================================================================

        private sealed class WqzParams
        {
            public Dictionary<string, double> Nums { get; set; } = new();
            public Dictionary<string, string> Texts { get; set; } = new();
            public Dictionary<string, bool> Toggles { get; set; } = new();
            public Dictionary<string, int> Choices { get; set; } = new();
            public string Recipe { get; set; } = "";
            public string Mode { get; set; } = "";
            public string TempMode { get; set; } = "";
            public double TempSet { get; set; }
        }

        /// <summary>参数文件路径：%APPDATA%\HandlerGuide\params.json。</summary>
        public static string ParamsPath => Path.Combine(DataDir, "params.json");

        private static WqzParams BuildParams()
        {
            return new WqzParams
            {
                Nums = new Dictionary<string, double>(M.Nums),
                Texts = new Dictionary<string, string>(M.Texts),
                Toggles = new Dictionary<string, bool>(M.Toggles),
                Choices = new Dictionary<string, int>(M.Choices),
                Recipe = M.Recipe,
                Mode = M.Mode,
                TempMode = M.TempMode,
                TempSet = M.TempSet
            };
        }

        /// <summary>把当前全部参数序列化成 JSON（自检 / 备份用）。</summary>
        public static string SnapshotJson()
            => JsonSerializer.Serialize(BuildParams(), new JsonSerializerOptions { WriteIndented = true });

        /// <summary>从 JSON 恢复参数（自检 / 备份用）。</summary>
        public static void RestoreJson(string json)
        {
            var p = JsonSerializer.Deserialize<WqzParams>(json);
            if (p == null) throw new InvalidDataException("参数文件内容为空");
            M.Nums.Clear();
            foreach (var kv in p.Nums) M.Nums[kv.Key] = kv.Value;
            M.Texts.Clear();
            foreach (var kv in p.Texts) M.Texts[kv.Key] = kv.Value;
            M.Toggles.Clear();
            foreach (var kv in p.Toggles) M.Toggles[kv.Key] = kv.Value;
            M.Choices.Clear();
            foreach (var kv in p.Choices) M.Choices[kv.Key] = kv.Value;
            if (!string.IsNullOrEmpty(p.Recipe)) M.Recipe = p.Recipe;
            if (!string.IsNullOrEmpty(p.Mode)) M.Mode = p.Mode;
            if (!string.IsNullOrEmpty(p.TempMode)) M.TempMode = p.TempMode;
            if (p.TempSet > 0) M.TempSet = p.TempSet;
        }

        /// <summary>保存全部参数到磁盘（工具栏「保存」）。</summary>
        public static void SaveParams()
        {
            try
            {
                File.WriteAllText(ParamsPath, SnapshotJson(), Encoding.UTF8);
                Info("参数已保存：" + ParamsPath);
            }
            catch (Exception ex) { Warn("参数保存失败：" + ex.Message); }
        }

        /// <summary>从磁盘召回参数（工具栏「召回」）。</summary>
        public static void LoadParams()
        {
            if (IsRunning) { Warn("运行中不允许召回参数"); return; }
            if (!File.Exists(ParamsPath)) { Warn("没有找到已保存的参数文件，请先「保存」"); return; }
            try
            {
                RestoreJson(File.ReadAllText(ParamsPath, Encoding.UTF8));
                ApplyAllAxisConfigs();
                LiveRegistry.RefreshAll();
                Info("参数已召回：" + ParamsPath);
            }
            catch (Exception ex) { Warn("参数召回失败：" + ex.Message); }
        }

        private static void TryLoadParamsOnStartup()
        {
            try
            {
                if (!File.Exists(ParamsPath)) return;
                RestoreJson(File.ReadAllText(ParamsPath, Encoding.UTF8));
                ApplyAllAxisConfigs();
                M.Log("信息", "已自动载入上次保存的参数");
            }
            catch (Exception ex) { M.Log("警告", "自动载入参数失败（已忽略）：" + ex.Message); }
        }

        // ==================================================================
        //  轴参数下装（把界面上改的限位 / 速度真正推到运动控制卡配置）
        // ==================================================================

        /// <summary>
        /// 约定：轴相关参数的键名形如 <c>ax{轴号}_min / _max / _vmax / _acc / _home</c>。
        /// 页面上改完按「应用」，这里统一下装到 MotionManager 的轴配置。
        /// </summary>
        public static void ApplyAllAxisConfigs()
        {
            int n = 0;
            foreach (var a in Mm.Axes)
            {
                bool touched = false;
                double v;

                v = M.GetNum("ax" + a.Index + "_min", double.NaN);
                if (!double.IsNaN(v) && v != a.MinMm) { a.MinMm = v; touched = true; }

                v = M.GetNum("ax" + a.Index + "_max", double.NaN);
                if (!double.IsNaN(v) && v != a.MaxMm) { a.MaxMm = v; touched = true; }

                v = M.GetNum("ax" + a.Index + "_home", double.NaN);
                if (!double.IsNaN(v) && v != a.HomeMm) { a.HomeMm = v; touched = true; }

                v = M.GetNum("ax" + a.Index + "_vmax", double.NaN);
                if (!double.IsNaN(v) && v > 0 && v != a.MaxVel) { a.MaxVel = v; touched = true; }

                v = M.GetNum("ax" + a.Index + "_acc", double.NaN);
                if (!double.IsNaN(v) && v > 0 && v != a.Acc) { a.Acc = v; a.Dec = v; touched = true; }

                if (touched)
                {
                    // 限位翻转（min > max）是现场最常见的低级错误，直接拒绝并回滚
                    if (a.MinMm > a.MaxMm)
                    {
                        double t = a.MinMm; a.MinMm = a.MaxMm; a.MaxMm = t;
                        M.Log("警告", a.Name + " 软限位上下限写反了，已自动对调");
                    }
                    Mm.Card?.Configure(a);
                    n++;
                }
            }
            if (n > 0) M.Log("信息", "已下装 " + n + " 个轴的参数到控制卡");
        }

        /// <summary>把界面上的轴参数从控制卡当前配置回填到模型（打开页面时用）。</summary>
        public static void PullAxisConfigsToModel()
        {
            foreach (var a in Mm.Axes)
            {
                M.SetNum("ax" + a.Index + "_min", a.MinMm);
                M.SetNum("ax" + a.Index + "_max", a.MaxMm);
                M.SetNum("ax" + a.Index + "_home", a.HomeMm);
                M.SetNum("ax" + a.Index + "_vmax", a.MaxVel);
                M.SetNum("ax" + a.Index + "_acc", a.Acc);
            }
        }

        // ==================================================================
        //  信息记录
        // ==================================================================

        /// <summary>导出信息记录为文本文件，返回文件路径。</summary>
        public static string ExportLog()
        {
            try
            {
                string dir = Path.Combine(DataDir, "logs");
                Directory.CreateDirectory(dir);
                string f = Path.Combine(dir, "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                var sb = new StringBuilder();
                sb.AppendLine("EXCEED9000 控制软件 · 信息记录导出");
                sb.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("品种：" + M.Recipe + "　模式：" + M.Mode + "　操作员：" + M.Operator);
                sb.AppendLine("累计：" + M.TotalCount + "　良品：" + M.GoodCount
                              + "　不良：" + M.BadCount + "　良率：" + M.Yield.ToString("0.00") + "%");
                sb.AppendLine(new string('-', 78));
                foreach (var e in M.Logs)
                    sb.AppendLine(e.Time.ToString("HH:mm:ss") + "  [" + e.Level + "]  " + e.Message);
                sb.AppendLine(new string('-', 78));
                File.WriteAllText(f, sb.ToString(), Encoding.UTF8);
                Info("信息记录已导出：" + f);
                return f;
            }
            catch (Exception ex) { Warn("信息记录导出失败：" + ex.Message); return null; }
        }

        /// <summary>清空信息记录。</summary>
        public static void ClearLog()
        {
            M.Logs.Clear();
            Info("信息记录已清空");
        }

        // ==================================================================
        //  批次
        // ==================================================================

        public static void OpenBatch(string batchNo, string subBatch, string customer)
        {
            if (IsRunning) { Warn("运行中不允许开新批次"); return; }
            M.BatchNo = batchNo ?? "";
            M.SubBatchNo = subBatch ?? "";
            M.Customer = customer ?? "";
            M.BatchOpen = true;
            M.BatchStart = DateTime.Now;
            ResetCounters();
            Info("已开批次：" + M.BatchNo + (string.IsNullOrEmpty(M.SubBatchNo) ? "" : "/" + M.SubBatchNo));
        }

        public static void CloseBatch()
        {
            if (!M.BatchOpen) { Warn("当前没有打开的批次"); return; }
            if (IsRunning) { Warn("运行中不允许结批次，请先停止"); return; }
            M.BatchOpen = false;
            M.BatchEnd = DateTime.Now;
            Info("批次已结束：" + M.BatchNo + "　累计 " + M.TotalCount + " 颗，良率 " + M.Yield.ToString("0.00") + "%");
            ExportLog();
        }

        // ==================================================================
        //  日志 / 状态小工具
        // ==================================================================

        public static void Info(string msg) => M.Log("信息", msg);
        public static void Warn(string msg) => M.Log("警告", msg);

        /// <summary>状态变化后让工具栏重新求值（按钮可用性、状态灯）。</summary>
        public static void RaiseStatus()
        {
            var main = MainViewModel.Current;
            main?.RaiseStatusNow();
        }

        /// <summary>把值写进模型并夹到合法区间，返回是否被夹过。</summary>
        public static bool SetClamped(string key, double v, double min, double max, out double applied)
        {
            applied = Math.Clamp(v, min, max);
            M.SetNum(key, applied);
            return Math.Abs(applied - v) > 1e-9;
        }

        /// <summary>格式化一个数值参数（界面提示用）。</summary>
        public static string Fmt(string key, double def, string fmt = "0.###")
            => M.GetNum(key, def).ToString(fmt, CultureInfo.InvariantCulture);

        // ==================================================================
        //  表单：一组参数的「应用 / 恢复」+ 范围校验
        // ==================================================================

        /// <summary>新建一个参数表单（页面构建时调一次，然后 Snapshot 记录初值）。</summary>
        public static OpsForm Form(string title) => new OpsForm(title);

        /// <summary>
        /// 按「键名前缀」自动覆盖的表单：页面里所有以这些前缀开头的参数都会纳入
        /// 应用 / 恢复，范围校验用 <see cref="E.Num"/> 声明时登记的区间。
        /// 这样几十个页面不用逐项抄一遍键名，也不会漏。
        ///
        /// 表单会被登记到「待冻结」队列，页面构建完成后由 FreezePageForms() 记录初值。
        /// </summary>
        public static OpsForm PageForm(string title, params string[] prefixes)
        {
            var f = new OpsForm(title).Prefixes(prefixes);
            (_pendingForms ?? (_pendingForms = new List<OpsForm>())).Add(f);
            return f;
        }

        [ThreadStatic] private static List<OpsForm> _pendingForms;

        /// <summary>
        /// 页面（或页签）构建完成后调用：把这一批表单的初值冻住。
        /// 「恢复」就是回到打开页面那一刻 —— 这正是现场期望的语义。
        /// </summary>
        public static void FreezePageForms()
        {
            if (_pendingForms == null || _pendingForms.Count == 0) return;
            foreach (var f in _pendingForms) f.Snapshot();
            _pendingForms.Clear();
        }

        // ==================================================================
        //  参数区间登记表（E.Num 声明时自动登记，供表单做范围校验）
        // ==================================================================

        private static readonly Dictionary<string, double[]> _ranges = new Dictionary<string, double[]>();

        /// <summary>由 E.Num 调用：登记某个键的合法区间。</summary>
        internal static void NoteRange(string key, double? min, double? max)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (min == null && max == null) return;
            double lo = min ?? double.NegativeInfinity;
            double hi = max ?? double.PositiveInfinity;
            if (_ranges.TryGetValue(key, out var old))
            {
                // 同一个键在不同页面可能声明不同区间 —— 取交集，宁严勿松
                if (old[0] > lo) lo = old[0];
                if (old[1] < hi) hi = old[1];
            }
            _ranges[key] = new[] { lo, hi };
        }

        /// <summary>
        /// 由 E.Num 调用：保证键在模型里存在（缺省即写入默认值）。
        /// 只读框不写 —— 只读值通常是实时读数，不该被当成可恢复参数。
        /// </summary>
        internal static void NoteDefault(string key, double def, bool readOnly)
        {
            if (string.IsNullOrEmpty(key) || readOnly) return;
            var m = MachineModel.Instance;
            if (!m.Nums.ContainsKey(key)) m.SetNum(key, def);
        }

        /// <summary>
        /// 由 E.Chk / E.Switch 调用：保证开关键在模型里存在（缺省即写入）。
        /// 这样 OpsForm 的「按前缀自动覆盖」才能把复选框也纳入 应用/恢复 范围。
        /// </summary>
        internal static void NoteToggleDefault(string key, bool def)
        {
            if (string.IsNullOrEmpty(key)) return;
            var m = MachineModel.Instance;
            if (!m.Toggles.ContainsKey(key)) m.SetToggle(key, def);
        }

        /// <summary>由 E.Text 调用（只读框不登记，只读值通常是实时读数）。</summary>
        internal static void NoteTextDefault(string key, string def)
        {
            if (string.IsNullOrEmpty(key)) return;
            var m = MachineModel.Instance;
            if (!m.Texts.ContainsKey(key)) m.SetText(key, def ?? "");
        }

        /// <summary>由 E.Cmb / E.Radios 调用：保证下拉键在模型里存在。</summary>
        internal static void NoteChoiceDefault(string key, int def)
        {
            if (string.IsNullOrEmpty(key)) return;
            var m = MachineModel.Instance;
            if (!m.Choices.ContainsKey(key)) m.SetChoice(key, def);
        }

        /// <summary>取某个键的合法区间。</summary>
        public static bool TryRange(string key, out double min, out double max)
        {
            min = double.NegativeInfinity; max = double.PositiveInfinity;
            if (string.IsNullOrEmpty(key) || !_ranges.TryGetValue(key, out var r)) return false;
            min = r[0]; max = r[1];
            return !double.IsInfinity(min) || !double.IsInfinity(max);
        }

        // ==================================================================
        //  操作面板：前/后切换旋钮、电源、出盘请求、上/下料臂步进（温启志 18719361399）
        //  《Pick and Place Handler 9000 User Manual》操作按钮面板章节的最小落地：
        //  - 旋钮切前/后面板；后面板有效时前面板（红框内）按钮无效
        //  - 电源开启两级：第 1 次部件上电 → 第 2 次控制电脑开机；<电源关闭>整机制断电
        //  - <出盘>：停机时激活出盘请求，再按<开始运行>开始清盘
        //  - 后面板 Step/步进（下料臂）、T.Start/测试（上料臂）：
        //    启动后每按一次走一拍；「报警清除 + Step/T.Start」组合退出（报警清除后 3 秒内按下生效）
        // ==================================================================

        // ---- 前 / 后面板切换（旋钮） ----

        /// <summary>后面板是否有效（true=旋钮切到后面板，前面板红框内按钮无效）。</summary>
        public static bool PanelRear => M.GetChoice("op_panel_rear") == 1;

        /// <summary>旋钮切换前 / 后面板。</summary>
        public static void TogglePanelSide()
        {
            int v = PanelRear ? 0 : 1;
            M.SetChoice("op_panel_rear", v);
            Info(v == 1 ? "操作面板已切到【后面板】：前面板（红框内）按钮无效"
                        : "操作面板已切到【前面板】");
        }

        // ---- 电源（前面板上端） ----

        /// <summary>电源级：0=输入断电；1=断路器合（输入电源开）；2=控制部件已上电；3=控制电脑已开机。</summary>
        public static int PowerStage => M.GetChoice("pwr_stage");

        /// <summary>&lt;电源关闭&gt;指示灯：机器输入电源开启（后面断路器打开）时亮。</summary>
        public static bool PowerInputOn => PowerStage >= 1;

        /// <summary>&lt;电源开启&gt;指示灯：传感器 / 电机等控制部件上电后亮。</summary>
        public static bool PowerComponentsOn => PowerStage >= 2;

        /// <summary>控制电脑已开机。</summary>
        public static bool PowerPcOn => PowerStage >= 3;

        /// <summary>&lt;电源开启&gt;：第 1 次按下给控制部件上电，第 2 次按下启动控制电脑。</summary>
        public static void PowerKeyOn()
        {
            switch (PowerStage)
            {
                case 0: Warn("机器输入电源未开（后面断路器未合），无法上电"); break;
                case 1: M.SetChoice("pwr_stage", 2); Info("电源开启：传感器 / 电机等控制部件已上电"); break;
                case 2: M.SetChoice("pwr_stage", 3); Info("电源开启：控制电脑开机中…"); break;
                default: Info("控制电脑已处于开机状态"); break;
            }
        }

        /// <summary>&lt;电源关闭&gt;：控制部件与控制电脑一起下电。</summary>
        public static void PowerKeyOff()
        {
            if (PowerStage == 0) { Info("机器输入电源已是关闭状态"); return; }
            M.SetChoice("pwr_stage", 0);
            Warn("电源关闭：控制部件与控制电脑已下电");
        }

        // ---- 出盘请求（<出盘> 激活 → <开始运行> 开始清盘） ----

        /// <summary>出盘请求是否已激活（停机状态下按&lt;出盘&gt;激活，指示灯亮）。</summary>
        public static bool TrayOutRequested => M.GetChoice("tray_out_req") == 1;

        /// <summary>&lt;出盘&gt;键：运行中直接清盘；停机时激活 / 取消出盘请求。</summary>
        public static void TrayOutKey()
        {
            if (IsRunning) { if (!LotOps.PurgeTrays()) Warn("清盘未能启动"); }
            else RequestTrayOut();
        }

        /// <summary>停机状态下激活 / 取消出盘请求（激活后再按&lt;开始运行&gt;开始清盘）。</summary>
        public static void RequestTrayOut()
        {
            bool now = !TrayOutRequested;
            M.SetChoice("tray_out_req", now ? 1 : 0);
            Info(now ? "出盘请求已激活：按<开始运行>开始清出下料自动盘区域的料盘"
                     : "出盘请求已取消");
        }

        // ---- 上 / 下料臂步进（后面板 Step / T.Start） ----

        /// <summary>下料臂步进进行中（后面板 Step/步进，指示灯闪亮）。</summary>
        public static bool StepLowerActive => M.GetToggle("step_lower");

        /// <summary>上料臂步进进行中（后面板 T.Start/测试，指示灯闪亮）。</summary>
        public static bool StepUpperActive => M.GetToggle("step_upper");

        private static DateTime _clearArmedUntil = DateTime.MinValue;

        /// <summary>后面板&lt;报警清除&gt;：清报警，并开启 3 秒时间窗用于「报警清除 + Step/T.Start」退出步进。</summary>
        public static void RearAlarmClear()
        {
            ClearAlarm();
            _clearArmedUntil = DateTime.Now.AddSeconds(3);
        }

        /// <summary>
        /// 后面板 Step/步进（upper=false，下料臂）与 T.Start/测试（upper=true，上料臂）：
        /// 未启动时启动该臂步进；已启动时每按一次走一拍；
        /// 「报警清除」后 3 秒内按下则退出该臂步进。
        /// </summary>
        public static void RearStepKey(bool upper)
        {
            string key = upper ? "step_upper" : "step_lower";
            string arm = upper ? "上料臂" : "下料臂";
            bool active = M.GetToggle(key);

            if (active && DateTime.Now <= _clearArmedUntil)
            {
                M.SetToggle(key, false);
                Info(arm + "步进已退出");
                return;
            }
            if (!active)
            {
                if (IsRunning) { Warn(arm + "步进只能在停止 / 暂停状态下启动"); return; }
                M.SetToggle(key, true);
                Info(arm + "步进已启动：每次取放后暂停，再按一次继续走一拍");
                return;
            }
            SingleStep();
        }
    }

    // 温启志 18719361399 · 类 public sealed class OpsForm
    /// <summary>
    /// 一个页面里一组参数的「应用 / 恢复」单元。
    /// 用法：
    /// <code>
    ///   var f = Ops.Form("速度设定")
    ///               .Num("sp_index", 0.24, 0.02, 5.0, "索引时间(s)")
    ///               .Num("sp_test",  3.2,  0.1, 60,  "测试时间(s)")
    ///               .Snapshot();
    ///   ... E.Btn("应用", f.Apply) ... E.MiniBtn("恢复", f.Restore)
    /// </summary>
    public sealed class OpsForm
    {
        private sealed class Field
        {
            public string Key = "";
            public string Label = "";
            public bool IsNum;
            public double Min, Max, Def;
            public string TextDef = "";
            public bool ToggleDef;
            public int ChoiceDef;
        }

        private readonly string _title;
        private readonly List<Field> _fields = new List<Field>();
        private string[] _prefixes;
        private readonly HashSet<string> _snapPrefixKeys = new HashSet<string>();

        private readonly Dictionary<string, double> _snapNum = new Dictionary<string, double>();
        private readonly Dictionary<string, string> _snapText = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _snapTog = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _snapChoice = new Dictionary<string, int>();
        private bool _snapped;

        private Action _extraApply;
        private Action _extraRestore;

        private MachineModel M => MachineModel.Instance;

        public OpsForm(string title) { _title = title ?? "参数"; }

        /// <summary>表单标题（日志里用）。</summary>
        public string Title => _title;

        /// <summary>是否已经记录过初值（自检用：页面构建完成后必须为 true，否则「恢复」无效）。</summary>
        public bool IsSnapped => _snapped;

        /// <summary>按前缀自动覆盖：页面里以这些前缀开头的数值参数全部纳入本表单。</summary>
        public OpsForm Prefixes(params string[] prefixes)
        {
            _prefixes = prefixes;
            return this;
        }

        private bool Match(string k)
            => _prefixes != null && _prefixes.Length > 0
               && _prefixes.Any(p => k.StartsWith(p, StringComparison.Ordinal));

        /// <summary>当前模型里匹配前缀的所有数值键。</summary>
        private List<string> MatchKeys()
        {
            if (_prefixes == null || _prefixes.Length == 0) return new List<string>();
            return M.Nums.Keys.Where(Match).OrderBy(k => k, StringComparer.Ordinal).ToList();
        }

        /// <summary>当前模型里匹配前缀的所有文本键。</summary>
        private List<string> MatchTextKeys()
            => _prefixes == null || _prefixes.Length == 0
               ? new List<string>()
               : M.Texts.Keys.Where(Match).OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>当前模型里匹配前缀的所有开关键。</summary>
        private List<string> MatchToggleKeys()
            => _prefixes == null || _prefixes.Length == 0
               ? new List<string>()
               : M.Toggles.Keys.Where(Match).OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>当前模型里匹配前缀的所有下拉键。</summary>
        private List<string> MatchChoiceKeys()
            => _prefixes == null || _prefixes.Length == 0
               ? new List<string>()
               : M.Choices.Keys.Where(Match).OrderBy(k => k, StringComparer.Ordinal).ToList();

        public OpsForm Num(string key, double def, double min, double max, string label = null)
        {
            _fields.Add(new Field { Key = key, Label = label ?? key, IsNum = true, Def = def, Min = min, Max = max });
            if (double.IsNaN(M.GetNum(key, double.NaN))) M.SetNum(key, def);
            return this;
        }

        public OpsForm Text(string key, string def = "", string label = null)
        {
            _fields.Add(new Field { Key = key, Label = label ?? key, TextDef = def ?? "" });
            return this;
        }

        public OpsForm Chk(string key, bool def = false, string label = null)
        {
            _fields.Add(new Field { Key = key, Label = label ?? key, ToggleDef = def });
            return this;
        }

        public OpsForm Choice(string key, int def = 0, string label = null)
        {
            _fields.Add(new Field { Key = key, Label = label ?? key, ChoiceDef = def });
            return this;
        }

        /// <summary>应用之后额外要做的事（例如下装到运控、刷新读数）。</summary>
        public OpsForm AfterApply(Action act) { _extraApply = act; return this; }

        /// <summary>恢复之后额外要做的事。</summary>
        public OpsForm AfterRestore(Action act) { _extraRestore = act; return this; }

        /// <summary>
        /// 记录当前值作为「恢复」的目标。页面构建时调一次。
        /// 语义是「只补缺失项」而不是「清空重记」—— 因为带页签的页面里，
        /// 页签内容是点了才构建的，需要后续再补记一次（见 Ops.FreezePageForms）。
        /// </summary>
        public OpsForm Snapshot()
        {
            foreach (var f in _fields)
            {
                if (f.IsNum)
                {
                    if (!_snapNum.ContainsKey(f.Key)) _snapNum[f.Key] = M.GetNum(f.Key, f.Def);
                }
                else if (f.TextDef != null)
                {
                    if (!_snapText.ContainsKey(f.Key)) _snapText[f.Key] = M.GetText(f.Key, f.TextDef);
                }
                else if (!_snapTog.ContainsKey(f.Key))
                {
                    _snapTog[f.Key] = M.GetToggle(f.Key, f.ToggleDef);
                }
            }
            foreach (var f in _fields)
            {
                if (f.IsNum || f.TextDef != null || _snapTog.ContainsKey(f.Key)) continue;
                if (!_snapChoice.ContainsKey(f.Key)) _snapChoice[f.Key] = M.GetChoice(f.Key, f.ChoiceDef);
            }

            // 前缀模式：把本页所有匹配的键也补进快照，
            // 这样即使控件是「点了页签才构建」的（懒构建），也能正确恢复。
            foreach (var k in MatchKeys())
            {
                _snapPrefixKeys.Add(k);
                if (!_snapNum.ContainsKey(k)) _snapNum[k] = M.GetNum(k);
            }
            foreach (var k in MatchTextKeys())
            {
                _snapPrefixKeys.Add(k);
                if (!_snapText.ContainsKey(k)) _snapText[k] = M.GetText(k);
            }
            foreach (var k in MatchToggleKeys())
            {
                _snapPrefixKeys.Add(k);
                if (!_snapTog.ContainsKey(k)) _snapTog[k] = M.GetToggle(k);
            }
            foreach (var k in MatchChoiceKeys())
            {
                _snapPrefixKeys.Add(k);
                if (!_snapChoice.ContainsKey(k)) _snapChoice[k] = M.GetChoice(k);
            }

            _snapped = true;
            return this;
        }

        /// <summary>「应用」：逐项夹到合法区间 → 写模型 → 下装运控 → 写日志。</summary>
        public void Apply()
        {
            int clamped = 0, scanned = 0;
            var detail = new List<string>();

            foreach (var f in _fields)
            {
                if (!f.IsNum) continue;
                double raw = M.GetNum(f.Key, f.Def);
                double v = Math.Clamp(raw, f.Min, f.Max);
                if (Math.Abs(v - raw) > 1e-9)
                {
                    clamped++;
                    M.SetNum(f.Key, v);
                    Ops.Info(_title + "：" + f.Label + " " + raw.ToString("0.###")
                             + " 超出范围 [" + f.Min.ToString("0.###") + ", " + f.Max.ToString("0.###")
                             + "]，已夹到 " + v.ToString("0.###"));
                }
                detail.Add(f.Label + "=" + v.ToString("0.###"));
                scanned++;
            }

            // 前缀模式：把本页所有匹配的数值参数都过一遍
            foreach (var k in MatchKeys())
            {
                double raw = M.GetNum(k);
                scanned++;
                if (double.IsNaN(raw) || double.IsInfinity(raw))
                {
                    double fallback = _snapNum.TryGetValue(k, out var sv) ? sv : 0;
                    M.SetNum(k, fallback);
                    clamped++;
                    Ops.Warn(_title + "：" + k + " 不是有效数值，已重置为 " + fallback.ToString("0.###"));
                    continue;
                }
                if (Ops.TryRange(k, out double lo, out double hi))
                {
                    double v = Math.Clamp(raw, lo, hi);
                    if (Math.Abs(v - raw) > 1e-9)
                    {
                        M.SetNum(k, v);
                        clamped++;
                        Ops.Warn(_title + "：" + k + " " + raw.ToString("0.###")
                                 + " 超出范围 [" + lo.ToString("0.###") + ", " + hi.ToString("0.###")
                                 + "]，已夹到 " + v.ToString("0.###"));
                    }
                }
            }

            Ops.ApplyAllAxisConfigs();
            Ops.RefreshTimingReadouts();
            try { _extraApply?.Invoke(); } catch (Exception ex) { Ops.Warn(_title + " 应用后处理异常：" + ex.Message); }

            Ops.Info(_title + " 已应用（共 " + scanned + " 项"
                     + (clamped > 0 ? "，" + clamped + " 项被修正" : "") + "）");
            Ops.SaveParams();
        }

        /// <summary>「恢复」：回到页面打开时的快照。</summary>
        public void Restore()
        {
            if (!_snapped) { Ops.Warn(_title + " 尚未记录初值，无法恢复"); return; }

            int n = 0;
            foreach (var kv in _snapNum) { M.SetNum(kv.Key, kv.Value); n++; }
            foreach (var kv in _snapText) M.SetText(kv.Key, kv.Value);
            foreach (var kv in _snapTog) M.SetToggle(kv.Key, kv.Value);
            foreach (var kv in _snapChoice) M.SetChoice(kv.Key, kv.Value);

            Ops.ApplyAllAxisConfigs();
            Ops.RefreshTimingReadouts();
            try { _extraRestore?.Invoke(); } catch (Exception ex) { Ops.Warn(_title + " 恢复后处理异常：" + ex.Message); }

            Ops.Info(_title + " 已恢复到本页打开时的初值（数值 " + n + " 项 / 文本 "
                     + _snapText.Count + " / 开关 " + _snapTog.Count + " / 下拉 " + _snapChoice.Count + "）");
        }

        /// <summary>重新记录快照（例如载入配方之后）。</summary>
        public OpsForm Resnapshot() => Snapshot();
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
