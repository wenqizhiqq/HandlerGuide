using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // ==================================================================
    //  E —— 界面元素工厂（对应旧版 Ui 工具箱，但产出的是视图模型而不是控件）
    // ==================================================================
    public static class E
    {
        public static MachineModel M => MachineModel.Instance;

        public static Brush R(string key) => Palette.R(key);
        public static Brush P => R("TextPrimaryBrush");
        public static Brush Sec => R("TextSecondaryBrush");
        public static Brush Acc => R("AccentBrush");
        public static Brush Div => R("DividerBrush");
        public static Brush CardBg => R("CardBgBrush");
        public static Brush WinBg => R("WinBgBrush");
        public static Brush Ok => R("SuccessBrush");
        public static Brush Warn => R("WarningBrush");
        public static Brush Tip => R("TipBrush");

        public static Brush BandBlue => Palette.BandBlue;
        public static Brush BandGreen => Palette.BandGreen;
        public static Brush BandAmber => Palette.BandAmber;
        public static Brush BandGrey => Palette.BandGrey;
        public static Brush BandRed => Palette.BandRed;
        public static Brush HeadBlue => Palette.HeadBlue;
        public static Brush HeadGreen => Palette.HeadGreen;
        public static Brush HeadAmber => Palette.HeadAmber;
        public static Brush HeadTeal => Palette.HeadTeal;
        public static Brush HeadGreyCell => Palette.CellDark;
        public static Brush LineGrey => Palette.LineGrey;
        public static Brush OffDot => Palette.OffDot;
        public static Brush OnRed => Palette.OnRed;
        public static Brush OnGreen => Palette.OnGreen;

        // ---------------- 基础元素 ----------------

        public static TextElement T(string text, double fs = 12.5, Brush fg = null, bool bold = false,
            TextWrapping wrap = TextWrapping.NoWrap, TextAlignment ta = TextAlignment.Left,
            HorizontalAlignment ha = HorizontalAlignment.Left, VerticalAlignment va = VerticalAlignment.Center)
        {
            return new TextElement
            {
                Text = text ?? "",
                FontSize = fs,
                Foreground = fg ?? P,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Wrap = wrap,
                Align = ta,
                HAlign = ha,
                VAlign = va
            };
        }

        public static TextElement Title(string text, double fs = 15) => T(text, fs, P, true);

        public static Element AsElement(object o)
        {
            if (o == null) return new StackElement();
            if (o is Element e) return e;
            if (o is string s) return T(s);
            if (o is IEnumerable<object> list) return V(list.ToArray());
            throw new ArgumentException("不支持的元素类型: " + o.GetType());
        }

        public static void Add(StackElement sp, object o)
        {
            if (o == null) return;
            if (o is IEnumerable<object> list && !(o is string))
            {
                foreach (var x in list) Add(sp, x);
                return;
            }
            sp.Children.Add(AsElement(o));
        }

        public static StackElement V(params object[] xs)
        {
            var sp = new StackElement { Orientation = Orientation.Vertical };
            foreach (var x in xs) Add(sp, x);
            return sp;
        }

        public static StackElement H(params object[] xs)
        {
            var sp = new StackElement { Orientation = Orientation.Horizontal };
            foreach (var x in xs) Add(sp, x);
            return sp;
        }

        public static WrapElement Wrap(params object[] xs)
        {
            var sp = new WrapElement();
            foreach (var x in xs) sp.Children.Add(AsElement(x));
            return sp;
        }

        public static ColsElement Cols(int n, params object[] xs)
        {
            var g = new ColsElement { Columns = n };
            for (int i = 0; i < xs.Length && i < n; i++) g.Children.Add(AsElement(xs[i]));
            return g;
        }

        public static SpacerElement Spacer(double h) => new SpacerElement { H = h };
        public static SpacerElement SpacerW(double w) => new SpacerElement { W = w };

        public static T Margin<T>(this T e, double l, double t, double r, double b) where T : Element
        {
            e.Margin = new Thickness(l, t, r, b);
            return e;
        }

        public static T Marg<T>(this T e, Thickness t) where T : Element { e.Margin = t; return e; }
        public static T Wd<T>(this T e, double w) where T : Element { e.Width = w; return e; }
        public static T Ht<T>(this T e, double h) where T : Element { e.Height = h; return e; }
        public static T Align<T>(this T e, HorizontalAlignment ha) where T : Element { e.HAlign = ha; return e; }

        /// <summary>
        /// 把元素及其子树里的输入框切换为紧凑样式。
        /// 表格单元格高度通常只有 22~30px，而标准输入框内边距较大，
        /// 直接放进去会导致文本被裁切而不显示。
        /// </summary>
        public static T Compact<T>(this T e) where T : Element
        {
            switch (e)
            {
                case NumberElement n: n.Compact = true; break;
                case TextInputElement t: t.Compact = true; break;
                case StackElement s: foreach (var c in s.Children) c.Compact(); break;
                case WrapElement w: foreach (var c in w.Children) c.Compact(); break;
                case ColsElement co: foreach (var c in co.Children) c.Compact(); break;
                case GridElement g: foreach (var c in g.Children) c.El.Compact(); break;
                case CellElement ce: ce.Content?.Compact(); break;
                case FieldRowElement fr: fr.Control?.Compact(); break;
                case FillRowElement fl: fl.Control?.Compact(); break;
            }
            return e;
        }

        public static GridElement Grid() => new GridElement();

        public static BoxElement Box(Element child, Brush bg = null, Brush border = null, double bt = 1,
            double radius = 0, Thickness pad = default(Thickness), Thickness margin = default(Thickness),
            double width = 0, double height = 0)
        {
            return new BoxElement
            {
                Child = child,
                Background = bg,
                BorderBrush = border,
                BorderThickness = bt,
                Radius = new CornerRadius(radius),
                Padding = pad,
                Margin = margin,
                Width = width > 0 ? width : double.NaN,
                Height = height > 0 ? height : double.NaN
            };
        }

        public static ScrollElement Scroll(Element content, double height = 0,
            ScrollBarVisibility h = ScrollBarVisibility.Disabled, ScrollBarVisibility v = ScrollBarVisibility.Auto)
        {
            // 纵向默认撑满：放进固定高度容器（如主界面三列）时占满可用高度
            return new ScrollElement { Content = content, ViewHeight = height > 0 ? height : double.NaN,
                HScroll = h, VScroll = v, VAlign = VerticalAlignment.Stretch };
        }

        // ---------------- 分组卡片 ----------------

        public static GroupElement Group(string title, params object[] content)
        {
            var g = new GroupElement { Title = title };
            foreach (var c in content)
            {
                if (c is IEnumerable<object> list && !(c is string)) { foreach (var x in list) g.Children.Add(AsElement(x)); }
                else g.Children.Add(AsElement(c));
            }
            return g;
        }

        public static BandElement Band(string title, Brush bg, params object[] content)
        {
            var g = new BandElement { Title = title, Background = bg };
            foreach (var c in content)
            {
                if (c is IEnumerable<object> list && !(c is string)) { foreach (var x in list) g.Children.Add(AsElement(x)); }
                else g.Children.Add(AsElement(c));
            }
            return g;
        }

        // ---------------- 表单行 ----------------

        public static FieldRowElement Row(string label, object ctrl, double labelW = 118, string unit = null, double gap = 10)
            => new FieldRowElement { Label = label, Control = AsElement(ctrl), LabelWidth = labelW, Unit = unit, Gap = gap };

        public static FillRowElement RowFill(string label, object ctrl, double labelW = 96)
            => new FillRowElement { Label = label, Control = AsElement(ctrl), LabelWidth = labelW };

        public static TwoColElement TwoCol(params object[] rows)
        {
            var g = new TwoColElement();
            foreach (var item in rows)
                if (item is object[] pair && pair.Length == 2)
                    g.Pairs.Add(new TwoColPair { Left = AsElement(pair[0]), Right = AsElement(pair[1]) });
            return g;
        }

        // ---------------- 输入控件 ----------------

        public static NumberElement Num(string key, double def = 0, double w = 92, string fmt = "0.###",
            double? min = null, double? max = null, bool readOnly = false)
            => new NumberElement(key, def, w, fmt, min, max, readOnly);

        public static TextInputElement Text(string key, string def = "", double w = 160, bool readOnly = false)
            => new TextInputElement(key, def, w, readOnly);

        public static ComboElement Cmb(string key, string[] opts, double w = 150, int def = 0)
            => new ComboElement(key, opts, w, def);

        public static CheckElement Chk(string key, string label, bool def = false)
            => new CheckElement(key, label, def);

        public static SwitchElement Switch(string key, bool def = false)
            => new SwitchElement(key, def);

        public static StackElement Radios(string key, string[] opts, bool vertical = true, int def = 0)
        {
            var sp = new StackElement { Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal };
            int cur = M.GetChoice(key, def);
            for (int i = 0; i < opts.Length; i++)
            {
                var rb = new RadioElement(key, opts[i], i, i == cur, vertical ? 0 : 18);
                sp.Children.Add(rb);
            }
            return sp;
        }

        public static ButtonElement Btn(string text, Action act = null, string style = "PrimaryButton", double fs = 12.5)
        {
            return new ButtonElement
            {
                Text = text,
                Command = act == null ? null : new RelayCommand(act),
                StyleKey = style,
                FontSize = fs,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 2, 8, 2)
            };
        }

        public static ButtonElement MiniBtn(string text, Action act = null, double w = 0)
        {
            return new ButtonElement
            {
                Text = text,
                Command = act == null ? null : new RelayCommand(act),
                StyleKey = "MiniButton",
                FontSize = 12,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 2, 6, 2),
                Width = w > 0 ? w : double.NaN
            };
        }

        public static SliderElement Sld(string key, double min, double max, double def, double w = 300)
            => new SliderElement(key, min, max, def, w);

        public static SliderRowElement SldRow(string label, string key, double def = 100)
            => new SliderRowElement(label, key, def);

        // ---------------- 只读指示灯 / 显示框 ----------------

        public static LampElement Lamp(bool on, bool square = false, double size = 13)
            => new LampElement { IsOn = on, Square = square, Size = size, Margin = new Thickness(2, 0, 2, 0) };

        public static LampElement RedLamp(bool on, bool square = false, double size = 13)
            => new LampElement { IsOn = on, IsRed = true, Square = square, Size = size, Margin = new Thickness(2, 0, 2, 0) };

        public static ClickLampElement ClickLamp(string key, bool isRed = false, bool square = false, double size = 13)
            => new ClickLampElement(key, isRed, square, size);

        public static LiveLampElement LiveLamp(Func<MachineModel, bool> get, bool isRed = false, bool square = false, double size = 13)
            => new LiveLampElement(get, isRed, square, size);

        public static DisplayElement Display(string text, double w = 0, Brush bg = null, Brush fg = null,
            TextAlignment ta = TextAlignment.Center, double fs = 12.5)
            => new DisplayElement(text, w, bg ?? BandGrey, fg ?? P, ta, fs);

        public static LiveElement Live(Func<MachineModel, object> get, string fmt = null, double w = 0, Brush bg = null,
            Brush fg = null, double fs = 13)
            => new LiveElement(get, fmt, w, bg ?? BandGrey, fg ?? P, fs);

        // ---------------- 矩阵 / 表格 ----------------

        public static CellElement Cell(string text, Brush bg, Brush fg = null, bool bold = false,
            TextAlignment ta = TextAlignment.Center)
        {
            return new CellElement
            {
                Background = bg,
                BorderBrush = LineGrey,
                BorderThickness = 0.5,
                Padding = new Thickness(3, 1, 3, 1),
                Content = T(text, 11.5, fg ?? P, bold, TextWrapping.NoWrap, ta, HorizontalAlignment.Center)
            };
        }

        public static CellElement CheckCell(string key, bool def = false, bool showBox = true)
        {
            var chk = Chk(key, "", def);
            chk.BoxVisible = showBox;
            chk.HAlign = HorizontalAlignment.Center;
            return new CellElement
            {
                Background = CardBg,
                BorderBrush = LineGrey,
                BorderThickness = 0.5,
                Content = chk
            };
        }

        public static MatrixElement Matrix(string[] colHeads, string[] rowHeads,
            Func<int, int, Element> cellFactory, double cellW = 40, double cellH = 26,
            Brush headBg = null, Brush rowHeadBg = null, double rowHeadW = 96, double colHeadH = 26)
        {
            var mx = new MatrixElement
            {
                CellWidth = cellW,
                CellHeight = cellH,
                RowHeadWidth = rowHeadW,
                ColHeadHeight = colHeadH
            };
            var blank = Cell("", headBg ?? HeadBlue, P, true);
            blank.Width = rowHeadW; blank.Height = colHeadH;
            mx.Header.Add(blank);
            for (int c = 0; c < colHeads.Length; c++)
            {
                var h = Cell(colHeads[c], headBg ?? HeadBlue, P, true);
                h.Width = cellW; h.Height = colHeadH;
                mx.Header.Add(h);
            }
            for (int r = 0; r < rowHeads.Length; r++)
            {
                var row = new MatrixRow();
                row.Head = Cell(rowHeads[r], rowHeadBg ?? HeadBlue, P, true);
                row.Head.Width = rowHeadW; row.Head.Height = cellH;
                for (int c = 0; c < colHeads.Length; c++)
                {
                    var e = cellFactory(r, c);
                    if (e == null) e = Cell("", CardBg);
                    e.Width = cellW; e.Height = cellH;
                    row.Cells.Add(e);
                }
                mx.Rows.Add(row);
            }
            return mx;
        }

        public static CellGridElement CellGrid(int rows, int cols, double cw = 17, double ch = 17,
            Func<int, int, Brush> bgFactory = null, Func<int, int, string> textFactory = null)
        {
            var g = new CellGridElement { CellWidth = cw, CellHeight = ch };
            for (int r = 0; r < rows; r++)
            {
                var row = new CellRow();
                for (int c = 0; c < cols; c++)
                {
                    var cell = new CellElement
                    {
                        Width = cw,
                        Height = ch,
                        BorderBrush = LineGrey,
                        BorderThickness = 0.5,
                        Background = bgFactory != null ? bgFactory(r, c) : new SolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xEC)),
                        Content = textFactory != null
                            ? T(textFactory(r, c), 8, P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)
                            : null
                    };
                    row.Cells.Add(cell);
                }
                g.Rows.Add(row);
            }
            return g;
        }

        public static Element TrayStatus(string title, int cols, int rows, double cw = 17, double ch = 17)
        {
            return V(
                T(title, 12, P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                CellGrid(rows, cols, cw, ch)
            ).Marg(new Thickness(0, 0, 14, 0));
        }

        // ---------------- 图形 ----------------

        public static CanvasElement TrayDiagram(int nx, int ny, double cell = 34, string name = null)
        {
            nx = Math.Max(1, Math.Min(nx, 10));
            ny = Math.Max(1, Math.Min(ny, 12));
            double padL = 78, padT = 54, padR = 76, padB = 40;
            double w = nx * cell, h = ny * cell;
            var cv = new CanvasElement { CanvasWidth = padL + w + padR, CanvasHeight = padT + h + padB };

            Action<double, double, double, double, double, Brush> line = (x1, y1, x2, y2, th, br) =>
            {
                // 所有标注线都是水平/竖直的，用细长矩形代替 Line（避免 Shape 在模板容器中的坐标问题）
                bool horiz = Math.Abs(y1 - y2) < 0.001;
                cv.Items.Add(new RectItem
                {
                    X = horiz ? Math.Min(x1, x2) : x1 - th / 2,
                    Y = horiz ? y1 - th / 2 : Math.Min(y1, y2),
                    W = horiz ? Math.Abs(x2 - x1) : th,
                    H = horiz ? th : Math.Abs(y2 - y1),
                    Fill = br ?? LineGrey,
                    StrokeThickness = 0
                });
            };
            Action<string, double, double, double> txt = (s, x, y, fs) =>
                cv.Items.Add(new TextItem
                {
                    Text = s, X = x - 35, Y = y, TextSize = fs, Foreground = P,
                    TextWidth = 70, Align = TextAlignment.Center
                });

            cv.Items.Add(new RectItem
            {
                X = padL, Y = padT, W = w, H = h,
                Fill = new SolidColorBrush(Color.FromRgb(0xD6, 0xD6, 0xDA)),
                Stroke = LineGrey, StrokeThickness = 1, Radius = 0
            });

            for (int r = 0; r < ny; r++)
                for (int c = 0; c < nx; c++)
                    cv.Items.Add(new RectItem
                    {
                        X = padL + c * cell + 4, Y = padT + r * cell + 4,
                        W = cell - 8, H = cell - 8,
                        Fill = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73)), StrokeThickness = 0
                    });

            line(padL, padT - 26, padL + cell, padT - 26, 1, null);
            line(padL, padT - 31, padL, padT - 21, 1, null);
            line(padL + cell, padT - 31, padL + cell, padT - 21, 1, null);
            txt("X起点", padL + cell / 2 - 14, padT - 48, 11);
            line(padL + cell, padT - 26, padL + 2 * cell, padT - 26, 1, null);
            line(padL + 2 * cell, padT - 31, padL + 2 * cell, padT - 21, 1, null);
            txt("X间距", padL + cell * 1.5 - 14, padT - 48, 11);
            line(padL + 2 * cell, padT - 26, padL + w, padT - 26, 1, null);
            txt("X数量", padL + w - 40, padT - 48, 11);

            line(padL - 26, padT, padL - 26, padT + cell, 1, null);
            line(padL - 31, padT, padL - 21, padT, 1, null);
            line(padL - 31, padT + cell, padL - 21, padT + cell, 1, null);
            txt("Y起点", padL - 70, padT + cell / 2 - 8, 11);
            line(padL - 26, padT + cell, padL - 26, padT + 2 * cell, 1, null);
            line(padL - 31, padT + 2 * cell, padL - 21, padT + 2 * cell, 1, null);
            txt("Y间距", padL - 70, padT + cell * 1.5 - 8, 11);
            line(padL - 26, padT + 2 * cell, padL - 26, padT + h, 1, null);
            txt("Y数量", padL - 70, padT + h - 12, 11);

            if (!string.IsNullOrEmpty(name))
                cv.Items.Add(new TextItem { Text = name, X = padL, Y = padT + h + 10, TextSize = 11.5, Foreground = Sec, TextWidth = 160, Align = TextAlignment.Left });

            return cv;
        }

        public static CanvasElement TraySide(double h = 200)
        {
            var cv = new CanvasElement { CanvasWidth = 44, CanvasHeight = h };
            cv.Items.Add(new RectItem
            {
                X = 14, Y = 20, W = 16, H = h * 0.8,
                Fill = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE4)),
                Stroke = LineGrey, StrokeThickness = 1
            });
            return cv;
        }

        public static CanvasElement PadGrid(int cols = 8, int rows = 14, double cell = 7)
        {
            var cv = new CanvasElement { CanvasWidth = cols * cell + 6, CanvasHeight = rows * cell + 6 };
            cv.Items.Add(new RectItem
            {
                X = 3, Y = 3, W = cols * cell, H = rows * cell,
                Stroke = LineGrey, StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF3))
            });
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cv.Items.Add(new RectItem
                    {
                        X = 3 + c * cell + 1, Y = 3 + r * cell + 1,
                        W = cell - 2, H = cell - 2, StrokeThickness = 0,
                        Fill = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x90))
                    });
            return cv;
        }

        public static Element ArmHead(string title, params object[] headerRows)
        {
            double w = 8 * 74;
            var beam = Box(null, new LinearGradientBrush(Color.FromRgb(0x8F, 0xA8, 0xE8), Color.FromRgb(0x5C, 0x7C, 0xC8), 90), null, 0, 6, default(Thickness), default(Thickness), w, 30);

            var heads = new CanvasElement { CanvasWidth = w, CanvasHeight = 200 };
            for (int i = 0; i < 8; i++)
            {
                int col = i % 4;
                int row = i / 4;
                heads.Items.Add(new RectItem
                {
                    X = col * 74 + 30, Y = row == 0 ? 4 : 68, W = 14, H = 60,
                    Fill = new LinearGradientBrush(Color.FromRgb(0xBD, 0xD4, 0xF0), Color.FromRgb(0x6D, 0x8F, 0xC4), 0),
                    StrokeThickness = 0
                });
                heads.Items.Add(new RectItem
                {
                    X = col * 74 + 20, Y = row == 0 ? 60 : 124, W = 34, H = 26, Radius = 4,
                    Fill = new LinearGradientBrush(Color.FromRgb(0x9F, 0xB8, 0xE0), Color.FromRgb(0x4A, 0x6A, 0xA8), 90),
                    StrokeThickness = 0,
                    Text = "P" + (i + 1), TextSize = 10.5, TextColor = Brushes.White
                });
            }

            var box = new StackElement { Orientation = Orientation.Vertical };
            box.Children.Add(T(title, 15, P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center).Marg(new Thickness(0, 0, 0, 10)));
            box.Children.Add(beam);
            foreach (var r in headerRows) box.Children.Add(AsElement(r));
            box.Children.Add(heads);

            return Box(box, new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF8)), null, 0, 10, new Thickness(14));
        }

        public static CanvasElement MachineMap()
        {
            var cv = new CanvasElement { CanvasWidth = 470, CanvasHeight = 430 };

            Action<double, double, double, double, Color, string, double, Color?> box = (x, y, w, h, c, text, fs, fc) =>
                cv.Items.Add(new RectItem
                {
                    X = x, Y = y, W = w, H = h, Radius = 3,
                    Fill = new SolidColorBrush(c),
                    Stroke = LineGrey, StrokeThickness = 0.5,
                    Text = text, TextSize = fs, TextColor = new SolidColorBrush(fc ?? Colors.White)
                });
            Action<double, double, double, double, Color, string> bar = (x, y, w, h, c, text) => box(x, y, w, h, c, text, 10.5, Colors.White);

            box(6, 6, 40, 76, Color.FromRgb(0xC0, 0x20, 0x2A), "加热盘1", 10.5, Colors.White);
            box(50, 6, 66, 40, Color.FromRgb(0x5B, 0x9B, 0xD5), "小车A", 10.5, Colors.White);
            box(50, 48, 66, 34, Color.FromRgb(0x4A, 0x7F, 0xC0), "小车B", 10.5, Colors.White);
            box(120, 6, 66, 40, Color.FromRgb(0xF0, 0xC0, 0x6A), "测试臂1", 10.5, Colors.White);
            box(120, 48, 66, 34, Color.FromRgb(0xF0, 0xC0, 0x6A), "测试臂2", 10.5, Colors.White);
            box(190, 6, 44, 40, Color.FromRgb(0xE0, 0xE0, 0xE4), "固定盘1", 10.5, Colors.White);
            box(238, 6, 44, 40, Color.FromRgb(0xE0, 0xE0, 0xE4), "固定盘2", 10.5, Colors.White);
            box(286, 6, 44, 40, Color.FromRgb(0xE0, 0xE0, 0xE4), "固定盘3", 10.5, Colors.White);
            box(190, 48, 66, 34, Color.FromRgb(0xC0, 0x20, 0x2A), "加热盘2", 10.5, Colors.White);

            bar(0, 92, 340, 14, Color.FromRgb(0x5B, 0xC0, 0xE0), "上料臂");

            box(6, 112, 62, 60, Color.FromRgb(0xC0, 0x20, 0x2A), "上料盘", 10.5, Colors.White);
            box(72, 112, 62, 60, Color.FromRgb(0x8A, 0x8A, 0xE0), "上料空盘", 10.5, Colors.White);
            box(138, 112, 62, 60, Color.FromRgb(0x8A, 0x8A, 0xE0), "下料空盘", 10.5, Colors.White);
            box(204, 112, 62, 60, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘1", 10.5, Colors.White);
            box(270, 112, 62, 60, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘2", 10.5, Colors.White);
            box(336, 112, 62, 60, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘3", 10.5, Colors.White);

            bar(0, 180, 340, 14, Color.FromRgb(0x5B, 0xC0, 0xE0), "下料臂");
            bar(80, 200, 200, 14, Color.FromRgb(0x5B, 0xC0, 0xE0), "空盘传送臂");

            box(6, 226, 60, 58, Color.FromRgb(0x8A, 0xB8, 0xD8), "上料盘", 10.5, Colors.White);
            box(70, 226, 60, 58, Color.FromRgb(0x8A, 0x8A, 0xE0), "上料空盘", 10.5, Colors.White);
            box(134, 226, 60, 58, Color.FromRgb(0x6A, 0x6A, 0xD0), "下料空盘", 10.5, Colors.White);
            box(198, 226, 60, 58, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘1", 10.5, Colors.White);
            box(262, 226, 60, 58, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘2", 10.5, Colors.White);
            box(326, 226, 60, 58, Color.FromRgb(0x2E, 0x9E, 0x5B), "自动盘3", 10.5, Colors.White);

            cv.Items.Add(new TextItem
            {
                Text = "对应手册“偏移设定”机台分区示意图",
                X = 6, Y = 296, TextSize = 10.5, Foreground = Sec,
                TextWidth = 300, Align = TextAlignment.Left
            });
            return cv;
        }

        public static Element JogBar()
        {
            string[] steps = { "-10mm", "-1mm", "-0.1mm", "-0.01mm", "0.01mm", "0.1mm", "1mm", "10mm", "++", "+", "-", "--" };
            var sp = new WrapElement();
            foreach (var s in steps)
                sp.Children.Add(MiniBtn(s, () => M.Log("信息", "Jog " + s)));
            return sp;
        }

        public static Element LampLine(string label, int count, bool red = true, bool square = false, string prefix = null, double labelW = 76)
        {
            var sp = H(T(label, 12, P, false, TextWrapping.NoWrap));
            sp.Children[0].Width = labelW;
            sp.Children[0].VAlign = VerticalAlignment.Center;
            for (int i = 0; i < count; i++)
            {
                string key = (prefix ?? label) + "_" + i;
                sp.Children.Add(ClickLamp(key, red, square));
            }
            return sp;
        }

        // ---------------- 作者水印（防替换） ----------------
        //  字符数组 + 运行时随机空格：源码中不存在连续的「温启志18719361399」，
        //  他人无法通过查找/替换整串来抹掉作者标记；每个字符之间插入 1~3 个随机空格。
        private static readonly char[] _ownerChars =
            { '温', '启', '志', '1', '8', '7', '1', '9', '3', '6', '1', '3', '9', '9' };
        private static readonly Random _wmRnd = new Random(20240921); // 固定种子 → 间距稳定可复现

        /// <summary>
        /// 生成作者水印：淡色、右对齐，每个字符间随机 1~3 个空格。
        /// 显示在每一个页面的底部角落，作为版权/所有权标记。
        /// </summary>
        public static Element OwnerWatermark()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _ownerChars.Length; i++)
            {
                sb.Append(_ownerChars[i]);
                if (i < _ownerChars.Length - 1)
                    sb.Append(' ', _wmRnd.Next(1, 4));
            }
            var t = T(sb.ToString(), 9, new SolidColorBrush(Color.FromRgb(0xC7, 0xC7, 0xCC)), false,
                      TextWrapping.NoWrap, TextAlignment.Right, HorizontalAlignment.Right, VerticalAlignment.Bottom);
            t.Margin = new Thickness(0, 12, 2, 0);
            return t;
        }

        // ---------------- 标签页 ----------------

        public static TabsElement Tabs(params (string title, Func<Element> build)[] tabs)
        {
            var t = new TabsElement();
            foreach (var tab in tabs)
            {
                var vm = new TabItemVm(tab.title, tab.build);
                vm.SelectCommand = new RelayCommand(() => SelectTab(t, vm));
                t.Tabs.Add(vm);
            }
            if (t.Tabs.Count > 0) SelectTab(t, t.Tabs[0]);
            return t;
        }

        public static void SelectTab(TabsElement t, TabItemVm vm)
        {
            foreach (var x in t.Tabs) x.IsActive = ReferenceEquals(x, vm);
            if (vm.Content == null) vm.Content = vm.Build != null ? vm.Build() : new StackElement();
            t.RefreshContent();
        }
    }
}
