using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HandlerGuide.Views
{
    /// <summary>把样式资源键转换成 Style；找不到时返回 UnsetValue（保留控件默认样式）。</summary>
    public class StyleKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as string;
            if (string.IsNullOrEmpty(key)) return DependencyProperty.UnsetValue;
            return Application.Current.TryFindResource(key) as Style ?? (object)DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>double → Thickness（图形元素的描边宽度）。</summary>
    public class DoubleToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = value is double v ? v : 0;
            return new Thickness(d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>double → CornerRadius（图形元素圆角）。</summary>
    public class DoubleToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = value is double v ? v : 0;
            return new CornerRadius(d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>值为 null 时返回 UnsetValue，避免用 null 覆盖样式里设定的属性。</summary>
    public class UnsetIfNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value ?? DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>双精度 NaN 表示“自动”，直接绑定 Width/Height 即可；此转换器用于 double→Visibility 之外的场景。</summary>
    public class BrushNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value ?? DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>把 "360,*,Auto" 这样的行列定义字符串应用到 Grid 上（XAML 里不能直接绑定 RowDefinitions）。</summary>
    public static class GridSpec
    {
        public static readonly DependencyProperty ColsProperty = DependencyProperty.RegisterAttached(
            "Cols", typeof(string), typeof(GridSpec), new PropertyMetadata(null, OnColsChanged));

        public static readonly DependencyProperty RowsProperty = DependencyProperty.RegisterAttached(
            "Rows", typeof(string), typeof(GridSpec), new PropertyMetadata(null, OnRowsChanged));

        public static void SetCols(DependencyObject d, string v) => d.SetValue(ColsProperty, v);
        public static string GetCols(DependencyObject d) => (string)d.GetValue(ColsProperty);
        public static void SetRows(DependencyObject d, string v) => d.SetValue(RowsProperty, v);
        public static string GetRows(DependencyObject d) => (string)d.GetValue(RowsProperty);

        private static void OnColsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var g = d as Grid;
            if (g == null) return;
            g.ColumnDefinitions.Clear();
            foreach (var s in Parse(e.NewValue as string))
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = Len(s) });
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var g = d as Grid;
            if (g == null) return;
            g.RowDefinitions.Clear();
            foreach (var s in Parse(e.NewValue as string))
                g.RowDefinitions.Add(new RowDefinition { Height = Len(s) });
        }

        private static string[] Parse(string spec)
            => string.IsNullOrWhiteSpace(spec) ? new[] { "*" } : spec.Split(',');

        private static GridLength Len(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return new GridLength(1, GridUnitType.Star);
            if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;
            if (s == "*") return new GridLength(1, GridUnitType.Star);
            if (s.EndsWith("*", StringComparison.Ordinal))
            {
                double f;
                if (double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out f))
                    return new GridLength(f, GridUnitType.Star);
            }
            double d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return new GridLength(d);
            return new GridLength(1, GridUnitType.Star);
        }
    }
}
