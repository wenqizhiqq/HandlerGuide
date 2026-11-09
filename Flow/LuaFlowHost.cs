// ============================================================================
//  温启志 18719361399 · 本文件由 温启志 18719361399 编写与维护
//  版权所有所有权利保留；界面与源码中文命名风格亦归温启志 18719361399 所有。
// ============================================================================
//  Flow/LuaFlowHost.cs —— 用 Lua 脚本写工艺流程的宿主：一条脚本 = 一个 Lua 环境
//  = 一条 while 线程，脚本之间只通过 VarBox（具名变量）与令牌交互，不互相调用。
//
//  契约（脚本必须遵守，宿主无法替它兜底的部分都写在这里）：
//   1. 脚本主体是一条 while running() do ... end。running() 返回假才会退出，
//      Lua 死循环从外部杀不掉，所以循环条件必须由脚本自己写。
//   2. 所有等待都走 pause()/box.wait()/stable()，它们内部带 Sleep 与超时；
//      脚本里写 while readin(...) do end 这种忙等是被禁止的。
//   3. 轴一律用真名（如 "上料X轴"），宿主按 HandoverTables 现解析；
//      写数字轴号会像平面仿真那样静默指错轴。
//   4. 软限位、急停、门禁、气压、共享导轨间距全部留在 C#（MotionManager 的 SR
//      规则）里，脚本只能被拒绝、不能绕过；返回假就是被拒绝，要自己让行。
//   5. NeoLua 能反射 .NET，这不是安全沙箱，脚本目录按内部可信对待。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NLua;
using HandlerGuide.Motion;

namespace HandlerGuide.Flow
{
    /// <summary>一条 Lua 流程的运行时状态。</summary>
    public sealed class LuaFlow
    {
        public string Name;
        public string Path;
        public int PeriodMs = 50;
        public volatile bool Running;
        public bool Started;
        public long Loops;
        public string Step = "未装载";
        public string Error = "";
        /// <summary>正在执行到哪一行（脚本里 step(...) 所在的行号，0 = 未知）。</summary>
        public int CurLine;
        /// <summary>当前这一段的耗时（ms，= 上一个标记行到这一行的间隔）。</summary>
        public double StepMs;
        /// <summary>逐行累计耗时（行号 → ms），执行过 step(...) 的行才有。</summary>
        public readonly Dictionary<int, double> LineMs = new Dictionary<int, double>();
        internal long LastTraceAt;

        /// <summary>断点行号集合（点行号左侧的红点切换）。</summary>
        public readonly HashSet<int> Breakpoints = new HashSet<int>();
        /// <summary>是否正断在某一行（true 时 Lua 线程停在 hook 里，等 Resume）。</summary>
        public volatile bool Paused;
        public int PauseLine;
        /// <summary>变量查看请求 / 结果（界面点标识符 → 暂停中由 Lua 线程自己取值）。</summary>
        public string InspectRequest;
        public string InspectName = "";
        public string InspectResult = "";
        internal Thread Thread;
        internal dynamic Env;
        internal readonly List<string> Held = new List<string>();
    }

    /// <summary>Lua 流程宿主。</summary>
    public static class LuaFlowHost
    {
        static readonly List<LuaFlow> flows = new List<LuaFlow>();
        static readonly object gate = new object();
        static volatile bool stopAll;

