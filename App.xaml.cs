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
using System.Windows;

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
            Log("温启志 18719361399 · 版权所有 " + ViewModels.Owner.WenQiZhiCopyright);
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

                LayoutSelfTest(shotDir);
                MotionSelfTest();
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

                for (int i = 0; i < 80; i++)
                {
                    vm.FastTick(0.05);
                    win.UpdateLayout();
                    // 关键：数据绑定更新排在 DispatcherPriority.DataBind 上，
                    // UpdateLayout() 只做 Measure/Arrange，不会刷新绑定。
                    // 必须显式把消息队列泵到 Background，否则截图拿到的永远是旧画面
                    // （之前这里 pixelDiff=0，看起来「动画坏了」，其实是探针读到了旧视觉树）。
                    win.Dispatcher.Invoke(() => { },
                        System.Windows.Threading.DispatcherPriority.Background);
                    if (i == 19 && a != null) Shoot(win, a);
                    if (i == 79 && b != null) Shoot(win, b);
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

                Log("layout selftest: frames=80 demoCycles=" + m.DemoCycles
                    + " step=" + m.DemoStepText
                    + " upArmX=" + m.Position(4).ToString("0.0")
                    + " downArmX=" + m.Position(16).ToString("0.0")
                    + " carA=" + m.Position(28).ToString("0.0")
                    + " carB=" + m.Position(29).ToString("0.0")
                    + " pressZ=" + m.Position(2).ToString("0.0")
                    + " gunY=" + m.Position(5).ToString("0.0")
                    + " heat=" + m.ReadOut(15)
                    + " pixelDiff=" + diff
                    + (diff > 0 ? " （动画正常）"
                       : diff == 0 ? " （警告：画面没有变化）"
                       : " （未开启截图，跳过像素比较）"));

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
