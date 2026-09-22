// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.IO;
using System.Windows.Markup;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（PageLoader）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_PageLoader
    internal static class WenQiZhiTag_PageLoader
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · PageLoader。</summary>
        internal const string WenQiZhiFile = "PageLoader";
    }

    // 温启志 18719361399 · 类 public static class PageLoader
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

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