        /// <summary>脚本目录：随程序走，改了不用重编译。</summary>
        public static string ScriptDir
        {
            get
            {
                var d = Path.Combine(AppContext.BaseDirectory, "Scripts");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }

        /// <summary>扫描脚本目录，每个 .lua 注册成一条流程（不启动）。</summary>
        public static int Scan()
        {
            lock (gate)
            {
                flows.Clear();
                foreach (var f in Directory.GetFiles(ScriptDir, "*.lua"))
                {
                    var lf = new LuaFlow { Name = Path.GetFileNameWithoutExtension(f), Path = f };
                    flows.Add(lf);
                }
                return flows.Count;
            }
        }

        public static int StartAll()
        {
            lock (gate)
            {
                // 布局页的〔启动演示节拍〕只调 StartAll，不调 Scan —— 如果用户没打开过
                // 流程脚本页，flows 还是空的，StartAll 会「启动 0 条」然后立刻返回，
                // 界面上就成了「仿真刚开始就结束」。这里兜底现扫一次。
                if (flows.Count == 0) Scan();
                // SafeOk 由 Motion/FlowEngine 的安全扫描线程发布；它没起来时运动类流程
                // 会一直『让行：未安全』—— 这里先把原因说清楚，免得看着像脚本没跑。
                if (!VarBox.GetFlag("SafeOk"))
                    MachineModel.Instance.Log("警告",
                        "SafeOk=0：运动类流程会原地让行。先到「运动仿真」页点〔流程启动〕拉起安全扫描线程。");
                stopAll = false;
                foreach (var f in flows) StartOne(f);
                return flows.Count;
            }
        }

        /// <summary>
        /// 只启动「主流程」（脚本目录里的整机流程，目前是 31_平移分选）。
        ///
        /// 需求 76：〔启动演示节拍〕以前直接调 StartAll() —— 把目录里 **31 条脚本全起了**，
        /// 其中 01~30 是单模块调试流程（01_上料XY / 10_下料XY / 15_前梭车 …），它们自己
        /// 也要动同一批轴，于是和 31 抢 ARM_X 令牌、互相把轴拽回自己的停放位：
        ///   · 整机流程走两步就被打断 ⇒ 看着就是「脚本没跑起来」；
        ///   · 01 的最后一步是 go("上料X轴", 上料.停放X=0) ⇒ 轴被拽到 0 位不动。
        /// 31 的脚本头注释本来就写着「生产期间独占上料臂，单模块流程请关掉」。
        /// 所以演示节拍只起主流程；单模块脚本仍可在「流程脚本」页逐条启动。
        /// </summary>
        public static int StartMain()
        {
            lock (gate)
            {
                if (flows.Count == 0) Scan();
                stopAll = false;
                LuaFlow main = null;
                foreach (var f in flows)
                    if (f.Name.Contains("平移分选")) { main = f; break; }
                if (main != null)
                {
                    StartOne(main);
                    return 1;
                }
                // 兜底：目录里没有主流程脚本时退回旧行为（全部启动），不至于什么都不动。
                foreach (var f in flows) StartOne(f);
                return flows.Count;
            }
        }

        public static void StopAll()
        {
            Thread[] ts;
            lock (gate)
            {
                stopAll = true;
                ts = new Thread[flows.Count];
                for (int i = 0; i < flows.Count; i++) ts[i] = flows[i].Thread;
            }
            foreach (var t in ts)
                if (t != null && t != Thread.CurrentThread && !t.Join(3000))
                    // .NET Core 起 Thread.Abort 直接抛 PlatformNotSupportedException；
                    // 脚本只能靠 running() 协作退出，这里只记一条日志（不抛、也不假装成功）。
                    MachineModel.Instance.Log("警告", "Lua 流程线程 3s 未退出：" + (t.Name ?? "?"));
        }

        // ------------------------------------------------------------ 单条流程控制
        //  编辑器面板的 [启动此条] [停止此条] 按钮用（公开 API 只增不改）。
        //  停止是协作式的：只把 Running 置假，脚本自己的 while running() 循环
        //  和 pause()/boxwait()/stable() 的内层检查会在一个节拍内退出来；
        //  绝不在 UI 线程 Join，免得卡界面。

        /// <summary>启动单条流程（已在跑就原样返回真）。找不到流程返回假。</summary>
        public static bool StartFlow(string name)
        {
            LuaFlow f = FindByName(name);
            if (f == null) return false;
            lock (gate) { stopAll = false; StartOne(f); }
            return true;
        }

        /// <summary>请求停止单条流程：置假 Running，由脚本的 running() 循环自己退出。</summary>
        public static bool StopFlow(string name)
        {
            LuaFlow f = FindByName(name);
            if (f == null) return false;
            f.Running = false;
            return true;
        }

        /// <summary>某条流程此刻是否在跑（编辑器用它决定只读态）。</summary>
        public static bool IsRunning(string name)
        {
            LuaFlow f = FindByName(name);
            return f != null && f.Running && !stopAll;
        }

        /// <summary>按流程名找条目（区分大小写，与脚本文件名一致）。</summary>
        static LuaFlow FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (gate)
                foreach (var f in flows)
                    if (f.Name == name) return f;
            return null;
        }

