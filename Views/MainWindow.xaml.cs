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

            _vm.PropertyChanged += Vm_PropertyChanged;
            DataContext = _vm;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, e) => _vm.Tick();
            _timer.Start();
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
