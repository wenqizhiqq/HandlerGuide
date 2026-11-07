// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
// ============================================================================
//  App.xaml.cs — 应用程序入口
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件界面复刻 · 苹果风格）
//  职责：WPF 应用启动，应用资源/字典加载，未做额外逻辑。
// ============================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using HandlerGuide.ViewModels;   // Ops / OpsForm（温启志 18719361399）

namespace HandlerGuide
{
    public partial class App : Application
    {
        /// <summary>把 WPF 数据绑定告警写入同一个日志文件。</summary>
        private sealed class DelegateTraceListener : TraceListener
        {
            public override void Write(string message) { }
            public override void WriteLine(string message)
            {
                // 绑定告警必须计数：页面 XAML 解析失败会被绑定引擎吞掉，
                // 只表现为“Content 取不到值”，此时 bad 仍是 0，界面却是空白。
                _bindWarnings++;
                Log("BIND " + message);
            }
        }

        /// <summary>自检期间累计的绑定告警条数（&gt;0 说明有页面没能真正渲染出来）。</summary>
        private static int _bindWarnings;

        internal static string LogPath => Path.Combine(Path.GetTempPath(), "handlerguide.log");

        /// <summary>作者水印（温启志 18719361399）。</summary>
        internal const string WenQiZhiAuthor = ViewModels.Owner.WenQiZhiMark;

        internal static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, ex) =>
            {
                Log("DispatcherUnhandledException: " + ex.Exception);
                ex.Handled = false;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Log("UnhandledException: " + ex.ExceptionObject);

            if (Environment.GetEnvironmentVariable("HG_SELFTEST") == "1")
            {
                SelfTest();
                Shutdown();
                return;
            }

            base.OnStartup(e);
            // 退出时兜底保存参数（自动保存的防丢网：万一 800ms 防抖还没触发）
            this.Exit += (s, ex) => { try { MachineModel.Instance.Save(); } catch { } };
        }

        /// <summary>把窗口内容渲染成 PNG（开发期自检用）。</summary>
        private static void Shoot(Window win, string path)
        {
            try
            {
                int w = (int)Math.Max(1, win.ActualWidth);
                int h = (int)Math.Max(1, win.ActualHeight);
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(win);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using (var fs = File.Create(path)) enc.Save(fs);
            }
            catch (Exception ex)
            {
                Log("SHOT FAIL " + path + " : " + ex.Message);
            }
        }

        /// <summary>HG_SELFTEST=1 时逐页渲染并记录异常与绑定错误（开发期自检）。</summary>
        private void SelfTest()
        {
            try
            {
                File.WriteAllText(LogPath, "");

                // 自检里会「关掉一个窗口再开一个新窗口」。
                // 默认 ShutdownMode 是 OnLastWindowClose：关掉唯一的窗口会立刻启动
                // Application.Shutdown()，于是 Application.Current 变成 null，
                // 之后任何 Palette.R(...) 都会 NullReferenceException。
                // 自检必须改成显式关闭，由 OnStartup 末尾的 Shutdown() 收尾。
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                PresentationTraceSources.Refresh();
                var bind = PresentationTraceSources.DataBindingSource;
                bind.Listeners.Add(new DelegateTraceListener());
                bind.Switch.Level = SourceLevels.Warning;
                Trace.AutoFlush = true;
                _bindWarnings = 0;

                Log("selftest start");
            Log("版权所有 " + ViewModels.Owner.WenQiZhiCopyright);
                var win = new Views.MainWindow();
                win.Show();
                win.UpdateLayout();
                var vm = win.Vm;

                string shotDir = Environment.GetEnvironmentVariable("HG_SHOT");
                if (!string.IsNullOrEmpty(shotDir))
                {
                    try { Directory.CreateDirectory(shotDir); } catch { shotDir = null; }
                }

                int ok = 0, bad = 0;
                foreach (var p in vm.AllPages)
                {
                    try
                    {
                        // 显式取一次 Content：XAML 解析异常会在这里抛出并被计入 bad，
                        // 否则它会被 ContentControl 的绑定吞掉，只留一条 BIND 告警。
                        var content = p.Content;
                        if (content == null) throw new InvalidOperationException("页面内容为空（构建返回 null）");
                        vm.Navigate(p);
                        win.UpdateLayout();
                        if (shotDir != null) Shoot(win, Path.Combine(shotDir, p.Id + ".png"));
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        bad++;
                        Log("PAGE FAIL " + p.Id + " : " + ex.GetType().Name + " " + ex.Message);
                    }
                }
                Log("selftest done ok=" + ok + " bad=" + bad + " bind=" + _bindWarnings);
                Trace.Flush();
                win.Close();

                Ops.DialogsSuppressed = true;   // 自检无人值守，禁止弹模态对话框
                LayoutSelfTest(shotDir);
                MotionSelfTest();
                OpsSelfTest();
            }
            catch (Exception ex)
            {
                Log("selftest crash: " + ex);
            }
        }

