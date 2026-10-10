// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Elements）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Elements
    internal static class WenQiZhiTag_Elements
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Elements。</summary>
        internal const string WenQiZhiFile = "Elements";
    }

    // ==================================================================
    //  声明式界面元素视图模型
    //  视图（Views/Elements.xaml 的 DataTemplate）负责渲染，这里只描述“界面长什么样”。
    //  视图中不出现任何 FrameworkElement，所有交互通过 Command / 双向绑定完成。
    // ==================================================================

    // 温启志 18719361399 · 类 public abstract class Element : ObservableObject
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

    // 温启志 18719361399 · 类 public class TextElement : Element
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

[ContentProperty("Children")]
        public class StackElement : Element
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        public List<Element> Children { get; } = new List<Element>();
    }

[ContentProperty("Children")]
        public class WrapElement : Element
    {
        public List<Element> Children { get; } = new List<Element>();
    }

    [ContentProperty("Children")]
    /// <summary>等宽多列栅格。</summary>
        public class ColsElement : Element
    {
        public int Columns { get; set; } = 1;
        public List<Element> Children { get; } = new List<Element>();
    }

    [ContentProperty("Children")]
    /// <summary>显式行列定义的栅格（对应原代码里手写的 Grid）。</summary>
        public class GridElement : Element
    {
        private List<string> _cols = new List<string>();
        private List<string> _rows = new List<string>();
        public List<GridChild> Children { get; } = new List<GridChild>();

        public string ColsSpec
        {
            get => _cols.Count == 0 ? "*" : string.Join(",", _cols);
            set { _cols = (value ?? "*").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList(); Raise(nameof(ColsSpec)); }
        }
        public string RowsSpec
        {
            get => _rows.Count == 0 ? "Auto" : string.Join(",", _rows);
            set { _rows = (value ?? "Auto").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList(); Raise(nameof(RowsSpec)); }
        }

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

    // 温启志 18719361399 · 类 public class GridChild
    [ContentProperty("El")]
    public class GridChild
    {
        public Element El { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;
    }

    // 温启志 18719361399 · 类 public class SpacerElement : Element
    public class SpacerElement : Element
    {
        public double W { get; set; }
        public double H { get; set; }
    }

[ContentProperty("Child")]
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

[ContentProperty("Content")]
        public class ScrollElement : Element
    {
        public Element Content { get; set; }
        public double ViewHeight { get; set; } = double.NaN;
        public ScrollBarVisibility HScroll { get; set; } = ScrollBarVisibility.Disabled;
        public ScrollBarVisibility VScroll { get; set; } = ScrollBarVisibility.Auto;
    }

    // ---------------- 分组 ----------------

[ContentProperty("Children")]
        public class GroupElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 0, 0, 2);

        public string Title { get; set; }
        public List<Element> Children { get; } = new List<Element>();
        public bool HasTitle => !string.IsNullOrEmpty(Title);
    }

[ContentProperty("Children")]
        public class BandElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 0, 0, 2);

        public string Title { get; set; }
        public Brush Background { get; set; }
        public List<Element> Children { get; } = new List<Element>();
        public bool HasTitle => !string.IsNullOrEmpty(Title);
    }

    // ---------------- 表单行 ----------------

[ContentProperty("Control")]
        public class FieldRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 2, 0, 2);

        public string Label { get; set; }
        public Element Control { get; set; }
        public double LabelWidth { get; set; } = 118;
        public string Unit { get; set; }
        public double Gap { get; set; } = 10;

        public string ColsSpec => LabelWidth.ToString(CultureInfo.InvariantCulture) + ",Auto,Auto,*";
        public bool HasLabel => !string.IsNullOrEmpty(Label);
        public bool HasUnit => !string.IsNullOrEmpty(Unit);
        public Thickness LabelMargin => new Thickness(0, 0, Gap, 0);
        public Thickness UnitMargin => new Thickness(Gap, 0, 0, 0);

        /// <summary>显式说明；为空时自动按 Label 到 ManualHints（说明书原文）里查。</summary>
        public string Hint { get; set; }
        public string ResolvedHint => !string.IsNullOrEmpty(Hint) ? Hint : ManualHints.For(Label);
        public bool HasHint => !string.IsNullOrEmpty(ResolvedHint);
        public Thickness HintMargin => new Thickness(12, 0, 0, 0);
        public string EffectiveToolTip => !string.IsNullOrEmpty(ToolTip) ? ToolTip : ResolvedHint;
    }

