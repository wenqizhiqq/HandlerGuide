// ============================================================================
//  InputBox.xaml.cs — 输入对话框 code-behind
//  项目：HandlerGuide（EXCEED9000 控制软件界面复刻）
//  职责：通用文本/密码输入；Result 返回输入内容，ShowDialog 用于授权确认。
// ============================================================================
using System.Windows;

namespace HandlerGuide
{
    public partial class InputBox : Window
    {
        public string Result { get; private set; }

        public InputBox(string prompt)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            PwdBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = PwdBox.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