        /// <summary>
        /// 平面仿真页「动画」自检。
        /// 日志全绿并不代表界面在动：本方法连续泵 4 秒仿真节拍（80 帧），
        /// 在 t≈1s 与 t≈4s 各截一张图，然后逐像素比较。
        /// 差异为 0 就说明画面根本没变化 —— 即使 ok=51 bad=0 也算失败。
        /// </summary>
        private static void LayoutSelfTest(string shotDir)
        {
            try
            {
                var m = Motion.MotionManager.Instance;
                m.ResetEStop();
                m.ClearAlarm();

                var win = new Views.MainWindow();
                win.Show();
                win.UpdateLayout();
                var vm = win.Vm;

                ViewModels.PageVm page = null;
                foreach (var p in vm.AllPages)
                {
                    if (p.Id == "motion_layout") { page = p; break; }
                }
                if (page == null)
                {
                    Log("layout selftest: 找不到 motion_layout 页面");
                    win.Close();
                    return;
                }

                // 显式构建一次：XAML/构建异常必须计入失败，不能被绑定吞掉。
                var content = page.Content;
                if (content == null) throw new InvalidOperationException("motion_layout 内容为空");
                vm.Navigate(page);
                win.UpdateLayout();

                string a = null, b = null;
                if (!string.IsNullOrEmpty(shotDir))
                {
                    a = Path.Combine(shotDir, "_layout_t1.png");
                    b = Path.Combine(shotDir, "_layout_t4.png");
                }

                // 包含性守卫：料盘是循环流动的，单看一帧抓不住越界，所以每帧都跑一遍，
                // 把越界描述去重收集起来（同一个箱体在不同相位可能报出不同的越界量）。
                var guardHits = new System.Collections.Generic.HashSet<string>();
                var guardCanvas = ViewModels.LayoutPages.LastLayoutCanvas;

                // 需求 43 · 可选钩子：HG_DEMO=1 时先启动演示节拍再渲染 80 帧 ——
                // 布局页动画只在 AutoCycle 为真时推进（需求 41 起全部门控），
                // 默认自检全程静态（pixelDiff=0 属预期）；设了 HG_DEMO 才能拍到
                // 剧本中间态（抓盘 / 皮带滚动 / 塔顶升），pixelDiff 也恢复诊断意义。
                // 循环结束后必须 StopDemo 还原，否则演示会泄漏进后面的 ops 自检。
                bool demoStarted = Environment.GetEnvironmentVariable("HG_DEMO") == "1";
                if (demoStarted) m.StartDemo();

                // HG_DEMO 时拍 slot 2 剧本序列：需求 43 起 HG_DEMO 下 Draw 的 dt 固定
                // 0.05（见 LayoutPages.cs），剧本事件在「动画时间轴」上逐帧确定、
                // 可复现 —— 触发瞬间由 req43 grab/drop 日志行记录（权威证据），
                // 序列截图只作辅助目检。注意 Draw 在每次 FastTick 的布局/泵帧里
                // 会被调用多次，动画时间 ≠ 循环帧号 × 0.05，按帧号定位事件不可靠。
                // 只跑到帧 491：曾试过延长到帧 900 想抓第 2 轮剧本（已完成验证，
                // 存证 evidence_c2_drop_tower2.png），但真轴演示节拍也随之多跑 410 拍，
                // StopDemo 后遗留的轴位置会让后续 motion/ops 自检顶上软限位误报 ——
                // 轮换回归的常驻哨兵是 artifacts/req41_verify.py 的 C3 断言。
                // 抓拍窗口可由 HG_SEQLO / HG_SEQHI 覆盖（默认 330..470 沿用需求 43 的
                // slot 2 剧本序列）；需求 48 要看取放臂挂料中间态，那些事件发生在更早的帧。
                int seqLo = int.TryParse(Environment.GetEnvironmentVariable("HG_SEQLO"), out int slo) ? slo : 330;
                int seqHi = int.TryParse(Environment.GetEnvironmentVariable("HG_SEQHI"), out int shi) ? shi : 470;
                int seqStep = int.TryParse(Environment.GetEnvironmentVariable("HG_SEQSTEP"), out int sst) ? sst : 2;
                int frames = 0, frameMax = demoStarted ? Math.Max(1000, seqHi + 60) : 80;
                while (frames < frameMax)
                {
                    vm.FastTick(0.05);
                    win.UpdateLayout();
                    // 关键：数据绑定更新排在 DispatcherPriority.DataBind 上，
                    // UpdateLayout() 只做 Measure/Arrange，不会刷新绑定。
                    // 必须显式把消息队列泵到 Background，否则截图拿到的永远是旧画面
                    // （之前这里 pixelDiff=0，看起来「动画坏了」，其实是探针读到了旧视觉树）。
                    win.Dispatcher.Invoke(() => { },
                        System.Windows.Threading.DispatcherPriority.Background);
                    if (frames == 19 && a != null) Shoot(win, a);
                    if (frames == 79 && b != null) Shoot(win, b);
                    if (demoStarted && frames >= seqLo && frames <= seqHi && frames % seqStep == 0)
                        Shoot(win, Path.Combine(shotDir, "_demo_seq_" + frames + ".png"));
                    if (demoStarted && frames > Math.Max(seqHi, 470) + 20) break;
                    frames++;
                    if (guardCanvas != null)
                        foreach (var g in guardCanvas.CheckGuards()) guardHits.Add(g);
                }

                if (guardCanvas == null)
                    Log("layout guard: 未拿到画布（LastLayoutCanvas 为空），跳过包含性校验");
                else if (guardHits.Count == 0)
                    Log("layout guard: 合格 guards=" + guardCanvas.Guards.Count + " violations=0");
                else
                {
                    Log("layout guard: 越界 guards=" + guardCanvas.Guards.Count
                        + " violations=" + guardHits.Count);
                    int shown = 0;
                    foreach (var g in guardHits)
                    {
                        Log("  ! " + g);
                        if (++shown >= 5) break;
                    }
                }

                long diff = -1;
                if (a != null && b != null) diff = ComparePng(a, b);

                Log("layout selftest: frames=" + frames + " demoCycles=" + m.DemoCycles
                    + " step=" + m.DemoStepText
                    + " upArmX=" + m.Position(m.AxisIndex("上料X轴")).ToString("0.0")
                    + " downArmX=" + m.Position(m.AxisIndex("下料X轴")).ToString("0.0")
                    + " carA=" + m.Position(m.AxisIndex("前梭车X轴")).ToString("0.0")
                    + " carB=" + m.Position(m.AxisIndex("后梭车X轴")).ToString("0.0")
                    + " pressZ=" + m.Position(m.AxisIndex("前压头Z轴")).ToString("0.0")
                    + " gunY=" + m.Position(m.AxisIndex("上料Y轴")).ToString("0.0")
                    + " heat=" + m.ReadOut(m.OutBitOf("加热使能"))
                    + " pixelDiff=" + diff
                    + (diff > 0 ? " （动画正常）"
                       : diff == 0 ? " （警告：画面没有变化）"
                       : " （未开启截图，跳过像素比较）"));

                if (demoStarted) m.StopDemo();     // 还原现场，别泄漏进 ops 自检
                win.Close();
            }
            catch (Exception ex)
            {
                Log("layout selftest crash: " + ex.Message);
            }
        }