[ContentProperty("Control")]
        public class FillRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 1, 0, 1);

        public string Label { get; set; }
        public Element Control { get; set; }
        public double LabelWidth { get; set; } = 96;

        public string ColsSpec => LabelWidth.ToString(CultureInfo.InvariantCulture) + ",*,Auto";
        public bool HasLabel => !string.IsNullOrEmpty(Label);

        /// <summary>显式说明；为空时自动按 Label 到 ManualHints（说明书原文）里查。</summary>
        public string Hint { get; set; }
        public string ResolvedHint => !string.IsNullOrEmpty(Hint) ? Hint : ManualHints.For(Label);
        public bool HasHint => !string.IsNullOrEmpty(ResolvedHint);
        public Thickness HintMargin => new Thickness(12, 0, 0, 0);
        public string EffectiveToolTip => !string.IsNullOrEmpty(ToolTip) ? ToolTip : ResolvedHint;
    }

[ContentProperty("Pairs")]
        public class TwoColElement : Element
    {
        public List<TwoColPair> Pairs { get; } = new List<TwoColPair>();
    }

    // 温启志 18719361399 · 类 public class TwoColPair
    public class TwoColPair
    {
        public Element Left { get; set; }
        public Element Right { get; set; }
    }

    // ---------------- 输入控件 ----------------

    // 温启志 18719361399 · 类 public class NumberElement : Element
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

    // 温启志 18719361399 · 类 public class TextInputElement : Element
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

    // 温启志 18719361399 · 类 public class ComboElement : Element
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

    // 温启志 18719361399 · 类 public class CheckElement : Element
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

    // 温启志 18719361399 · 类 public class SwitchElement : Element
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

    // 温启志 18719361399 · 类 public class RadioElement : Element
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

    // 温启志 18719361399 · 类 public class ButtonElement : Element
    public class ButtonElement : Element, ILiveValue
    {
        private Func<bool> _enabled;
        private bool _isEnabled = true;

        private string _styleKey;

        public string Text { get; set; }
        public RelayCommand Command { get; set; }

        /// <summary>
        /// 样式资源键。赋值时会顺带补齐该样式的默认底色/描边（见 <see cref="ApplyStyleDefaults"/>）。
        /// </summary>
        public string StyleKey
        {
            get => _styleKey;
            set { _styleKey = value; ApplyStyleDefaults(); }
        }

        public double FontSize { get; set; } = 12.5;
        public Thickness Padding { get; set; }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public Brush BorderBrush { get; set; }
        public Thickness? BorderThickness { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;

        /// <summary>
        /// 按钮文字换行方式（温启志 18719361399）。
        /// 默认 NoWrap 保持旧行为；列宽不够的长文字按钮（如操作面板）
        /// 置为 Wrap 后会自动折行，避免文字被裁掉。
        /// </summary>
        public TextWrapping Wrap { get; set; } = TextWrapping.NoWrap;

        /// <summary>
        /// 动态可用性（温启志 18719361399）。
        /// 为 null 时按钮始终可用；非 null 时每个刷新周期重新求值，
        /// 让「主界面命令中心」的启动/暂停/继续/停止等按钮能跟着运行状态自动灰显。
        /// </summary>
        public Func<bool> Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (value != null)
                {
                    LiveRegistry.Register(this);
                    Refresh();
                }
            }
        }

        /// <summary>当前是否可用（绑定到视图的 IsEnabled）。</summary>
        public bool IsEnabled { get => _isEnabled; private set => Set(ref _isEnabled, value); }

        /// <summary>ILiveValue：按 <see cref="Enabled"/> 重新求值可用性。</summary>
        public void Refresh()
        {
            if (_enabled == null) return;
            bool v;
            try { v = _enabled(); }
            catch { v = false; }
            IsEnabled = v;
        }

        /// <summary>
        /// 按样式键补齐默认底色 / 文字色 / 描边（温启志 18719361399）。
        /// 视图模板用 <c>{Binding Background, Converter=UnsetIfNull}</c> 绑定这些属性，
        /// 而「绑定返回 UnsetValue」不会回退到 Style 的 Setter，导致样式里写好的配色被吃掉；
        /// 因此在元素这一侧补上同名默认值。只补 null，调用方显式设置的颜色始终优先。
        /// </summary>
        private void ApplyStyleDefaults()
        {
            switch (_styleKey)
            {
                case "PrimaryButton":
                    if (Background == null) Background = Palette.Accent;
                    if (Foreground == null) Foreground = Brushes.White;
                    if (BorderThickness == null) BorderThickness = new Thickness(0);
                    break;

                case "DangerButton":
                    if (Background == null) Background = Palette.OnRed;
                    if (Foreground == null) Foreground = Brushes.White;
                    if (BorderThickness == null) BorderThickness = new Thickness(0);
                    break;

                case "MiniButton":
                    if (Background == null) Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF4));
                    if (Foreground == null) Foreground = Palette.Primary;
                    if (BorderBrush == null) BorderBrush = Palette.Divider;
                    if (BorderThickness == null) BorderThickness = new Thickness(1);
                    break;

                case "CmdButton":
                    if (Background == null) Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF4));
                    if (Foreground == null) Foreground = Palette.Primary;
                    if (BorderBrush == null) BorderBrush = Palette.Divider;
                    if (BorderThickness == null) BorderThickness = new Thickness(1);
                    break;

                case "ListItemButton":
                    if (Background == null) Background = Brushes.White;
                    if (Foreground == null) Foreground = Palette.Primary;
                    if (BorderBrush == null) BorderBrush = Palette.Divider;
                    if (BorderThickness == null) BorderThickness = new Thickness(1);
                    break;
            }
        }
    }

    // 温启志 18719361399 · 类 public class SliderElement : Element
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

    // 温启志 18719361399 · 类 public class SliderRowElement : Element
    public class SliderRowElement : Element
    {
        /// <summary>该类型在视图中的默认外边距。</summary>
        protected override Thickness DefaultMargin => new Thickness(0, 2, 0, 2);

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

    // 温启志 18719361399 · 类 public class LampElement : Element
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

    // 温启志 18719361399 · 类 public class ClickLampElement : Element
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

    // 温启志 18719361399 · 类 public class LiveLampElement : Element, ILiveValue
    /// <summary>由定时器驱动的只读指示灯（诊断页状态灯）。</summary>
    public class LiveLampElement : Element, ILiveValue
    {
        private Func<MachineModel, bool> _get;
        private string _signal;
        private bool _on;

        public LiveLampElement() { }
        public LiveLampElement(Func<MachineModel, bool> get, bool isRed, bool square, double size)
        {
            _get = get; IsRed = isRed; Square = square; Size = size;
            Margin = new Thickness(2, 0, 2, 0);
            LiveRegistry.Register(this);
            Refresh();
        }

        /// <summary>XAML 用：通过命名信号从 LiveSignals 注册表取谓词。</summary>
        public string Signal
        {
            get => _signal;
            set
            {
                _signal = value;
                if (!string.IsNullOrEmpty(value) && LiveSignals.TryGet(value, out var f))
                {
                    _get = f;
                    LiveRegistry.Register(this);
                    Refresh();
                }
            }
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
            try { IsOn = _get != null ? _get(MachineModel.Instance) : false; }
            catch { IsOn = false; }
        }
    }

    // 温启志 18719361399 · 类 public class DisplayElement : Element
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

    // 温启志 18719361399 · 类 public class LiveElement : Element, ILiveValue
    /// <summary>绑定到模型的实时只读显示框（由 LiveRegistry 每 tick 刷新）。</summary>
    public class LiveElement : Element, ILiveValue
    {
        private Func<MachineModel, object> _get;
        private string _fmt;
        private string _signal;
        private string _value = "";

        public LiveElement() { }
        public LiveElement(Func<MachineModel, object> get, string fmt, double w, Brush bg, Brush fg, double fs)
        {
            _get = get; _fmt = fmt; Width = w > 0 ? w : double.NaN;
            Background = bg; Foreground = fg; FontSize = fs;
            LiveRegistry.Register(this);
            Refresh();
        }

        /// <summary>XAML 用：通过命名信号从 LiveValues 注册表取数值源。</summary>
        public string Signal
        {
            get => _signal;
            set
            {
                _signal = value;
                if (!string.IsNullOrEmpty(value) && LiveValues.TryGet(value, out var f))
                {
                    _get = f;
                    LiveRegistry.Register(this);
                    Refresh();
                }
            }
        }

        public string Format
        {
            get => _fmt;
            set { _fmt = value; Refresh(); }
        }

        public string Value { get => _value; private set => Set(ref _value, value); }
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public double FontSize { get; set; } = 13;
        public TextAlignment Align { get; set; } = TextAlignment.Center;

        /// <summary>文本换行方式（默认不换行；报文日志等长文本可设为 Wrap）。温启志 18719361399</summary>
        public TextWrapping Wrap { get; set; } = TextWrapping.NoWrap;

        /// <summary>文本框水平对齐（默认居中；报文日志等长文本设为 Left）。温启志 18719361399</summary>
        public HorizontalAlignment TextHAlign { get; set; } = HorizontalAlignment.Center;

        public void Refresh()
        {
            object raw;
            try { raw = _get != null ? _get(MachineModel.Instance) : null; }
            catch { raw = null; }

            // 格式串有两种约定，必须区分处理：
            //   · 含 '{' → 复合格式（string.Format，如 "{0}"）；
            //   · 不含 '{' → 数值/自定义格式（IFormattable，如 "0.###"、"0.00"）。
            // 之前一律走 string.Format，导致 "0.###" 这类数值格式被原样输出成字面量。
            string s;
            if (raw == null) s = "";
            else if (string.IsNullOrEmpty(_fmt)) s = raw.ToString();
            else if (_fmt.IndexOf('{') >= 0) s = string.Format(CultureInfo.InvariantCulture, _fmt, raw);
            else if (raw is IFormattable f) s = f.ToString(_fmt, CultureInfo.InvariantCulture);
            else s = raw.ToString();

            Value = s;
        }
    }

    // 温启志 18719361399 · 类 public class LiveBadgeElement : Element, ILiveValue
    /// <summary>
    /// 实时文字 + 实时底色的圆角徽标。
    /// 主界面「命令中心」的运行状态徽标用它：底色随「停止 / 运行 / 暂停」变化。
    /// </summary>
    public class LiveBadgeElement : Element, ILiveValue
    {
        private Func<MachineModel, string> _text;
        private Func<MachineModel, Brush> _bg;
        private string _value = "";
        private Brush _background;

        public LiveBadgeElement() { }

        public LiveBadgeElement(Func<MachineModel, string> text, Func<MachineModel, Brush> bg, double w, double fs)
        {
            _text = text; _bg = bg;
            Width = w > 0 ? w : double.NaN;
            FontSize = fs;
            Margin = new Thickness(0, 0, 7, 0);
            LiveRegistry.Register(this);
            Refresh();
        }

        public string Text { get => _value; private set => Set(ref _value, value); }
        public Brush Background { get => _background; private set => Set(ref _background, value); }
        public double FontSize { get; set; } = 13;

        public void Refresh()
        {
            try { Text = _text != null ? _text(MachineModel.Instance) : ""; }
            catch { Text = ""; }
            try { Background = _bg != null ? _bg(MachineModel.Instance) : null; }
            catch { /* 配色取不到时保留上一次 */ }
        }
    }

    // ---------------- 表格 / 矩阵 ----------------

[ContentProperty("Content")]
        public class CellElement : Element
    {
        public Element Content { get; set; }
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public double BorderThickness { get; set; } = 0.5;
        public Thickness Padding { get; set; }
    }

    // 温启志 18719361399 · 类 public class CellRow : Element
    public class CellRow : Element
    {
        public List<CellElement> Cells { get; } = new List<CellElement>();
    }

    // 温启志 18719361399 · 类 public class MatrixRow : Element
    public class MatrixRow : Element
    {
        public CellElement Head { get; set; }
        public List<Element> Cells { get; } = new List<Element>();
    }

    // 温启志 18719361399 · 类 public class MatrixElement : Element
    public class MatrixElement : Element
    {
        public List<CellElement> Header { get; } = new List<CellElement>();
        public List<MatrixRow> Rows { get; } = new List<MatrixRow>();
        public double CellWidth { get; set; } = 40;
        public double CellHeight { get; set; } = 26;
        public double RowHeadWidth { get; set; } = 96;
        public double ColHeadHeight { get; set; } = 26;
    }

    // 温启志 18719361399 · 类 public class CellGridElement : Element
    public class CellGridElement : Element
    {
        public List<CellRow> Rows { get; } = new List<CellRow>();
        public double CellWidth { get; set; } = 17;
        public double CellHeight { get; set; } = 17;
    }

    // ---------------- 图形（料盘 / 机台布局 / 吸头） ----------------

    // 温启志 18719361399 · 类 public abstract class DiagramItem : ObservableObject
    /// <summary>
    /// 画布图形基类。继承 ObservableObject 是为了让仿真动画可以逐帧
    /// 直接改写 X/Y/W/H/Fill/Text 等属性而无需重建整个 Items 集合
    /// （X/Y 变更会连带通知 Left/Top，因为 Canvas.Left/Top 绑定的是后者）。
    /// </summary>
    public abstract class DiagramItem : ObservableObject
    {
        private double _x;
        private double _y;

        public double X
        {
            get => _x;
            set { if (Set(ref _x, value)) Raise(nameof(Left)); }
        }

        public double Y
        {
            get => _y;
            set { if (Set(ref _y, value)) Raise(nameof(Top)); }
        }

        public virtual double Left => X;
        public virtual double Top => Y;
    }

    // 温启志 18719361399 · 类 public class RectItem : DiagramItem
    public class RectItem : DiagramItem
    {
        private double _w, _h;
        private Brush _fill;
        private string _text;

        public double W { get => _w; set => Set(ref _w, value); }
        public double H { get => _h; set => Set(ref _h, value); }
        public Brush Fill { get => _fill; set => Set(ref _fill, value); }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; } = 1;
        public double Radius { get; set; }
        public string Text { get => _text; set => Set(ref _text, value); }
        public double TextSize { get; set; } = 10.5;
        public Brush TextColor { get; set; }
        public FontWeight TextWeight { get; set; } = FontWeights.SemiBold;
    }

    // 温启志 18719361399 · 类 public class EllipseItem : DiagramItem
    public class EllipseItem : DiagramItem
    {
        private double _w, _h;
        private Brush _fill;

        public double W { get => _w; set => Set(ref _w, value); }
        public double H { get => _h; set => Set(ref _h, value); }
        public Brush Fill { get => _fill; set => Set(ref _fill, value); }
    }

    // 温启志 18719361399 · 类 public class TextItem : DiagramItem
    public class TextItem : DiagramItem
    {
        private string _text;
        private Brush _foreground;

        public string Text { get => _text; set => Set(ref _text, value); }
        public double TextSize { get; set; } = 11;
        public Brush Foreground { get => _foreground; set => Set(ref _foreground, value); }
        public double TextWidth { get; set; } = double.NaN;
        public TextAlignment Align { get; set; } = TextAlignment.Center;
    }

    // 温启志 18719361399 · 类 public class CanvasElement : Element, ILiveValue
    [ContentProperty("Items")]
    public class CanvasElement : Element, ILiveValue
    {
        public CanvasElement()
        {
            // 注册到「快速刷新」通道：仿真动画需要 20fps 级别的重绘，
            // 与普通读数（500ms）分开，避免拖慢整个界面。
            LiveRegistry.RegisterFast(this);
        }

        public double CanvasWidth { get; set; } = 300;
        public double CanvasHeight { get; set; } = 200;
        public List<DiagramItem> Items { get; } = new List<DiagramItem>();
        /// <summary>
        /// 自检用「包含性守卫」：布局页把「必须落在某个箱体里」的图形登记进来
        /// （闭包返回 null 表示合格，返回字符串表示越界描述），自检时逐帧校验。
        /// 生产运行时该列表为空，零开销。
        /// </summary>
        public List<Func<string>> Guards { get; } = new List<Func<string>>();

        /// <summary>跑一遍所有守卫，返回越界描述（空列表 = 全部合格）。</summary>
        public List<string> CheckGuards()
        {
            var bad = new List<string>();
            for (int i = 0; i < Guards.Count; i++)
            {
                try
                {
                    string s = Guards[i]();
                    if (!string.IsNullOrEmpty(s)) bad.Add(s);
                }
                catch (Exception ex)
                {
                    bad.Add("guard[" + i + "] 抛异常: " + ex.Message);
                }
            }
            return bad;
        }


        /// <summary>
        /// 逐帧重绘回调（仿真页用）。为 null 时本画布是静态图，不产生任何开销。
        /// </summary>
        public Action<CanvasElement> Draw { get; set; }

        /// <summary>由 LiveRegistry 的快速通道调用。</summary>
        public void Refresh()
        {
            try { Draw?.Invoke(this); }
            catch { /* 重绘异常不允许影响界面 */ }
        }
    }

    // ---------------- 标签页 ----------------

    // 温启志 18719361399 · 类 public class TabItemVm : ObservableObject
    [ContentProperty("Content")]
    public class TabItemVm : ObservableObject
    {
        private bool _active;
        private Element _content;

        public TabItemVm() { }
        public TabItemVm(string title, Func<Element> build) { Title = title; Build = build; }

        public string Title { get; set; }
        public Func<Element> Build { get; set; }
        public RelayCommand SelectCommand { get; set; }

        public bool IsActive { get => _active; set => Set(ref _active, value); }

        public Element Content
        {
            get
            {
                if (_content == null && Build != null)
                {
                    _content = Build();
                    // 页签内容是「点了才构建」的，构建完同样要冻结参数表单初值
                    Ops.FreezePageForms();
                }
                return _content;
            }
            set => Set(ref _content, value);
        }
    }

    // 温启志 18719361399 · 类 public class TabItemCollection : System.Collections.ObjectModel.ObservableCollection<TabItemVm>
    /// <summary>XAML 中作为 TabsElement 的内容集合；新增页签时自动接好 SelectCommand 并默认激活首项。</summary>
    public class TabItemCollection : System.Collections.ObjectModel.ObservableCollection<TabItemVm>
    {
        public TabsElement Parent { get; set; }

        protected override void InsertItem(int index, TabItemVm item)
        {
            base.InsertItem(index, item);
            if (item.SelectCommand == null)
                item.SelectCommand = new RelayCommand(() =>
                {
                    foreach (var x in this) x.IsActive = ReferenceEquals(x, item);
                    Parent?.RefreshContent();
                });
            if (Count == 1) item.IsActive = true;
        }

        protected override void RemoveItem(int index)
        {
            var it = this[index];
            base.RemoveItem(index);
            if (it != null && it.IsActive && Count > 0) this[0].IsActive = true;
            Parent?.RefreshContent();
        }
    }

    // 温启志 18719361399 · 类 public class TabsElement : Element
    [ContentProperty("Tabs")]
    public class TabsElement : Element
    {
        public TabItemCollection Tabs { get; } = new TabItemCollection();

        public TabsElement() { Tabs.Parent = this; }

        public Element SelectedContent => Tabs.FirstOrDefault(t => t.IsActive)?.Content;
        public void RefreshContent() => Raise(nameof(SelectedContent));
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
