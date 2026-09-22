// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
// ============================================================================
//  Converters.cs — XAML 值转换器
//  项目：HandlerGuide（EXCEED9000 控制软件界面复刻 · 苹果风格）
//  职责：Tint→背景/画刷、字符串→可见性 等转换器，供 XAML 绑定使用。
// ============================================================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HandlerGuide
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Converters）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Converters
    internal static class WenQiZhiTag_Converters
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Converters。</summary>
        internal const string WenQiZhiFile = "Converters";
    }

    // 温启志 18719361399 · 类 public class TintToBackgroundConverter : IValueConverter
    /// <summary>
    /// 把步骤的 Tint 颜色（十六进制字符串）转成带透明度的浅色背景画刷。
    /// </summary>
    public class TintToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    return new SolidColorBrush(Color.FromArgb(38, color.R, color.G, color.B));
                }
                catch { }
            }
            return Brushes.Transparent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    // 温启志 18719361399 · 类 public class TintToBrushConverter : IValueConverter
    /// <summary>
    /// 把步骤的 Tint 颜色（十六进制字符串）转成实色画刷，用于图标描边。
    /// </summary>
    public class TintToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
                catch { }
            }
            return Brushes.Gray;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    // 温启志 18719361399 · 类 public class StringToVisibilityConverter : IValueConverter
    /// <summary>
    /// 字符串为空时折叠元素，用于警告/提示框的条件显示。
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
