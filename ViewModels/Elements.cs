using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // ==================================================================
    //  声明式界面元素视图模型
    //  视图（Views/Elements.xaml 的 DataTemplate）负责渲染，这里只描述“界面长什么样”。
    //  视图中不出现任何 FrameworkElement，所有交互通过 Command / 双向绑定完成。
    // ==================================================================

    public abstract class Element : ObservableObject
    {
        private Thickness _margin;
        private double _width = double.NaN;
        private double _height = double.NaN;
        private HorizontalAlignment _hAlign = HorizontalAlignment.Stretch;
        private VerticalAlignment _vAlign = VerticalAlignment.Center;
        private string _toolTip;

        /// <summary>外边距；未显式设置时视图层使用 <see cref="DefaultMargin"/>。</summary>
        public Thickness Margin
        {
            get => _margin;
            set { if (Set(ref _margin, value)) Raise(nameof(EffectiveMargin)); }
        }

        /// <summary>宽度 / 高度；0 会被当作“零宽”，需要自适应请用 double.NaN。</summary>
        public double Width { get => _width; set => Set(ref _width, value); }
        public double Height { get => _height; set => Set(ref _height, value); }
        public HorizontalAlignment HAlign { get => _hAlign; set => Set(ref _hAlign, value); }
        public VerticalAlignment VAlign { get => _vAlign; set => Set(ref _vAlign, value); }
        public string ToolTip { get => _toolTip; set => Set(ref _toolTip, value); }

        /// <summary>子类可重写的默认外边距（例如分组卡片之间的间距）。</summary>
        protected virtual Thickness DefaultMargin => default(Thickness);

        /// <summary>视图层统一的绑定目标：显式 Margin 优先，否则用 DefaultMargin。</summary>
        public Thickness EffectiveMargin => _margin == default(Thickness) ? DefaultMargin : _margin;
    }

    // ---------------- 文本 ----------------

    public class TextElement : Element
    {
        private string _text = "";
        public string Text { get => _text; set => Set(ref _text, value); }
        public double FontSize { get; set; } = 12.5;
        public Brush Foreground { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;
        public TextWrapping Wrap { get; set; } = TextWrapping.NoWrap;
        public TextAlignment Align { get; set; } = TextAlignment.Left;
    }

    // ---------------- 布局容器 ----------------

    public class StackElement : Element
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        public List<Element> Children { get; } = new List<Element>();
    }

    public class WrapElement : Element
    {
        public List<Element> Children { get; } = new List<Element>();
    }

    /// <summary>等宽多列栅格。</summary>
    public class ColsElement : Element
    {
        public int Columns { get; set; } = 1;
        public List<Element> Children { get; } = new List<Element>();
    }

    /// <summary>显式行列定义的栅格（对应原代码里手写的 Grid）。</summary>
    public class GridElement : Element
    {
        private readonly List<string> _cols = new List<string>();
        private readonly List<string> _rows = new List<string>();
        public List<GridChild> Children { get; } = new List<GridChild>();

        public string ColsSpec => _cols.Count == 0 ? "*" : string.Join(",", _cols);
        public string RowsSpec => _rows.Count == 0 ? "Auto" : string.Join(",", _rows);

        public GridElement Col(string spec) { _cols.Add(spec); Raise(nameof(ColsSpec)); return this; }
        public GridElement Row(string spec) { _rows.Add(spec); Raise(nameof(RowsSpec)); return this; }
        public GridElement Cols(params string[] specs) { foreach (var s in specs) _cols.Add(s); Raise(nameof(ColsSpec)); return this; }
        public GridElement Rows(params string[] specs) { foreach (var s in specs) _rows.Add(s); Raise(nameof(RowsSpec)); return this; }

        public GridElement Add(Element e, int row = 0, int col = 0, int rowSpan = 1, int colSpan = 1)
        {
            if (e == null) return this;
            while (_rows.Count <= row) _rows.Add("Auto");
            while (_cols.Count <= col) _cols.Add("*");
            Children.Add(new GridChild { El = e, Row = row, Col = col, RowSpan = rowSpan, ColSpan = colSpan });
            Raise(nameof(Children));
            Raise(nameof(ColsSpec));
            Raise(nameof(RowsSpec));
            return this;
        }
    }

    public class GridChild
    {
        public Element El { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;
    }

    public class SpacerElement : Element
    {
        public double W { get; set; }
        public double H { get; set; }
    }

    public class BoxElement : Element
    {
        public Element Child { get; set; }
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public double BorderThickness { get; set; } = 1;
        public CornerRadius Radius { get; set; }
        public Thickness Padding { get; set; }
        public double ChildWidth { get; set; } = double.NaN;
        public double ChildHeight { get; set; } = double.NaN;
        public HorizontalAlignment ChildHAlign { get; set; } = HorizontalAlignment.Stretch;
        public bool Clickable { get; set; }
        public RelayCommand Command { get; set; }
    }

    public class ScrollElement : Element
    {
        public Element Content { get; set; }
        public double ViewHeight { get; set; } = double.NaN;
        public ScrollBarVisibility HScroll { get; set; } = ScrollBarVisibility.Disabled;
        public ScrollBarVisibility VScroll { get; set; } = ScrollBarVisibility.Auto;
    }

    // ---------------- 分组 ----------------

    public class GroupElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 0, 0, 12);

        public string Title { get; set; }
        public List<Element> Children { get; } = new List<Element>();
        public bool HasTitle => !string.IsNullOrEmpty(Title);
    }

    public class BandElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 0, 0, 10);

        public string Title { get; set; }
        public Brush Background { get; set; }
        public List<Element> Children { get; } = new List<Element>();
        public bool HasTitle => !string.IsNullOrEmpty(Title);
    }

    // ---------------- 表单行 ----------------

    public class FieldRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 3, 0, 3);

        public string Label { get; set; }
        public Element Control { get; set; }
        public double LabelWidth { get; set; } = 118;
        public string Unit { get; set; }
        public double Gap { get; set; } = 10;

        public string ColsSpec => LabelWidth.ToString(CultureInfo.InvariantCulture) + ",Auto,*";
        public bool HasLabel => !string.IsNullOrEmpty(Label);
        public bool HasUnit => !string.IsNullOrEmpty(Unit);
        public Thickness LabelMargin => new Thickness(0, 0, Gap, 0);
        public Thickness UnitMargin => new Thickness(Gap, 0, 0, 0);
    }

    public class FillRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 2, 0, 2);

        public string Label { get; set; }
        public Element Control { get; set; }
        public double LabelWidth { get; set; } = 96;

        public string ColsSpec => LabelWidth.ToString(CultureInfo.InvariantCulture) + ",*";
        public bool HasLabel => !string.IsNullOrEmpty(Label);
    }

    public class TwoColElement : Element
    {
        public List<TwoColPair> Pairs { get; } = new List<TwoColPair>();
    }

    public class TwoColPair
    {
        public Element Left { get; set; }
        public Element Right { get; set; }
    }

    // ---------------- 输入控件 ----------------

    public class NumberElement : Element
    {
        private readonly string _key;
        private readonly double _def, _min, _max;
        private readonly string _fmt;
        private string _text;
        private bool _busy;

        public NumberElement(string key, double def, double w, string fmt, double? min, double? max, bool readOnly)
        {
            _key = key; _def = def; _fmt = fmt ?? "0.###";
            _min = min ?? double.NaN; _max = max ?? double.NaN;
            Width = w; IsReadOnly = readOnly;
            _text = MachineModel.Instance.GetNum(key, def).ToString(_fmt, CultureInfo.InvariantCulture);
        }

        public bool IsReadOnly { get; set; }

        /// <summary>紧凑显示（放进表格单元格时使用，降低内边距避免文本被裁切）。</summary>
        public bool Compact { get; set; }

        public string Text
        {
            get => _text;
            set
            {
                if (!Set(ref _text, value) || _busy) return;
                var m = MachineModel.Instance;
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                {
                    if (!double.IsNaN(_min)) v = Math.Max(_min, v);
                    if (!double.IsNaN(_max)) v = Math.Min(_max, v);
                    m.SetNum(_key, v);
                    _busy = true;
                    _text = v.ToString(_fmt, CultureInfo.InvariantCulture);
                    _busy = false;
                    Raise(nameof(Text));
                }
                else
                {
                    _busy = true;
                    _text = m.GetNum(_key, _def).ToString(_fmt, CultureInfo.InvariantCulture);
                    _busy = false;
                    Raise(nameof(Text));
                }
            }
        }
    }

    public class TextInputElement : Element
    {
        private readonly string _key;
        private string _text;

        public TextInputElement(string key, string def, double w, bool readOnly)
        {
            _key = key; Width = w; IsReadOnly = readOnly;
            _text = MachineModel.Instance.GetText(key, def ?? "");
        }

        public bool IsReadOnly { get; set; }

        /// <summary>紧凑显示（放进表格单元格时使用）。</summary>
        public bool Compact { get; set; }

        public string Text
        {
            get => _text;
            set { if (Set(ref _text, value)) MachineModel.Instance.SetText(_key, value ?? ""); }
        }
    }

    public class ComboElement : Element
    {
        private readonly string _key;
        private readonly string[] _opts;
        private int _index;

        public ComboElement(string key, string[] opts, double w, int def)
        {
            _key = key; _opts = opts ?? new string[0]; Width = w;
            _index = Math.Min(Math.Max(MachineModel.Instance.GetChoice(key, def), 0), Math.Max(_opts.Length - 1, 0));
        }

        public string[] Items => _opts;
        public Action<int> OnChanged { get; set; }

        public int SelectedIndex
        {
            get => _index;
            set
            {
                if (!Set(ref _index, value)) return;
                if (value >= 0) MachineModel.Instance.SetChoice(_key, value);
                OnChanged?.Invoke(value);
            }
        }
    }

    public class CheckElement : Element
    {
        private readonly string _key;
        private bool _checked;

        public CheckElement(string key, string label, bool def)
        {
            _key = key; Label = label;
            _checked = MachineModel.Instance.GetToggle(key, def);
        }

        public string Label { get; set; }
        public bool BoxVisible { get; set; } = true;
        public TextAlignment Align { get; set; } = TextAlignment.Left;

        public bool IsChecked
        {
            get => _checked;
            set { if (Set(ref _checked, value)) MachineModel.Instance.SetToggle(_key, value); }
        }
    }

    public class SwitchElement : Element
    {
        private readonly string _key;
        private bool _on;

        public SwitchElement(string key, bool def)
        {
            _key = key;
            _on = MachineModel.Instance.GetToggle(key, def);
        }

        public bool IsChecked
        {
            get => _on;
            set { if (Set(ref _on, value)) MachineModel.Instance.SetToggle(_key, value); }
        }
    }

    public class RadioElement : Element
    {
        private readonly string _key;
        private readonly int _index;
        private bool _on;

        public RadioElement(string key, string label, int index, bool on, double gap)
        {
            _key = key; Label = label; _index = index; _on = on;
            GroupName = "g_" + key;
            Margin = new Thickness(0, 3, gap, 3);
        }

        public string Label { get; set; }
        public string GroupName { get; set; }

        public bool IsChecked
        {
            get => _on;
            set { if (Set(ref _on, value) && value) MachineModel.Instance.SetChoice(_key, _index); }
        }
    }

    public class ButtonElement : Element
    {
        public string Text { get; set; }
        public RelayCommand Command { get; set; }
        public string StyleKey { get; set; }
        public double FontSize { get; set; } = 12.5;
        public Thickness Padding { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public Brush BorderBrush { get; set; }
        public Thickness? BorderThickness { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;
    }

    public class SliderElement : Element
    {
        private readonly string _key;
        private double _value;

        public SliderElement(string key, double min, double max, double def, double w)
        {
            _key = key; Min = min; Max = max; Width = w;
            _value = MachineModel.Instance.GetNum(key, def);
        }

        public double Min { get; set; }
        public double Max { get; set; }

        public double Value
        {
            get => _value;
            set
            {
                if (!Set(ref _value, value)) return;
                MachineModel.Instance.SetNum(_key, Math.Round(value));
                Raise(nameof(ValueText));
            }
        }

        public string ValueText => _value.ToString("0") + "%";
    }

    public class SliderRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 4, 0, 4);

        private readonly string _key;
        private double _value;

        public SliderRowElement(string label, string key, double def)
        {
            Label = label; _key = key;
            _value = MachineModel.Instance.GetNum(key, def);
        }

        public string Label { get; set; }
        public double Min => 0;
        public double Max => 200;

        public double Value
        {
            get => _value;
            set
            {
                if (!Set(ref _value, value)) return;
                MachineModel.Instance.SetNum(_key, Math.Round(value));
                Raise(nameof(ValueText));
            }
        }

        public string ValueText => _value.ToString("0") + "%";
    }

    // ---------------- 只读显示 / 指示灯 ----------------

    public class LampElement : Element
    {
        private bool _on;
        public bool IsOn { get => _on; set { if (Set(ref _on, value)) Raise(nameof(Fill)); } }
        public bool IsRed { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; } = 13;
        public Brush Fill => _on ? (IsRed ? Palette.OnRed : Palette.OnGreen) : Palette.OffDot;
        public CornerRadius Rad => new CornerRadius(Square ? 2 : Size / 2);
    }

    public class ClickLampElement : Element
    {
        private readonly string _key;
        private bool _on;

        public ClickLampElement(string key, bool isRed, bool square, double size)
        {
            _key = key; IsRed = isRed; Square = square; Size = size;
            Margin = new Thickness(2, 0, 2, 0);
            _on = MachineModel.Instance.GetToggle(key);
            ToggleCommand = new RelayCommand(() => IsOn = !IsOn);
        }

        public bool IsRed { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; }
        public RelayCommand ToggleCommand { get; set; }
        public Brush Fill => _on ? (IsRed ? Palette.OnRed : Palette.OnGreen) : Palette.OffDot;
        public CornerRadius Rad => new CornerRadius(Square ? 2 : Size / 2);

        public bool IsOn
        {
            get => _on;
            set { if (Set(ref _on, value)) { MachineModel.Instance.SetToggle(_key, value); Raise(nameof(Fill)); } }
        }
    }

    /// <summary>由定时器驱动的只读指示灯（诊断页状态灯）。</summary>
    public class LiveLampElement : Element, ILiveValue
    {
        private readonly Func<MachineModel, bool> _get;
        private bool _on;

        public LiveLampElement(Func<MachineModel, bool> get, bool isRed, bool square, double size)
        {
            _get = get; IsRed = isRed; Square = square; Size = size;
            Margin = new Thickness(2, 0, 2, 0);
            LiveRegistry.Register(this);
            Refresh();
        }

        public bool IsRed { get; set; }
        public bool Square { get; set; }
        public double Size { get; set; }
        public Brush Fill => _on ? (IsRed ? Palette.OnRed : Palette.OnGreen) : Palette.OffDot;
        public CornerRadius Rad => new CornerRadius(Square ? 2 : Size / 2);

        public bool IsOn
        {
            get => _on;
            private set { if (Set(ref _on, value)) Raise(nameof(Fill)); }
        }

        public void Refresh()
        {
            try { IsOn = _get(MachineModel.Instance); }
            catch { IsOn = false; }
        }
    }

    public class DisplayElement : Element
    {
        private string _text;
        public DisplayElement(string text, double w, Brush bg, Brush fg, TextAlignment ta, double fs)
        {
            _text = text ?? ""; Width = w > 0 ? w : double.NaN;
            Background = bg; Foreground = fg; Align = ta; FontSize = fs;
        }

        public string Text { get => _text; set => Set(ref _text, value); }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public TextAlignment Align { get; set; } = TextAlignment.Center;
        public double FontSize { get; set; } = 12.5;
    }

    /// <summary>绑定到模型的实时只读显示框（由 LiveRegistry 每 tick 刷新）。</summary>
    public class LiveElement : Element, ILiveValue
    {
        private readonly Func<MachineModel, object> _get;
        private readonly string _fmt;
        private string _value = "";

        public LiveElement(Func<MachineModel, object> get, string fmt, double w, Brush bg, Brush fg, double fs)
        {
            _get = get; _fmt = fmt; Width = w > 0 ? w : double.NaN;
            Background = bg; Foreground = fg; FontSize = fs;
            LiveRegistry.Register(this);
            Refresh();
        }

        public string Value { get => _value; private set => Set(ref _value, value); }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public double FontSize { get; set; } = 13;
        public TextAlignment Align { get; set; } = TextAlignment.Center;

        public void Refresh()
        {
            object raw;
            try { raw = _get(MachineModel.Instance); }
            catch { raw = null; }
            string s = _fmt != null ? string.Format(CultureInfo.InvariantCulture, _fmt, raw) : (raw?.ToString() ?? "");
            Value = s;
        }
    }

    // ---------------- 表格 / 矩阵 ----------------

    public class CellElement : Element
    {
        public Element Content { get; set; }
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public double BorderThickness { get; set; } = 0.5;
        public Thickness Padding { get; set; }
    }

    public class CellRow : Element
    {
        public List<CellElement> Cells { get; } = new List<CellElement>();
    }

    public class MatrixRow : Element
    {
        public CellElement Head { get; set; }
        public List<Element> Cells { get; } = new List<Element>();
    }

    public class MatrixElement : Element
    {
        public List<CellElement> Header { get; } = new List<CellElement>();
        public List<MatrixRow> Rows { get; } = new List<MatrixRow>();
        public double CellWidth { get; set; } = 40;
        public double CellHeight { get; set; } = 26;
        public double RowHeadWidth { get; set; } = 96;
        public double ColHeadHeight { get; set; } = 26;
    }

    public class CellGridElement : Element
    {
        public List<CellRow> Rows { get; } = new List<CellRow>();
        public double CellWidth { get; set; } = 17;
        public double CellHeight { get; set; } = 17;
    }

    // ---------------- 图形（料盘 / 机台布局 / 吸头） ----------------

    public abstract class DiagramItem
    {
        public double X { get; set; }
        public double Y { get; set; }
        public virtual double Left => X;
        public virtual double Top => Y;
    }

    public class RectItem : DiagramItem
    {
        public double W { get; set; }
        public double H { get; set; }
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; } = 1;
        public double Radius { get; set; }
        public string Text { get; set; }
        public double TextSize { get; set; } = 10.5;
        public Brush TextColor { get; set; }
        public FontWeight TextWeight { get; set; } = FontWeights.SemiBold;
    }

    public class EllipseItem : DiagramItem
    {
        public double W { get; set; }
        public double H { get; set; }
        public Brush Fill { get; set; }
    }

    public class TextItem : DiagramItem
    {
        public string Text { get; set; }
        public double TextSize { get; set; } = 11;
        public Brush Foreground { get; set; }
        public double TextWidth { get; set; } = double.NaN;
        public TextAlignment Align { get; set; } = TextAlignment.Center;
    }

    public class CanvasElement : Element
    {
        public double CanvasWidth { get; set; } = 300;
        public double CanvasHeight { get; set; } = 200;
        public List<DiagramItem> Items { get; } = new List<DiagramItem>();
    }

    // ---------------- 标签页 ----------------

    public class TabItemVm : ObservableObject
    {
        private bool _active;
        private Element _content;

        public TabItemVm(string title, Func<Element> build) { Title = title; Build = build; }

        public string Title { get; set; }
        public Func<Element> Build { get; set; }
        public RelayCommand SelectCommand { get; set; }

        public bool IsActive { get => _active; set => Set(ref _active, value); }

        public Element Content
        {
            get => _content;
            set => Set(ref _content, value);
        }
    }

    public class TabsElement : Element
    {
        public List<TabItemVm> Tabs { get; } = new List<TabItemVm>();
        public Element SelectedContent => Tabs.FirstOrDefault(t => t.IsActive)?.Content;
        public void RefreshContent() => Raise(nameof(SelectedContent));
    }
}