        /// <summary>逐像素比较两张 PNG，返回不同的像素个数（-1 = 尺寸不一致）。</summary>
        private static long ComparePng(string pa, string pb)
        {
            try
            {
                if (!File.Exists(pa) || !File.Exists(pb)) return -1;
                var da = new System.Windows.Media.Imaging.PngBitmapDecoder(
                    new Uri(pa), System.Windows.Media.Imaging.BitmapCreateOptions.None,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var db = new System.Windows.Media.Imaging.PngBitmapDecoder(
                    new Uri(pb), System.Windows.Media.Imaging.BitmapCreateOptions.None,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var fa = da.Frames[0];
                var fb = db.Frames[0];
                int w = fa.PixelWidth, h = fa.PixelHeight;
                if (w != fb.PixelWidth || h != fb.PixelHeight) return -1;
                int stride = w * 4;
                var ba = new byte[stride * h];
                var bb = new byte[stride * h];
                fa.CopyPixels(ba, stride, 0);
                fb.CopyPixels(bb, stride, 0);
                long diff = 0;
                for (int i = 0; i + 2 < ba.Length; i += 4)
                {
                    if (ba[i] != bb[i] || ba[i + 1] != bb[i + 1] || ba[i + 2] != bb[i + 2]) diff++;
                }
                return diff;
            }
            catch (Exception ex)
            {
                Log("ComparePng fail: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// 运动控制无头自检：驱动仿真卡跑完整演示节拍，校验
        /// 「物理积分 / 软限位不越界 / 共享导轨间隙 / 互锁拒绝计数」。
        /// </summary>
        /// <summary>
        /// Ops 命令通路自检（温启志 18719361399）。
        ///
        /// 为什么需要它：界面全绿只证明「页面能渲染」，不证明「按钮真的有用」。
        /// 本方法把每个按钮背后的 Ops 方法都真调一遍，并断言状态按预期变化，
        /// 任何一条抛异常或状态没动都记 OPS FAIL —— 这样才不会出现
        /// 「点了应用按钮，其实什么都没发生」这种最坑人的情况。
        /// </summary>
        private static void OpsSelfTest()
        {
            int ok = 0, bad = 0;
            var fails = new System.Collections.Generic.List<string>();

            void Chk(string name, Action act, Func<bool> expect = null)
            {
                try
                {
                    act();
                    if (expect != null && !expect())
                    {
                        bad++;
                        fails.Add(name + " : 状态未按预期变化");
                        return;
                    }
                    ok++;
                }
                catch (Exception ex)
                {
                    bad++;
                    fails.Add(name + " : " + ex.GetType().Name + " " + ex.Message);
                }
            }

            void Pump(int frames, double dt = 0.05)
            {
                for (int i = 0; i < frames; i++)
                {
                    Motion.MotionManager.Instance.Tick(dt);
                    Ops.TickHome(dt);
                    Ops.TickTemp(dt);
                    Ops.TickTestArmCalibration(dt);
                    Ops.TickManualPulse(dt);
                    Ops.TickTestIoPulse(dt);
                }
            }

            // 流程线程是后台线程，它们按墙钟 Sleep(20ms) 轮询，而仿真位置只有 Tick 才推进 ——
            // 所以自检里给流程计时必须「边泵边等」：每轮泵 5 帧再让出 5ms 墙钟。
            // 返回值 = 累计仿真秒数（与墙钟秒分开报，两个口径都给）。
            double PumpWall(double wallSec, double dt = 0.02, int frames = 5)
            {
                double sim = 0;
                var w = System.Diagnostics.Stopwatch.StartNew();
                while (w.Elapsed.TotalSeconds < wallSec)
                {
                    Pump(frames, dt);
                    sim += frames * dt;
                    System.Threading.Thread.Sleep(5);
                }
                return sim;
            }

            try
            {
                var mm = Motion.MotionManager.Instance;
                var M = MachineModel.Instance;

                // ---------- 0. 前置：复位到干净状态 ----------
                Chk("ResetEmergency", Ops.ResetEmergency);
                Chk("ClearAlarm", Ops.ClearAlarm);
                Chk("ClearBusAlarm", Ops.ClearBusAlarm);
                Chk("StopAllAxes", Ops.StopAllAxes);
                Chk("UseSimCard", Ops.UseSimCard);
                Chk("ConnectCard", Ops.ConnectCard, () => mm.Connected);

                // ---------- 1. 伺服 / IO ----------
                Chk("ServoAll(on)", () => Ops.ServoAll(true), () => Ops.ServoOnCount > 0);
                Chk("ServoAxis(0,on)", () => Ops.ServoAxis(0, true));
                Chk("SetOut(0,true)", () => Ops.SetOut(0, true), () => mm.ReadOut(0));
                Chk("ToggleOut(0)", () => Ops.ToggleOut(0), () => !mm.ReadOut(0));
                // 注意：IN0 是「急停按钮」输入。这里必须点两次复原，
                // 否则后面所有运动都会被 SafetyCheck 以「急停按钮处于按下状态」拒绝。
                Chk("ToggleInput(0) 再复原", () => { Ops.ToggleInput(0); Ops.ToggleInput(0); });
                Chk("ResetOutputs", Ops.ResetOutputs, () => !mm.ReadOut(0));

                // ---------- 2. 归零队列 ----------
                Chk("HomeAll", () => Ops.HomeAll(), () => Ops.HomeActive);
                Pump(600);
                Chk("HomeAll 完成", () => { }, () => !Ops.HomeActive);
                // 队列清空不等于归零成功（被互锁拒绝也会清空），所以要用 Homed 位判定
                Chk("HomeAll 真的归零了", () => { }, () => mm.Axes.All(a => mm.State(a.Index).Homed));
                // 自检里这几处要用一根「长行程水平轴」：真表 0 号轴是 前压头Z轴（0~25），
                // 拿它做 100 / 160mm 定位会被软限位直接拒绝，所以统一走 前压头Y轴（0~320）。
                int axH = mm.AxisIndex("前压头Y轴");
                Chk("HomeAll 后位置=回零位", () => { },
                    () => Math.Abs(mm.Position(axH) - mm.Cfg(axH).HomeMm) < 1.0);
                // 单轴归零直接走 Mm.Home()，不经过 Ops 的归零队列 —— 所以判返回是否被接受
                Chk("HomeAxisByName", () =>
                {
                    if (!Ops.HomeAxisByName("料盘传送臂")) throw new Exception("单轴归零被拒绝");
                });
                Chk("HomeAxis(前压头Y轴)", () =>
                {
                    if (!Ops.HomeAxis(axH)) throw new Exception("单轴归零被拒绝");
                });
                Pump(400);
                Chk("单轴归零后已回零位", () => { },
                    () => Math.Abs(mm.Position(axH) - mm.Cfg(axH).HomeMm) < 1.0);

                // ---------- 3. 轴运动 ----------
                // 先离开回零位，否则 Jog 的位移可能被软限位吃掉
                mm.MoveAbs(axH, 160);
                Pump(200);
                double p0 = mm.Position(axH);
                Chk("JogAxis(+1)", () => Ops.JogAxis(axH, 1));
                Pump(20);
                Chk("JogAxis 生效", () => { }, () => Math.Abs(mm.Position(axH) - p0) > 0.001);
                Chk("StopAxis", () => Ops.StopAxis(axH));
                Chk("MoveAxisTo(100)", () => Ops.MoveAxisTo(axH, 100));
                Pump(200);
                Chk("MoveAxisTo 到位", () => { }, () => Math.Abs(mm.Position(axH) - 100) < 1.0);
                Chk("MoveAxisByName", () => Ops.MoveAxisByName("测试位移动Y1", 50));
                Pump(200);
                Chk("JogAxisByName", () => Ops.JogAxisByName("测试位下压Z1", 1));
                Pump(10);
                Chk("StopAxisByName", () => Ops.StopAxisByName("测试位下压Z1"));
                Chk("JogAxisBy", () => Ops.JogAxisBy(axH, -5));
                Pump(10);
                Chk("JogStep 读写", () => { Ops.JogStep = 2.5; }, () => Math.Abs(Ops.JogStep - 2.5) < 1e-9);

                // ---------- 4. 工作基点 / 轴名映射 ----------
                Chk("AxisNameForKey(mb_y0_)", () => { }, () => Ops.AxisNameForKey("mb_y0_car") == "测试位移动Y1");
                Chk("AxisNameForKey(mb_ld_z3_1)", () => { }, () => Ops.AxisNameForKey("mb_ld_z3_1") == "上料取放臂Z3轴");
                Chk("AxisNameForKey(mb_car_in_1)", () => { }, () => Ops.AxisNameForKey("mb_car_in_1") == "测试运料小车B");
                Chk("AxisNameForKey(mb_ta_Safe)", () => { }, () => Ops.AxisNameForKey("mb_ta_Safe") == "料盘传送臂");
                Chk("CapturePosTo(mb_y0_car)", () => Ops.CapturePosTo("mb_y0_car"), () => M.Nums.ContainsKey("mb_y0_car"));
                Chk("CapturePosTo 未绑定键", () => Ops.CapturePosTo("no_such_key_at_all"), () => true);
                Chk("ApplyBasePoints", Ops.ApplyBasePoints);
                Chk("CaptureArmXY(上料)", () => Ops.CaptureArmXY(true), () => M.Nums.ContainsKey("mb_ld_cx"));
                Chk("ApplyArmXYChange(上料)", () => Ops.ApplyArmXYChange(true));
                Chk("ArmZPos(Top)", () => Ops.ArmZPos(true, 0, true));
                Chk("ArmZAll(TZ)", () => Ops.ArmZAll(false, false));
                Chk("FindAxis", () => { }, () => Ops.FindAxis("测试位下压Z2") != null);

                // ---------- 5. 生产节拍 ----------
                Chk("StartProduction", () => Ops.StartProduction(), () => Ops.IsRunning);
                Chk("CanShutdown 运行中必须为 false", () => { }, () => !Ops.CanShutdown(out _));
                Pump(3000);          // 150 s 仿真：足够跑完多个循环
                Chk("产量累加", () => { }, () => M.TotalCount > 0);
                Chk("良率已计算", () => { }, () => M.Yield > 0);
                Chk("PauseProduction", () => Ops.PauseProduction(), () => Ops.IsPaused);
                Pump(100);
                Chk("ResumeProduction", () => Ops.ResumeProduction(), () => Ops.IsRunning);
                Chk("SingleStep", Ops.SingleStep);
                Pump(100);
                Chk("StopProduction", () => Ops.StopProduction(), () => Ops.IsIdle);
                Chk("RefreshTimingReadouts", Ops.RefreshTimingReadouts, () => M.UPH > 0);
                Chk("ResetCounters", Ops.ResetCounters, () => M.TotalCount == 0);

                // ---------- 6. 急停 ----------
                Chk("EmergencyStop", Ops.EmergencyStop, () => mm.EStop);
                Chk("StartProduction 被急停拦下", () => { }, () => !Ops.StartProduction());
                Chk("ResetEmergency", Ops.ResetEmergency, () => !mm.EStop);

                // ---------- 7. 温控闭环 ----------
                // 温控闭环自检。
                // 注意：前面的节拍泵已经累计泵了 200 秒以上，实际温度早被带到设定值附近，
                // 直接测「升温」测不出变化。所以先用「目标 < 实际」这条确定性路径验证降温/断电，
                // 再切高温验证升温/吸合。
                M.SetChoice("tc_mode", 2);                     // 低温档：目标 10℃（远低于实际）
                Chk("ApplyTemp(低温)", Ops.ApplyTemp, () => M.TempSet < 25);
                double t0 = Ops.TempActual;
                Pump(200);
                Chk("目标低于实际 → 加热器断开", () => { }, () => !Ops.HeaterOn);
                Chk("加热器断开后温度回落", () => { }, () => Ops.TempActual < t0);
                Chk("温度被夹在 20~120℃", () => { }, () => Ops.TempActual >= 20 && Ops.TempActual <= 120);

                M.SetChoice("tc_mode", 1);                     // 高温档：目标 50℃
                Chk("ApplyTemp(高温)", Ops.ApplyTemp, () => M.TempSet >= 45);
                double t1 = Ops.TempActual;
                Pump(200);
                Chk("目标高于实际 → 温度上升", () => { }, () => Ops.TempActual > t1);
                Chk("加热输出已吸合(OUT15)", () => { }, () => Ops.HeaterOn);
                Chk("ApplyTempControl", Ops.ApplyTempControl, () => M.TempSet > 0);
                Chk("ApplyTempRecipe", Ops.ApplyTempRecipe);

                // ---------- 8. 测试臂 / 测试接口 ----------
                // 急停会把伺服全部断使能，校准前置条件要求伺服已使能
                Chk("ServoAll(on) 复位后重新使能", () => Ops.ServoAll(true), () => Ops.ServoOnCount > 0);
                Chk("StartTestArmCalibration", Ops.StartTestArmCalibration, () => Ops.TestArmCalibrating);
                Pump(600);
                Chk("校准自动结束", () => { }, () => !Ops.TestArmCalibrating);
                Chk("TestArmStep", Ops.TestArmStep);
                Chk("ManualTestTrigger", Ops.ManualTestTrigger);
                Pump(5);
                Chk("TickManualPulse 回落", () => { }, () => !mm.ReadOut(Ops.OutBit("测试机启动")));
                Chk("ExitTestArmCalibration", Ops.ExitTestArmCalibration);
                Chk("TestIo_Sot", Ops.TestIo_Sot);
                Pump(5);
                Chk("TickTestIoPulse 回落", () => { }, () => !mm.ReadOut(Ops.OutBit("测试机启动")));
                Chk("TestIo_Send", () => Ops.TestIo_Send("SELFTEST"), () => Ops.TestIoRx.Contains("SELFTEST"));
                Chk("TestIo_TestInfo", Ops.TestIo_TestInfo, () => Ops.TestIoRx.Contains("测试系统信息"));

                // ---------- 9. Bin / 视觉 / 清洁 ----------
                Chk("ApplyBinYieldCtrl", Ops.ApplyBinYieldCtrl);
                Chk("ToggleBinYieldCtrl", Ops.ToggleBinYieldCtrl);
                Chk("ToggleBinYieldCtrl 复原", Ops.ToggleBinYieldCtrl);
                M.SetToggle("bv_head0", true);
                Chk("VisionMoveToCapturePos", Ops.VisionMoveToCapturePos);
                Chk("DefaultContactForce", Ops.DefaultContactForce, () => M.GetNum("ta_force") > 0);
                Chk("RestoreCleanDefault", Ops.RestoreCleanDefault);
                Chk("ApplyCleanOffset", Ops.ApplyCleanOffset);
                Chk("ResetCleanCounters", Ops.ResetCleanCounters, () => M.GetNum("cp_counter") == 0);

                // ---------- 10. 配方文件 ----------
                string rec = "SELFTEST_RECIPE";
                Chk("NewRecipe", () => Ops.NewRecipe(rec));
                Chk("SaveRecipeAs", () => Ops.SaveRecipeAs(rec));
                Chk("LoadRecipe", () => Ops.LoadRecipe(rec));
                Chk("ApplyRecipe", () => Ops.ApplyRecipe(rec));
                Chk("CopyRecipe", () => Ops.CopyRecipe(rec, rec + "_C"));
                Chk("RenameRecipe", () => Ops.RenameRecipe(rec + "_C", rec + "_R"));
                Chk("DeleteRecipe(副本)", () => Ops.DeleteRecipe(rec + "_R"));
                Chk("SelectRecipe", () => Ops.SelectRecipe(rec), () => M.Recipe == rec);
                Chk("DeleteRecipe(本体)", () => Ops.DeleteRecipe(rec));
                Chk("SyncRecipeListFromDisk", Ops.SyncRecipeListFromDisk);
                Chk("ExportRecipeTo", () =>
                {
                    string p = Path.Combine(Path.GetTempPath(), "hg_ops_selftest_recipe.json");
                    Ops.ExportRecipeTo(M.Recipe, p);
                    if (!File.Exists(p)) throw new IOException("导出文件不存在：" + p);
                });
                Chk("ImportRecipeFrom", () =>
                {
                    string p = Path.Combine(Path.GetTempPath(), "hg_ops_selftest_recipe.json");
                    if (File.Exists(p)) Ops.ImportRecipeFrom(p);
                });
                Chk("CopyTrayType", () => Ops.CopyTrayType(2, 1), () => M.Nums.ContainsKey("tp_nx_2"));
                Chk("ApplyTrayDefine", Ops.ApplyTrayDefine);
                Chk("SelectSiteLayout", () => Ops.SelectSiteLayout("Sixteen-Sites_A"),
                    () => M.SiteLayoutName == "Sixteen-Sites_A");

                // ---------- 11. 参数持久化 ----------
                Chk("SaveParams", Ops.SaveParams, () => File.Exists(Ops.ParamsPath));
                Chk("SnapshotJson", () => { }, () => Ops.SnapshotJson().Length > 10);
                Chk("LoadParams", Ops.LoadParams);
                Chk("RestoreJson", () => Ops.RestoreJson(Ops.SnapshotJson()));
                Chk("ApplyAllAxisConfigs", Ops.ApplyAllAxisConfigs);
                Chk("PullAxisConfigsToModel", Ops.PullAxisConfigsToModel);

                // ---------- 11b. 界面状态：最后显示的页面（温启志 18719361399） ----------
                // 需求：开机要「主动」回到用户上次停留的那一页，导航栏按钮高亮跟着还原。
                // 这里只校验 UiState 的落盘 / 回读 / 文件结构，**不动导航** ——
                // 自检走到这一步主窗口已经 Close()，再 Navigate 会去构建页面内容，
                // 平白扰动后面的检查（导航侧由 MainViewModel.Navigate + FindPageById 收口）。
                // 测完把原值写回去，免得自检把「开机页面」永久改成 motion_layout。
                string uiBackup = UiState.LastPageId;
                Chk("UiState.Save", () => UiState.LastPageId = "motion_layout",
                    () => UiState.LastPageId == "motion_layout");
                Chk("UiState.File", () => { }, () => File.Exists(UiState.FilePath));
                Chk("UiState.FileShape", () => { },
                    () => File.ReadAllText(UiState.FilePath).Contains("\"LastPage\""));
                Chk("UiState.IdIsRealPage", () => { }, () =>
                {
                    var mv = MainViewModel.Current;
                    return mv != null && mv.AllPages.Any(pg => pg.Id == UiState.LastPageId);
                });
                Chk("UiState.Restore", () => UiState.LastPageId = uiBackup,
                    () => UiState.LastPageId == uiBackup);

                // ---------- 12. 批次 / 用户 ----------
                Chk("OpenBatch", () => Ops.OpenBatch("MO-SELFTEST", "SUB-1", "Tester"), () => M.BatchOpen);
                Chk("CloseBatch", Ops.CloseBatch, () => !M.BatchOpen);
                Chk("ZeroStats", Ops.ZeroStats, () => M.TotalCount == 0);
                Chk("LoadUsers", Ops.LoadUsers, () => Ops.UserCount >= 3);
                M.SetText("mu_user", "Tester");
                M.SetText("mu_pass", "999");
                M.SetChoice("mu_level", 1);
                Chk("AddOrUpdateUser", Ops.AddOrUpdateUser, () => Ops.UserCount >= 4);
                Chk("SetPrivilegeLevel", Ops.SetPrivilegeLevel);
                M.SetText("mu_user", "Tester");
                Chk("DeleteUser", Ops.DeleteUser, () => Ops.UserCount >= 3);
                Chk("ApplyUsers", Ops.ApplyUsers);
                Chk("UserCell", () => { }, () => !string.IsNullOrEmpty(Ops.UserCell(0, 0)));

                // ---------- 13. 工具 / 主界面 / 控制板 ----------
                Chk("ApplyWorkMode", Ops.ApplyWorkMode);
                Chk("ApplyEnable", Ops.ApplyEnable);
                Chk("ApplyLightSetting", Ops.ApplyLightSetting);
                Chk("CheckChannelMap(auto)", () => Ops.CheckChannelMap(true));
                Chk("CheckChannelMap(manual)", () => Ops.CheckChannelMap(false));
                Chk("RunClean(auto)", () => Ops.RunClean(true));
                Chk("RunClean(manual)", () => Ops.RunClean(false));
                Chk("DisassembleArm", Ops.DisassembleArm);
                Chk("DisassembleArm 后轴在动 → 禁止关机", () => { }, () => !Ops.CanShutdown(out _));
                Pump(600);           // 等机械走完
                Chk("运动结束后允许关机", () => { }, () => Ops.CanShutdown(out _));
                Chk("SnapMachineState", Ops.SnapMachineState, () => Directory.GetFiles(Ops.DataDir, "snap_*.log").Length > 0);
                Chk("RefreshAlarmStats", Ops.RefreshAlarmStats);
                Chk("UpdateLog", Ops.UpdateLog);
                Chk("LogCell", () => { }, () => Ops.LogCell(0, 1) != null);
                Chk("LogRowCount", () => { }, () => Ops.LogRowCount >= 0);
                Chk("CanShutdown 空闲为 true", () => { }, () => Ops.CanShutdown(out _));
                Chk("ConfirmHome", Ops.ConfirmHome, () => Ops.HomeActive);
                Chk("CancelHome(2)", Ops.CancelHome, () => !Ops.HomeActive);

                // ---------- 14. 臂 Go / 传送臂 ----------
                Chk("ArmGo(上料)", () => Ops.ArmGo(true, 2, 0, 100, 100, 5));
                Pump(400);
                Chk("TrayArmGo", () => Ops.TrayArmGo("Safe", -722.99));
                Pump(400);
                Chk("TestArmPos", () => Ops.TestArmPos("测试位下压Z1", 0));
                Pump(100);

                // ---------- 15. 表单（按前缀自动覆盖） ----------
                Chk("PageForm+Apply", () =>
                {
                    var f = Ops.PageForm("自检表单", "ta_", "tp_");
                    f.Snapshot();
                    f.Apply();
                    f.Restore();
                });
                Chk("Form+字段", () =>
                {
                    var f = Ops.Form("自检字段")
                             .Num("st_door_t", 30, 0, 600, "开门处理时间")
                             .Chk("st_skt_empty", true, "测试座空检查")
                             .Text("sf_ftp_ip", "192.168.0.100", "FTP 地址");
                    f.Snapshot();
                    f.Apply();
                    f.Restore();
                });
                // ta_drop 在《品种 → 测试臂》里声明了区间 [-15, 0]
                // XAML 页面（Pages/*.xaml 覆盖了同名 C# 构建函数）的按页表单：
                // 页面渲染阶段应该已经通过 PageVm.MakeContent → Ops.PageFormById 建好表单，
                // 并在页面构建完成后由 FreezePageForms() 记下初值。这里逐个验证。
                Chk("XAML 页表单全部已冻初值", () => { }, () => Ops.PageFormsNotSnapped().Count == 0);
                foreach (var pid in new[]
                {
                    "enable", "inforec", "maint_light", "maint_toffset", "mode", "motorhome", "tempctl",
                    "unit_apply", "unit_arm", "unit_cart", "unit_io", "unit_offset",
                    "unit_speed", "unit_testarm", "unit_transfer"
                })
                {
                    string page = pid;
                    Chk("ApplyPageForm(" + page + ")", () => Ops.ApplyPageForm(page));
                    Chk("RestorePageForm(" + page + ")", () => Ops.RestorePageForm(page));
                }
                Chk("未知页表单被拒绝", () => Ops.ApplyPageForm("no_such_page"), () => true);

                Chk("TryRange(ta_drop)", () => { }, () => Ops.TryRange("ta_drop", out var rlo, out var rhi)
                                                            && Math.Abs(rlo + 15) < 1e-9 && Math.Abs(rhi) < 1e-9);
                Chk("TryRange(未声明键)", () => { }, () => !Ops.TryRange("no_range_key", out _, out _));
                Chk("CountKeys", () => { }, () => Ops.CountKeys("ta_") >= 0);

                // ---------- 16. 日志 ----------
                Chk("ExportLog", () => { }, () => !string.IsNullOrEmpty(Ops.ExportLog()));
                Chk("ExportLogTo", () =>
                {
                    string p = Path.Combine(Path.GetTempPath(), "hg_ops_selftest_log.txt");
                    Ops.ExportLogTo(p);
                    if (!File.Exists(p)) throw new IOException("导出文件不存在：" + p);
                });

                // ---------- 17. 偏移设定：13 个区域各自一套偏移（温启志 18719361399） ----------
                Chk("SelectOffsetRegion 越界被拒", () => { }, () => !Ops.SelectOffsetRegion(0) && !Ops.SelectOffsetRegion(99));
                Chk("SelectOffsetRegion(#1 已在该区)", () => { }, () => !Ops.SelectOffsetRegion(1));
                Chk("SelectOffsetRegion(#5 切换成功)", () => Ops.SelectOffsetRegion(5), () => Ops.CurrentOffsetRegion == 5);
                Chk("区域名映射", () => { }, () => Ops.OffsetRegionName(5) == "加热盘2" && Ops.OffsetRegionName(13) == "固定盘3");
                Chk("切换后区域文本已更新", () => { }, () => M.GetText("uo_region") == "#5-加热盘2");
                Chk("在 #5 改值后再切走", () =>
                {
                    M.SetNum("uo_x", 1.25);
                    M.SetNum("uo_y", -0.75);
                    M.SetNum("uo_place", -3.5);
                    M.SetNum("uo_pick", 0.4);
                    Ops.SelectOffsetRegion(9);            // 存 #5、载入 #9
                }, () => Ops.CurrentOffsetRegion == 9);
                Chk("#9 是首次访问 → 载入出厂缺省", () => { },
                    () => Math.Abs(M.GetNum("uo_x", 999)) < 1e-9
                       && Math.Abs(M.GetNum("uo_place", 999) + 2.0) < 1e-9);
                Chk("#5 的改动被保存下来了", () => { },
                    () => Ops.OffsetRegionSnapshot(5) != null
                       && Math.Abs(Ops.OffsetRegionSnapshot(5)[0] - 1.25) < 1e-9
                       && Math.Abs(Ops.OffsetRegionSnapshot(5)[2] + 3.5) < 1e-9);
                Chk("切回 #5 能取回刚才的值", () => Ops.SelectOffsetRegion(5),
                    () => Math.Abs(M.GetNum("uo_x", 0) - 1.25) < 1e-9
                       && Math.Abs(M.GetNum("uo_y", 0) + 0.75) < 1e-9
                       && Math.Abs(M.GetNum("uo_place", 0) + 3.5) < 1e-9);
                Chk("区域数 = 13", () => { }, () => Ops.OffsetRegionCount == 13);
                Chk("ResetAllOffsetRegions", () => Ops.ResetAllOffsetRegions(),
                    () => Ops.CurrentOffsetRegion == 1 && Ops.OffsetRegionSnapshot(5) == null);
                Chk("恢复 unit_offset 页会连带清区域", () => Ops.RestorePageForm("unit_offset"),
                    () => Ops.CurrentOffsetRegion == 1);

                // ---------- 18. 主页统计页签（Counter / UPH-Tray / Timer） ----------
                Chk("主页可构建", () => { }, () => HomePages.MainScreen() != null);

                // ---------- 19. 批次管理 / 运行统计（温启志 18719361399） ----------
                Chk("开批校验：批号为空会被拒", () =>
                {
                    M.SetText("f_BatchNo", "");
                    M.SetNum("f_LotSize", 0);
                }, () => LotOps.CheckCanOpenLot() != null);

                Chk("开批校验：批量为 0 会被拒", () =>
                {
                    M.SetText("f_BatchNo", "HG_LOT_1");
                    M.SetNum("f_LotSize", 0);
                }, () => LotOps.CheckCanOpenLot() != null);

                Chk("开批成功", () =>
                {
                    M.SetText("f_BatchNo", "HG_LOT_1");
                    M.SetText("f_SubBatchNo", "SUB-1");
                    M.SetText("f_Customer", "JHT");
                    M.SetText("f_Operator", "OP-001");
                    M.SetText("f_Shift", "A");
                    M.SetChoice("f_output_type", 0);
                    M.SetNum("f_LotSize", 500);
                    if (!LotOps.OpenLot()) throw new Exception("开批失败：" + (LotOps.CheckCanOpenLot() ?? "未知原因"));
                }, () => M.BatchOpen && M.LotSize == 500 && M.Shift == "A");

                Chk("开批后统计被清零", () => { },
                    () => M.TotalCount == 0 && M.JamCount == 0 && M.RunTimeMin == 0);
                Chk("重复开批会被拒", () => { }, () => LotOps.CheckCanOpenLot() != null);
                Chk("批次状态文本", () => { }, () => LotOps.LotStateText == "已开批");

                Chk("运行时间累计（运行中）", () =>
                {
                    Ops.StartProduction();
                    for (int i = 0; i < 120; i++) LotOps.Tick(1.0 / 120.0);   // 1 分钟
                }, () => Math.Abs(M.RunTimeMin - 1.0) < 0.01);

                Chk("停机时间累计（暂停后）", () =>
                {
                    Ops.PauseProduction();
                    for (int i = 0; i < 60; i++) LotOps.Tick(1.0 / 120.0);    // 0.5 分钟
                }, () => Math.Abs(M.StopTimeMin - 0.5) < 0.01);

                Chk("产量与良率统计", () =>
                {
                    Ops.ResumeProduction();
                    Pump(400);
                }, () => M.TotalCount > 0 && M.Yield >= 0 && M.Yield <= 100);

                Chk("卡料计数与 MTBA", () =>
                {
                    LotOps.JamOccurred("自检模拟卡料");
                    LotOps.JamOccurred("自检模拟卡料2");
                }, () => M.JamCount == 2 && LotOps.MtbaMin > 0);

                Chk("批次时间 = 运行 + 卡料 + 停机", () => { },
                    () => Math.Abs(LotOps.LotTimeMin - (M.RunTimeMin + M.JamTimeMin + M.StopTimeMin)) < 0.01);
                Chk("批次进度 / 剩余数量", () => { },
                    () => LotOps.ProgressPercent >= 0 && LotOps.Remaining == Math.Max(0, M.LotSize - M.TotalCount));
                Chk("Gross/Nett PPH 非负", () => { },
                    () => LotOps.GrossPPH >= 0 && LotOps.NettPPH >= 0);

                Chk("停产后才能切重扫模式", () =>
                {
                    Ops.StopProduction();
                    if (!LotOps.SetRescanMode(true)) throw new Exception("切换重扫模式失败");
                }, () => M.RescanMode && !Ops.IsRunning);

                Chk("Tray 模式下换载带会被拒", () => { }, () => !LotOps.ChangeCarrier(1));
                Chk("载带编号越界会被拒", () => { }, () => !LotOps.ChangeCarrier(3));
                Chk("切到 Tape 后可换载带", () =>
                {
                    M.OutputType = "Tape";
                    M.Carrier1Count = 123;
                    if (!LotOps.ChangeCarrier(1)) throw new Exception("换载带失败");
                }, () => M.Carrier1Count == 0);

                Chk("KGD 补盘可切换", () => LotOps.SetKgdTraySort(true), () => M.KgdTraySort);
                Chk("自动打印可切换", () => LotOps.SetAutoPrinting(false), () => !M.AutoPrinting);
                Chk("停止后才能中止批次", () =>
                {
                    Ops.StopProduction();
                    if (!LotOps.AbortLot()) throw new Exception("中止批次失败");
                }, () => M.Purging);
                Chk("清料完成", () => LotOps.FinishPurge(), () => !M.Purging);
                Chk("清盘", () => { if (!LotOps.PurgeTrays()) throw new Exception("清盘失败"); }, () => M.Purging);
                Chk("清料完成后可结批", () => LotOps.FinishPurge(), () => !M.Purging);

                Chk("事件日志 / 错误日志", () => { },
                    () => LotOps.EventLog().Count > 0 && !string.IsNullOrEmpty(LotOps.ExportEventLog())
                       && !string.IsNullOrEmpty(LotOps.ExportErrorLog()));
                Chk("批次汇总文本非空", () => { }, () => LotOps.PrintLotSummary().Length > 100);

                Chk("结批成功并写批次记录", () =>
                {
                    if (!LotOps.CloseLot()) throw new Exception("结批失败：" + (LotOps.CheckCanCloseLot() ?? "未知原因"));
                }, () => !M.BatchOpen && M.BatchEnd.HasValue);

                Chk("批次记录可被检索到", () => { },
                    () => LotOps.SearchLotRecords(null, null, "HG_LOT_1").Any(r => r.LotNo == "HG_LOT_1"));
                Chk("按班别检索", () => { },
                    () => LotOps.SearchLotRecords(null, null, null, null, "A").Any(r => r.LotNo == "HG_LOT_1"));
                Chk("按不存在的批号检索为空", () => { },
                    () => !LotOps.SearchLotRecords(null, null, "NO_SUCH_LOT_ZZZ").Any());
                Chk("批次记录总数 > 0", () => { }, () => LotOps.LotRecordCount > 0);
                Chk("重复结批会被拒", () => { }, () => LotOps.CheckCanCloseLot() != null);

                // ---------- 20. 收尾：把自检自己造出来的东西清干净 ----------
                Chk("清理自检批次记录", () =>
                {
                    try
                    {
                        foreach (var f in System.IO.Directory.GetFiles(LotOps.LotDir, "*.crd"))
                        {
                            var lines = System.IO.File.ReadAllLines(f)
                                .Where(l => l.IndexOf("HG_LOT_1", StringComparison.OrdinalIgnoreCase) < 0)
                                .ToArray();
                            System.IO.File.WriteAllLines(f, lines);
                        }
                        foreach (var f in System.IO.Directory.GetFiles(LotOps.LotDir, "HG_LOT_1.lot"))
                            System.IO.File.Delete(f);
                    }
                    catch { /* 清理失败不影响自检结论 */ }
                }, () => !LotOps.SearchLotRecords(null, null, "HG_LOT_1").Any());

                Chk("清理自检品种", () =>
                {
                    Ops.DeleteRecipe("hg_ops_selftest_recipe");
                    Ops.DeleteRecipe(rec);
                    Ops.SyncRecipeListFromDisk();
                }, () => !MachineModel.Instance.Recipes.Any(r => r.StartsWith("SELFTEST")
                                                                || r.StartsWith("hg_ops_selftest")));

                // ---------- 21. 工位流程线程（Motion/FlowEngine.cs：一工位一条 while 线程，运动已接） ----------
                var fe = Motion.FlowEngine.Instance;
                double simSec = 0;

                Chk("流程线程数 = 19", () => { },
                    () => fe.Threads.Count == 19);
                Chk("FlowStart 拉起全部线程", () => Ops.FlowStart(), () => Ops.FlowRunning);
                Chk("安全扫描线程发布 SafeOk", () => { simSec += PumpWall(2); },
                    () => Motion.VarBox.GetFlag("SafeOk"));
                Chk("19 条线程循环数全部 > 0", () => { simSec += PumpWall(25); },
                    () => fe.Threads.Count(t2 => t2.Loops > 0) == 19);
                Chk("流程已下发真实运动指令（不再是空转）", () => { },
                    () => Ops.FlowMoveIssued > 0);
                Chk("投料颗数 > 0（守恒行不再恒显示尚未投料）", () => { },
                    () => Motion.VarBox.GetD("LotIn") > 0);
                Chk("守恒等式差值为 0（投料 = ΣBin + 在制 + 报废）", () => { },
                    () => Math.Abs(Motion.VarBox.GetD("LotIn")
                                   - (Motion.FlowEngine.BinSum() + Motion.FlowEngine.Wip()
                                      + Motion.VarBox.GetD("报废"))) < 0.001);
                Chk("守恒自检行已按真实计数出文本", () => { },
                    () => Ops.FlowConservation.Contains("投料") && Ops.FlowConservation.Contains("ΣBin")
                       && Ops.FlowConservation.Contains("在制") && Ops.FlowConservation.Contains("报废")
                       && Ops.FlowConservation.Contains("口径"));
                Chk("逐轴合计 = 引擎总下发（每根轴的动作都有归属，没有漏记）", () => { },
                    () =>
                    {
                        long sum = 0;
                        foreach (var a in mm.Axes) sum += Ops.FlowAxisMoves(a.Index);
                        return sum == Ops.FlowMoveIssued;
                    });
                Chk("被流程动过的轴全部过了安全层（放过或拦下）", () => { },
                    () =>
                    {
                        foreach (var a in mm.Axes)
                            if (Ops.FlowAxisMoves(a.Index) > 0 && !fe.AxisGuarded(a.Index)) return false;
                        return true;
                    });
                Chk("越软限位次数 = 0", () => { }, () => Ops.FlowSoftLimitBreaches == 0);
                Chk("流程线程异常次数 = 0", () => { }, () => Ops.FlowErrors == 0);
                Chk("输入等待超时次数 = 0", () => { }, () => fe.WaitTimeouts == 0);
                Chk("变量黑板快照非空（含相位键与在制键）", () => { },
                    () =>
                    {
                        var s = Ops.VarBoxSnapshot();
                        return s.Count > 40 && s.Any(x => x.Contains("相位·"))
                            && s.Any(x => x.Contains("在制_上料吸嘴")) && s.Any(x => x.Contains("LotIn"));
                    });
                Chk("FlowStop 停机", () => Ops.FlowStop(), () => !Ops.FlowRunning);
                Chk("Stop 后泄漏令牌数 == 0", () => { }, () => Ops.FlowLeakedTokens == 0);

                // 收尾把 45 根轴全部归回停放位：后面两条「逐根过安全层」的判据要的是确定状态，
                // 不能因为流程停在半途（某根 Z 还在下位）就让判据变成随机结果。
                Chk("流程收尾：垂直轴全部先抬回抬起位", () =>
                {
                    foreach (var a in mm.Axes.Where(x => x.IsVertical)) mm.MoveAbs(a.Index, a.HomeMm);
                    simSec += PumpWall(5);
                }, () => mm.Axes.Where(x => x.IsVertical).All(x => Math.Abs(mm.Position(x.Index) - x.HomeMm) < 1.0));
                Chk("流程收尾：水平轴全部回停放位", () =>
                {
                    foreach (var a in mm.Axes.Where(x => !x.IsVertical)) mm.MoveAbs(a.Index, a.HomeMm);
                    simSec += PumpWall(8);
                }, () => mm.Axes.Where(x => !x.IsVertical).All(x => Math.Abs(mm.Position(x.Index) - x.HomeMm) < 1.0));

                // 互锁没有被流程改出新后门：越界目标照旧被拒、急停照旧全拦
                Chk("互锁仍在：越界目标被 SafetyCheck 拒绝", () => { },
                    () => !mm.SafetyCheck(0, 5000).Accepted);
                Chk("互锁仍在：45 根轴逐根「回零位放行 + 越界拒绝」（旧口径 31 根）", () => { },
                    () =>
                    {
                        int pass = 0, reject = 0;
                        foreach (var a in mm.Axes)
                        {
                            if (mm.SafetyCheck(a.Index, a.HomeMm).Accepted) pass++;
                            if (!mm.SafetyCheck(a.Index, Math.Max(a.MinMm, a.MaxMm) + 1000).Accepted) reject++;
                        }
                        Log("flow guard axes: total=" + mm.Axes.Count + " 放行=" + pass + " 拒绝=" + reject
                            + "（旧口径 31 根，现按交接资料真表 45 根）");
                        return mm.Axes.Count == 45 && pass == 45 && reject == 45;
                    });
                Chk("互锁仍在：急停中所有轴一律拒绝", () =>
                {
                    Ops.EmergencyStop();
                }, () => mm.Axes.All(a => !mm.SafetyCheck(a.Index, mm.Cfg(a.Index).HomeMm).Accepted));
                Chk("急停复位", () => Ops.ResetEmergency(), () => !mm.EStop);

                Log("flow selftest: 墙钟 30s ≈ 仿真 " + simSec.ToString("0.0") + "s　"
                    + "下发=" + Ops.FlowMoveIssued + " 被拒=" + Ops.FlowMoveRejected
                    + " 让行=" + Ops.FlowGuardYields + " 异常=" + Ops.FlowErrors);
                Log("flow conservation: " + Ops.FlowConservation);
                Log("flow stat: " + Ops.FlowStat);
                Log("flow yields top: " + fe.YieldTop(6));

                Log("ops selftest: ok=" + ok + " bad=" + bad);
                foreach (var f in fails) Log("OPS FAIL " + f);
                if (bad == 0) Log("ops selftest done ALL PASS ok=" + ok);
            }
            catch (Exception ex)
            {
                Log("ops selftest crash: " + ex);
            }
        }

        private static void MotionSelfTest()
        {
            try
            {
                var m = Motion.MotionManager.Instance;

                m.Connect();
                // 探测说明由 Connect() 记录：没连上真卡时这里就是具体原因
                // （驱动缺失 / 未插卡 / 位数不匹配），现场排查先看这一条。
                // 注意不要在 Connect() 之外再调一次 dmc_board_init —— 那会重复初始化板卡。
                Log("motion probe: " + m.LastProbeInfo);
                Log("motion: card=" + m.CardName + " sim=" + m.IsSim
                    + " axes=" + m.Axes.Count + " in=" + m.Inputs.Count + " out=" + m.Outputs.Count);

                m.ResetEStop();
                m.ClearAlarm();
                m.StartDemo();

                // 40 秒仿真（步长 50ms），足够跑完 22 步节拍并循环多轮；
                // 过程中持续跟踪共享导轨的最小间距（防撞的核心指标）。
                double minGap = double.MaxValue;
                for (int i = 0; i < 800; i++)
                {
                    m.Tick(0.05);
                    if (i % 4 != 0) continue;
                    foreach (var rail in new[] { "ARM_X", "SHUTTLE" })
                    {
                        var lst = new System.Collections.Generic.List<Motion.AxisConfig>();
                        foreach (var a in m.Axes) if (a.RailId == rail) lst.Add(a);
                        for (int x = 0; x < lst.Count; x++)
                            for (int y = x + 1; y < lst.Count; y++)
                                minGap = Math.Min(minGap,
                                    Math.Abs(m.Position(lst[x].Index) - m.Position(lst[y].Index)));
                    }
                }

                // 1) 所有轴都不得越过软限位
                int outOfLimit = 0;
                double worst = 0;
                foreach (var a in m.Axes)
                {
                    double p = m.Position(a.Index);
                    double lo = Math.Min(a.MinMm, a.MaxMm), hi = Math.Max(a.MinMm, a.MaxMm);
                    if (p < lo - 0.5 || p > hi + 0.5)
                    {
                        outOfLimit++;
                        worst = Math.Max(worst, Math.Max(lo - p, p - hi));
                    }
                }

                // 2) 互锁必须真的拦下过越界指令
                bool limitRejected = !m.SafetyCheck(4, 5000).Accepted;

                Log("motion selftest: cycles=" + m.DemoCycles + " step=" + m.DemoStepText
                    + " moves=" + m.MoveCount + " rejects=" + m.RejectCount
                    + " outOfLimit=" + outOfLimit + " worstOver=" + worst.ToString("0.###")
                    + " minRailGap=" + (minGap == double.MaxValue ? -1 : Math.Round(minGap, 1))
                    + " limitGuard=" + limitRejected
                    + " alarm=" + m.AnyAlarm());

                // 3) 打印最常见的拒绝原因（去重后取前 5 条）
                var seen = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var r in m.RecentRejects)
                {
                    string key = r.Length > 70 ? r.Substring(0, 70) : r;
                    seen[key] = seen.TryGetValue(key, out var c) ? c + 1 : 1;
                }
                int shown = 0;
                foreach (var kv in seen)
                {
                    Log("motion reject[" + kv.Value + "] " + kv.Key);
                    if (++shown >= 5) break;
                }

                m.StopDemo();
            }
            catch (Exception ex)
            {
                Log("motion selftest FAIL " + ex);
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
