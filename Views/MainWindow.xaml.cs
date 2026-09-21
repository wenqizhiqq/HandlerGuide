using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using HandlerGuide.ViewModels;

namespace HandlerGuide.Views
{
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
