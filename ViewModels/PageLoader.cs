using System;
using System.IO;
using System.Windows.Markup;

namespace HandlerGuide.ViewModels
{
    /// <summary>
    /// 页面 XAML 加载器：把 Views/Pages/{id}.xaml 解析为 Element 视图模型树。
    /// 这是基础设施（声明式 XAML 的解析），不是页面逻辑里的“动态添加控件”。
    /// </summary>
    public static class PageLoader
    {
        /// <summary>加载页面 XAML；文件不存在返回 null（调用方回退到旧 Build）。解析失败则抛出异常以便自查。</summary>
        public static Element Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pages", id + ".xaml");
            if (!File.Exists(path)) return null;
            return (Element)XamlReader.Parse(File.ReadAllText(path));
        }
    }
}
