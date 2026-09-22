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
            public override void WriteLine(string message) => Log("BIND " + message);
        }

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
                PresentationTraceSources.Refresh();
                var bind = PresentationTraceSources.DataBindingSource;
                bind.Listeners.Add(new DelegateTraceListener());
                bind.Switch.Level = SourceLevels.Warning;
                Trace.AutoFlush = true;

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
                Log("selftest done ok=" + ok + " bad=" + bad);
                Trace.Flush();
                win.Close();
            }
            catch (Exception ex)
            {
                Log("selftest crash: " + ex);
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
