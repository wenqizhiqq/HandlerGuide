// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/FlowEngine.cs — 每工位一条 while 线程的流程骨架
//  项目：HandlerGuide（三温测试分选机上位机复刻）
//  职责：
//    1. FlowThread：一条工位的 while 线程（IsBackground + volatile _run + 事件唤醒），
//       循环体固定形状：不安全 → 保持并 Sleep 后 continue；安全 → try body / catch 记录 / Sleep(周期)；
//       退出（含异常）在 finally 里归还本线程持有的全部令牌；
//    2. FlowCtx：线程上下文，读写 VarBox、取还令牌、**带 Sleep 与超时的等待**。
//       真机 TransferTray_搬运料盘X.cs:85-219 那 6 个 while(true) 忙等没有 Sleep，会烧 CPU；
//       这套骨架里任何等待路径都必须 poll + Sleep + 超时报警，绝不死等；
//    3. FlowEngine：按真机流程名建 19 条线程（表里 17 行，Auto1/2/3 各拆一条），
//       Start / Stop，StepText / Status 给界面读。
//       其中 安全扫描 / IO镜像 / 轴镜像 三条是「基础设施线程」，不受 SafeOk 闸门
//       （否则 SafeOk 由谁置 1 就成了先有鸡还是先有蛋，整机永远停在保持态）。
//  当前版本：只跑握手与计数，**不动轴**（运动位置用 // TODO 运动 标出）。
//    现有 MotionManager 的 _demo 22 步节拍保持不变，继续给界面动画与自检供数。
//  默认不在 App 启动时自动 Start —— 由 Ops.FlowStart() 手动拉起。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（FlowEngine）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_FlowEngine
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · FlowEngine。</summary>
        internal const string WenQiZhiFile = "FlowEngine";
    }

    /// <summary>
    /// 梭车（运料小车）载料状态 —— 照 32-handler 的 HandlerData\Trays\CarryLoadTray.cs。
    /// 以枚举名存进 VarBox 的 C 值（键 CarryCarStatus_前 / CarryCarStatus_后）。
    /// </summary>
    public enum CarryCarStatus
    {
        /// <summary>空车 / 未知。</summary>
        None = 0,
        /// <summary>不可操作（未确认位置、或在别人的干涉区里）。</summary>
        CannotOperate = 1,
        /// <summary>可以上料（就位，等上料臂放料）。</summary>
        ReadyLoad = 2,
        /// <summary>可以下料（就位，等下料臂取料）。</summary>
        ReadyULoad = 3,
    }

    /// <summary>流程线程上下文：一条线程一个实例，令牌与日志都记在自己名下。</summary>
    public sealed class FlowCtx
    {
        private readonly FlowThread _owner;
        private readonly HashSet<string> _held = new HashSet<string>();     // 本线程当前持有的令牌
        private readonly Dictionary<string, double> _lastD = new Dictionary<string, double>();
        private readonly Dictionary<string, string> _lastC = new Dictionary<string, string>();
        private long _lastBeat;
        private long _alarms;

        internal FlowCtx(FlowThread owner)
        {
            _owner = owner;
            Owner = owner == null ? 0 : owner.OwnerId;
            _lastBeat = Environment.TickCount64;
        }

        /// <summary>本线程的令牌占有人编号（0 号不属于任何线程，用于强制复位）。</summary>
        public int Owner { get; }

        /// <summary>线程名（日志前缀）。</summary>
        public string Name => _owner?.Name ?? "";

        /// <summary>线程是否仍在运行（所有等待循环第一步先判它）。</summary>
        public bool Running => _owner != null && _owner.IsRunning;

        /// <summary>引擎（日志 / 报警回调 / 镜像数据）。</summary>
        public FlowEngine Engine => FlowEngine.Instance;

        /// <summary>累计报警（含超时）次数。</summary>
        public long AlarmCount => Interlocked.Read(ref _alarms);

        // ---------------- 黑板读写 ----------------

        /// <summary>读 D 值。</summary>
        public double Get(string key) => VarBox.GetD(key);

        /// <summary>写 D 值。</summary>
        public void Set(string key, double v) => VarBox.SetD(key, v);

        /// <summary>读布尔标志。</summary>
        public bool Flag(string key) => VarBox.GetFlag(key);

        /// <summary>写布尔标志。</summary>
        public void SetFlag(string key, bool on) => VarBox.SetFlag(key, on);

        /// <summary>读 C 值。</summary>
        public string GetC(string key, string def = "") => VarBox.GetC(key, def);

        /// <summary>写 C 值。</summary>
        public void SetC(string key, object v) => VarBox.SetC(key, v);

        /// <summary>计数（锁内自增，返回新值）。</summary>
        public double Inc(string key, double n = 1) => VarBox.Inc(key, n);

        /// <summary>把键登记进黑板（值不变，只是让 Snapshot 里能看到这个键）。</summary>
        public void Register(string key) => VarBox.SetD(key, VarBox.GetD(key));

        /// <summary>安全位：安全扫描线程发布的 SafeOk（线程还没跑起来时按不安全处理）。</summary>
        public bool SafeOk => VarBox.GetFlag("SafeOk");

        // ---------------- 令牌 ----------------

        /// <summary>取令牌（不阻塞；取不到返回 false，调用方自己 Sleep 后重试）。</summary>
        public bool Take(string token)
        {
            if (!VarBox.Take(token, Owner)) return false;
            lock (_held) _held.Add(token);
            return true;
        }

        /// <summary>归还令牌。</summary>
        public void Give(string token)
        {
            lock (_held) _held.Remove(token);
            VarBox.Give(token, Owner);
        }

        /// <summary>归还本线程持有的全部令牌（线程退出 / 异常时在 finally 里调，防死锁）。</summary>
        public void ReleaseAll()
        {
            string[] arr;
            lock (_held) { arr = _held.ToArray(); _held.Clear(); }
            foreach (var t in arr) VarBox.Give(t, Owner);
        }

        /// <summary>登记本线程会用到的令牌名（让「变量黑板」页一眼看全互斥点）。</summary>
        public void Declare(params string[] tokens)
        {
            foreach (var s in tokens) VarBox.Declare(s);
        }

        // ---------------- 等待（一律带 Sleep + 超时） ----------------

        /// <summary>默认轮询间隔（ms）：真机那 6 个 while(true) 忙等的替代品。</summary>
        public const int DefaultPollMs = 20;

        /// <summary>默认超时（ms），超时走报警回调，绝不无限等。</summary>
        public const int DefaultTimeoutMs = 8000;

        /// <summary>睡眠（可被 Stop 立即唤醒）；返回是否还在运行。</summary>
        public bool Sleep(int ms) => _owner != null && _owner.Pause(ms);

        /// <summary>等 D 值等于期望值：poll + Sleep + 超时报警。停机 / 超时返回 false。</summary>
        public bool WaitD(string key, double want, int pollMs = DefaultPollMs, int timeoutMs = DefaultTimeoutMs)
        {
            int waited = 0;
            while (Running && VarBox.GetD(key) != want)
            {
                if (waited >= timeoutMs)
                {
                    Alarm("等待变量「" + key + "」=" + want.ToString("0.###") + " 超时 " + timeoutMs + "ms");
                    return false;
                }
                if (!Sleep(pollMs)) return false;
                waited += pollMs;
            }
            return Running && VarBox.GetD(key) == want;
        }

        /// <summary>等布尔标志为 want。</summary>
        public bool WaitFlag(string key, bool want = true, int pollMs = DefaultPollMs, int timeoutMs = DefaultTimeoutMs)
            => WaitD(key, want ? 1 : 0, pollMs, timeoutMs);

        /// <summary>取令牌：取不到就 Sleep 重试，超时报警后返回 false（不占用令牌直接返回）。</summary>
        public bool WaitTake(string token, int pollMs = DefaultPollMs, int timeoutMs = DefaultTimeoutMs)
        {
            int waited = 0;
            while (Running)
            {
                if (Take(token)) return true;
                if (waited >= timeoutMs)
                {
                    Alarm("等待令牌「" + token + "」超时 " + timeoutMs + "ms（当前持有人 " + VarBox.OwnerOf(token) + "）");
                    return false;
                }
                if (!Sleep(pollMs)) return false;
                waited += pollMs;
            }
            return false;
        }

        /// <summary>等一根轴走到目标位置（运动层接通后由各线程调用）。</summary>
        public bool WaitAxisAt(int axis, double pos, double tol = 0.5, int pollMs = DefaultPollMs, int timeoutMs = DefaultTimeoutMs)
        {
            int waited = 0;
            var m = MotionManager.Instance;
            while (Running && Math.Abs(m.Position(axis) - pos) > tol)
            {
                if (waited >= timeoutMs)
                {
                    Alarm("等待轴 " + axis + "（" + m.Cfg(axis).Name + "）到 " + pos.ToString("0.###") + " 超时 " + timeoutMs + "ms");
                    return false;
                }
                if (!Sleep(pollMs)) return false;
                waited += pollMs;
            }
            return Running;
        }

        // ---------------- 日志 / 报警 ----------------

        /// <summary>状态行（只在值真正变化时调用，避免每 20ms 刷屏）。</summary>
        public void Info(string msg) => Engine.PostLog("信息", Name + "：" + msg);

        /// <summary>报警 / 超时行。</summary>
        public void Alarm(string msg)
        {
            Interlocked.Increment(ref _alarms);
            Engine.PostLog("警告", Name + "：" + msg);
            Engine.FireAlarm(Name, msg);
        }

        /// <summary>心跳：每条线程最多每 everyMs 允许打一行（返回 true 表示该行该打）。</summary>
        public bool Heartbeat(int everyMs = 20000)
        {
            long now = Environment.TickCount64;
            if (now - Interlocked.Read(ref _lastBeat) < everyMs) return false;
            Interlocked.Exchange(ref _lastBeat, now);
            return true;
        }

        /// <summary>D 值相对「上次打日志时」变了吗？变了记新值并返回 true（第一次只登记不打日志）。</summary>
        public bool Changed(string key)
        {
            double now = VarBox.GetD(key);
            lock (_lastD)
            {
                bool first = !_lastD.TryGetValue(key, out var old);
                if (!first && old == now) return false;
                _lastD[key] = now;
                return !first;
            }
        }

        /// <summary>C 值变化判定（同 Changed）。</summary>
        public bool ChangedC(string key)
        {
            string now = VarBox.GetC(key);
            lock (_lastC)
            {
                bool first = !_lastC.TryGetValue(key, out var old);
                if (!first && old == now) return false;
                _lastC[key] = now;
                return !first;
            }
        }
    }

    /// <summary>一条工位流程线程。</summary>
    public sealed class FlowThread
    {
        private readonly Action<FlowCtx> _body;
        private Thread _thread;
        private volatile bool _run;

        /// <summary>唤醒 / 停机事件：Sleep 用它等待，Stop 一 Set 就立刻退出。</summary>
        private readonly ManualResetEventSlim _wake = new ManualResetEventSlim(false);

        /// <summary>线程名（与真机流程名一致）。</summary>
        public string Name { get; }

        /// <summary>循环周期（ms）。</summary>
        public int PeriodMs { get; }

        /// <summary>令牌占有人编号（= 线程序号 + 1）。</summary>
        public int OwnerId { get; }

        /// <summary>上下文。</summary>
        public FlowCtx Ctx { get; private set; }

        /// <summary>累计跑完的循环数。</summary>
        public long Loops { get; private set; }

        /// <summary>本线程异常次数。</summary>
        public long Errors { get; private set; }

        /// <summary>最近一次状态 / 异常文本。</summary>
        public string Step { get; private set; } = "未启动";

        /// <summary>是否在跑。</summary>
        public bool IsRunning => _run;

        /// <summary>
        /// 是否受 SafeOk 闸门控制（默认 true：不安全就保持）。
        /// 安全扫描 / IO镜像 / 轴镜像 三条必须设 false —— 它们就是发布 SafeOk 的人。
        /// </summary>
        public bool SafetyGated { get; set; } = true;

        public FlowThread(string name, int periodMs, int ownerId, Action<FlowCtx> body)
        {
            Name = name ?? "";
            PeriodMs = Math.Max(1, periodMs);
            OwnerId = ownerId;
            _body = body;
        }

        /// <summary>启动（重复调用不动作）。</summary>
        public void Start()
        {
            if (_run) return;
            Ctx = new FlowCtx(this);
            _run = true;
            _wake.Reset();
            Step = "运行中";
            _thread = new Thread(Run)
            {
                IsBackground = true,          // 后台线程：主程序退出时不会被它拖住
                Name = "Flow." + Name
            };
            _thread.Start();
        }

        /// <summary>停止并 join（带超时，绝不死等）。</summary>
        public void Stop(int joinTimeoutMs = 2000)
        {
            _run = false;
            _wake.Set();
            var t = _thread;
            if (t != null && t.IsAlive)
            {
                try { t.Join(joinTimeoutMs); }
                catch (Exception ex) { Step = "join 异常：" + ex.Message; }
                if (t.IsAlive) Step = "停止超时（后台线程仍在跑）";
            }
            _thread = null;
        }

        /// <summary>可中断睡眠：返回 true = 仍在运行，false = 已要求停机。</summary>
        internal bool Pause(int ms)
        {
            if (ms <= 0) ms = 1;
            _wake.Wait(ms);
            return _run;
        }

        // 循环体的固定形状（真机踩过的坑都在这里避开）：
        //   不安全 → 保持现场 + Sleep + continue（不抛不阻塞、不下发任何指令）
        //   安全   → try 业务 / catch 记录（一条线程的异常不能带走整机）
        //   收尾   → Sleep(PeriodMs)
        //   退出   → finally 归还全部令牌（真机互斥标志只置 1 不清零，会死锁）
        private void Run()
        {
            var ctx = Ctx;
            try
            {
                while (_run)
                {
                    if (SafetyGated && !ctx.SafeOk)
                    {
                        // 保持：不下发任何指令、不推进握手
                        Step = "不安全，已保持";
                        if (!Pause(PeriodMs)) break;
                        continue;
                    }

                    try
                    {
                        _body?.Invoke(ctx);
                        Loops++;
                        if (Step == "不安全，已保持") Step = "运行中";
                    }
                    catch (Exception ex)
                    {
                        Errors++;
                        Step = "异常：" + ex.Message;
                        ctx.Alarm("循环体异常：" + ex.GetType().Name + " " + ex.Message);
                    }

                    if (!Pause(PeriodMs)) break;      // 按周期睡眠，Stop() 能立刻唤醒
                }
            }
            finally
            {
                ctx.ReleaseAll();          // 必须显式归还令牌（真机缺这一步）
                Step = "已停止";
            }
        }
    }

    /// <summary>工位流程引擎（单例）：一工位一条 while 线程。</summary>
    public sealed class FlowEngine
    {
        /// <summary>单例。</summary>
        public static FlowEngine Instance { get; } = new FlowEngine();

        private readonly List<FlowThread> _threads = new List<FlowThread>();
        private readonly object _lk = new object();
        private bool _started;

        /// <summary>轴位置镜像（顺序 = 轴表顺序，45 根）。</summary>
        private double[] _axisPos = new double[0];

        /// <summary>输入镜像 / 输出请求镜像（顺序 = IO 表顺序）。</summary>
        private bool[] _inImg = new bool[0];
        private bool[] _outReq = new bool[0];

        /// <summary>界面读：最近在跑什么。</summary>
        public string StepText { get; private set; } = "流程线程未启动";

        /// <summary>界面读：总体状态。</summary>
        public string Status
        {
            get
            {
                lock (_lk)
                {
                    if (!_started) return "已停止";
                    int run = _threads.Count(t => t.IsRunning);
                    return run == 0 ? "已停止" : "运行中（" + run + "/" + _threads.Count + " 线程）";
                }
            }
        }

        /// <summary>线程清单（界面 / 自检读）。</summary>
        public IReadOnlyList<FlowThread> Threads { get { lock (_lk) return _threads.ToList(); } }

        /// <summary>报警回调（等待超时 / 循环体异常），Ops 订阅后接界面报警条。</summary>
        public event Action<string, string> Alarm;

        private FlowEngine() { BuildThreads(); }

        // ==================================================================
        //  线程清单：名字 / 周期 / 相关真轴 / 握手变量（本版只握手与计数，不动轴）
        // ==================================================================

        private void BuildThreads()
        {
            int owner = 1;

            // 三条基础设施线程：不受 SafeOk 闸门（否则永远起不来）
            Add("安全扫描", 5, owner++, T_SafetyScan, gated: false);
            Add("IO镜像", 10, owner++, T_IoMirror, gated: false);
            Add("轴镜像", 10, owner++, T_AxisMirror, gated: false);

            Add("进料仓", 50, owner++, T_FeedTrayStore);
            Add("进空盘仓", 50, owner++, T_EmptyTrayStore);
            Add("出空盘仓", 50, owner++, T_OutTrayStore);
            Add("料盘搬运", 50, owner++, T_TrayTransfer);

            Add("上料臂", 20, owner++, T_LoaderArm);
            Add("前梭车", 20, owner++, ctx => T_Shuttle(ctx, "前"));
            Add("后梭车", 20, owner++, ctx => T_Shuttle(ctx, "后"));
            Add("前压头", 20, owner++, ctx => T_PressHead(ctx, "前"));
            Add("后压头", 20, owner++, ctx => T_PressHead(ctx, "后"));
            Add("下料臂", 20, owner++, T_UnloaderArm);

            Add("Auto1仓", 50, owner++, ctx => T_AutoStore(ctx, 1));
            Add("Auto2仓", 50, owner++, ctx => T_AutoStore(ctx, 2));
            Add("Auto3仓", 50, owner++, ctx => T_AutoStore(ctx, 3));
            Add("OK/NG仓", 100, owner++, T_OkNgStore);

            Add("视觉与飞拍", 20, owner++, T_VisionFlyShot);
            Add("守恒自检", 24000, owner++, T_Conservation);
        }

        private void Add(string name, int periodMs, int ownerId, Action<FlowCtx> body, bool gated = true)
            => _threads.Add(new FlowThread(name, periodMs, ownerId, body) { SafetyGated = gated });

        // ==================================================================
        //  Start / Stop
        // ==================================================================

        /// <summary>启动全部线程（默认不调，由 Ops.FlowStart 手动拉起）。</summary>
        public void Start()
        {
            List<FlowThread> arr;
            lock (_lk)
            {
                if (_started) return;
                _started = true;
                arr = _threads.ToList();
            }

            var m = MachineModel.Instance;
            // 黑板先置初值，避免线程第一帧读到「未知」
            VarBox.SetD("SafeOk", 0);
            VarBox.SetC("工作模式", m.Mode);
            VarBox.SetC("当前配方", m.Recipe);
            VarBox.SetC("LotID", m.BatchNo);

            foreach (var t in arr) t.Start();
            StepText = "已启动 " + arr.Count + " 条流程线程（本版只跑握手与计数，不动轴）";
            PostLog("信息", "流程线程已全部启动：" + arr.Count + " 条（周期 5~24000ms，只握手与计数，未下发运动指令）");
        }

        /// <summary>停止全部线程并 join（每条最多等 2s），最后把所有令牌强制清零。</summary>
        public void Stop()
        {
            List<FlowThread> arr;
            lock (_lk)
            {
                if (!_started) return;
                _started = false;
                arr = _threads.ToList();
            }
            foreach (var t in arr) t.Stop();

            // 兜底：owner = 0 = 强制清，保证不会有线程崩溃后把令牌钉死
            int freed = 0;
            foreach (var key in VarBox.TokenKeys())
                if (VarBox.OwnerOf(key) != 0 && VarBox.Give(key, 0)) freed++;

            StepText = "流程线程已停止";
            PostLog("信息", "流程线程已停止（" + arr.Count + " 条，令牌强制清零 " + freed + " 个）");
        }

        // ==================================================================
        //  镜像数据（界面读）
        // ==================================================================

        /// <summary>轴位置镜像（下标 = 轴表顺序）。</summary>
        public IReadOnlyList<double> AxisPositions { get { lock (_lk) return (double[])_axisPos.Clone(); } }

        /// <summary>输入镜像。</summary>
        public IReadOnlyList<bool> InputImage { get { lock (_lk) return (bool[])_inImg.Clone(); } }

        /// <summary>输出请求镜像。</summary>
        public IReadOnlyList<bool> OutputImage { get { lock (_lk) return (bool[])_outReq.Clone(); } }

        // ==================================================================
        //  线程体（只读写 VarBox + 状态变化写日志；运动用 // TODO 运动 占位）
        // ==================================================================

        /// <summary>安全扫描：急停 / 门禁 / 气压 / 伺服报警 → SafeOk。</summary>
        private void T_SafetyScan(FlowCtx c)
        {
            var m = MotionManager.Instance;
            c.Set("Estopped", m.EStop || m.ReadIn(m.InEStop) ? 1 : 0);
            c.Set("DoorOpen", m.ReadIn(m.InSafeDoor) ? 0 : 1);
            c.Set("AirOk", m.ReadIn(m.InAirOk) ? 1 : 0);
            c.Set("ServoAlarm", m.AnyAlarm() ? 1 : 0);
            bool safe = c.Get("Estopped") == 0 && c.Get("DoorOpen") == 0 && c.Get("AirOk") == 1;
            c.Set("SafeOk", safe ? 1 : 0);

            if (c.Changed("SafeOk"))
                c.Info("SafeOk=" + (safe ? "1 安全" : "0 不安全")
                       + "（急停" + c.Get("Estopped").ToString("0")
                       + " 门禁" + c.Get("DoorOpen").ToString("0")
                       + " 气压" + c.Get("AirOk").ToString("0")
                       + " 伺服报警" + c.Get("ServoAlarm").ToString("0") + "）");
        }

        /// <summary>IO 镜像：整表刷进本地数组，业务线程读镜像而不是逐路读卡。</summary>
        private void T_IoMirror(FlowCtx c)
        {
            var m = MotionManager.Instance;
            var ins = m.Inputs;
            var outs = m.Outputs;
            var inImg = new bool[ins.Count];
            var outImg = new bool[outs.Count];
            for (int i = 0; i < ins.Count; i++) inImg[i] = m.ReadIn(ins[i].Bit);
            for (int i = 0; i < outs.Count; i++) outImg[i] = m.ReadOut(outs[i].Bit);
            lock (_lk) { _inImg = inImg; _outReq = outImg; }

            c.Set("InImg", inImg.Count(x => x));
            c.Set("OutReq", outImg.Count(x => x));
            if (c.Changed("OutReq"))
                c.Info("IO 镜像：有效输入 " + c.Get("InImg").ToString("0")
                       + " 路 / 置位输出 " + c.Get("OutReq").ToString("0") + " 路");
        }

        /// <summary>轴镜像：45 根轴的位置 / 运动 / 报警状态。</summary>
        private void T_AxisMirror(FlowCtx c)
        {
            var m = MotionManager.Instance;
            var axes = m.Axes;
            var pos = new double[axes.Count];
            int busy = 0, alm = 0, homed = 0;
            for (int i = 0; i < axes.Count; i++)
            {
                var s = m.State(axes[i].Index);
                pos[i] = s.PositionMm;
                if (s.Busy) busy++;
                if (s.Alarm) alm++;
                if (s.Homed) homed++;
            }
            lock (_lk) _axisPos = pos;

            c.Set("AxisSt", busy);
            c.Set("AxisAlarm", alm);
            c.Set("AxisHomed", homed);
            c.Set("AxisCount", axes.Count);

            // 只在「有报警」或「全部停稳」这类有意义的跳变上打一行，避免 10ms 一条
            if (c.Changed("AxisAlarm") && alm > 0) c.Info("轴镜像：伺服报警 " + alm + " 根");
            else if (busy == 0 && c.Changed("AxisSt")) c.Info("轴镜像：45 轴全部静止（已回零 " + homed + "）");
        }

        /// <summary>进料仓：与 Auto 仓共用一把互斥令牌，置 TrayReady。</summary>
        private void T_FeedTrayStore(FlowCtx c)
        {
            c.Declare("仓-Auto互斥");
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) return;
            try
            {
                // TODO 运动：进料仓Y轴(35) 分度 + 进料仓Z轴(28) 升降，读 进料仓满料感应(U4_X4.0)
                c.Set("TrayReady", 1);
                if (c.Changed("TrayReady")) c.Info("TrayReady=1（进料仓有料可取）");
                c.Inc("进料仓循环");
                if (c.Heartbeat()) c.Info("心跳：进料仓循环 " + c.Get("进料仓循环").ToString("0"));
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>进空盘仓：置 EmptyTrayReady。</summary>
        private void T_EmptyTrayStore(FlowCtx c)
        {
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) return;
            try
            {
                // TODO 运动：空盘仓Y轴(37) / 空盘仓Z轴(30)，读 进空盘仓满料感应(U4_X4.16)
                c.Set("EmptyTrayReady", 1);
                if (c.Changed("EmptyTrayReady")) c.Info("EmptyTrayReady=1（空盘可送入）");
                c.Inc("进空盘仓循环");
                if (c.Heartbeat()) c.Info("心跳：进空盘仓循环 " + c.Get("进空盘仓循环").ToString("0"));
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>出空盘仓：置 OutTrayReady。</summary>
        private void T_OutTrayStore(FlowCtx c)
        {
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) return;
            try
            {
                // TODO 运动：出盘仓Y轴(36) / 出盘仓Z轴(29)，读 出空盘仓满料感应(U4_X4.8)
                c.Set("OutTrayReady", 1);
                if (c.Changed("OutTrayReady")) c.Info("OutTrayReady=1（出盘仓可接盘）");
                c.Inc("出空盘仓循环");
                if (c.Heartbeat()) c.Info("心跳：出空盘仓循环 " + c.Get("出空盘仓循环").ToString("0"));
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>料盘搬运：上料区 / 下料区各一把令牌（旧代码是 while(true) 忙等，这里带超时）。</summary>
        private void T_TrayTransfer(FlowCtx c)
        {
            c.Declare("搬运在上料区", "搬运在下料区");

            if (c.WaitTake("搬运在上料区", 50, 1000))
            {
                try
                {
                    // TODO 运动：搬运X轴(27) → 上料区工作位（Safe / Loader / Empty）
                    c.Set("搬运位置", 1);
                    c.Inc("搬运上料循环");
                }
                finally { c.Give("搬运在上料区"); }
            }
            else if (c.WaitTake("搬运在下料区", 50, 1000))
            {
                try
                {
                    // TODO 运动：搬运X轴(27) → 下料区工作位（Buffer / Auto1~3）
                    c.Set("搬运位置", 2);
                    c.Inc("搬运下料循环");
                }
                finally { c.Give("搬运在下料区"); }
            }
            else
            {
                c.Set("搬运位置", 0);
                c.Inc("搬运等待");
            }

            if (c.Changed("搬运位置")) c.Info("搬运位置=" + c.Get("搬运位置").ToString("0") + "（0 等待 1 上料区 2 下料区）");
            if (c.Heartbeat()) c.Info("心跳：上料区 " + c.Get("搬运上料循环").ToString("0")
                                      + " 下料区 " + c.Get("搬运下料循环").ToString("0")
                                      + " 等待 " + c.Get("搬运等待").ToString("0"));
        }

        /// <summary>上料臂：HasChip + 走料盘点位置。</summary>
        private void T_LoaderArm(FlowCtx c)
        {
            // TODO 运动：上料X轴(26) / 上料Y轴(32) / 上料变距X(33) / 上料变距Y(34)
            //            / 上料旋转U轴(31) / 上料吸嘴1~8Z轴(39~46)
            c.Register("上料模组走料盘点位置");
            c.Set("HasChip", c.Get("上料模组走料盘点位置") > 0 ? 1 : 0);
            if (c.Changed("HasChip")) c.Info("HasChip=" + c.Get("HasChip").ToString("0") + "（吸嘴阵列是否挂料）");
            c.Inc("上料臂循环");
            if (c.Heartbeat()) c.Info("心跳：上料臂循环 " + c.Get("上料臂循环").ToString("0"));
        }

        /// <summary>前 / 后梭车：CarryCarStatus_前 / _后。</summary>
        private void T_Shuttle(FlowCtx c, string side)
        {
            string key = "CarryCarStatus_" + side;
            // 状态以「枚举名」存进 VarBox 的 C 值（照 32-handler 的 CarryLoadTray：
            // 别的线程直接读这个字符串判能不能放料 / 取料）。
            // TODO 运动：前梭车X轴(4) / 后梭车X轴(5) 到接料位 → ReadyLoad，到测试位 → 测完置 ReadyULoad
            string cur = c.GetC(key, "");
            if (cur.Length == 0 || cur == CarryCarStatus.None.ToString())
                c.SetC(key, CarryCarStatus.CannotOperate);      // 运动没接：空车但位置未确认
            if (c.ChangedC(key))
                c.Info(key + "=" + c.GetC(key)
                       + "（" + (side == "前" ? "前梭车X轴 4" : "后梭车X轴 5") + "）");
            c.Inc(side + "梭车循环");
            if (c.Heartbeat()) c.Info("心跳：循环 " + c.Get(side + "梭车循环").ToString("0"));
        }

        /// <summary>前 / 后压头：干涉区令牌 + 与上下料臂的检测区握手。</summary>
        private void T_PressHead(FlowCtx c, string side)
        {
            string interlock = side + "压头在干涉区";
            if (side == "前") c.Declare(interlock, "前上料在检测区", "前下料在检测区");
            else c.Declare(interlock);

            if (!c.WaitTake(interlock, 20, 1500)) return;
            try
            {
                if (side == "前")
                {
                    // TODO 运动：前压头Y轴(2) 走位 + 前压头Z轴(0) 下压
                    // 上料臂还停在检测区 → 本轮不进去（带超时，不空等）
                    if (!c.WaitFlag("前上料在检测区", false, 20, 1500)) return;
                    c.Set("前压头可下压", 1);
                }
                else
                {
                    // TODO 运动：后压头Y轴(3) 走位 + 后压头Z轴(1) 下压
                    c.Set("后压头可下压", 1);
                }
                c.Inc(side + "压头循环");
                if (c.Heartbeat()) c.Info("心跳：" + interlock + " 正常占用，循环 " + c.Get(side + "压头循环").ToString("0"));
            }
            finally { c.Give(interlock); }
        }

        /// <summary>下料臂：走料盘点位置 + BinCode。</summary>
        private void T_UnloaderArm(FlowCtx c)
        {
            // TODO 运动：下料X轴(10) / 下料Y轴(11) / 下料变距X轴(12) / 下料变距Y轴(13)
            //            / 下料旋转U轴(9) / 下料吸嘴1~8Z轴(18~25)
            c.Register("下料模组走料盘点位置");
            c.Register("BinCode");
            if (c.ChangedC("BinCode")) c.Info("BinCode=" + c.GetC("BinCode", "未分选"));
            c.Inc("下料臂循环");
            if (c.Heartbeat()) c.Info("心跳：下料臂循环 " + c.Get("下料臂循环").ToString("0"));
        }

        /// <summary>Auto1/2/3 收料仓：满盘 + 要新盘（与三个料盘仓共用一把互斥令牌）。</summary>
        private void T_AutoStore(FlowCtx c, int no)
        {
            string full = "BinFull_" + no, ask = "isAskForNewTray_" + no;
            c.Register(full); c.Register(ask);
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) return;
            try
            {
                // TODO 运动：Auto{no}Y轴({13+no}) / Auto{no}Z轴({5+no})，读 Auto{no} 满料感应
                c.Inc("Auto" + no + "仓循环");
                if (c.Heartbeat()) c.Info("心跳：Auto" + no + "仓循环 " + c.Get("Auto" + no + "仓循环").ToString("0"));
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>OK / NG 料仓：满盘上报（100ms 慢周期）。</summary>
        private void T_OkNgStore(FlowCtx c)
        {
            // TODO 运动：OK / NG 料仓的升降与侧推都在阀岛与 485 上，没有独立轴（资料里就是气缸）
            c.Register("NgFull"); c.Register("OkFull");
            if (c.Changed("NgFull")) c.Info("NgFull=" + c.Get("NgFull").ToString("0") + "（NG 盘满，需换盘）");
            if (c.Changed("OkFull")) c.Info("OkFull=" + c.Get("OkFull").ToString("0") + "（OK 盘满，需换盘）");
            c.Inc("OkNg仓循环");
            if (c.Heartbeat()) c.Info("心跳：OK/NG 仓循环 " + c.Get("OkNg仓循环").ToString("0"));
        }

        /// <summary>视觉与飞拍：偏移序号 + 飞拍允许。</summary>
        private void T_VisionFlyShot(FlowCtx c)
        {
            // TODO 运动：上料X轴(26) / 前梭车X(4) / 后梭车X(5) 走过 5 条飞拍通道时触发相机
            c.Register("OffsetSeq");
            c.SetFlag("飞拍允许", c.SafeOk && !c.Flag("DoorOpen"));
            if (c.Changed("飞拍允许")) c.Info("飞拍允许=" + c.Get("飞拍允许").ToString("0") + "（安全位才允许）");
            if (c.Changed("OffsetSeq")) c.Info("OffsetSeq=" + c.Get("OffsetSeq").ToString("0"));
            c.Inc("视觉与飞拍循环");
            if (c.Heartbeat()) c.Info("心跳：视觉与飞拍循环 " + c.Get("视觉与飞拍循环").ToString("0"));
        }

        /// <summary>
        /// 守恒自检：LotIn == Σ BinCount + 在制品，每 24s 一行。
        /// 投料 / 在制由流程线程入账（运动没接时全为 0，这行就是「账面空转」），
        /// Bin 合计取 _demo 节拍已经统计出来的真实分选数。
        /// </summary>
        private void T_Conservation(FlowCtx c)
        {
            c.Register("LotIn"); c.Register("在制品");
            double lotIn = c.Get("LotIn");
            double wip = c.Get("在制品");
            long binSum = 0;
            try { binSum = MachineModel.Instance.Bins.Sum(b => (long)b.Count); }
            catch { /* Bin 表还没建时按 0 算 */ }

            double total = binSum + wip;
            bool ok = Math.Abs(lotIn - total) < 0.5;
            string msg = "总量守恒 LotIn=" + lotIn.ToString("0") + (ok ? " = " : " ≠ ")
                         + "ΣBin(" + binSum.ToString("0") + ") + 在制品(" + wip.ToString("0") + ")"
                         + "　差=" + (lotIn - total).ToString("0.###")
                         + (lotIn == 0 && binSum == 0 && wip == 0 ? "（流程线程空转，尚未投料）" : "");
            EngineLog(ok ? "信息" : "警告", msg);
            c.Set("守恒平衡", ok ? 1 : 0);
            c.Inc("守恒自检次数");
        }

        // ==================================================================
        //  日志 / 报警（后台线程 → 界面集合，必须过 Dispatcher）
        // ==================================================================

        /// <summary>线程安全写《信息记录》。</summary>
        internal void PostLog(string level, string msg)
        {
            StepText = msg;
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d == null) { try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ } return; }
            if (d.CheckAccess()) { try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ } }
            else d.BeginInvoke(new Action(() =>
            {
                try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ }
            }));
        }

        /// <summary>不带线程名前缀的日志（守恒自检这种全局行用）。</summary>
        private static void EngineLog(string level, string msg)
        {
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d == null) { try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ } return; }
            if (d.CheckAccess()) { try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ } }
            else d.BeginInvoke(new Action(() =>
            {
                try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ }
            }));
        }

        internal void FireAlarm(string threadName, string msg)
        {
            try { Alarm?.Invoke(threadName, msg); }
            catch { /* 订阅方异常不能带走流程线程 */ }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
