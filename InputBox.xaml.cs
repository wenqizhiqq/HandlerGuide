// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
// ============================================================================
//  InputBox.xaml.cs — 输入对话框 code-behind
//  项目：HandlerGuide（EXCEED9000 控制软件界面复刻）
//  职责：通用文本/密码输入；Result 返回输入内容，ShowDialog 用于授权确认。
// ============================================================================
using System.Windows;

namespace HandlerGuide
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（InputBox_xaml）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_InputBox_xaml
    internal static class WenQiZhiTag_InputBox_xaml
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · InputBox_xaml。</summary>
        internal const string WenQiZhiFile = "InputBox_xaml";
    }

    // 温启志 18719361399 · 类 public partial class InputBox : Window
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

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
