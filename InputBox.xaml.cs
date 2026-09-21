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