        /// <summary>给界面用的行：名称 / 状态 / 循环数 / 当前步 / 持有令牌 / 错误。</summary>
        public static List<string[]> Rows()
        {
            var r = new List<string[]>();
            lock (gate)
                foreach (var f in flows)
                    lock (f)
                        r.Add(new[] { f.Name, f.Running ? "运行" : (f.Started ? "已停" : "未启动"),
                            f.Loops.ToString(), f.Step, string.Join(",", f.Held.ToArray()), f.Error });
            return r;
        }

        /// <summary>切换某一条流程的断点行。</summary>
        public static void ToggleBreakpoint(string name, int line)
        {
            if (line <= 0) return;
            lock (gate) foreach (var f in flows)
            {
                if (f.Name != name) continue;
                lock (f) { if (!f.Breakpoints.Remove(line)) f.Breakpoints.Add(line); }
                return;
            }
        }

        /// <summary>某条流程当前的断点行快照。</summary>
        public static HashSet<int> BreakpointsOf(string name)
        {
            lock (gate) foreach (var f in flows)
            {
                if (f.Name != name) continue;
                lock (f) return new HashSet<int>(f.Breakpoints);
            }
            return new HashSet<int>();
        }

        /// <summary>是否正断住 + 断在哪一行。</summary>
        public static bool IsPaused(string name, out int line)
        {
            line = 0;
            lock (gate) foreach (var f in flows)
            {
                if (f.Name != name) continue;
                lock (f) { line = f.PauseLine; return f.Paused; }
            }
            return false;
        }

        /// <summary>继续（把停住的 Lua 线程放行）。</summary>
        public static void Resume(string name)
        {
            lock (gate) foreach (var f in flows)
            {
                if (f.Name != name) continue;
                lock (f) { f.Paused = false; f.Step = "运行中"; }
                return;
            }
        }

        /// <summary>点变量取值：暂停时交给 Lua 线程自己算（最多等 400ms）。</summary>
        public static string Inspect(string name, string ident)
        {
            if (string.IsNullOrEmpty(ident)) return "";
            LuaFlow hit = null;
            lock (gate) foreach (var f in flows) if (f.Name == name) { hit = f; break; }
            if (hit == null) return "";
            bool paused; lock (hit) paused = hit.Paused;
            if (!paused) return "（运行中取不到局部变量，断住再点）";
            lock (hit) { hit.InspectRequest = ident; hit.InspectResult = ""; }
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(20);
                lock (hit) if (hit.InspectRequest == null && hit.InspectResult.Length > 0) return hit.InspectResult;
            }
            return "（超时：没取到）";
        }

        /// <summary>取某条流程的逐行耗时快照（行号 → ms）与当前行；界面 500ms 拉一次。</summary>
        public static void GetTrace(string name, out int curLine, out double stepMs, out Dictionary<int, double> lineMs)
        {
            curLine = 0; stepMs = 0; lineMs = new Dictionary<int, double>();
            lock (gate)
            {
                foreach (var f in flows)
                {
                    if (f.Name != name) continue;
                    lock (f)
                    {
                        curLine = f.CurLine; stepMs = f.StepMs;
                        lineMs = new Dictionary<int, double>(f.LineMs);
                    }
                    return;
                }
            }
        }

        public static string ReadScript(string name)
        {
            foreach (var f in flows) if (f.Name == name) return File.ReadAllText(f.Path, Encoding.UTF8);
            return "";
        }

        public static void WriteScript(string name, string text)
        {
            foreach (var f in flows)
                if (f.Name == name) { File.WriteAllText(f.Path, text, new UTF8Encoding(false)); return; }
        }

        // ---------------------------------------------------------------- 绑定

