// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandlerGuide.ViewModels;

namespace HandlerGuide.Views
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Diagrams）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Diagrams
    internal static class WenQiZhiTag_Diagrams
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = HandlerGuide.ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = HandlerGuide.ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Diagrams。</summary>
        internal const string WenQiZhiFile = "Diagrams";
    }

    // 温启志 18719361399 · 类 public static class Diagrams
    /// <summary>
    /// 数据驱动的示意图 / 表格生成器（原 E.cs 中的图形与矩阵部分）。
    /// 把这些“按模型数据循环构造 Element”的逻辑放到 Views 层，
    /// 页面 .xaml 通过 &lt;vw:D Method="..."/&gt; 声明式引用，ViewModels 中不再有任何 new/Children.Add。
    /// </summary>
    public static class Diagrams
    {
        // ---------------- 单元格助手 ----------------

        private static CellElement Cell(string text, Brush bg, Brush fg = null, bool bold = false,
            TextAlignment ta = TextAlignment.Center, double w = 0, double h = 0)
        {
            var c = new CellElement
            {
                Background = bg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Padding = new Thickness(3, 1, 3, 1),
                Width = w,
                Height = h
            };
            c.Content = new TextElement
            {
                Text = text ?? "",
                FontSize = 11.5,
                Foreground = fg ?? Palette.Primary,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Wrap = TextWrapping.NoWrap,
                Align = ta,
                HAlign = HorizontalAlignment.Center
            };
            return c;
        }

        private static CellElement CheckCell(string key, bool def = false, bool showBox = true)
        {
            var chk = new CheckElement(key, "", def) { BoxVisible = showBox, HAlign = HorizontalAlignment.Center };
            return new CellElement
            {
                Background = Palette.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Content = chk
            };
        }

        // ---------------- 矩阵 / 表格 ----------------

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
            var blank = Cell("", headBg ?? Palette.HeadBlue, Palette.Primary, true);
            blank.Width = rowHeadW; blank.Height = colHeadH;
            mx.Header.Add(blank);
            for (int c = 0; c < colHeads.Length; c++)
            {
                var h = Cell(colHeads[c], headBg ?? Palette.HeadBlue, Palette.Primary, true);
                h.Width = cellW; h.Height = colHeadH;
                mx.Header.Add(h);
            }
            for (int r = 0; r < rowHeads.Length; r++)
            {
                var row = new MatrixRow();
                row.Head = Cell(rowHeads[r], rowHeadBg ?? Palette.HeadBlue, Palette.Primary, true);
                row.Head.Width = rowHeadW; row.Head.Height = cellH;
                for (int c = 0; c < colHeads.Length; c++)
                {
                    var e = cellFactory(r, c);
                    if (e == null) e = Cell("", Palette.CardBg);
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
                        BorderBrush = Palette.LineGrey,
                        BorderThickness = 0.5,
                        Background = bgFactory != null ? bgFactory(r, c) : new SolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xEC)),
                        Content = textFactory != null
                            ? new TextElement
                            {
                                Text = textFactory(r, c),
                                FontSize = 8,
                                Foreground = Palette.Primary,
                                Wrap = TextWrapping.NoWrap,
                                Align = TextAlignment.Center,
                                HAlign = HorizontalAlignment.Center
                            }
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
            var v = new StackElement { Orientation = Orientation.Vertical };
            v.Children.Add(new TextElement
            {
                Text = title,
                FontSize = 12,
                Foreground = Palette.Primary,
                FontWeight = FontWeights.SemiBold,
                Wrap = TextWrapping.NoWrap,
                Align = TextAlignment.Center,
                HAlign = HorizontalAlignment.Center
            });
            v.Children.Add(CellGrid(rows, cols, cw, ch));
            v.Margin = new Thickness(0, 0, 14, 0);
            return v;
        }

        // ---------------- 示意图 ----------------

        public static CanvasElement TrayDiagram(int nx, int ny, double cell = 34, string name = null)
        {
            nx = Math.Max(1, Math.Min(nx, 10));
            ny = Math.Max(1, Math.Min(ny, 12));
            double padL = 78, padT = 54, padR = 76, padB = 40;
            double w = nx * cell, h = ny * cell;
            var cv = new CanvasElement { CanvasWidth = padL + w + padR, CanvasHeight = padT + h + padB };

            Action<double, double, double, double, double, Brush> line = (x1, y1, x2, y2, th, br) =>
            {
                bool horiz = Math.Abs(y1 - y2) < 0.001;
                cv.Items.Add(new RectItem
                {
                    X = horiz ? Math.Min(x1, x2) : x1 - th / 2,
                    Y = horiz ? y1 - th / 2 : Math.Min(y1, y2),
                    W = horiz ? Math.Abs(x2 - x1) : th,
                    H = horiz ? th : Math.Abs(y2 - y1),
                    Fill = br ?? Palette.LineGrey,
                    StrokeThickness = 0
                });
            };
            Action<string, double, double, double> txt = (s, x, y, fs) =>
                cv.Items.Add(new TextItem
                {
                    Text = s,
                    X = x - 35,
                    Y = y,
                    TextSize = fs,
                    Foreground = Palette.Primary,
                    TextWidth = 70,
                    Align = TextAlignment.Center
                });

            cv.Items.Add(new RectItem
            {
                X = padL,
                Y = padT,
                W = w,
                H = h,
                Fill = new SolidColorBrush(Color.FromRgb(0xD6, 0xD6, 0xDA)),
                Stroke = Palette.LineGrey,
                StrokeThickness = 1,
                Radius = 0
            });

            for (int r = 0; r < ny; r++)
                for (int c = 0; c < nx; c++)
                    cv.Items.Add(new RectItem
                    {
                        X = padL + c * cell + 4,
                        Y = padT + r * cell + 4,
                        W = cell - 8,
                        H = cell - 8,
                        Fill = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73)),
                        StrokeThickness = 0
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
                cv.Items.Add(new TextItem { Text = name, X = padL, Y = padT + h + 10, TextSize = 11.5, Foreground = Palette.Secondary, TextWidth = 160, Align = TextAlignment.Left });

            return cv;
        }

        public static CanvasElement TraySide(double h = 200)
        {
            var cv = new CanvasElement { CanvasWidth = 44, CanvasHeight = h };
            cv.Items.Add(new RectItem
            {
                X = 14,
                Y = 20,
                W = 16,
                H = h * 0.8,
                Fill = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE4)),
                Stroke = Palette.LineGrey,
                StrokeThickness = 1
            });
            return cv;
        }

        public static CanvasElement PadGrid(int cols = 8, int rows = 14, double cell = 7)
        {
            var cv = new CanvasElement { CanvasWidth = cols * cell + 6, CanvasHeight = rows * cell + 6 };
            cv.Items.Add(new RectItem
            {
                X = 3,
                Y = 3,
                W = cols * cell,
                H = rows * cell,
                Stroke = Palette.LineGrey,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF3))
            });
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cv.Items.Add(new RectItem
                    {
                        X = 3 + c * cell + 1,
                        Y = 3 + r * cell + 1,
                        W = cell - 2,
                        H = cell - 2,
                        StrokeThickness = 0,
                        Fill = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x90))
                    });
            return cv;
        }

        public static Element ArmHead(string title, params object[] headerRows)
        {
            double w = 8 * 74;
            var beam = new BoxElement
            {
                Background = new LinearGradientBrush(Color.FromRgb(0x8F, 0xA8, 0xE8), Color.FromRgb(0x5C, 0x7C, 0xC8), 90),
                Radius = new CornerRadius(6),
                Width = w,
                Height = 30
            };

            var heads = new CanvasElement { CanvasWidth = w, CanvasHeight = 200 };
            for (int i = 0; i < 8; i++)
            {
                int col = i % 4;
                int row = i / 4;
                heads.Items.Add(new RectItem
                {
                    X = col * 74 + 30,
                    Y = row == 0 ? 4 : 68,
                    W = 14,
                    H = 60,
                    Fill = new LinearGradientBrush(Color.FromRgb(0xBD, 0xD4, 0xF0), Color.FromRgb(0x6D, 0x8F, 0xC4), 0),
                    StrokeThickness = 0
                });
                heads.Items.Add(new RectItem
                {
                    X = col * 74 + 20,
                    Y = row == 0 ? 60 : 124,
                    W = 34,
                    H = 26,
                    Radius = 4,
                    Fill = new LinearGradientBrush(Color.FromRgb(0x9F, 0xB8, 0xE0), Color.FromRgb(0x4A, 0x6A, 0xA8), 90),
                    StrokeThickness = 0,
                    Text = "P" + (i + 1),
                    TextSize = 10.5,
                    TextColor = Brushes.White
                });
            }

            var box = new StackElement { Orientation = Orientation.Vertical };
            var titleEl = new TextElement
            {
                Text = title,
                FontSize = 15,
                Foreground = Palette.Primary,
                FontWeight = FontWeights.SemiBold,
                Wrap = TextWrapping.NoWrap,
                Align = TextAlignment.Center,
                HAlign = HorizontalAlignment.Center
            };
            titleEl.Margin = new Thickness(0, 0, 0, 10);
            box.Children.Add(titleEl);
            box.Children.Add(beam);
            box.Children.Add(heads);

            return new BoxElement
            {
                Child = box,
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF8)),
                Radius = new CornerRadius(10),
                Padding = new Thickness(14)
            };
        }

        public static CanvasElement MachineMap()
        {
            var cv = new CanvasElement { CanvasWidth = 470, CanvasHeight = 430 };

            Action<double, double, double, double, Color, string, double, Color?> box = (x, y, w, h, c, text, fs, fc) =>
                cv.Items.Add(new RectItem
                {
                    X = x,
                    Y = y,
                    W = w,
                    H = h,
                    Radius = 3,
                    Fill = new SolidColorBrush(c),
                    Stroke = Palette.LineGrey,
                    StrokeThickness = 0.5,
                    Text = text,
                    TextSize = fs,
                    TextColor = new SolidColorBrush(fc ?? Colors.White)
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
                X = 6,
                Y = 296,
                TextSize = 10.5,
                Foreground = Palette.Secondary,
                TextWidth = 300,
                Align = TextAlignment.Left
            });
            return cv;
        }

        public static Element JogBar()
        {
            string[] steps = { "-10mm", "-1mm", "-0.1mm", "-0.01mm", "0.01mm", "0.1mm", "1mm", "10mm", "++", "+", "-", "--" };
            var sp = new WrapElement();
            foreach (var s in steps)
                sp.Children.Add(new ButtonElement
                {
                    Text = s,
                    StyleKey = "MiniButton",
                    FontSize = 12,
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 2, 6, 2),
                    Command = PageCommands.Command("Log", "Jog " + s)
                });
            return sp;
        }

        public static Element LampLine(string label, int count, bool red = true, bool square = false, string prefix = null, double labelW = 76)
        {
            var sp = new StackElement { Orientation = Orientation.Horizontal };
            var lbl = new TextElement
            {
                Text = label,
                FontSize = 12,
                Foreground = Palette.Primary,
                Wrap = TextWrapping.NoWrap,
                VAlign = VerticalAlignment.Center
            };
            lbl.Width = labelW;
            sp.Children.Add(lbl);
            for (int i = 0; i < count; i++)
            {
                string key = (prefix ?? label) + "_" + i;
                sp.Children.Add(E.ClickLamp(key, red, square));
            }
            return sp;
        }

        // ---------------- 调度 ----------------

        /// <summary>按名称返回生成器结果（页面 XAML 通过 vw:D 调用）。未知名称返回空 Stack。</summary>
        public static Element InfoLog()
        {
            string[] heads = { "pidx", "UnitName", "SubArea", "Code", "Message", "DateTime", "Recover", "StopTime", "RunMode", "LotId", "MesId" };
            double[] widths = { 46, 76, 70, 100, 320, 150, 60, 70, 80, 60, 60 };
            // 温启志 18719361399：表格改为读真实日志（MachineModel.Logs）。
            // 每格用 LiveElement 绑定 Ops.LogCell(行, 列)，由 LiveRegistry 每 500ms 统一刷新，
            // 这样「更新 / 导出」之后表格内容会立刻跟着变，而不是永远显示死数据。
            var host = new StackElement();
            var head = new StackElement { Orientation = Orientation.Horizontal };
            for (int i = 0; i < heads.Length; i++)
                head.Children.Add(new DisplayElement(heads[i], widths[i], Palette.HeadBlue, Palette.Primary, TextAlignment.Left, 11) { Margin = new Thickness(0, 0, 1, 1) });
            host.Children.Add(head);

            for (int r = 0; r < 40; r++)
            {
                int rr = r;
                var row = new StackElement { Orientation = Orientation.Horizontal };
                for (int c = 0; c < heads.Length; c++)
                {
                    int cc = c;
                    row.Children.Add(new LiveElement(_ => ViewModels.Ops.LogCell(rr, cc), null, widths[c],
                        System.Windows.Media.Brushes.White, Palette.Primary, 11)
                    { Margin = new Thickness(0, 0, 1, 1) });
                }
                host.Children.Add(row);
            }
            return new ScrollElement { Content = host };
        }

public static Element Invoke(string method)
        {
            switch (method)
            {
                case "TrayDiagram": return TrayDiagram(4, 3);
                case "TraySide": return TraySide();
                case "PadGrid": return PadGrid();
                case "MachineMap": return MachineMap();
                case "JogBar": return JogBar();
                case "InfoLog": return InfoLog();
                default:
                    // 温启志 18719361399 · 反射回退：支持页面 XAML 经 vw:D 调任意
                    // “public static Element Xxx()” 构建器（与 InfoLog/MachineMap 同机制），
                    // 避免为每个页面在 switch 里加 case。找不到时返回空 Stack。
                    return InvokeByName(method) ?? new StackElement();
            }
        }

        // 温启志 18719361399 · 反射查找 ViewModels 程序集内“无参、返回 Element”的静态方法。
        private static Element InvokeByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var t in typeof(Diagrams).Assembly.GetTypes())
            {
                if (!t.IsAbstract || !t.IsSealed) continue; // 只扫静态类
                var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (m != null && typeof(Element).IsAssignableFrom(m.ReturnType))
                    return (Element)m.Invoke(null, null);
            }
            return null;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
