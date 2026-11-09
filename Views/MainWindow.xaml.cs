// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
// ============================================================================
//  Views/MainWindow.xaml.cs — 主窗口 code-behind
//  项目：HandlerGuide（EXCEED9000 控制软件界面复刻）
//  职责：注入对话框服务（密码/确认/提示）、启动 500ms 定时器驱动实时仿真与刷新、
//        导航切换时滚动到页面顶部。
// ============================================================================
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using HandlerGuide.ViewModels;

namespace HandlerGuide.Views
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MainWindow_xaml）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_MainWindow_xaml
    internal static class WenQiZhiTag_MainWindow_xaml
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = HandlerGuide.ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = HandlerGuide.ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · MainWindow_xaml。</summary>
        internal const string WenQiZhiFile = "MainWindow_xaml";
    }

    // 温启志 18719361399 · 类 public partial class MainWindow : Window
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new MainViewModel();
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _fastTimer;
        private readonly System.Diagnostics.Stopwatch _fastClock = new System.Diagnostics.Stopwatch();
        private double _lastFastSec;

        /// <summary>急停/卡顿下最多一次补多少秒，超过就丢掉（避免界面挂起后暴走）。</summary>
        private const double CatchUpMaxSeconds = 5.0;

        /// <summary>供自检 / 测试访问视图模型。</summary>
        public MainViewModel Vm => _vm;

        public MainWindow()
        {
            InitializeComponent();

            // 视图负责提供对话框服务，视图模型保持与 Window 解耦
            _vm.ConfirmPassword = (prompt, expected) =>
            {
                var dlg = new InputBox(prompt) { Owner = this };
                return dlg.ShowDialog() == true && dlg.Result == expected;
            };
            _vm.Confirm = (title, msg) =>
                MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            _vm.Alert = (title, msg) =>
                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);

            // 文件对话框：视图负责弹窗，视图模型只拿结果
            _vm.PickOpenFiles = (title, filter) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    Multiselect = true,
                    CheckFileExists = true
                };
                return dlg.ShowDialog(this) == true ? dlg.FileNames : null;
            };
            _vm.PickSaveFile = (title, filter, defaultName) =>
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = title,
                    Filter = filter,
                    FileName = defaultName,
                    OverwritePrompt = true
                };
                return dlg.ShowDialog(this) == true ? dlg.FileName : null;
            };

            _vm.PropertyChanged += Vm_PropertyChanged;
            DataContext = _vm;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, e) => _vm.Tick();
            _timer.Start();

            // 运动仿真快速通道：约 20fps 推进梯形速度曲线与轨迹动画。
            // ⚠ 这里**绝对不能**把 dt 压成常数（旧版 dt>0.5 就替换成 0.05）。
            //   压了就等于机器时间比墙钟慢：Lua 脚本里 go()/pause() 用的是真实
            //   时间，而轴的位置靠 dt 积分 —— 两者不同步时，一圈 8.5s 的脚本
            //   会被拖成几分钟，节拍/UPH 全部失去对照意义。
            //   改用单调计时器取真实间隔，MotionManager.Tick 内部再按 20ms 分步。
            _fastClock.Restart();
            _lastFastSec = 0;
            _fastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _fastTimer.Tick += (s, e) =>
            {
                double nowSec = _fastClock.Elapsed.TotalSeconds;
                double dt = nowSec - _lastFastSec;
                _lastFastSec = nowSec;
                if (dt <= 0) return;
                if (dt > CatchUpMaxSeconds) dt = CatchUpMaxSeconds;
                _vm.FastTick(dt);
            };
            _fastTimer.Start();

            // 关窗必须停表：DispatcherTimer 的回调持有窗口引用，不停就会在窗口
            // 关掉之后继续推进仿真（自检里连开两个窗口 ⇒ 仿真时钟跑成 2 倍速）。
            Closed += (s, e) =>
            {
                try { _timer.Stop(); } catch { }
                try { _fastTimer.Stop(); } catch { }
            };

            _vm.Tick();
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentPage) || string.IsNullOrEmpty(e.PropertyName))
                PageScroll.ScrollToTop();
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