        /// <summary>把 API 装进某个流程的 Lua 环境。</summary>
        static void Install(LuaFlow f, dynamic e)
        {
            e["running"] = (Func<bool>)(() => f.Running && !stopAll);
            e["pause"] = (Action<int>)(ms =>
            {
                if (ms <= 0) ms = 1;
                int left = ms;
                while (left > 0 && f.Running && !stopAll)
                {
                    int s = left > 20 ? 20 : left;
                    Thread.Sleep(s);
                    left -= s;
                }
            });
            e["step"] = (Action<string>)(s => { lock (f) f.Step = Unhex(s); });
            e["log"] = (Action<string>)(s => MachineModel.Instance.Log("信息", "[" + f.Name + "] " + Unhex(s)));
            e["alarm"] = (Action<string>)(s => MachineModel.Instance.Log("警告", "[" + f.Name + "] " + Unhex(s)));

            e["ax"] = (Func<string, int>)(n => MotionManager.Instance.AxisIndex(Unhex(n)));
            e["lasterr"] = (Func<string>)(() => f.Error);
            e["moveabs"] = (Func<string, double, bool>)((n, v) =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(Unhex(n));
                if (i2 < 0) { f.Error = "轴不在真表: " + Unhex(n); return false; }
                var r = mm.MoveAbs(i2, v); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["moverel"] = (Func<string, double, bool>)((n, v) =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(Unhex(n));
                if (i2 < 0) { f.Error = "轴不在真表: " + Unhex(n); return false; }
                var r = mm.MoveRel(i2, v); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["home"] = (Func<string, bool>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(Unhex(n));
                if (i2 < 0) return false;
                var r = mm.Home(i2); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["pos"] = (Func<string, double>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(Unhex(n));
                return i2 < 0 ? double.NaN : mm.Position(i2);
            });
            e["inpos"] = (Func<string, bool>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(Unhex(n));
                return i2 >= 0 && !mm.State(i2).Busy && !mm.State(i2).Alarm;
            });
            e["axiscount"] = (Func<int>)(() => MotionManager.Instance.Axes.Count);

            e["readin"] = (Func<string, bool>)(n =>
            {
                var p2 = MotionManager.Instance.Inputs.Find(x => x.Name == Unhex(n));
                return p2 != null && MotionManager.Instance.ReadIn(p2.Bit);
            });
            e["writeout"] = (Func<string, bool, bool>)((n, v) =>
            {
                string nm = Unhex(n);
                var p2 = MotionManager.Instance.Outputs.Find(x => x.Name == nm);
                if (p2 == null) { f.Error = "IO 不在真表: " + nm; return false; }
                MotionManager.Instance.WriteOut(p2.Bit, v); return true;
            });
            e["outval"] = (Func<string, bool>)(n =>
            {
                var p2 = MotionManager.Instance.Outputs.Find(x => x.Name == Unhex(n));
                return p2 != null && MotionManager.Instance.ReadOut(p2.Bit);
            });

            e["boxget"] = (Func<string, double>)(k2 => VarBox.GetD(Unhex(k2)));
            e["boxset"] = (Action<string, double>)((k2, v) => VarBox.SetD(Unhex(k2), v));
            e["boxinc"] = (Func<string, double, double>)((k2, v) => VarBox.Inc(Unhex(k2), v));
            e["boxwait"] = (Func<string, double, int>)((k2, want) =>
            {
                string key = Unhex(k2);
                for (int n = 0; n < 250 && f.Running && !stopAll; n++)
                {
                    if (VarBox.GetD(key) == want) return 1;
                    Thread.Sleep(20);
                }
                f.Error = "等变量超时: " + key; return 0;
            });
            e["stable"] = (Func<string, int, bool>)((n, ms) =>
            {
                string nm = Unhex(n);
                int need = ms <= 0 ? 50 : ms, got = 0;
                while (got < need && f.Running && !stopAll)
                {
                    if (!MotionManager.Instance.Inputs.Exists(x => x.Name == nm)) return false;
                    if (!readIn(nm)) { got = 0; } else got += 20;
                    Thread.Sleep(20);
                }
                return got >= need;
            });
            e["take"] = (Func<string, int, bool>)((k2, o) =>
            {
                string key = Unhex(k2);
                bool ok = VarBox.Take(key, o);
                if (ok) lock (f) if (!f.Held.Contains(key)) f.Held.Add(key);
                return ok;
            });
            e["give"] = (Action<string, int>)((k2, o) =>
            {
                string key = Unhex(k2);
                VarBox.Give(key, o);
                lock (f) f.Held.Remove(key);
            });
            e["held"] = (Func<string[]>)(() => { lock (f) return f.Held.ToArray(); });
            e["point"] = (Func<string, double>)(k2 => VarBox.GetD("点位." + Unhex(k2)));

            // 仿真慢放（1=真机速度，2=半速…）：只缩仿真卡推进的时间，轴参数与安全判据不变。
            // 真机上没有意义，调用它也不会影响当真卡时的任何运动参数。
            e["simspeed"] = (Action<double>)(v => { MotionManager.Instance.SimSpeedScale = v; });

            // 逐行跟踪：step() 在 Lua 侧包一层，先拿 debug.getinfo 的行号，再记时间。
            // 耗时算在**上一行的标记**上（上一个 step → 这一个 step 之间的那段就是上一段的耗时）。
            // 断点：Lua 的 line hook 每执行一行回调一次；命中就把这条线程停在 hook 里等 Resume。
            e["__isbp"] = (Func<int, bool>)(ln => { lock (f) return f.Breakpoints.Count > 0 && f.Breakpoints.Contains(ln); });
            e["__on_break"] = (Action<int>)(ln =>
            {
                lock (f) { f.Paused = true; f.PauseLine = ln; f.Step = "断点：第 " + ln + " 行"; }
            });
            e["__paused"] = (Func<bool>)(() => f.Paused && f.Running && !stopAll);
            e["__sleep"] = (Action<int>)(ms => { if (ms > 0) Thread.Sleep(ms); });
            e["__service"] = (Action)(() => ServiceInspect(f, e));

            e["__line"] = (Action<int, string>)((line, text) =>
            {
                long now = Environment.TickCount64;
                lock (f)
                {
                    if (f.LastTraceAt > 0 && f.CurLine > 0)
                    {
                        double dt = now - f.LastTraceAt;
                        double old;
                        f.LineMs.TryGetValue(f.CurLine, out old);
                        f.LineMs[f.CurLine] = old + dt;
                        f.StepMs = dt;
                    }
                    f.LastTraceAt = now;
                    f.CurLine = line;
                }
            });
        }

        static bool readIn(string name)
        {
            var p = MotionManager.Instance.Inputs.Find(x => x.Name == name);
            return p != null && MotionManager.Instance.ReadIn(p.Bit);
        }


        static void StartOne(LuaFlow f)
        {
            if (f.Thread != null && f.Thread.IsAlive) return;
            f.Running = true;
            f.Started = true;
            f.Error = "";
            f.Paused = false;
            var t = new Thread(() => Run(f)) { IsBackground = true, Name = "Lua:" + f.Name };
            f.Thread = t;
            t.Start();
        }

        /// <summary>
        /// 读脚本源码（UTF-8，剥 BOM），并且**显式识别「透明加密」文件**。
        ///
        /// 需求 76 的现场事故：`31_平移分选.lua` 被本机 DLP/透明加密落成密文
        /// （文件头 `88 7D 1C`）。本程序没在解密白名单里，`File.ReadAllText` 拿到的是
        /// 二进制垃圾，AsciiSafe 把非法字节全变成 `\u{FFFD}`，Lua 第一行就报
        /// `unexpected symbol near '\'` —— 界面上只看到「脚本跑不起来」，日志里什么都没有。
        /// 这里先认密文，抛一条人话错误，直接说清是文件被加密了。
        /// </summary>
        static string ReadScriptText(string path)
        {
            var raw = File.ReadAllBytes(path);
            // ① DLP 透明加密的文件头标记
            if (raw.Length > 8 && raw[0] == 0x88 && raw[1] == 0x7D && raw[2] == 0x1C)
                throw new InvalidDataException(
                    "脚本文件是加密的（DLP 透明加密，文件头 88 7D 1C），本程序读到的是密文。"
                    + "请用编辑器把该文件另存为明文后再启动。");

            string text = Encoding.UTF8.GetString(raw);
            // ② 兜底：UTF-8 解不开的字节会被替换成 U+FFFD，正常 Lua 源码里不该成片出现
            int bad = 0, probe = Math.Min(text.Length, 400);
            for (int i = 0; i < probe; i++) if (text[i] == '\uFFFD') bad++;
            if (probe > 0 && bad > probe / 8)
                throw new InvalidDataException(
                    "脚本文件不是有效的 UTF-8 文本（疑似加密/编码损坏），请另存为 UTF-8 明文。");

            // ③ BOM：Lua 会把它当源码第一个字符，转义后变成 `\u{FEFF}` 直接语法错误
            return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
        }

        /// <summary>
        /// 解开脚本侧的名字编码：`"h:E58886E98089"` → `"分选"`。
        /// <para>
        /// 为什么需要它：实测（NLua 1.7.9 + KeraLua 1.4.9 / Lua 5.4）字符串 interop 走的是
        /// 系统 ANSI（GBK），Lua 里的中文到达 C# 时会被打成 `?`（0x3F），且**不可逆**。
        /// 于是 `boxget("仿真.演示")` 永远查不到 → 主循环永远落进待机分支，
        /// 界面表现就是「脚本没跑起来 / 轴一直在原位不动」。
        /// </para>
        /// <para>
        /// 脚本 31_平移分选.lua 在加载时把所有 API 的名字参数统一编成 `h:` + UTF-8 十六进制
        /// （纯 ASCII，过边界不会被打码），这里解回来。没有 `h:` 前缀的字符串原样返回 ——
        /// 01_上料XY 这类没装包装的旧脚本照样按原样工作。
        /// </para>
        /// </summary>
        internal static string Unhex(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            if (s.Length < 6 || (s.Length - 2) % 2 != 0) return s;
            if (s[0] != 'h' || s[1] != ':') return s;
            int n = (s.Length - 2) / 2;
            var buf = new byte[n];
            for (int i = 0; i < n; i++)
            {
                int hi = HexVal(s[2 + i * 2]), lo = HexVal(s[3 + i * 2]);
                if (hi < 0 || lo < 0) return s;
                buf[i] = (byte)((hi << 4) | lo);
            }
            try { return Encoding.UTF8.GetString(buf); }
            catch { return s; }
        }

        static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
        /// <summary>暂停期间由 Lua 线程自己取一个变量的值（只有它拥有这个 Lua 状态）。</summary>
        static void ServiceInspect(LuaFlow f, dynamic e)
        {
            string name;
            lock (f) { name = f.InspectRequest; if (name == null) return; f.InspectRequest = null; }
            string res;
            try
            {
                var r = e.DoString("return __lookup(" + QuoteLua(name) + ")");
                res = (r != null && r.Length > 0 && r[0] != null) ? Convert.ToString(r[0]) : "nil";
            }
            catch (Exception ex) { res = "取不到：" + ex.Message; }
            lock (f) { f.InspectName = name; f.InspectResult = res; }
        }

        /// <summary>拼一个 Lua 字符串字面量（只用于变量名）。</summary>
        static string QuoteLua(string s)
        {
            string q = ((char)34).ToString();
            string bs = ((char)92).ToString();
            string body = (s ?? "").Replace(bs, bs + bs).Replace(q, bs + q);
            return q + body + q;
        }

        static void Run(LuaFlow f)
        {
            try
            {
                // NLua 的 Lua 状态不是线程安全的：一条流程一个独立实例，正好配一条线程
                using (var e = new Lua())
                {
                    f.Env = e;
                    Install(f, e);
                    // step(...) = 记录“我在第几行 + 上一段花了多久”再走原逻辑。
                    // debug 库没开时 line 传 0，不影响脚本执行。
                    // 用逐字字符串塞 Lua 源码，避免转义把换行写坏
                    e.DoString(@"
local __raw_step = step
function step(s)
  local line = 0
  if debug and debug.getinfo then
    local i = debug.getinfo(2, 'l')
    if i then line = i.currentline or 0 end
  end
  __line(line, tostring(s))
  __raw_step(s)
end

-- 变量查看：先按名字找局部变量（从调用栈逐层找），找不到再看全局
function __lookup(n)
  if debug and debug.getlocal then
    for lvl = 2, 14 do
      local i = 1
      while true do
        local nm, val = debug.getlocal(lvl, i)
        if not nm then break end
        if nm == n then return tostring(val) end
        i = i + 1
      end
    end
  end
  local g = _G[n]
  if g == nil then return 'nil（局部变量已出作用域或未定义）' end
  return tostring(g)
end

-- 断点：line hook，命中就原地等 __paused() 变假
if debug and debug.sethook then
  debug.sethook(function(ev, line)
    if line and __isbp(line) then
      __on_break(line)
      while __paused() do
        __service()
        __sleep(40)
      end
    end
  end, 'l')
end
", "step-wrapper");
                    // ⚠ 这里**必须**走字节级加载，不能用 e.DoString(string)。
                    // 实测（后台探针 artifacts/_luahost_probeC.txt）：
                    //   NLua 1.7.9 的 DoString(string) 会把源码按系统 ANSI(GBK) 编码再交给
                    //   luaL_loadbuffer —— 源码里每一个中文字符在进入 Lua 之前就已经变成 '?'
                    //   （0x3F）。于是 boxget("仿真.演示") 查永远是 0，主循环永远落进待机分支，
                    //   界面上就是「脚本没跑起来 / 轴一直在原位」。这个损失在 Lua 内部，
                    //   任何转义、任何 C# 侧解码都救不回来。
                    //   LoadBuffer(byte[]) 把 UTF-8 原始字节直接交给 Lua，中文完整保留。
                    var scriptText = ReadScriptText(f.Path);
                    var blob = Encoding.UTF8.GetBytes(scriptText);
                    var st = e.State;
                    // 取栈顶错误消息（取完弹栈）。KeraLua 各版本的取串方法名不一致，
                    // 这里逐个试，取不到就给空串 —— 不要把「报错取不到」再变成一次崩溃。
                    string TakeErr()
                    {
                        try
                        {
                            string m = null;
                            try { m = st.ToString(-1); } catch { }
                            try { st.Pop(1); } catch { }
                            return m ?? "";
                        }
                        catch { return ""; }
                    }
                    var rc = st.LoadBuffer(blob, f.Name);
                    if (rc != KeraLua.LuaStatus.OK)
                        throw new InvalidOperationException("脚本编译失败（" + rc + "）：" + TakeErr());
                    var rcRun = st.PCall(0, 0, 0);
                    if (rcRun != KeraLua.LuaStatus.OK)
                        throw new InvalidOperationException("脚本运行出错（" + rcRun + "）：" + TakeErr());
                }
            }
            catch (Exception ex)
            {
                f.Error = ex.Message;
                // 脚本异常退出以前只写进 f.Error（界面上小字，日志里什么都没有），
                // 排查「脚本没跑起来」时看不出是哪条、为什么。必须在日志里留一行。
                try { MachineModel.Instance.Log("警告", "[lua] " + f.Name + " 异常退出：" + ex.Message); }
                catch { }
                try { App.Log("[lua] " + f.Name + " 异常退出：" + ex.Message); } catch { }
            }
            finally
            {
                lock (f)
                {
                    foreach (var k in f.Held.ToArray()) VarBox.Give(k, 0);
                    f.Held.Clear();
                }
                // 正常退出（running() 变假）不打日志：〔停止演示〕时 31 条一起退会刷屏。
                // 只有「没被要求停却自己退了」才是问题，那种情况记一行。
                if (f.Running && string.IsNullOrEmpty(f.Error))
                {
                    try
                    {
                        MachineModel.Instance.Log("警告",
                            "[lua] " + f.Name + " 脚本自己退出了（最后一处 step：" + (f.Step ?? "") + "）");
                    }
                    catch { }
                    try { App.Log("[lua] " + f.Name + " 脚本自己退出了（最后一处 step：" + (f.Step ?? "") + "）"); }
                    catch { }
                }
                f.Running = false;
                f.Step = "已退出";
            }
        }
    }
}
