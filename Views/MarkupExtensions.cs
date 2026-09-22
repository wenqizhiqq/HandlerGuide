using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using HandlerGuide.ViewModels;

namespace HandlerGuide.Views
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MarkupExtensions）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_MarkupExtensions
    internal static class WenQiZhiTag_MarkupExtensions
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = HandlerGuide.ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = HandlerGuide.ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · MarkupExtensions。</summary>
        internal const string WenQiZhiFile = "MarkupExtensions";
    }

    // 温启志 18719361399 · 类 public static class BrushUtil
    /// <summary>
    /// XAML 标记扩展：把界面元素的“工厂”从 ViewModels 移到 Views 层。
    /// 页面 .xaml 通过 &lt;vw:Num .../&gt; 等方式声明式地引用它们，
    /// ViewModels 中不再出现任何 new TextBox / Children.Add 之类的动态构造。
    /// </summary>
    public static class BrushUtil
    {
        public static Brush BrushOf(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (s[0] == '#')
            {
                try { return (Brush)new BrushConverter().ConvertFromString(s); }
                catch { return null; }
            }
            var b = Palette.R(s);
            return b ?? (Brush)new BrushConverter().ConvertFromString(s);
        }
    }

    // 温启志 18719361399 · 类 public class NumExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class NumExtension : MarkupExtension
    {
        public NumExtension() { }
        public NumExtension(string key) { Key = key; }
        public string Key { get; set; }
        public double Default { get; set; }
        public double Width { get; set; } = 92;
        public string Format { get; set; } = "0.###";
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool ReadOnly { get; set; }
        public bool Compact { get; set; }
        public override object ProvideValue(IServiceProvider sp)
        {
            var e = new NumberElement(Key, Default, Width, Format, Min, Max, ReadOnly) { Compact = Compact };
            return e;
        }
    }

    // 温启志 18719361399 · 类 public class CmbExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class CmbExtension : MarkupExtension
    {
        public CmbExtension() { }
        public CmbExtension(string key) { Key = key; }
        public string Key { get; set; }
        public string[] Options { get; set; }
        /// <summary>XAML 友好：用 | 分隔的选项串，例如 "A|B|C"。</summary>
        public string OptionsText { get; set; }
        public double Width { get; set; } = 150;
        public int Default { get; set; }
        public override object ProvideValue(IServiceProvider sp)
        {
            string[] opts = Options;
            if (opts == null && !string.IsNullOrEmpty(OptionsText))
                opts = OptionsText.Split('|');
            return new ComboElement(Key, opts, Width, Default);
        }
    }

    // 温启志 18719361399 · 类 public class ChkExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class ChkExtension : MarkupExtension
    {
        public ChkExtension() { }
        public ChkExtension(string key) { Key = key; }
        public string Key { get; set; }
        public string Label { get; set; }
        public bool Default { get; set; }
        public bool BoxVisible { get; set; } = true;
        public override object ProvideValue(IServiceProvider sp)
            => new CheckElement(Key, Label, Default) { BoxVisible = BoxVisible };
    }

    // 温启志 18719361399 · 类 public class SwitchExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class SwitchExtension : MarkupExtension
    {
        public SwitchExtension() { }
        public SwitchExtension(string key) { Key = key; }
        public string Key { get; set; }
        public bool Default { get; set; }
        public override object ProvideValue(IServiceProvider sp) => new SwitchElement(Key, Default);
    }

    // 温启志 18719361399 · 类 public class RadioExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class RadioExtension : MarkupExtension
    {
        public RadioExtension() { }
        public RadioExtension(string key) { Key = key; }
        public string Key { get; set; }
        public string Label { get; set; }
        public int Index { get; set; }
        public bool On { get; set; }
        public double Gap { get; set; }
        public override object ProvideValue(IServiceProvider sp)
            => new RadioElement(Key, Label, Index, On, Gap);
    }

    // 温启志 18719361399 · 类 public class BtnExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class BtnExtension : MarkupExtension
    {
        public BtnExtension() { }
        public BtnExtension(string text) { Text = text; }
        public string Text { get; set; }
        public string Style { get; set; } = "PrimaryButton";
        public double FontSize { get; set; } = 12.5;
        public double Width { get; set; }
        public double Height { get; set; }
        public string Bg { get; set; }
        public string Fg { get; set; }
        public string FontWeight { get; set; }
        public Thickness BorderThickness { get; set; }
        public string Cmd { get; set; }
        public string Arg { get; set; }
        public override object ProvideValue(IServiceProvider sp)
        {
            var b = new ButtonElement
            {
                Text = Text,
                StyleKey = Style,
                FontSize = FontSize,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 2, 8, 2),
                Command = PageCommands.Command(Cmd, Arg),
                Width = Width > 0 ? Width : double.NaN,
                Height = Height > 0 ? Height : double.NaN,
                Background = BrushUtil.BrushOf(Bg),
                Foreground = BrushUtil.BrushOf(Fg),
                BorderThickness = BorderThickness
            };
            if (!string.IsNullOrEmpty(FontWeight))
            {
                var conv = new FontWeightConverter();
                if (conv.ConvertFromString(FontWeight) is FontWeight fw) b.FontWeight = fw;
            }
            return b;
        }
    }

    // 温启志 18719361399 · 类 public class MiniBtnExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class MiniBtnExtension : MarkupExtension
    {
        public MiniBtnExtension() { }
        public MiniBtnExtension(string text) { Text = text; }
        public string Text { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Bg { get; set; }
        public string Fg { get; set; }
        public string FontWeight { get; set; }
        public Thickness BorderThickness { get; set; }
        public string Cmd { get; set; }
        public string Arg { get; set; }
        public override object ProvideValue(IServiceProvider sp)
        {
            var b = new ButtonElement
            {
                Text = Text,
                StyleKey = "MiniButton",
                FontSize = 12,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 2, 6, 2),
                Width = Width > 0 ? Width : double.NaN,
                Height = Height > 0 ? Height : double.NaN,
                Background = BrushUtil.BrushOf(Bg),
                Foreground = BrushUtil.BrushOf(Fg),
                BorderThickness = BorderThickness,
                Command = PageCommands.Command(Cmd, Arg)
            };
            if (!string.IsNullOrEmpty(FontWeight))
            {
                var conv = new FontWeightConverter();
                if (conv.ConvertFromString(FontWeight) is FontWeight fw) b.FontWeight = fw;
            }
            return b;
        }
    }

    // 温启志 18719361399 · 类 public class ClickLampExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class ClickLampExtension : MarkupExtension
    {
        public ClickLampExtension() { }
        public ClickLampExtension(string key) { Key = key; }
        public string Key { get; set; }
        public bool Red { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; } = 13;
        public override object ProvideValue(IServiceProvider sp)
            => new ClickLampElement(Key, Red, Square, Size);
    }

    // 温启志 18719361399 · 类 public class LiveLampExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class LiveLampExtension : MarkupExtension
    {
        public LiveLampExtension() { }
        public LiveLampExtension(string signal) { Signal = signal; }
        public string Signal { get; set; }
        public bool Red { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; } = 13;
        public override object ProvideValue(IServiceProvider sp)
            => new LiveLampElement { Signal = Signal, IsRed = Red, Square = Square, Size = Size };
    }

    // 温启志 18719361399 · 类 public class LampExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class LampExtension : MarkupExtension
    {
        public LampExtension() { }
        public LampExtension(bool on) { On = on; }
        public bool On { get; set; }
        public bool Red { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; } = 13;
        public override object ProvideValue(IServiceProvider sp)
            => new LampElement { IsOn = On, IsRed = Red, Square = Square, Size = Size, Margin = new Thickness(2, 0, 2, 0) };
    }

    // 温启志 18719361399 · 类 public class RedLampExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class RedLampExtension : MarkupExtension
    {
        public RedLampExtension() { }
        public RedLampExtension(bool on) { On = on; }
        public bool On { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; } = 13;
        public override object ProvideValue(IServiceProvider sp)
            => new LampElement { IsOn = On, IsRed = true, Square = Square, Size = Size, Margin = new Thickness(2, 0, 2, 0) };
    }

    // 温启志 18719361399 · 类 public class DisplayExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class DisplayExtension : MarkupExtension
    {
        public DisplayExtension() { }
        public DisplayExtension(string text) { Text = text; }
        public string Text { get; set; }
        public double Width { get; set; }
        public string Bg { get; set; }
        public string Fg { get; set; }
        public string Ta { get; set; } = "Center";
        public double FontSize { get; set; } = 12.5;
        public override object ProvideValue(IServiceProvider sp)
            => new DisplayElement(Text, Width, BrushUtil.BrushOf(Bg), BrushUtil.BrushOf(Fg),
                (TextAlignment)Enum.Parse(typeof(TextAlignment), Ta), FontSize);
    }

    // 温启志 18719361399 · 类 public class LiveExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class LiveExtension : MarkupExtension
    {
        public LiveExtension() { }
        public LiveExtension(string signal) { Signal = signal; }
        public string Signal { get; set; }
        public string Format { get; set; }
        public double Width { get; set; }
        public string Bg { get; set; }
        public string Fg { get; set; }
        public double FontSize { get; set; } = 13;
        public override object ProvideValue(IServiceProvider sp)
        {
            var e = new LiveElement
            {
                Signal = Signal,
                Background = BrushUtil.BrushOf(Bg),
                Foreground = BrushUtil.BrushOf(Fg),
                FontSize = FontSize
            };
            e.Width = Width > 0 ? Width : double.NaN;
            e.Format = Format;
            return e;
        }
    }

    // 温启志 18719361399 · 类 public class SldExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class SldExtension : MarkupExtension
    {
        public SldExtension() { }
        public SldExtension(string key) { Key = key; }
        public string Key { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Default { get; set; }
        public double Width { get; set; } = 300;
        public override object ProvideValue(IServiceProvider sp)
            => new SliderElement(Key, Min, Max, Default, Width);
    }

    // 温启志 18719361399 · 类 public class SldRowExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class SldRowExtension : MarkupExtension
    {
        public SldRowExtension() { }
        public SldRowExtension(string label) { Label = label; }
        public string Label { get; set; }
        public string Key { get; set; }
        public double Default { get; set; } = 100;
        public override object ProvideValue(IServiceProvider sp)
            => new SliderRowElement(Label, Key, Default);
    }

    // 温启志 18719361399 · 类 public class RowExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class RowExtension : MarkupExtension
    {
        public RowExtension() { }
        public RowExtension(string label) { Label = label; }
        public string Label { get; set; }
        public object Ctrl { get; set; }
        public double LabelWidth { get; set; } = 118;
        public string Unit { get; set; }
        public double Gap { get; set; } = 10;
        public override object ProvideValue(IServiceProvider sp)
            => new FieldRowElement
            {
                Label = Label,
                Control = Ctrl as Element,
                LabelWidth = LabelWidth,
                Unit = Unit,
                Gap = Gap
            };
    }

    // 温启志 18719361399 · 类 public class RowFillExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class RowFillExtension : MarkupExtension
    {
        public RowFillExtension() { }
        public RowFillExtension(string label) { Label = label; }
        public string Label { get; set; }
        public object Ctrl { get; set; }
        public double LabelWidth { get; set; } = 96;
        public override object ProvideValue(IServiceProvider sp)
            => new FillRowElement { Label = Label, Control = Ctrl as Element, LabelWidth = LabelWidth };
    }

    // 温启志 18719361399 · 类 public class CellExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class CellExtension : MarkupExtension
    {
        public CellExtension() { }
        public CellExtension(string text) { Text = text; }
        public string Text { get; set; }
        public string Bg { get; set; }
        public string Fg { get; set; }
        public bool Bold { get; set; }
        public string Ta { get; set; } = "Center";
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Compact { get; set; }
        public override object ProvideValue(IServiceProvider sp)
        {
            var c = new CellElement
            {
                Background = BrushUtil.BrushOf(Bg) ?? Palette.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Padding = new Thickness(3, 1, 3, 1),
                Width = Width,
                Height = Height
            };
            var t = new TextElement
            {
                Text = Text ?? "",
                FontSize = 11.5,
                Foreground = BrushUtil.BrushOf(Fg) ?? Palette.Primary,
                FontWeight = Bold ? FontWeights.SemiBold : FontWeights.Normal,
                Wrap = TextWrapping.NoWrap,
                Align = (TextAlignment)Enum.Parse(typeof(TextAlignment), Ta),
                HAlign = HorizontalAlignment.Center
            };
            if (Compact) t.Compact();
            c.Content = t;
            return c;
        }
    }

    // 温启志 18719361399 · 类 public class CheckCellExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class CheckCellExtension : MarkupExtension
    {
        public CheckCellExtension() { }
        public CheckCellExtension(string key) { Key = key; }
        public string Key { get; set; }
        public bool Default { get; set; }
        public bool ShowBox { get; set; } = true;
        public override object ProvideValue(IServiceProvider sp)
        {
            var chk = new CheckElement(Key, "", Default) { BoxVisible = ShowBox, HAlign = HorizontalAlignment.Center };
            return new CellElement
            {
                Background = Palette.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Content = chk
            };
        }
    }

    // 温启志 18719361399 · 类 public class DExtension : MarkupExtension
    /// <summary>调用 Views/Diagrams 中的具名生成器（返回 Element），声明式地引用数据驱动表格/示意图。</summary>
    [MarkupExtensionReturnType(typeof(Element))]
    public class DExtension : MarkupExtension
    {
        public DExtension() { }
        public DExtension(string method) { Method = method; }
        public string Method { get; set; }
        public override object ProvideValue(IServiceProvider sp)
            => Diagrams.Invoke(Method);
    }

    // 温启志 18719361399 · 类 public class RadiosExtension : MarkupExtension
    /// <summary>作者水印（原 E.OwnerWatermark），放到每个页面末尾。</summary>
    [MarkupExtensionReturnType(typeof(Element))]
    public class RadiosExtension : MarkupExtension
    {
        public RadiosExtension() { }
        public RadiosExtension(string key) { Key = key; }
        public string Key { get; set; }
        public string[] Options { get; set; }
        public string OptionsText { get; set; }
        public int Default { get; set; }
        public double Gap { get; set; } = 12;
        public override object ProvideValue(IServiceProvider sp)
        {
            var opts = Options ?? (OptionsText != null ? OptionsText.Split("|") : new string[0]);
            var s = new StackElement { Orientation = Orientation.Horizontal };
            for (int i = 0; i < opts.Length; i++)
                s.Children.Add(new RadioElement(Key, opts[i], i, i == Default, Gap));
            return s;
        }
    }

    // 温启志 18719361399 · 类 public class SpacerExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class SpacerExtension : MarkupExtension
    {
        public double H { get; set; } = 6;
        public double W { get; set; }
        public override object ProvideValue(IServiceProvider sp)
            => new SpacerElement { H = H, W = W };
    }

    // 温启志 18719361399 · 类 public class MatrixExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class MatrixExtension : MarkupExtension
    {
        public string Cols { get; set; } = "a,b,c,d,e,f,g,h";
        public string Rows { get; set; } = "A,B";
        public string Prefix { get; set; } = "m";
        public double Cw { get; set; } = 44;
        public double Rh { get; set; } = 30;
        public bool Def { get; set; } = true;
        public double LabelW { get; set; } = 22;
        public override object ProvideValue(IServiceProvider sp)
        {
            var cols = Cols.Split(",");
            var rows = Rows.Split(",");
            var outer = new StackElement();
            var head = new StackElement { Orientation = Orientation.Horizontal };
            head.Children.Add(new TextElement { Text = "", Width = LabelW, Margin = new Thickness(0, 4, 0, 0) });
            foreach (var c in cols)
                head.Children.Add(new CellElement { Width = Cw, Height = 22, Background = Palette.HeadBlue,
                                                    BorderBrush = Palette.LineGrey, BorderThickness = 1,
                                                    Content = new TextElement { Text = c, FontSize = 11.5, Align = TextAlignment.Center,
                                                                                  VAlign = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold } });
            outer.Children.Add(head);
            foreach (var r in rows)
            {
                var row = new StackElement { Orientation = Orientation.Horizontal };
                var lbl = new CellElement { Width = LabelW, Height = Rh, Background = Palette.HeadBlue,
                                              BorderBrush = Palette.LineGrey, BorderThickness = 1,
                                              Content = new TextElement { Text = r, FontSize = 11.5, FontWeight = FontWeights.SemiBold,
                                                                            Align = TextAlignment.Center, VAlign = VerticalAlignment.Center } };
                row.Children.Add(lbl);
                foreach (var c in cols)
                {
                    var chk = new CheckElement(Prefix + "_" + r + "_" + c, "", Def) { BoxVisible = true, HAlign = HorizontalAlignment.Center };
                    row.Children.Add(new CellElement { Width = Cw, Height = Rh, Background = Palette.CardBg,
                                                      BorderBrush = Palette.LineGrey, BorderThickness = 1,
                                                      Content = chk });
                }
                outer.Children.Add(row);
            }
            return outer;
        }
    }

    // 温启志 18719361399 · 类 public class MotorsExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class MotorsExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider sp)
        {
            var v = new StackElement();
            foreach (var mo in MachineModel.Instance.Motors)
            {
                var row = new StackElement { Orientation = Orientation.Horizontal };
                row.Children.Add(new CheckElement("mh_" + mo.Name, "", true));
                row.Children.Add(new TextElement { Text = mo.Name, FontSize = 12, Foreground = Palette.Primary,
                                                   VAlign = VerticalAlignment.Center, Width = 160,
                                                   Margin = new Thickness(0, 0, 16, 0) });
                row.Children.Add(new DisplayElement("Safe", 90, null, null, TextAlignment.Center, 12));
                row.Children.Add(new DisplayElement("HomedStop", 110, null, null, TextAlignment.Center, 12));
                row.Children.Add(new DisplayElement("-------√", 90, null, null, TextAlignment.Center, 12));
                v.Children.Add(row);
            }
            return v;
        }
    }

    // 温启志 18719361399 · 类 public class TextInputExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class TextInputExtension : MarkupExtension
    {
        public TextInputExtension() { }
        public TextInputExtension(string key) { Key = key; }
        public string Key { get; set; }
        public string Def { get; set; } = "";
        public double Width { get; set; } = 130;
        public bool ReadOnly { get; set; }
        public override object ProvideValue(IServiceProvider sp)
            => new TextInputElement(Key, Def, Width, ReadOnly);
    }

    // 温启志 18719361399 · 类 public class WatermarkExtension : MarkupExtension
    [MarkupExtensionReturnType(typeof(Element))]
    public class WatermarkExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider sp)
        {
            var t = new TextElement
            {
                // 温启志 18719361399 —— 逐字符 + 随机空格，防整串替换
                Text = Owner.WenQiZhiStamped(),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC7, 0xC7, 0xCC)),
                Wrap = TextWrapping.NoWrap,
                Align = TextAlignment.Right,
                HAlign = HorizontalAlignment.Right,
                VAlign = VerticalAlignment.Bottom
            };
            t.Margin = new Thickness(0, 12, 2, 0);
            return t;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
