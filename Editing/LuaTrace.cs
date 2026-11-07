// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
// ============================================================================
//  Editing/LuaTrace.cs —— 执行跟踪的两个可视化件（需求 58）
//
//  1) LuaTraceLineColorizer：脚本「执行到哪一行」—— 该行铺一层浅黄底（跟光标行区分开）。
//  2) LuaLineTimingMargin：编辑器右边缘的「行耗时」标尺 —— 每个执行过的行在右侧画
//     累计毫秒数；当前行用橙色强调。
//
//  数据来源：Flow/LuaFlowHost.cs。脚本里的 step(...) 在 Lua 侧被包了一层，
//  用 debug.getinfo(2,'l') 拿调用行号，再交给宿主累计「上一段 → 这一段」的耗时。
//  所以标尺上的毫秒数 = 该行那次 step 到下一次 step 之间实际花掉的时间。
// ============================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace HandlerGuide.Editing
{
    /// <summary>正在执行的那一行铺浅黄底。</summary>
    public sealed class LuaTraceLineColorizer : DocumentColorizingTransformer
    {
        static readonly Brush Bg = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC4));
        static readonly Brush BgPaused = new SolidColorBrush(Color.FromRgb(0xFF, 0xD9, 0xA8));

        /// <summary>当前执行行（0 = 没在跑）。</summary>
        public int Line { get; set; }
        /// <summary>这一行是不是断点停住的（停住时换橙色）。</summary>
        public bool Paused { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (Line <= 0 || line.LineNumber != Line) return;
            ChangeLinePart(line.Offset, line.EndOffset,
                v => v.TextRunProperties.SetBackgroundBrush(Paused ? BgPaused : Bg));
        }
    }

    /// <summary>编辑器右侧的「行耗时（ms）」标尺。</summary>
    /// <remarks>
    /// AvalonEdit 只有 LeftMargins，没有 RightMargins；所以这里用背景渲染器，
    /// 直接把数字画在文本视图的右边缘（KnownLayer.Selection，位于正文之下、背景之上）。
    /// </remarks>
    public sealed class LuaLineTimingRenderer : IBackgroundRenderer
    {
        /// <summary>取当前流程的（执行行, 行→ms 快照）；由编辑器注入。</summary>
        public Func<Tuple<int, Dictionary<int, double>>> Source;

        readonly Typeface _tf = new Typeface(new FontFamily("Cascadia Mono, Consolas"),
                                            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9F));
        static readonly Brush InkHot = new SolidColorBrush(Color.FromRgb(0xC0, 0x6A, 0x00));
        static readonly Brush Band = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext dc)
        {
            if (textView == null || !textView.VisualLinesValid) return;
            var src = Source;
            if (src == null) return;
            var data = src();
            if (data == null || data.Item2 == null || data.Item2.Count == 0) return;

            int cur = data.Item1;
            var map = data.Item2;
            double dpi = VisualTreeHelper.GetDpi(textView).PixelsPerDip;

            // 右侧 56px 的一条“标尺带”，先铺一层半透明白，避免和代码撞在一起
            double stripX = Math.Max(0, textView.ActualWidth - 60);
            dc.DrawRectangle(Band, null, new Rect(stripX, 0, 60, Math.Max(0, textView.ActualHeight)));

            foreach (var vl in textView.VisualLines)
            {
                int n = vl.FirstDocumentLine.LineNumber;
                double ms;
                if (!map.TryGetValue(n, out ms) || ms < 1) continue;

                var ft = new FormattedText(ms.ToString("0"), CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, _tf, 10.5, n == cur ? InkHot : Ink, dpi);
                dc.DrawText(ft, new Point(textView.ActualWidth - 8 - ft.Width,
                                          vl.VisualTop - textView.VerticalOffset + 2));
            }
        }
    }

    /// <summary>行号左边那一列断点：红点 = 已下断点，点一下切换。</summary>
    public sealed class LuaBreakpointMargin : AbstractMargin
    {
        /// <summary>取当前流程的断点行集合。</summary>
        public Func<HashSet<int>> Source;
        /// <summary>点一下切换断点；由编辑器注入。</summary>
        public Action<int> Toggle;

        public double MarginWidth { get; set; } = 18;

        static readonly Brush Dot = new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));

        protected override Size MeasureOverride(Size availableSize) => new Size(MarginWidth, 0);

        protected override void OnRender(DrawingContext dc)
        {
            var tv = TextView;
            if (tv == null || !tv.VisualLinesValid) return;
            var src = Source;
            var set = src != null ? src() : null;
            if (set == null || set.Count == 0) return;
            foreach (var vl in tv.VisualLines)
            {
                if (!set.Contains(vl.FirstDocumentLine.LineNumber)) continue;
                double y = vl.VisualTop - tv.VerticalOffset + vl.Height / 2;
                dc.DrawEllipse(Dot, null, new Point(MarginWidth / 2, y), 4.6, 4.6);
            }
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            var tv = TextView;
            if (tv == null || !tv.VisualLinesValid) return;
            var p = e.GetPosition(this);
            foreach (var vl in tv.VisualLines)
            {
                double top = vl.VisualTop - tv.VerticalOffset;
                if (p.Y < top || p.Y > top + vl.Height) continue;
                Toggle?.Invoke(vl.FirstDocumentLine.LineNumber);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }
    }
}
