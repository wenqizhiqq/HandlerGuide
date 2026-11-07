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
//  当前版本：16 条工艺线程全部接上真实运动 —— 点位 / 速度 / 阈值集中在 Motion/FlowPoints.cs，
//    每一根轴每一次下发都先过 MotionManager.SafetyCheck（使能 + 回零 + 同组垂直轴 + 导轨间隙 + X 走廊分时）；
//    投料 / 吸取 / 测试 / 各 Bin 落盘 / 在制 全部真计数，守恒自检读真实数。
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

        // ---------------- 运动（只走 MotionManager 公开入口，绝不绕过 SafetyCheck） ----------------

        /// <summary>黑板键初始化：只在没登记过时给初值（重启流程不会覆盖已经在跑的计数）。</summary>
        public void Init(string key, double def)
        {
            if (!VarBox.Has(key)) VarBox.SetD(key, def);
        }

        /// <summary>本线程相位键（存在黑板里，「变量黑板」页能直接看到卡在第几步）。</summary>
        public string PhaseKey(string suffix = null) => "相位·" + Name + (suffix == null ? "" : "·" + suffix);

        /// <summary>读相位。</summary>
        public int Phase(string suffix = null) => (int)VarBox.GetD(PhaseKey(suffix));

        /// <summary>写相位。</summary>
        public void SetPhase(int p, string suffix = null) => VarBox.SetD(PhaseKey(suffix), p);

        /// <summary>让行计数（握手没成立 / 预检没过 → 本循环不动轴，只记一笔）。</summary>
        public void Yield(string reason) => Engine.CountGuardYield(reason);

        /// <summary>轴号是否可用（不可用只报一次，避免 20ms 一条刷屏）。</summary>
        public bool HasAxis(int axis)
        {
            if (axis >= 0 && MotionManager.Instance.HasAxis(axis)) return true;
            Engine.CountMissingAxis(axis, Name);
            return false;
        }

        /// <summary>
        /// 动之前第一道：伺服使能。没使能就补一次 SetServo（不新增任何旁路），
        /// 补完仍不成立就返回 false，调用方让行。
        /// </summary>
        public bool ServoReady(int axis)
        {
            if (!HasAxis(axis)) return false;
            var m = MotionManager.Instance;
            if (m.State(axis).ServoOn) return true;
            m.SetServo(axis, true);
            Engine.CountServoOn(axis);
            Info(CName(axis) + " 断使能，已自动补使能（流程下发）");
            return m.State(axis).ServoOn;
        }

        /// <summary>
        /// 动之前第二道：必须已回零。未回零的当场下发回零并等到回零位，
        /// 本次动作让行（返回 false），下一循环再来 —— 不绕过、不假装已回零。
        /// </summary>
        public bool HomedReady(int axis)
        {
            if (!HasAxis(axis)) return false;
            var m = MotionManager.Instance;
            if (m.State(axis).Homed) return true;
            var r = m.Home(axis);
            if (!r.Accepted)
            {
                Engine.CountMoveRejected(axis, Name, "回零被拒：" + r.Reason);
                return false;
            }
            Engine.CountHome(axis);
            Info(CName(axis) + " 未回零，已下发回零（本次动作让行）");
            WaitAxisAt(axis, m.Cfg(axis).HomeMm, 0.5, FlowPoints.PollMs, FlowPoints.MoveTimeoutMs);
            return m.State(axis).Homed;
        }

        /// <summary>轴名（日志用，解析失败给「轴#N」）。</summary>
        public string CName(int axis)
        {
            var m = MotionManager.Instance;
            return m.HasAxis(axis) ? m.Cfg(axis).Name : ("轴#" + axis);
        }

        /// <summary>
        /// 水平轴运动预检：同组垂直轴必须全部抬起（位置 ≥ ZUpMm）。
        /// 这就是 MotionManager.SafetyCheck 的同一条规则，提前判一次，
        /// 免得每条被拒的指令都往信息记录里灌一行。
        /// </summary>
        public bool VerticalUp(int axis)
        {
            var m = MotionManager.Instance;
            if (!m.HasAxis(axis)) return false;
            var g = m.Cfg(axis).Group;
            foreach (var z in m.Axes)
            {
                if (z.Group != g || !z.IsVertical) continue;
                if (m.Position(z.Index) < z.ZUpMm) return false;
            }
            return true;
        }

        /// <summary>垂直轴下行预检：同组水平轴必须都已停稳（避免边走边压）。</summary>
        public bool HorizontalIdle(int axis)
        {
            var m = MotionManager.Instance;
            if (!m.HasAxis(axis)) return false;
            var g = m.Cfg(axis).Group;
            foreach (var h in m.Axes)
                if (h.Group == g && !h.IsVertical && m.State(h.Index).Busy) return false;
            return true;
        }

        /// <summary>
        /// 运动前置检查：使能 → 回零 → 同组互锁预检（这几条与 SafetyCheck 同源，不是新加的规则）。
        /// 返回 false = 本次让行，一条指令都不下发。
        /// </summary>
        public bool PrecheckMove(int axis, double pos)
        {
            if (!Running) return false;
            if (!ServoReady(axis)) { Yield("伺服未使能：" + CName(axis)); return false; }
            if (!HomedReady(axis)) { Yield("未回零：" + CName(axis)); return false; }

            var m = MotionManager.Instance;
            bool vertical = m.HasAxis(axis) && m.Cfg(axis).IsVertical;
            if (!vertical && !VerticalUp(axis)) { Yield("同组垂直轴未抬起：" + CName(axis)); return false; }
            if (vertical && pos < m.Position(axis) - 0.01 && !HorizontalIdle(axis))
            {
                Yield("同组水平轴还在动，禁止下压：" + CName(axis));
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下发一条绝对定位 —— 只走 MotionManager.MoveAbs，它内部第一道就是 SafetyCheck。
        /// 被拒就记数让行，这里不存在任何能跳过互锁的后门。
        /// </summary>
        public bool IssueMove(int axis, double pos)
        {
            var r = MotionManager.Instance.MoveAbs(axis, pos);
            if (!r.Accepted)
            {
                Engine.CountMoveRejected(axis, Name, r.Reason);
                return false;
            }
            Engine.CountMoveIssued(axis);
            return true;
        }

        /// <summary>
        /// 单轴「前置 → 下发 → 等到位」。返回 true = 已到位（容差 0.5）。
        /// 每一根轴每一次运动都完整过这三步，不给自己开第二条通道。
        /// </summary>
        public bool MoveTo(int axis, double pos, string what, int timeoutMs = FlowPoints.MoveTimeoutMs)
        {
            if (!PrecheckMove(axis, pos)) return false;
            if (!IssueMove(axis, pos)) return false;
            if (!WaitAxisAt(axis, pos, 0.5, FlowPoints.PollMs, timeoutMs))
            {
                Engine.CountMoveTimeout(axis, Name, what);
                return false;
            }
            return Running && Math.Abs(MotionManager.Instance.Position(axis) - pos) <= 0.5;
        }

        /// <summary>按轴名定位（名字解析失败只报一次）。</summary>
        public bool MoveTo(string axisName, double pos, string what, int timeoutMs = FlowPoints.MoveTimeoutMs)
            => MoveTo(FlowEngine.AxisOf(axisName), pos, what, timeoutMs);

        /// <summary>
        /// 一组轴「全部前置 → 全部下发 → 统一等到位」：8 根吸嘴 Z 就是这样同升同降的；
        /// 逐根走完再下一根的话，一批要白白多花 7 个到位周期。
        /// 任何一根没到位都返回 false（本动作让行，下一循环从头重试，账不会重复记）。
        /// </summary>
        public bool MoveAll(int[] axes, double pos, string what, int timeoutMs = FlowPoints.MoveTimeoutMs)
        {
            if (!Running || axes == null || axes.Length == 0) return false;
            if (axes.Length == 1) return MoveTo(axes[0], pos, what, timeoutMs);

            for (int i = 0; i < axes.Length; i++)
                if (!PrecheckMove(axes[i], pos)) return false;

            int issued = 0;
            for (int i = 0; i < axes.Length; i++)
            {
                if (!IssueMove(axes[i], pos)) break;         // 已下发的会各自走到自己的目标位，不构成风险
                issued++;
            }
            if (issued < axes.Length) return false;

            var m = MotionManager.Instance;
            for (int i = 0; i < axes.Length; i++)
                if (!WaitAxisAt(axes[i], pos, 0.5, FlowPoints.PollMs, timeoutMs))
                {
                    Engine.CountMoveTimeout(axes[i], Name, what + "（" + (i + 1) + "/" + axes.Length + "）");
                    return false;
                }
            for (int i = 0; i < axes.Length; i++)
                if (Math.Abs(m.Position(axes[i]) - pos) > 0.5) return false;
            return Running;
        }

        /// <summary>轴是否已在目标位（容差 0.5mm，不算运动，只当握手用）。</summary>
        public bool At(int axis, double pos, double tol = 0.5)
        {
            var m = MotionManager.Instance;
            return m.HasAxis(axis) && Math.Abs(m.Position(axis) - pos) <= tol;
        }

        /// <summary>只在状态真的要变时写输出（MotionManager.WriteOut 每写一次记一行日志）。</summary>
        public void Out(string pointName, bool on)
        {
            var m = MotionManager.Instance;
            int bit = m.OutBitOf(pointName);
            if (bit < 0) { Engine.CountMissingPoint(pointName, Name); return; }
            if (m.ReadOut(bit) == on) return;
            m.WriteOut(bit, on);
            Engine.CountIoIssued();
        }

        /// <summary>按名读输入点（解析不到返回 false 并记一次）。</summary>
        public bool In(string pointName)
        {
            var m = MotionManager.Instance;
            int bit = m.InBitOf(pointName);
            if (bit < 0) { Engine.CountMissingPoint(pointName, Name); return false; }
            return m.ReadIn(bit);
        }

        /// <summary>等单个输入点（带超时，超时走报警）。</summary>
        public bool WaitIn(string pointName, bool want, int timeoutMs = FlowPoints.VacInTimeoutMs)
        {
            var m = MotionManager.Instance;
            int bit = m.InBitOf(pointName);
            if (bit < 0) { Engine.CountMissingPoint(pointName, Name); return false; }
            int waited = 0;
            while (Running && m.ReadIn(bit) != want)
            {
                if (waited >= timeoutMs)
                {
                    Engine.CountWaitTimeout(Name, "等待输入「" + pointName + "」=" + (want ? "ON" : "OFF")
                                                    + " 超时 " + timeoutMs + "ms");
                    return false;
                }
                if (!Sleep(FlowPoints.PollMs)) return false;
                waited += FlowPoints.PollMs;
            }
            return Running && m.ReadIn(bit) == want;
        }

        /// <summary>等一组输入点全部到位（8 路真空反馈就是这样判「整排都吸起来了」）。</summary>
        public bool WaitIns(string[] names, bool want, int timeoutMs = FlowPoints.VacInTimeoutMs)
        {
            if (names == null || names.Length == 0) return true;
            var m = MotionManager.Instance;

            // 名字解析不到的先记一笔（仿真里 8 路都在真表上，正常不会走到）
            for (int i = 0; i < names.Length; i++)
                if (m.InBitOf(names[i]) < 0) Engine.CountMissingPoint(names[i], Name);

            int waited = 0;
            while (Running)
            {
                bool all = true;
                for (int i = 0; i < names.Length; i++)
                {
                    int bit = m.InBitOf(names[i]);
                    if (bit >= 0 && m.ReadIn(bit) != want) { all = false; break; }
                }
                if (all) return true;
                if (waited >= timeoutMs)
                {
                    Engine.CountWaitTimeout(Name, "等待成组输入（首路「" + names[0] + "」等 "
                                                    + names.Length + " 路）=" + (want ? "ON" : "OFF")
                                                    + " 超时 " + timeoutMs + "ms");
                    return false;
                }
                if (!Sleep(FlowPoints.PollMs)) return false;
                waited += FlowPoints.PollMs;
            }
            return false;
        }

        /// <summary>
        /// 多键原子增减（守恒等式靠它：一颗芯片从 A 键转到 B 键时，
        /// 任何一瞬间读到的总和都不会多一颗、也不会少一颗）。
        /// </summary>
        public void Adds(string[] keys, double[] deltas) => VarBox.Adds(keys, deltas);

        /// <summary>把 n 颗料从一个键原子转到另一个键。</summary>
        public void Hand(string from, string to, double n) => VarBox.Transfer(from, n, to);

        /// <summary>同一件事只报警一次的入口（转调引擎的 WarnOnce，避免 20ms 一条刷屏）。</summary>
        public void WarnOnceFlow(string key, string msg) => Engine.WarnOnce(key, () => Alarm(msg));

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

            InitFlowKeys();
            lock (_lk)
            {
                _moveIssued = _moveRejected = _moveTimeout = _guardYield = 0;
                _softLimit = _servoOn = _homeIssued = _ioIssued = _waitTimeout = 0;
                Array.Clear(_axIssued, 0, _axIssued.Length);
                Array.Clear(_axRejected, 0, _axRejected.Length);
                _yieldBy.Clear();
                _warnOnce.Clear();
            }

            foreach (var t in arr) t.Start();
            StepText = "已启动 " + arr.Count + " 条流程线程（运动已接，全部经 SafetyCheck 裁决）";
            PostLog("信息", "流程线程已全部启动：" + arr.Count
                            + " 条（周期 5~24000ms；点位表 Motion/FlowPoints.cs；本版已下发真实运动指令）");
            PostLog("信息", "流程点位推导项（资料里没有、现场必须校核）：" + FlowPoints.DerivedSummary());
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
            PostLog("信息", "流程线程已停止（" + arr.Count + " 条，令牌强制清零 " + freed
                            + " 个，残余令牌 " + LeakedTokens() + " 个）");
            PostLog("信息", "流程统计（本轮）：" + StatText());
            PostLog("信息", ConservationText());
        }

        // ==================================================================
        //  运行计数（自检 / 界面 / 无头探针都读这一组数）
        // ==================================================================

        private long _moveIssued, _moveRejected, _moveTimeout, _guardYield;
        private long _softLimit, _servoOn, _homeIssued, _ioIssued, _waitTimeout;
        private readonly long[] _axIssued = new long[64];
        private readonly long[] _axRejected = new long[64];
        private readonly Dictionary<string, long> _yieldBy = new Dictionary<string, long>();
        private readonly HashSet<string> _warnOnce = new HashSet<string>();

        /// <summary>流程线程下发并被 SafetyCheck 放行、真正进卡的运动指令条数。</summary>
        public long MoveIssued { get { lock (_lk) return _moveIssued; } }

        /// <summary>流程线程下发但被安全层拒绝的次数（口径：只算流程自己发的，不含手动 / 演示节拍）。</summary>
        public long MoveRejectedByFlow { get { lock (_lk) return _moveRejected; } }

        /// <summary>下发后等到位超时的次数。</summary>
        public long MoveTimeouts { get { lock (_lk) return _moveTimeout; } }

        /// <summary>握手 / 预检让行次数（根本没下发指令，避免反复撞互锁刷日志）。</summary>
        public long GuardYields { get { lock (_lk) return _guardYield; } }

        /// <summary>越软限位次数（轴镜像线程每 10ms 扫一遍 45 根轴，超出 [Min,Max] 0.5mm 以上才记）。</summary>
        public long SoftLimitBreaches { get { lock (_lk) return _softLimit; } }

        /// <summary>输入点等待超时报警次数。</summary>
        public long WaitTimeouts { get { lock (_lk) return _waitTimeout; } }

        /// <summary>流程写输出次数（只在状态真要变时写）。</summary>
        public long IoIssued { get { lock (_lk) return _ioIssued; } }

        /// <summary>流程补发的回零指令次数。</summary>
        public long HomeIssued { get { lock (_lk) return _homeIssued; } }

        /// <summary>流程补发的伺服使能次数。</summary>
        public long ServoOnIssued { get { lock (_lk) return _servoOn; } }

        /// <summary>全部线程累计循环数。</summary>
        public long TotalLoops { get { lock (_lk) return _threads.Sum(t => t.Loops); } }

        /// <summary>全部线程累计异常数。</summary>
        public long TotalErrors { get { lock (_lk) return _threads.Sum(t => t.Errors); } }

        /// <summary>某根轴被流程下发过多少次运动（探针按轴核对）。</summary>
        public long AxisMoves(int axis) { lock (_lk) return axis >= 0 && axis < _axIssued.Length ? _axIssued[axis] : 0; }

        /// <summary>某根轴被安全层拦下过多少次。</summary>
        public long AxisRejects(int axis) { lock (_lk) return axis >= 0 && axis < _axRejected.Length ? _axRejected[axis] : 0; }

        /// <summary>
        /// 某根轴是否「被安全层拦过或放过」——自检要的判据：
        /// 只要这根轴进过流程，就必须有一条被放行或被拒绝的记录，二者都没有就是没管过它。
        /// </summary>
        public bool AxisGuarded(int axis) => AxisMoves(axis) + AxisRejects(axis) > 0;

        /// <summary>当前还被占着的令牌数（Stop 之后必须为 0）。</summary>
        public int LeakedTokens()
        {
            int n = 0;
            foreach (var k in VarBox.TokenKeys())
                if (VarBox.OwnerOf(k) != 0) n++;
            return n;
        }

        /// <summary>让行原因排行（前 N 条，界面 / 探针读）。</summary>
        public string YieldTop(int n = 5)
        {
            lock (_lk)
            {
                var q = _yieldBy.OrderByDescending(kv => kv.Value).Take(n)
                                 .Select(kv => kv.Key + " ×" + kv.Value);
                return string.Join("；", q);
            }
        }

        /// <summary>一行统计文本（口径全写在里面）。</summary>
        public string StatText()
        {
            return "线程 " + Threads.Count + " 条　循环 " + TotalLoops + " 次　运动指令 " + MoveIssued
                 + " 条（被拒 " + MoveRejectedByFlow + "　超时 " + MoveTimeouts + "）　预检让行 " + GuardYields
                 + " 次　写输出 " + IoIssued + " 次　补回零 " + HomeIssued + " 次　补使能 " + ServoOnIssued
                 + " 次　异常 " + TotalErrors + " 次　越软限位 " + SoftLimitBreaches
                 + " 次　占用令牌 " + LeakedTokens() + " 个";
        }

        // ---------------- 计数入口（FlowCtx 调） ----------------

        internal void CountMoveIssued(int axis)
        {
            lock (_lk)
            {
                _moveIssued++;
                if (axis >= 0 && axis < _axIssued.Length) _axIssued[axis]++;
            }
        }

        internal void CountMoveRejected(int axis, string by, string reason)
        {
            lock (_lk)
            {
                _moveRejected++;
                if (axis >= 0 && axis < _axRejected.Length) _axRejected[axis]++;
            }
            // MotionManager.NoteReject 已经写过一行信息记录，这里只在同一根轴第一次被拒时补一行线程名
            WarnOnce("rej#" + axis, () => PostLog("警告", by + "：下发被安全层拒绝：" + reason));
        }

        internal void CountMoveTimeout(int axis, string by, string what)
        {
            lock (_lk) _moveTimeout++;
            PostLog("警告", by + "：" + what + " 到位超时（轴 " + axis + "）");
        }

        internal void CountGuardYield(string reason)
        {
            lock (_lk)
            {
                _guardYield++;
                long v; _yieldBy.TryGetValue(reason, out v);
                _yieldBy[reason] = v + 1;
            }
        }

        internal void CountWaitTimeout(string by, string msg)
        {
            lock (_lk) _waitTimeout++;
            PostLog("警告", by + "：" + msg);
        }

        internal void CountServoOn(int axis) { lock (_lk) _servoOn++; }
        internal void CountHome(int axis) { lock (_lk) _homeIssued++; }
        internal void CountIoIssued() { lock (_lk) _ioIssued++; }

        /// <summary>轴名 / 点名字典（一次性解析，之后走缓存）。</summary>
        private static readonly Dictionary<string, int> _axCache = new Dictionary<string, int>();

        /// <summary>按真名解析轴号（解析不到返回 -1，调用方会记一次日志）。</summary>
        public static int AxisOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            lock (_axCache)
            {
                int v;
                if (_axCache.TryGetValue(name, out v)) return v;
                v = MotionManager.Instance.AxisIndex(name);
                _axCache[name] = v;
                return v;
            }
        }

        /// <summary>上料吸嘴 1~8Z 轴号缓存。</summary>
        private static int[] _ldrZ;

        /// <summary>下料吸嘴 1~8Z 轴号缓存。</summary>
        private static int[] _uldZ;

        internal void CountMissingAxis(int axis, string by)
            => WarnOnce("ax#" + axis, () => PostLog("警告", by + "：轴号 " + axis + " 不在轴表内，本动作跳过（检查轴表 / 别名表）"));

        internal void CountMissingPoint(string name, string by)
            => WarnOnce("pt#" + name, () => PostLog("警告", by + "：IO 点「" + name + "」在表里找不到，本步跳过"));

        /// <summary>同一件事只报一次（20ms 一条会把信息记录冲爆）。</summary>
        internal void WarnOnce(string key, Action say)
        {
            bool first;
            lock (_lk) first = _warnOnce.Add(key);
            if (first) try { say(); } catch { /* 日志不能带走流程线程 */ }
        }

        /// <summary>越软限位计数（轴镜像线程每 10ms 扫一遍，自检项要求恒为 0）。</summary>
        internal void CountSoftLimit(int n, string by)
        {
            if (n <= 0) return;
            lock (_lk) _softLimit += n;
            WarnOnce("softlimit", () => PostLog("警告", by + "：检测到 " + n
                        + " 根轴位置越过软限位 0.5mm 以上（累计 " + SoftLimitBreaches.ToString("0") + " 次）"));
        }


        /// <summary>
        /// 黑板键初始化：让「变量黑板」页开机就能看到全套键（0 = 本轮还没投料）。
        /// 只在键没登记过时给初值，重启流程不会把已经在跑的计数抹掉。
        /// </summary>
        private void InitFlowKeys()
        {
            var d = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var k in new[]
            {
                "LotIn", "PickOk", "TestOk", "前测完成", "报废", "守恒平衡", "守恒自检次数",
                "在制_上料吸嘴", "在制_前梭车", "在制_缓存台", "在制_后梭车", "在制_下料吸嘴",
                "待测盘已备好", "上料位有盘", "上料位剩余颗数", "出盘仓待接盘",
                "前上料在检测区", "前下料在检测区", "飞拍允许",
            }) d[k] = 0;
            d["进料仓盘数"] = FlowPoints.FeedTrayStack;
            d["空盘仓盘数"] = FlowPoints.EmptyTrayStack;
            for (int n = 1; n <= 3; n++)
            {
                d["空盘数_" + n] = FlowPoints.AutoEmptyBuffer;
                d["盘内颗数_" + n] = 0;
                d["BinFull_" + n] = 0;
                d["isAskForNewTray_" + n] = 0;
                d["满盘待取_" + n] = 0;
                d["Auto" + n + "仓循环"] = 0;
            }
            for (int i = FlowPoints.GoodBinFirst; i <= FlowPoints.BadBinLast; i++) d["BinCount_" + i] = 0;
            d["盘内颗数_NG"] = 0;
            d["盘内颗数_OK"] = 0;

            lock (_lk)
            {
                foreach (var kv in d) if (!VarBox.Has(kv.Key)) VarBox.SetD(kv.Key, kv.Value);
                VarBox.SetC("BinCode", "未分选");
                VarBox.SetC("CarryCarStatus_前", CarryCarStatus.None.ToString());
                VarBox.SetC("CarryCarStatus_后", CarryCarStatus.None.ToString());
            }
            foreach (var t in new[] { "仓-Auto互斥", "X走廊", "搬运在上料区", "搬运在下料区",
                                      "前压头在干涉区", "后压头在干涉区",
                                      "前上料在检测区", "前下料在检测区" })
                VarBox.Declare(t);
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

            // 越软限位扫描（自检项：这个数必须恒为 0）
            int breach = 0;
            for (int i = 0; i < axes.Count; i++)
            {
                double lo = Math.Min(axes[i].MinMm, axes[i].MaxMm);
                double hi = Math.Max(axes[i].MinMm, axes[i].MaxMm);
                if (pos[i] < lo - 0.5 || pos[i] > hi + 0.5) breach++;
            }
            if (breach > 0) c.Alarm("轴镜像：" + breach + " 根轴位置越过软限位（详见统计行）");
            CountSoftLimit(breach, "轴镜像");

            c.Set("AxisSt", busy);
            c.Set("AxisAlarm", alm);
            c.Set("AxisHomed", homed);
            c.Set("AxisCount", axes.Count);

            // 只在「有报警」或「全部停稳」这类有意义的跳变上打一行，避免 10ms 一条
            if (c.Changed("AxisAlarm") && alm > 0) c.Info("轴镜像：伺服报警 " + alm + " 根");
            else if (busy == 0 && c.Changed("AxisSt")) c.Info("轴镜像：45 轴全部静止（已回零 " + homed + "）");
        }

        // ==================================================================
        //  线程体
        // ==================================================================

        // ==================================================================
        //  工艺线程体
        //  · 运动一律走 MotionManager 公开入口（MoveAbs / Home / SetServo / WriteOut / ReadIn），
        //    每一根轴每一次下发都先过 SafetyCheck，被拒 / 超时只让行不硬闯；
        //  · 点位一律取自 FlowPoints，不许在 if 里散落数字；
        //  · 握手一律「先查变量、取不到令牌就让行」，所有等待都是 轮询 + Sleep + 超时报警；
        //  · 计数走 VarBox.Adds（多键原子），守恒等式在任何瞬间都不会看到半个状态。
        // ==================================================================

        /// <summary>吸嘴 Z 轴组（上料 / 下料各 8 根）。</summary>
        private static int[] LdrZs()
        {
            if (_ldrZ == null)
            {
                var a = new int[FlowPoints.NozzleCount];
                for (int i = 0; i < a.Length; i++) a[i] = AxisOf(FlowPoints.LdrNozzleZ(i + 1));
                _ldrZ = a;
            }
            return _ldrZ;
        }

        /// <summary>下料吸嘴 Z 轴组。</summary>
        private static int[] UldZs()
        {
            if (_uldZ == null)
            {
                var a = new int[FlowPoints.NozzleCount];
                for (int i = 0; i < a.Length; i++) a[i] = AxisOf(FlowPoints.UldrNozzleZ(i + 1));
                _uldZ = a;
            }
            return _uldZ;
        }

        private static string[] VacOkNames(Func<int, string> f)
        {
            var a = new string[FlowPoints.NozzleCount];
            for (int i = 0; i < a.Length; i++) a[i] = f(i + 1);
            return a;
        }

        /// <summary>整排吸真空（吸嘴 Z 已在下位）。</summary>
        private static void Suck(FlowCtx c, Func<int, string> on, Func<int, string> off)
        {
            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
            {
                c.Out(on(i), true);
                c.Out(off(i), false);
            }
        }

        /// <summary>整排破真空放料。</summary>
        private static void Blow(FlowCtx c, Func<int, string> on, Func<int, string> off)
        {
            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
            {
                c.Out(on(i), false);
                c.Out(off(i), true);
            }
        }

        /// <summary>放完把破真空也关掉（省电省气，真机就是这么收的）。</summary>
        private static void ValveIdle(FlowCtx c, Func<int, string> on, Func<int, string> off)
        {
            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
            {
                c.Out(on(i), false);
                c.Out(off(i), false);
            }
        }

        /// <summary>
        /// 工位「残留回收」：上一批在半路让行（超时 / 被拒 / 停机）时，可能把轴留在
        /// X 走廊里或把压头留在下压位上 —— 这些残留会卡死分时互锁（其余两条臂从此全被拒，
        /// 整机静默死掉）。所以每条手臂 / 压头线程在循环开头先判一次，不合法就先收干净。
        /// 收回只走同一套 MoveTo：垂直上升与「回停放位」都是 SafetyCheck 明确放行的方向。
        /// </summary>
        private bool Dirty(FlowCtx c, int axis, double safe)
        {
            var m = MotionManager.Instance;
            return m.HasAxis(axis) && Math.Abs(m.Position(axis) - safe) > 0.5;
        }

        /// <summary>一组轴里是否有任何一根不在安全位。</summary>
        private bool DirtyAny(FlowCtx c, int[] axes, double safe)
        {
            for (int i = 0; i < axes.Length; i++) if (Dirty(c, axes[i], safe)) return true;
            return false;
        }

        /// <summary>本批没走完时把手臂收回停放位（X 走廊规则：回停放位永远放行）。</summary>
        private void Park(FlowCtx c, int axis, double pos, string what)
        {
            if (!c.MoveTo(axis, pos, what, 3000))
                c.Alarm("回停放位超时：" + what + "（下一循环继续重试）");
        }

        /// <summary>
        /// 一座仓做一次完整的「走位 + 分盘 + 送 / 接盘 + 回位」：
        ///   Z 先抬起 → Y 走到交接位 → Z 下降一个盘距（松开下一片）→ Z 抬起（顶住 / 敲击）→ Y 回位。
        /// 顺序不能换：同组垂直轴没抬到 ZUp 以上时水平轴会被 SR 直接拒（FlowCtx.VerticalUp 预检）。
        /// </summary>
        private bool StoreIndex(FlowCtx c, int y, int z, string what)
        {
            if (!c.MoveTo(z, FlowPoints.StoreZUp, what + " Z 抬起")) return false;
            if (!c.MoveTo(y, FlowPoints.StoreYSend, what + " Y 走到交接位")) return false;
            if (!c.MoveTo(z, FlowPoints.StoreZDivide, what + " Z 下降到分盘位（-盘距 "
                        + FlowPoints.TrayPitchMm.ToString("0.###") + "）")) return false;
            if (!c.MoveTo(z, FlowPoints.StoreZUp, what + " Z 抬起（敲击 / 顶下一片盘）")) return false;
            if (!c.MoveTo(y, FlowPoints.StoreYHome, what + " Y 回位")) return false;
            return true;
        }

        // ==================================================================
        //  六个料盘仓
        // ==================================================================

        /// <summary>
        /// 进料仓：把一片待测满盘送到搬运位（进料仓Y 走位 + 进料仓Z 分盘）。
        /// 与 Auto 仓 / 空盘仓 / 出盘仓共用「仓-Auto互斥」一把令牌（这几座仓本来就是串行的）。
        /// </summary>
        private void T_FeedTrayStore(FlowCtx c)
        {
            c.Declare("仓-Auto互斥");
            c.Init("进料仓盘数", FlowPoints.FeedTrayStack);
            c.Register("待测盘已备好");

            c.Inc("进料仓循环");

            if (c.Get("待测盘已备好") >= 1)
            {
                if (c.Heartbeat()) c.Info("心跳：进料仓一片待测盘在交接位，等搬运臂取走；循环 "
                                          + c.Get("进料仓循环").ToString("0"));
                return;
            }
            if (c.Get("进料仓盘数") <= 0)
            {
                c.Set("TrayReady", 0);
                if (c.Changed("TrayReady")) c.Alarm("TrayReady=0：进料仓盘塔已空，投料停止（需人工补盘）");
                return;
            }
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) { c.Yield("仓-Auto互斥 被其他仓占用"); return; }
            try
            {
                if (!StoreIndex(c, AxisOf("进料仓Y轴"), AxisOf("进料仓Z轴"), "进料仓")) return;
                c.Inc("进料仓盘数", -1);
                c.Inc("进料仓送出盘数");
                c.Set("待测盘已备好", 1);
                c.Set("TrayReady", 1);
                if (c.Changed("TrayReady")) c.Info("TrayReady=1（进料仓已把待测盘送到搬运位）");
                if (c.Heartbeat()) c.Info("心跳：进料仓循环 " + c.Get("进料仓循环").ToString("0")
                                          + "，盘塔剩 " + c.Get("进料仓盘数").ToString("0")
                                          + " 片，满料感应=" + (c.In("进料仓满料感应") ? "有" : "无"));
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>进空盘仓：哪座 Auto 仓的空盘用完了就补一片（空盘仓Y 走位 + 空盘仓Z 分盘）。</summary>
        private void T_EmptyTrayStore(FlowCtx c)
        {
            c.Declare("仓-Auto互斥");
            c.Init("空盘仓盘数", FlowPoints.EmptyTrayStack);
            for (int n = 1; n <= 3; n++) c.Init("空盘数_" + n, FlowPoints.AutoEmptyBuffer);

            c.Inc("进空盘仓循环");

            int need = 0;
            for (int n = 1; n <= 3; n++) if (c.Get("空盘数_" + n) <= 0) { need = n; break; }
            if (need == 0)
            {
                c.Set("EmptyTrayReady", 1);
                if (c.Changed("EmptyTrayReady")) c.Info("EmptyTrayReady=1（三座 Auto 仓都还自备空盘）");
                if (c.Heartbeat()) c.Info("心跳：进空盘仓循环 " + c.Get("进空盘仓循环").ToString("0")
                                          + "，空盘塔 " + c.Get("空盘仓盘数").ToString("0") + " 片");
                return;
            }
            if (c.Get("空盘仓盘数") <= 0)
            {
                c.Set("EmptyTrayReady", 0);
                if (c.Changed("EmptyTrayReady")) c.Alarm("EmptyTrayReady=0：空盘仓已空，Auto" + need + " 仓等盘");
                return;
            }
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) { c.Yield("仓-Auto互斥 被其他仓占用"); return; }
            try
            {
                if (!StoreIndex(c, AxisOf("空盘仓Y轴"), AxisOf("空盘仓Z轴"), "进空盘仓")) return;
                c.Inc("空盘仓盘数", -1);
                c.Inc("空盘数_" + need, 1);
                c.Set("isAskForNewTray_" + need, 0);
                c.Set("EmptyTrayReady", 1);
                c.Inc("进空盘仓送出盘数");
                if (c.Changed("EmptyTrayReady")) c.Info("EmptyTrayReady=1（空盘已送入 Auto" + need + " 仓）");
                if (c.Heartbeat()) c.Info("心跳：进空盘仓循环 " + c.Get("进空盘仓循环").ToString("0")
                                          + "，本轮给 Auto" + need + "，空盘塔剩 "
                                          + c.Get("空盘仓盘数").ToString("0") + " 片");
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>出空盘仓：把搬运臂送回来的满盘接进盘塔堆好（出盘仓Y 走位 + 出盘仓Z 堆盘）。</summary>
        private void T_OutTrayStore(FlowCtx c)
        {
            c.Declare("仓-Auto互斥");
            c.Init("出盘仓已堆", 0);
            c.Register("出盘仓待接盘");

            c.Inc("出空盘仓循环");

            if (c.Get("出盘仓待接盘") < 1)
            {
                c.Set("OutTrayReady", c.Get("出盘仓已堆") < FlowPoints.OutTrayCapacity ? 1 : 0);
                if (c.Changed("OutTrayReady")) c.Info("OutTrayReady=" + c.Get("OutTrayReady").ToString("0")
                                                      + "（出盘仓已堆 " + c.Get("出盘仓已堆").ToString("0")
                                                      + " / " + FlowPoints.OutTrayCapacity.ToString("0") + " 盘）");
                if (c.Heartbeat()) c.Info("心跳：出空盘仓循环 " + c.Get("出空盘仓循环").ToString("0")
                                          + "，已堆 " + c.Get("出盘仓已堆").ToString("0")
                                          + " 盘，满料感应=" + (c.In("出空盘仓满料感应") ? "有" : "无"));
                return;
            }
            if (c.Get("出盘仓已堆") >= FlowPoints.OutTrayCapacity)
            {
                c.Set("OutTrayReady", 0);
                c.Alarm("出盘仓盘塔已满（" + FlowPoints.OutTrayCapacity.ToString("0")
                        + " 盘），满盘取不走、收料暂停 —— 需人工取盘");
                return;
            }
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) { c.Yield("仓-Auto互斥 被其他仓占用"); return; }
            try
            {
                if (!StoreIndex(c, AxisOf("出盘仓Y轴"), AxisOf("出盘仓Z轴"), "出空盘仓")) return;
                c.Inc("出盘仓已堆", 1);
                c.Inc("出空盘仓收盘数");
                c.Set("出盘仓待接盘", 0);
                c.Set("OutTrayReady", 1);
                if (c.Heartbeat()) c.Info("心跳：出空盘仓循环 " + c.Get("出空盘仓循环").ToString("0")
                                          + "，接盘后已堆 " + c.Get("出盘仓已堆").ToString("0") + " 盘");
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        // ==================================================================
        //  料盘搬运臂（搬运X + 夹爪气缸）
        // ==================================================================

        /// <summary>
        /// 一轮只做一件（优先级从上到下），三件事都只动 搬运X轴 一根轴：
        ///   1) 上料位待测盘取空 → 甩到空盘仓线（回收的空盘就是这么来的）；
        ///   2) 进料仓备好的待测满盘 → 放到上料工作位；
        ///   3) Auto 仓顶出来的满盘 → 送到出盘仓线。
        /// X 走廊（上料X / 下料X / 搬运X）三臂分时互斥是 SafetyCheck 里的硬规则，
        /// 所以动手前必须先拿到「X走廊」令牌，拿不到就让行 —— 不硬试、不死等。
        /// </summary>
        private void T_TrayTransfer(FlowCtx c)
        {
            c.Declare("搬运在上料区", "搬运在下料区", "X走廊");
            c.Init("上料位有盘", 0);
            c.Init("上料位剩余颗数", 0);
            for (int n = 1; n <= 3; n++) c.Register("满盘待取_" + n);

            int x = AxisOf("搬运X轴");

            // 残留回收：搬运X 停在走廊半途会让上料 / 下料两臂全被分时互锁拒绝
            if (Dirty(c, x, FlowPoints.TfSafe))
            {
                c.Inc("搬运残留回收次数");
                c.Out(FlowPoints.GripperOpen, true);
                c.MoveTo(x, FlowPoints.TfSafe, "搬运X 收回安全位（残留）", 3000);
                return;
            }

            int job = 0;                                        // 0 = 没活
            int autoNo = 0;
            if (c.Get("待测盘已备好") >= 1 && c.Get("上料位有盘") < 1
                && c.Get("上料位剩余颗数") < FlowPoints.BatchSize) job = 2;
            if (job == 0 && c.Get("上料位有盘") >= 1 && c.Get("上料位剩余颗数") < FlowPoints.BatchSize) job = 1;
            if (job == 0)
                for (int n = 1; n <= 3; n++)
                    if (c.Get("满盘待取_" + n) >= 1) { job = 3; autoNo = n; break; }

            if (job == 0)
            {
                c.Set("搬运位置", 0);
                if (c.Changed("搬运位置")) c.Info("搬运位置=0（0 等待 1 上料区 2 下料区）");
                c.Inc("搬运等待");
                if (c.Heartbeat()) c.Info("心跳：上料区 " + c.Get("搬运上料循环").ToString("0")
                                          + " 下料区 " + c.Get("搬运下料循环").ToString("0")
                                          + " 等待 " + c.Get("搬运等待").ToString("0")
                                          + "，夹爪有无感应=" + (c.In(FlowPoints.GripperSensor) ? "有盘" : "空"));
                return;
            }

            string zone = job == 3 ? "搬运在下料区" : "搬运在上料区";
            if (!c.WaitTake("X走廊", FlowPoints.PollMs, FlowPoints.TokenTimeoutMs))
            {
                c.Yield("X 走廊被上料/下料/搬运三臂分时占用（搬运）");
                return;
            }
            if (!c.WaitTake(zone, 50, 1000)) { c.Give("X走廊"); c.Yield(zone + " 被占"); return; }

            bool ok = false;
            try
            {
                c.Set("搬运位置", job == 3 ? 2 : 1);
                if (c.Changed("搬运位置")) c.Info("搬运位置=" + c.Get("搬运位置").ToString("0")
                                                  + "（0 等待 1 上料区 2 下料区）");

                if (job == 2) ok = TransferBringFeedTray(c, x);
                else if (job == 1) ok = TransferTakeEmptyTray(c, x);
                else ok = TransferTakeFullTray(c, x, autoNo);

                if (ok) c.Inc(job == 3 ? "搬运下料循环" : "搬运上料循环");
                else c.Inc("搬运让行次数");
            }
            finally
            {
                Park(c, x, FlowPoints.TfSafe, "搬运X 退回安全位");
                c.Give(zone);
                c.Give("X走廊");
            }
            if (c.Heartbeat()) c.Info("心跳：搬运臂 上料区 " + c.Get("搬运上料循环").ToString("0")
                                      + " 下料区 " + c.Get("搬运下料循环").ToString("0"));
        }

        /// <summary>搬运：从进料仓线取一片待测满盘 → 放到上料工作位。</summary>
        private bool TransferBringFeedTray(FlowCtx c, int x)
        {
            if (!c.MoveTo(x, FlowPoints.TfLoader, "搬运X → 进料仓取盘位")) return false;
            c.Out(FlowPoints.GripperClose, true);
            c.Out(FlowPoints.GripperOpen, false);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Set("待测盘已备好", 0);
            c.Inc("搬运夹盘次数");

            if (!c.MoveTo(x, FlowPoints.TfBuffer, "搬运X → 上料工作位（缓存台）")) return false;
            c.Out(FlowPoints.GripperClose, false);
            c.Out(FlowPoints.GripperOpen, true);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Inc("搬运放盘次数");

            // 先给颗数再宣布「有盘」，上料臂就不会看到「有盘 1 / 剩余 0」的半个状态
            c.Set("上料位剩余颗数", FlowPoints.TrayChips);
            c.Set("上料位有盘", 1);
            c.Inc("待测盘上料次数");
            c.Info("搬运臂已把一片待测盘放到上料位（" + FlowPoints.TrayChips.ToString("0") + " 颗 / 盘）");
            return true;
        }

        /// <summary>搬运：把上料位取空的盘拿下来甩到空盘仓线（回收空盘 → 进空盘仓的盘塔）。</summary>
        private bool TransferTakeEmptyTray(FlowCtx c, int x)
        {
            if (!c.MoveTo(x, FlowPoints.TfBuffer, "搬运X → 上料工作位取空盘")) return false;
            c.Out(FlowPoints.GripperClose, true);
            c.Out(FlowPoints.GripperOpen, false);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Set("上料位有盘", 0);
            c.Inc("空盘回收次数");

            if (!c.MoveTo(x, FlowPoints.TfEmpty, "搬运X → 空盘仓线（资料里出 / 空盘共用这一个工位名）")) return false;
            c.Out(FlowPoints.GripperClose, false);
            c.Out(FlowPoints.GripperOpen, true);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Inc("空盘仓盘数", 1);
            c.Info("上料位空盘已甩到空盘仓线（空盘仓盘数 +1，剩余 " + c.Get("上料位剩余颗数").ToString("0") + " 颗）");
            return true;
        }

        /// <summary>搬运：把 Auto{n} 顶出来的满盘送到出盘仓线，交给出空盘仓堆盘。</summary>
        private bool TransferTakeFullTray(FlowCtx c, int x, int no)
        {
            if (!c.MoveTo(x, FlowPoints.TfAuto(no), "搬运X → Auto" + no + " 仓取满盘")) return false;
            c.Out(FlowPoints.GripperClose, true);
            c.Out(FlowPoints.GripperOpen, false);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Set("满盘待取_" + no, 0);
            c.Inc("满盘取走次数");
            c.Inc("已回仓颗数", FlowPoints.TrayCapacity);       // 口径：整盘 32 颗记入「已回仓颗数」，不回退 BinCount

            if (!c.MoveTo(x, FlowPoints.TfEmpty, "搬运X → 出盘仓线")) return false;
            c.Out(FlowPoints.GripperClose, false);
            c.Out(FlowPoints.GripperOpen, true);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            c.Set("出盘仓待接盘", 1);
            c.Inc("出盘仓待接次数");
            c.Info("Auto" + no + " 满盘已送到出盘仓线（" + FlowPoints.TrayCapacity.ToString("0") + " 颗整盘回仓）");
            return true;
        }

        // ==================================================================
        //  上料臂
        // ==================================================================

        /// <summary>上料臂：从上料位待测盘吸 8 颗（一批）→ 放到前梭车接料位。</summary>
        private void T_LoaderArm(FlowCtx c)
        {
            c.Declare("X走廊");
            c.Init("上料位有盘", 0);
            c.Init("上料位剩余颗数", 0);
            int lx = AxisOf("上料X轴"), ly = AxisOf("上料Y轴"), lu = AxisOf("上料旋转U轴");
            int px = AxisOf("上料变距X"), py = AxisOf("上料变距Y");
            int[] zs = LdrZs();

            c.Set("HasChip", c.Get("在制_上料吸嘴") >= 1 ? 1 : 0);
            if (c.Changed("HasChip")) c.Info("HasChip=" + c.Get("HasChip").ToString("0")
                                             + "（吸嘴阵列挂料 " + c.Get("在制_上料吸嘴").ToString("0") + " 颗）");

            // 残留回收：吸嘴还压在下位 / 上料X 还停在走廊里 → 先抬起来收回停放位再谈下一批
            if (DirtyAny(c, zs, FlowPoints.LdrZUp) || Dirty(c, lx, FlowPoints.LdrXHome))
            {
                c.Inc("上料臂残留回收次数");
                c.MoveAll(zs, FlowPoints.LdrZUp, "上料吸嘴1~8Z 回收抬起");
                c.MoveTo(lx, FlowPoints.LdrXHome, "上料X 收回停放位（残留）", 3000);
                return;
            }

            bool holding = c.Get("在制_上料吸嘴") >= 1;          // 已吸上料：必须先放掉，不许重新去取
            if (!holding)
            {
                if (c.Get("上料位有盘") < 1 || c.Get("上料位剩余颗数") < FlowPoints.BatchSize)
                { c.Yield("上料位没盘 / 剩余不足一批"); return; }
                if (c.GetC("CarryCarStatus_前", "") != "ReadyLoad" || c.Get("在制_前梭车") >= 1)
                { c.Yield("前梭车不在接料位或上面还有料"); return; }
            }

            if (!c.WaitTake("X走廊", FlowPoints.PollMs, FlowPoints.TokenTimeoutMs))
            { c.Yield("X 走廊被上料/下料/搬运三臂分时占用（上料）"); return; }

            // 一次走廊时段里把「吸一批 + 放一批」做完（真机也是这样占位的），
            // 只有已经吸在手上、上一轮没放掉的，才直接进放料段。
            bool ok;
            try
            {
                ok = holding || LoaderPick(c, lx, ly, lu, px, py, zs);
                if (ok) ok = LoaderPlace(c, lx, ly, lu, px, py, zs);
            }
            finally
            {
                Park(c, lx, FlowPoints.LdrXHome, "上料X 退回停放位");
                c.Set("前上料在检测区", 0);                       // 兜底清（正常由 LoaderPlace 自己清）
                c.Give("X走廊");
            }

            c.Inc("上料臂循环");
            if (!ok) { c.Inc("上料臂让行批次"); return; }
            if (c.Heartbeat()) c.Info("心跳：上料臂循环 " + c.Get("上料臂循环").ToString("0")
                                      + "　投料 " + c.Get("LotIn").ToString("0") + " 颗"
                                      + "　吸取成功 " + c.Get("PickOk").ToString("0") + " 颗"
                                      + "　取料失败 " + c.Get("取料失败批次").ToString("0") + " 批");
        }

        /// <summary>上料：吸料段（走完 8 路真空反馈才记投料，没吸起来一颗都不计）。</summary>
        private bool LoaderPick(FlowCtx c, int lx, int ly, int lu, int px, int py, int[] zs)
        {
            if (!c.MoveAll(zs, FlowPoints.LdrZUp, "上料吸嘴1~8Z 抬起")) return false;
            if (!c.MoveTo(px, FlowPoints.LdrPitchPick, "上料变距X 收取料间距")) return false;
            if (!c.MoveTo(py, FlowPoints.LdrPitchPick, "上料变距Y 收取料间距")) return false;
            if (!c.MoveTo(lu, FlowPoints.LdrUPick, "上料旋转U 到取料朝向")) return false;
            if (!c.MoveTo(ly, FlowPoints.LdrYPick, "上料Y → 待测盘行")) return false;
            if (!c.MoveTo(lx, FlowPoints.LdrXPick, "上料X → 取料位")) return false;
            if (!c.MoveAll(zs, FlowPoints.LdrZDown, "上料吸嘴1~8Z 下降到取料高度")) return false;

            Suck(c, FlowPoints.LdrVacOn, FlowPoints.LdrVacOff);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            if (!c.WaitIns(VacOkNames(FlowPoints.LdrVacOk), true))
            {
                ValveIdle(c, FlowPoints.LdrVacOn, FlowPoints.LdrVacOff);
                c.MoveAll(zs, FlowPoints.LdrZUp, "上料吸嘴抬起（取料失败）");
                c.Inc("取料失败批次");
                c.Alarm("上料取料失败：8 路「左上料真空」反馈未在 " + FlowPoints.VacInTimeoutMs
                        + "ms 内全部到位，本批一颗都不记投料");
                return false;
            }

            // LotIn / PickOk / 在制_上料吸嘴 / 上料位剩余颗数 一次原子更新 —— 守恒等式看不到半个状态
            c.Adds(new[] { "LotIn", "PickOk", "在制_上料吸嘴", "上料位剩余颗数", "上料模组走料盘点位置" },
                   new double[] { FlowPoints.BatchSize, FlowPoints.BatchSize, FlowPoints.BatchSize,
                                  -FlowPoints.BatchSize, 1 });
            return true;
        }

        /// <summary>上料：放料段（放到前梭车上，交料瞬间把在制从吸嘴原子转到梭车）。</summary>
        private bool LoaderPlace(FlowCtx c, int lx, int ly, int lu, int px, int py, int[] zs)
        {
            if (!c.MoveAll(zs, FlowPoints.LdrZUp, "上料吸嘴1~8Z 抬起")) return false;
            if (!c.MoveTo(px, FlowPoints.LdrPitchPlace, "上料变距X 放到梭车间距")) return false;
            if (!c.MoveTo(py, FlowPoints.LdrPitchPlace, "上料变距Y 放到梭车间距")) return false;
            if (!c.MoveTo(lu, FlowPoints.LdrUPlace, "上料旋转U 换向到放料朝向")) return false;
            if (!c.MoveTo(ly, FlowPoints.LdrYPlace, "上料Y → 前梭车放料行")) return false;
            c.Set("前上料在检测区", 1);                           // 进梭车检测区（压头线程判这个位）
            if (!c.MoveTo(lx, FlowPoints.LdrXPlace, "上料X → 放料位")) return false;
            if (!c.MoveAll(zs, FlowPoints.LdrZDown, "上料吸嘴1~8Z 下降到放料高度")) return false;

            Blow(c, FlowPoints.LdrVacOn, FlowPoints.LdrVacOff);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            ValveIdle(c, FlowPoints.LdrVacOn, FlowPoints.LdrVacOff);

            c.Hand("在制_上料吸嘴", "在制_前梭车", FlowPoints.BatchSize);
            c.Inc("上料放料批次");
            c.Set("前上料在检测区", 0);                           // 料已交出去，退出检测区

            if (!c.MoveAll(zs, FlowPoints.LdrZUp, "上料吸嘴1~8Z 抬起")) return false;
            return true;
        }

        // ==================================================================
        //  前 / 后梭车
        // ==================================================================

        /// <summary>
        /// 梭车：交接位待料 → 进测试位 → 等压头测完 → 退回交接位 →
        ///         前梭车把这一批交给中转缓存台；后梭车挂 ReadyULoad 等下料臂取。
        /// 前后共一条 SHUTTLE 导轨、安全间隙 120，所以两台各自的带位不重叠
        /// （前 0~500、后 700~1200），带位数值见 FlowPoints 第 4 节。
        /// </summary>
        private void T_Shuttle(FlowCtx c, string side)
        {
            bool front = side == "前";
            string key = "CarryCarStatus_" + side;
            string held = "在制_" + side + "梭车";
            string inTest = "梭车在测试位_" + side;
            string done = "测试完毕_" + side;
            int x = AxisOf(side + "梭车X轴");
            double hand = front ? FlowPoints.ShFPick : FlowPoints.ShBHand;
            double test = front ? FlowPoints.ShFTest : FlowPoints.ShBTest;

            c.Register(done);
            c.Register(inTest);

            int ph = c.Phase();
            bool moved = false;

            switch (ph)
            {
                case 0:                                     // 空车 → 回到交接位待料
                    if (c.Get(held) >= 1) { c.SetPhase(5); return; }
                    if (!c.At(x, hand))
                    {
                        c.SetC(key, CarryCarStatus.CannotOperate);
                        if (!c.MoveTo(x, hand, side + "梭车 → " + (front ? "接料位" : "下料交接位"))) return;
                        moved = true;
                    }
                    c.SetC(key, CarryCarStatus.ReadyLoad);
                    if (!moved && !front && c.Get("在制_缓存台") >= FlowPoints.BatchSize)
                    {
                        // 缓存台 → 后梭车：资料里中转是气缸 + 皮带，没有伺服轴，按交接事件 + 保持时间演
                        if (!c.Sleep(FlowPoints.BufferDwellMs)) return;
                        c.Hand("在制_缓存台", held, FlowPoints.BatchSize);
                        c.Inc("缓存台交后梭车批次");
                        c.SetC(key, CarryCarStatus.CannotOperate);
                        c.SetPhase(2);
                        return;
                    }
                    c.SetPhase(moved ? 0 : 1);
                    break;

                case 1:                                     // 待料：前 = 等上料臂放 8 颗；后 = 从中转缓存台取 8 颗
                    if (c.Get(held) >= FlowPoints.BatchSize) { c.SetPhase(2); return; }
                    c.SetC(key, CarryCarStatus.ReadyLoad);
                    if (!front && c.Get("在制_缓存台") >= FlowPoints.BatchSize)
                    {
                        // 缓存台 → 后梭车：资料里中转是气缸 + 皮带，没有伺服轴，按交接事件 + 保持时间演
                        if (!c.Sleep(FlowPoints.BufferDwellMs)) return;
                        c.Hand("在制_缓存台", held, FlowPoints.BatchSize);
                        c.Inc("缓存台交后梭车批次");
                        c.SetC(key, CarryCarStatus.CannotOperate);
                        c.SetPhase(2);
                    }
                    break;

                case 2:                                     // 进测试位
                    if (c.Get(held) < FlowPoints.BatchSize) { c.SetPhase(0); return; }
                    if (!c.At(x, test))
                    {
                        c.SetC(key, CarryCarStatus.CannotOperate);
                        c.Set(inTest, 1);
                        if (!c.MoveTo(x, test, side + "梭车 → 测试位")) return;
                    }
                    c.SetPhase(3);
                    break;

                case 3:                                     // 等压头测完
                    if (c.Get(done) >= 1) c.SetPhase(4);
                    break;

                case 4:                                     // 退回交接位
                    if (!c.At(x, hand))
                    {
                        c.SetC(key, CarryCarStatus.CannotOperate);
                        if (!c.MoveTo(x, hand, side + "梭车 退回交接位")) return;
                    }
                    c.Set(inTest, 0);
                    c.Set(done, 0);
                    c.SetPhase(5);
                    break;

                case 5:                                     // 交料
                    if (front)
                    {
                        if (c.Get("在制_缓存台") >= FlowPoints.BatchSize)
                        { c.Yield("中转缓存台还没被后梭车取走，前梭车让行"); break; }
                        if (!c.Sleep(FlowPoints.BufferDwellMs)) return;
                        c.Hand(held, "在制_缓存台", FlowPoints.BatchSize);
                        c.Inc("缓存台收前梭车批次");
                        c.Inc("梭车交接批次");
                        c.SetC(key, CarryCarStatus.ReadyLoad);
                        c.SetPhase(0);
                    }
                    else
                    {
                        c.SetC(key, CarryCarStatus.ReadyULoad);   // 挂「可以下料」等下料臂取
                        c.SetPhase(6);
                    }
                    break;

                case 6:                                     // 后梭车：等下料臂把 8 颗取走
                    if (c.Get(held) < 1) { c.SetC(key, CarryCarStatus.ReadyLoad); c.SetPhase(0); }
                    break;
            }

            if (c.ChangedC(key))
                c.Info(key + "=" + c.GetC(key)
                       + "（" + (front ? "前梭车X轴 " + FlowPoints.ShFPick.ToString("0")
                                       + "/" + FlowPoints.ShFTest.ToString("0")
                                       : "后梭车X轴 " + FlowPoints.ShBHand.ToString("0")
                                       + "/" + FlowPoints.ShBTest.ToString("0")) + "）");
            c.Inc(side + "梭车循环");
            if (c.Heartbeat()) c.Info("心跳：" + side + "梭车循环 " + c.Get(side + "梭车循环").ToString("0")
                                      + " 颗数 " + c.Get(held).ToString("0") + " 相位 " + c.Phase());
        }

        // ==================================================================
        //  前 / 后压头
        // ==================================================================

        /// <summary>
        /// 压头：干涉区令牌 + 与上下料臂的检测区握手 + Y 走位两站 + Z 两次下压保压。
        /// 前后压头 Z 与前后压头 Y 同组（工站「压头测试工位」），所以任何一方走 Y 之前
        /// 两根 Z 都得在抬起位 —— 这就是 FlowCtx.VerticalUp 预检要过的同一条规则。
        /// 前压头 = 前段温度测试（记「前测完成」），后压头 = 后段（末段）测试，测完才记 TestOk。
        /// </summary>
        private void T_PressHead(FlowCtx c, string side)
        {
            bool front = side == "前";
            string interlock = side + "压头在干涉区";
            string inTest = "梭车在测试位_" + side;
            string done = "测试完毕_" + side;
            string held = "在制_" + side + "梭车";
            string canPress = side + "压头可下压";
            int y = AxisOf(side + "压头Y轴"), z = AxisOf(side + "压头Z轴");

            if (front) c.Declare(interlock, "前上料在检测区", "前下料在检测区");
            else c.Declare(interlock);
            c.Register(done);

            // 残留回收：压头 Z 没抬起来时，同组的另一台压头 Y 全被 SR 拒掉（同组垂直轴规则），
            // 所以不管握手成不成立，先把自己的 Z 抬起、Y 收回停放位。
            if (Dirty(c, z, FlowPoints.PressZUp) || Dirty(c, y, FlowPoints.PressYHome))
            {
                c.Inc(side + "压头残留回收次数");
                c.MoveTo(z, FlowPoints.PressZUp, side + "压头Z 回收抬起", 3000);
                c.MoveTo(y, FlowPoints.PressYHome, side + "压头Y 回收停放位", 3000);
                return;
            }

            if (c.Get(inTest) < 1 || c.Get(held) < FlowPoints.BatchSize || c.Get(done) >= 1)
            {
                c.Set(canPress, 0);
                c.Yield(side + "压头等梭车进测试位");
                return;
            }
            if (c.Get("前上料在检测区") >= 1) { c.Yield("上料臂还在检测区"); return; }
            if (c.Get("前下料在检测区") >= 1) { c.Yield("下料臂还在检测区"); return; }

            if (!c.WaitTake(interlock, 20, 1500)) { c.Yield(interlock + " 被占"); return; }
            bool ok = false;
            try
            {
                ok = PressCycle(c, front, y, z, side);
                if (ok)
                {
                    // 前压头记「前测完成」，后压头（末段）才记 TestOk —— 同一颗芯片两段都测
                    c.Inc(front ? "前测完成" : "TestOk", FlowPoints.BatchSize);
                    c.Set(done, 1);
                    c.Inc(side + "压头测试批次");
                }
                else c.Inc(side + "压头让行次数");
            }
            finally
            {
                c.MoveTo(z, FlowPoints.PressZUp, side + "压头Z 收到抬起位");
                c.MoveTo(y, FlowPoints.PressYHome, side + "压头Y 回停放位");
                c.Set(canPress, 0);
                c.Give(interlock);
            }

            c.Inc(side + "压头循环");
            if (c.Heartbeat()) c.Info("心跳：" + interlock + " 正常占用，循环 "
                                      + c.Get(side + "压头循环").ToString("0")
                                      + "　测试完成（末段）" + c.Get("TestOk").ToString("0") + " 颗");
        }

        /// <summary>压头一测：Y 到站1 → Z 下压保压 → 抬起 → Y 到站2 → 下压保压 → 抬起 → 回位。</summary>
        private bool PressCycle(FlowCtx c, bool front, int y, int z, string side)
        {
            if (!c.MoveTo(y, FlowPoints.PressYSite1, side + "压头Y → 测试位1")) return false;
            c.Set(side + "压头可下压", 1);
            if (!c.MoveTo(z, FlowPoints.PressZDown, side + "压头Z 下压到 " + FlowPoints.PressZDown.ToString("0.###"))) return false;
            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
            {
                c.Out(FlowPoints.PressVacOn(front, i), true);
                c.Out(FlowPoints.PressVacOff(front, i), false);
            }
            if (!c.Sleep(FlowPoints.TestDwellMs)) return false;
            if (!c.MoveTo(z, FlowPoints.PressZUp, side + "压头Z 抬起")) return false;

            if (!c.MoveTo(y, FlowPoints.PressYSite2, side + "压头Y → 测试位2")) return false;
            if (!c.MoveTo(z, FlowPoints.PressZDown, side + "压头Z 再下压")) return false;
            if (!c.Sleep(FlowPoints.TestDwellMs)) return false;
            if (!c.MoveTo(z, FlowPoints.PressZUp, side + "压头Z 抬起")) return false;

            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
            {
                c.Out(FlowPoints.PressVacOn(front, i), false);
                c.Out(FlowPoints.PressVacOff(front, i), true);
            }
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            for (int i = 1; i <= FlowPoints.NozzleCount; i++)
                c.Out(FlowPoints.PressVacOff(front, i), false);
            return true;
        }

        // ==================================================================
        //  下料臂
        // ==================================================================

        /// <summary>
        /// 下料臂：从后梭车取 8 颗 → 按 Bin 分到 Auto1/2/3（良品）与 NG 仓（不良）。
        /// Bin → 仓 的映射与满盘阈值都在 FlowPoints 第 10 节（照 Ops 的良品池 / 不良池划分）。
        /// </summary>
        private void T_UnloaderArm(FlowCtx c)
        {
            c.Declare("X走廊");
            int ux = AxisOf("下料X轴"), uy = AxisOf("下料Y轴"), uu = AxisOf("下料旋转U轴");
            int upx = AxisOf("下料变距X轴"), upy = AxisOf("下料变距Y轴");
            int[] zs = UldZs();

            c.Register("BinCode");

            // 残留回收：同 上料臂，吸嘴 / 下料X 留在走廊里会把三臂分时互锁卡死
            if (DirtyAny(c, zs, FlowPoints.UldrZUp) || Dirty(c, ux, FlowPoints.UldrXHome))
            {
                c.Inc("下料臂残留回收次数");
                c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 回收抬起");
                c.MoveTo(ux, FlowPoints.UldrXHome, "下料X 收回停放位（残留）", 3000);
                return;
            }

            bool holding = c.Get("在制_下料吸嘴") >= 1;

            if (!holding)
            {
                if (c.GetC("CarryCarStatus_后", "") != "ReadyULoad" || c.Get("在制_后梭车") < FlowPoints.BatchSize)
                { c.Yield("后梭车没挂 ReadyULoad 或上面不足一批"); return; }
                if (FreeBinStore(c) < 0) { c.Yield("三座 Auto 仓都在等换盘，下料让行"); return; }
            }

            if (!c.WaitTake("X走廊", FlowPoints.PollMs, FlowPoints.TokenTimeoutMs))
            { c.Yield("X 走廊被上料/下料/搬运三臂分时占用（下料）"); return; }

            bool ok;
            try
            {
                ok = holding || UnloaderPick(c, ux, uy, uu, upx, upy, zs);
                if (ok) ok = UnloaderDrop(c, ux, uy, uu, upx, upy, zs);
            }
            finally
            {
                Park(c, ux, FlowPoints.UldrXHome, "下料X 退回停放位");
                c.Set("前下料在检测区", 0);                       // 兜底清（正常由 UnloaderPick 自己清）
                c.Give("X走廊");
            }

            c.Inc("下料臂循环");
            if (!ok) { c.Inc("下料臂让行批次"); return; }
            if (c.Heartbeat()) c.Info("心跳：下料臂循环 " + c.Get("下料臂循环").ToString("0")
                                      + "　BinCode=" + c.GetC("BinCode", "未分选")
                                      + "　落盘合计 " + BinSum().ToString("0") + " 颗");
        }

        /// <summary>下料：取料段（从后梭车吸 8 颗，并把这一批的良品 / 不良 Bin 目标锁存）。</summary>
        private bool UnloaderPick(FlowCtx c, int ux, int uy, int uu, int upx, int upy, int[] zs)
        {
            if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
            if (!c.MoveTo(upx, FlowPoints.UldrPitch, "下料变距X 收间距")) return false;
            if (!c.MoveTo(upy, FlowPoints.UldrPitch, "下料变距Y 收间距")) return false;
            if (!c.MoveTo(uu, FlowPoints.UldrUPick, "下料旋转U 到取料朝向")) return false;
            if (!c.MoveTo(uy, FlowPoints.UldrYShuttle, "下料Y → 后梭车取料行")) return false;
            c.Set("前下料在检测区", 1);                           // 进梭车检测区（压头线程判这个位）
            if (!c.MoveTo(ux, FlowPoints.UldrXPick, "下料X → 取料位")) return false;
            if (!c.MoveAll(zs, FlowPoints.UldrZDown, "下料吸嘴1~8Z 下降到取料高度")) return false;

            Suck(c, FlowPoints.UldrVacOn, FlowPoints.UldrVacOff);
            if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
            if (!c.WaitIns(VacOkNames(FlowPoints.UldrVacOk), true, 600))
            {
                // 口径：仿真卡的「输出→输入」反馈联动只接了左上料那 8 路，右下料这 8 路没接；
                //       真机是硬反馈，现场把这段改回「反馈不齐 = 取料失败不记数」即可。
                c.Inc("下料真空反馈未接入次数");
                c.WarnOnceFlow("uldVac", "下料 8 路「右下料真空」反馈没接进仿真联动，本批按下料成功记账（真机要判失败）");
            }

            int per = FlowPoints.BatchSize;
            int pm = (int)Math.Clamp(MachineModel.Instance.GetNum("recipe_badrate", 8), 0, 1000);
            long carry = (long)c.Get("不良千分位余量") + (long)per * pm;   // 确定性分摊，不用随机数，保证颗数可加
            int badN = (int)(carry / 1000);
            carry -= (long)badN * 1000;
            int goodN = per - badN;
            int goodBin = PickGoodBin(c), badBin = PickBadBin(c);

            c.Set("本批良品颗数", goodN);
            c.Set("本批不良颗数", badN);
            c.Set("本批良品Bin", goodBin);
            c.Set("本批不良Bin", badBin);
            c.Set("不良千分位余量", carry);
            c.SetC("BinCode", "Bin" + (goodBin > 0 ? goodBin.ToString("0") : "无"));

            c.Hand("在制_后梭车", "在制_下料吸嘴", per);
            c.Inc("下料取料批次");
            c.Set("前下料在检测区", 0);                           // 料已吸到手，退出检测区

            if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
            c.SetPhase(0);
            return true;
        }

        /// <summary>下料：放料段（良品先落 Auto 仓，不良再落 NG 仓；两段各自原子转帐，可断点续放）。</summary>
        private bool UnloaderDrop(FlowCtx c, int ux, int uy, int uu, int upx, int upy, int[] zs)
        {
            int goodN = (int)c.Get("本批良品颗数");
            int badN = (int)c.Get("本批不良颗数");
            int goodBin = (int)c.Get("本批良品Bin");
            int badBin = (int)c.Get("本批不良Bin");

            if (c.Phase() == 0 && goodN > 0)
            {
                int store = FlowPoints.StoreOfBin(goodBin);
                if (store < 1) { c.Alarm("良品 Bin" + goodBin + " 没映射到 Auto 仓，本批不落盘"); return false; }
                if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
                if (!c.MoveTo(uu, FlowPoints.UldrUPlace, "下料旋转U 换向到放料朝向")) return false;
                if (!c.MoveTo(uy, FlowPoints.UldrYStore(store), "下料Y → Auto" + store + " 仓行")) return false;
                if (!c.MoveTo(ux, FlowPoints.UldrXPlace, "下料X → 出料位")) return false;
                if (!c.MoveAll(zs, FlowPoints.UldrZDown, "下料吸嘴1~8Z 下降到放料高度")) return false;
                Blow(c, FlowPoints.UldrVacOn, FlowPoints.UldrVacOff);
                if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
                ValveIdle(c, FlowPoints.UldrVacOn, FlowPoints.UldrVacOff);

                c.Adds(new[] { "在制_下料吸嘴", "BinCount_" + goodBin, "盘内颗数_" + store,
                               "良品落盘批次", "下料模组走料盘点位置" },
                       new double[] { -goodN, goodN, goodN, 1, 1 });
                c.Set("本批良品颗数", 0);
                c.SetPhase(1);
                if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
            }

            if (c.Phase() == 1 && badN > 0)
            {
                if (c.Get("盘内颗数_NG") + badN > FlowPoints.TrayCapacity || c.Get("NgFull") >= 1)
                { c.Yield("NG 仓等换盘，不良品让行"); return false; }
                if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
                if (!c.MoveTo(uy, FlowPoints.UldrYNg, "下料Y → NG 仓行")) return false;
                if (!c.MoveTo(ux, FlowPoints.UldrXPlace, "下料X → 出料位")) return false;
                if (!c.MoveAll(zs, FlowPoints.UldrZDown, "下料吸嘴1~8Z 下降到放料高度")) return false;
                Blow(c, FlowPoints.UldrVacOn, FlowPoints.UldrVacOff);
                if (!c.Sleep(FlowPoints.VacuumDwellMs)) return false;
                ValveIdle(c, FlowPoints.UldrVacOn, FlowPoints.UldrVacOff);

                c.Adds(new[] { "在制_下料吸嘴", "BinCount_" + badBin, "盘内颗数_NG",
                               "不良落盘批次", "下料模组走料盘点位置" },
                       new double[] { -badN, badN, badN, 1, 1 });
                c.Set("本批不良颗数", 0);
                c.SetC("BinCode", "Bin" + badBin + "(NG)");
                if (!c.MoveAll(zs, FlowPoints.UldrZUp, "下料吸嘴1~8Z 抬起")) return false;
            }

            c.SetPhase(0);
            return c.Get("在制_下料吸嘴") < 1;
        }

        /// <summary>良品轮转选 Bin（Bin1..12 顺序轮转，跳过正在换盘 / 盘内已满的 Auto 仓）；没有可用仓返回 -1。</summary>
        private int PickGoodBin(FlowCtx c)
        {
            int step = (int)c.Get("良品轮转步");
            for (int k = 0; k < FlowPoints.GoodBinLast - FlowPoints.GoodBinFirst + 1; k++)
            {
                int bin = FlowPoints.GoodBinAt(step + k);
                int store = FlowPoints.StoreOfBin(bin);
                if (store < 1) continue;
                if (c.Get("BinFull_" + store) >= 1) continue;
                if (c.Get("盘内颗数_" + store) >= FlowPoints.TrayCapacity) continue;
                c.Set("良品轮转步", step + k + 1);
                return bin;
            }
            return -1;
        }

        /// <summary>不良轮转选 Bin（Bin13..16 顺序轮转，统一落 NG 仓）。</summary>
        private int PickBadBin(FlowCtx c)
        {
            int step = (int)c.Get("不良轮转步");
            c.Set("不良轮转步", step + 1);
            return FlowPoints.BadBinAt(step);
        }

        /// <summary>有没有还能收料的 Auto 仓（0 = 有，-1 = 三仓都在等换盘）。</summary>
        private int FreeBinStore(FlowCtx c)
        {
            for (int n = 1; n <= 3; n++)
                if (c.Get("BinFull_" + n) < 1 && c.Get("盘内颗数_" + n) < FlowPoints.TrayCapacity) return n;
            return -1;
        }

        // ==================================================================
        //  Auto1/2/3 收料仓
        // ==================================================================

        /// <summary>Auto{n} 收料仓：满 32 颗整盘顶出到交接位等搬运臂取走，同时从自备空盘里换一片上来。</summary>
        private void T_AutoStore(FlowCtx c, int no)
        {
            string full = "BinFull_" + no, ask = "isAskForNewTray_" + no;
            c.Register(full); c.Register(ask);
            c.Declare("仓-Auto互斥");
            c.Init("盘内颗数_" + no, 0);
            c.Init("空盘数_" + no, FlowPoints.AutoEmptyBuffer);
            c.Register("满盘待取_" + no);

            c.Inc("Auto" + no + "仓循环");

            double onTray = c.Get("盘内颗数_" + no);
            if (onTray < FlowPoints.TrayCapacity && c.Get(full) < 1)
            {
                if (c.Heartbeat()) c.Info("心跳：Auto" + no + " 仓循环 " + c.Get("Auto" + no + "仓循环").ToString("0")
                                          + "，盘内 " + onTray.ToString("0") + "/" + FlowPoints.TrayCapacity.ToString("0")
                                          + " 颗，自备空盘 " + c.Get("空盘数_" + no).ToString("0") + " 片");
                return;
            }
            if (c.Get(full) < 1)
            {
                c.Set(full, 1);
                c.Info(full + "=1：满盘落定（" + FlowPoints.TrayCapacity.ToString("0") + " 颗/盘），准备回仓换新盘");
            }
            if (c.Get("满盘待取_" + no) >= 1)
            {
                if (c.Heartbeat()) c.Info("心跳：Auto" + no + " 满盘已在交接位，等搬运臂取走");
                return;
            }
            if (c.Get("空盘数_" + no) < 1)
            {
                c.Set(ask, 1);
                if (c.Changed(ask)) c.Alarm(ask + "=1：Auto" + no + " 仓自备空盘用完，等进空盘仓补盘");
                return;
            }
            if (!c.WaitTake("仓-Auto互斥", 50, 2000)) { c.Yield("仓-Auto互斥 被其他仓占用"); return; }
            try
            {
                if (!StoreIndex(c, AxisOf("Auto" + no + "Y轴"), AxisOf("Auto" + no + "Z轴"), "Auto" + no + "仓")) return;
                c.Set("满盘待取_" + no, 1);
                c.Set(full, 0);
                c.Set(ask, 0);
                c.Inc("空盘数_" + no, -1);
                c.Inc("盘内颗数_" + no, -FlowPoints.TrayCapacity);
                c.Inc("Auto" + no + "换盘次数");
                // 满盘里的颗数不回退 BinCount：BinCount_* 是「累计落盘颗数」口径，整盘回仓只减「盘内颗数」
                c.Info("Auto" + no + " 仓已顶出满盘到交接位并换入空盘（累计落盘 "
                       + c.Get("BinCount_" + no).ToString("0") + " 颗口径不变）");
            }
            finally { c.Give("仓-Auto互斥"); }
        }

        /// <summary>
        /// OK / NG 料仓：资料里这两座的升降与侧推都在阀岛和 485 上，没有独立伺服轴，
        /// 所以本线程只做「满盘判定 + 换盘事件」，运动计数为 0 是符合资料的（不是漏接）。
        /// </summary>
        private void T_OkNgStore(FlowCtx c)
        {
            c.Register("NgFull"); c.Register("OkFull");
            c.Init("盘内颗数_NG", 0);
            c.Init("盘内颗数_OK", 0);
            c.Init("NG换盘起点", 0);

            c.Inc("OkNg仓循环");

            if (c.Get("盘内颗数_NG") >= FlowPoints.TrayCapacity && c.Get("NgFull") < 1)
            {
                c.Set("NgFull", 1);
                c.Set("NG换盘起点", 0);
            }
            if (c.Get("NgFull") >= 1)
            {
                if (c.Get("NG换盘起点") <= 0)
                {
                    c.Set("NG换盘起点", Environment.TickCount64);
                    if (c.Changed("NgFull")) c.Info("NgFull=1：NG 盘满（" + FlowPoints.TrayCapacity.ToString("0")
                                                    + " 颗），气缸换盘 / 人工取盘开始计时");
                }
                else if (Environment.TickCount64 - c.Get("NG换盘起点") >= FlowPoints.NgSwapMs)
                {
                    c.Inc("盘内颗数_NG", -FlowPoints.TrayCapacity);
                    c.Inc("NG回仓颗数", FlowPoints.TrayCapacity);
                    c.Inc("NG回仓盘数");
                    c.Set("NgFull", 0);
                    c.Set("NG换盘起点", 0);
                    c.Info("NG 料仓换盘完成（气缸 / 人工，资料无轴）：整盘 "
                           + FlowPoints.TrayCapacity.ToString("0") + " 颗回仓");
                }
            }

            // OK 仓本流程不投料（手动排料 / 未启用），只把状态与感应读出来挂到黑板
            c.Set("OkFull", c.Get("盘内颗数_OK") >= FlowPoints.TrayCapacity ? 1 : 0);
            if (c.Changed("NgFull")) c.Info("NgFull=" + c.Get("NgFull").ToString("0") + "（NG 盘满，需换盘）");
            if (c.Changed("OkFull")) c.Info("OkFull=" + c.Get("OkFull").ToString("0") + "（OK 盘满，需换盘）");
            if (c.Heartbeat()) c.Info("心跳：OK/NG 仓循环 " + c.Get("OkNg仓循环").ToString("0")
                                      + "　NG 盘内 " + c.Get("盘内颗数_NG").ToString("0") + "/"
                                      + FlowPoints.TrayCapacity.ToString("0")
                                      + "　OK 满料感应=" + (c.In("OK料仓满料感应") ? "有" : "无")
                                      + "　NG 满料感应=" + (c.In("NG料仓1满料感应") ? "有" : "无"));
        }

        // ==================================================================
        //  视觉与飞拍
        // ==================================================================

        /// <summary>
        /// 视觉与飞拍：绑定的轴走过飞拍通道时触发一次相机（通道表 = HandoverTables.FlyShot 5 条），
        /// 偏移序号 OffsetSeq 每次触发轮转一条通道；「飞拍允许」仍按安全位 + 门禁判。
        /// </summary>
        private void T_VisionFlyShot(FlowCtx c)
        {
            c.Register("OffsetSeq");
            c.SetFlag("飞拍允许", c.SafeOk && !c.Flag("DoorOpen"));
            if (c.Changed("飞拍允许")) c.Info("飞拍允许=" + c.Get("飞拍允许").ToString("0") + "（安全位才允许）");

            int ch = FlowEngine.FlyChannels();
            var m = MotionManager.Instance;
            for (int i = 0; i < ch; i++)
            {
                string axisName = FlowEngine.FlyAxisName(i);
                int ax = AxisOf(axisName);
                if (ax < 0) continue;                                   // 定拍通道（压头左 / 右）没绑伺服轴，跳过
                string busyKey = "飞拍在通道_" + i;
                bool inside = m.State(ax).Busy;
                if (inside && c.Get(busyKey) < 1)
                {
                    c.Set(busyKey, 1);
                    c.Inc("飞拍触发次数");
                    c.Set("OffsetSeq", (c.Get("OffsetSeq") + 1) % Math.Max(1, ch));
                    c.Out("视觉触发", true);
                }
                else if (!inside && c.Get(busyKey) >= 1)
                {
                    c.Set(busyKey, 0);
                    c.Out("视觉触发", false);
                }
            }

            if (c.Changed("OffsetSeq")) c.Info("OffsetSeq=" + c.Get("OffsetSeq").ToString("0")
                                               + "（当前飞拍通道，共 " + ch + " 条）");
            c.Inc("视觉与飞拍循环");
            if (c.Heartbeat()) c.Info("心跳：视觉与飞拍循环 " + c.Get("视觉与飞拍循环").ToString("0")
                                      + "　飞拍触发 " + c.Get("飞拍触发次数").ToString("0") + " 次");
        }

        // ==================================================================
        //  守恒自检
        // ==================================================================

        /// <summary>
        /// 守恒自检：投料 LotIn == ΣBin + 在制 + 报废，每 24s 一行。
        /// 口径（就写在日志文本里，方便现场对表）：
        ///   投料   = 流程线程从上料位待测盘吸取成功的颗数（8 路真空反馈齐了才记，基线 0 = 本轮起算）；
        ///   ΣBin   = 流程自己的累计落盘颗数 BinCount_1..16（满盘整盘回仓不回退这一项）；
        ///   在制   = 吸嘴 / 梭车 / 中转缓存台上正持有的颗数之和；
        ///   报废   = 已投料但中途掉落 / 超时丢弃的颗数（仿真里目前没有这条路，恒 0，口径照写）；
        ///   旧口径 = 演示节拍 Ops.OnCycleCompleted 写进 MachineModel.Bins 的合计（只统计 _demo 那 22 步），
        ///            两套数各自闭环、互不校核 —— 这里同时报出来。
        /// </summary>
        private void T_Conservation(FlowCtx c)
        {
            c.Register("LotIn");
            double wip = Wip();
            double bin = BinSum();
            double scrap = VarBox.GetD("报废");
            double lotIn = VarBox.GetD("LotIn");
            double total = bin + wip + scrap;
            double diff = lotIn - total;
            bool ok = Math.Abs(diff) < 0.5;

            EngineLog(ok ? "信息" : "警告", ConservationText());
            if (!ok) c.Alarm("守恒等式不平：差 " + diff.ToString("0.###") + " 颗（口径见本行）");

            c.Set("守恒平衡", ok ? 1 : 0);
            c.Inc("守恒自检次数");
        }

        /// <summary>在制品颗数（吸嘴 + 两台梭车 + 中转缓存台）。</summary>
        public static double Wip()
        {
            return VarBox.GetD("在制_上料吸嘴") + VarBox.GetD("在制_前梭车")
                 + VarBox.GetD("在制_缓存台") + VarBox.GetD("在制_后梭车")
                 + VarBox.GetD("在制_下料吸嘴");
        }

        /// <summary>流程自己的累计落盘颗数 ΣBin（BinCount_1..16）。</summary>
        public static double BinSum()
        {
            double s = 0;
            for (int i = FlowPoints.GoodBinFirst; i <= FlowPoints.BadBinLast; i++)
                s += VarBox.GetD("BinCount_" + i);
            return s;
        }

        /// <summary>守恒等式文本（线程 / 界面 / 自检共用同一句，避免两个地方口径不一致）。</summary>
        public static string ConservationText()
        {
            double lotIn = VarBox.GetD("LotIn");
            double wip = Wip();
            double bin = BinSum();
            double scrap = VarBox.GetD("报废");
            double total = bin + wip + scrap;
            double diff = lotIn - total;

            long legacyBin = 0;
            try { legacyBin = MachineModel.Instance.Bins.Sum(b => (long)b.Count); } catch { /* Bin 表没建按 0 */ }

            string head = "总量守恒　投料 " + lotIn.ToString("0")
                          + (diff == 0 ? " = " : " ≠ ")
                          + "ΣBin " + bin.ToString("0") + " + 在制 " + wip.ToString("0")
                          + " + 报废 " + scrap.ToString("0")
                          + "，差 " + diff.ToString("0.###");
            string tail = "　口径：投料=流程吸取成功颗数(LotIn，真空反馈齐才记)，ΣBin=流程累计落盘 BinCount_1..16（满盘回仓不回退），"
                          + "在制=吸嘴/梭车/缓存台持有，报废=中途掉落丢弃；"
                          + "旧口径(演示节拍 Ops→MachineModel.Bins)ΣBin=" + legacyBin.ToString("0")
                          + "、总产量=" + MachineModel.Instance.TotalCount.ToString("0")
                          + "；两套各自闭环，基线均为本轮清零后 0";
            if (lotIn == 0 && bin == 0 && wip == 0)
                head += "（本轮尚未投料：流程线程已启动但没走完一批）";
            return head + tail;
        }

        /// <summary>飞拍通道数（资料表 5 条）。</summary>
        public static int FlyChannels()
        {
            var t = HandoverTables.FlyShot;
            return t == null ? 0 : t.Length;
        }

        /// <summary>第 i 条飞拍通道绑定的轴名（资料里「压头左 / 压头右」是定拍，没有伺服轴）。</summary>
        public static string FlyAxisName(int i)
        {
            var t = HandoverTables.FlyShot;
            if (t == null || i < 0 || i >= t.Length || t[i] == null || t[i].Length == 0) return "";
            string s = t[i][0];
            if (s == "前梭车") return "前梭车X轴";
            if (s == "后梭车") return "后梭车X轴";
            return s;
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
