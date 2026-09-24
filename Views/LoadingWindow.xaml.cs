using System;
using System.Threading.Tasks;
using System.Windows;

namespace HandlerGuide.Views
{
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) 参数自动载入（MachineModel 构造里已 Load；这里显式触发并给出提示）
                SetStatus("正在加载参数配置…", 8);
                var m = MachineModel.Instance;
                await Task.Delay(150);

                // 2) 初始化运动控制卡（无卡自动降级为仿真卡，不会抛错）
                SetStatus("正在初始化运动控制卡…", 40);
                try { Motion.MotionManager.Instance.ResetEStop(); Motion.MotionManager.Instance.ClearAlarm(); }
                catch { }
                await Task.Delay(150);

                // 3) 构建设置界面（主窗口构建，可能稍慢）
                SetStatus("正在构建设置界面…", 75);
                var win = new MainWindow();
                win.Show();
                await Task.Delay(80);

                // 4) 完成
                SetStatus("准备完成", 100);
                await Task.Delay(180);
                this.Close();
            }
            catch (Exception ex)
            {
                SetStatus("启动出错：" + ex.Message, 100);
                await Task.Delay(1600);
                this.Close();
            }
        }

        private void SetStatus(string text, int pct)
        {
            Dispatcher.Invoke(() =>
            {
                Status.Text = text;
                double w = Track.ActualWidth > 0 ? Track.ActualWidth : 392;
                BarFill.Width = (pct / 100.0) * w;
            });
        }
    }
}
