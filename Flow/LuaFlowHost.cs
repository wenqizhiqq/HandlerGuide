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
            e["step"] = (Action<string>)(s => { lock (f) f.Step = s; });
            e["log"] = (Action<string>)(s => MachineModel.Instance.Log("信息", "[" + f.Name + "] " + s));
            e["alarm"] = (Action<string>)(s => MachineModel.Instance.Log("警告", "[" + f.Name + "] " + s));

            e["ax"] = (Func<string, int>)(n => MotionManager.Instance.AxisIndex(n));
            e["lasterr"] = (Func<string>)(() => f.Error);
            e["moveabs"] = (Func<string, double, bool>)((n, v) =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(n);
                if (i2 < 0) { f.Error = "轴不在真表: " + n; return false; }
                var r = mm.MoveAbs(i2, v); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["moverel"] = (Func<string, double, bool>)((n, v) =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(n);
                if (i2 < 0) { f.Error = "轴不在真表: " + n; return false; }
                var r = mm.MoveRel(i2, v); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["home"] = (Func<string, bool>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(n);
                if (i2 < 0) return false;
                var r = mm.Home(i2); if (!r.Accepted) f.Error = r.Reason; return r.Accepted;
            });
            e["pos"] = (Func<string, double>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(n);
                return i2 < 0 ? double.NaN : mm.Position(i2);
            });
            e["inpos"] = (Func<string, bool>)(n =>
            {
                var mm = MotionManager.Instance; int i2 = mm.AxisIndex(n);
                return i2 >= 0 && !mm.State(i2).Busy && !mm.State(i2).Alarm;
            });
            e["axiscount"] = (Func<int>)(() => MotionManager.Instance.Axes.Count);

            e["readin"] = (Func<string, bool>)(n =>
            {
                var p2 = MotionManager.Instance.Inputs.Find(x => x.Name == n);
                return p2 != null && MotionManager.Instance.ReadIn(p2.Bit);
            });
            e["writeout"] = (Func<string, bool, bool>)((n, v) =>
            {
                var p2 = MotionManager.Instance.Outputs.Find(x => x.Name == n);
                if (p2 == null) { f.Error = "IO 不在真表: " + n; return false; }
                MotionManager.Instance.WriteOut(p2.Bit, v); return true;
            });
            e["outval"] = (Func<string, bool>)(n =>
            {
                var p2 = MotionManager.Instance.Outputs.Find(x => x.Name == n);
                return p2 != null && MotionManager.Instance.ReadOut(p2.Bit);
            });

            e["boxget"] = (Func<string, double>)(k2 => VarBox.GetD(k2));
            e["boxset"] = (Action<string, double>)((k2, v) => VarBox.SetD(k2, v));
            e["boxinc"] = (Func<string, double, double>)((k2, v) => VarBox.Inc(k2, v));
            e["boxwait"] = (Func<string, double, int>)((k2, want) =>
            {
                for (int n = 0; n < 250 && f.Running && !stopAll; n++)
                {
                    if (VarBox.GetD(k2) == want) return 1;
                    Thread.Sleep(20);
                }
                f.Error = "等变量超时: " + k2; return 0;
            });
            e["stable"] = (Func<string, int, bool>)((n, ms) =>
            {
                int need = ms <= 0 ? 50 : ms, got = 0;
                while (got < need && f.Running && !stopAll)
                {
                    if (!MotionManager.Instance.Inputs.Exists(x => x.Name == n)) return false;
                    if (!readIn(n)) { got = 0; } else got += 20;
                    Thread.Sleep(20);
                }
                return got >= need;
            });
            e["take"] = (Func<string, int, bool>)((k2, o) =>
            {
                bool ok = VarBox.Take(k2, o);
                if (ok) lock (f) if (!f.Held.Contains(k2)) f.Held.Add(k2);
                return ok;
            });
            e["give"] = (Action<string, int>)((k2, o) =>
            {
                VarBox.Give(k2, o);
                lock (f) f.Held.Remove(k2);
            });
            e["held"] = (Func<string[]>)(() => { lock (f) return f.Held.ToArray(); });
            e["point"] = (Func<string, double>)(k2 => VarBox.GetD("点位." + k2));
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
            var t = new Thread(() => Run(f)) { IsBackground = true, Name = "Lua:" + f.Name };
            f.Thread = t;
            t.Start();
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
                    e.DoString(File.ReadAllText(f.Path, Encoding.UTF8), f.Name);
                }
            }
            catch (Exception ex) { f.Error = ex.Message; }
            finally
            {
                lock (f)
                {
                    foreach (var k in f.Held.ToArray()) VarBox.Give(k, 0);
                    f.Held.Clear();
                }
                f.Running = false;
                f.Step = "已退出";
            }
        }
    }
}
