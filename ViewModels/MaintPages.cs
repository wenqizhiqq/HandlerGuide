// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MaintPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_MaintPages
    internal static class WenQiZhiTag_MaintPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · MaintPages。</summary>
        internal const string WenQiZhiFile = "MaintPages";
    }

    // ==================================================================
    //  “维护配置”工具条：工作基点 / 接触力校准 / 用户管理   —— 复刻 image82 ~ image106
    //  工作基点（maint_base）与接触力校准（maint_force）已改为纯声明式 Pages/*.xaml；
    //  此处仅保留「用户管理」里数据驱动的用户表生成器（被 maint_user.xaml 以 <vw:D> 引用）。
    // ==================================================================
    // 温启志 18719361399 · 类 public static class MaintPages
    public static class MaintPages
    {
        private static CellElement Head(string s, double w = 0)
            => new CellElement
            {
                Background = E.HeadBlue, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 26,
                Width = w > 0 ? w : double.NaN,
                Padding = new Thickness(3, 1, 3, 1),
                Content = E.T(s, 11.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)
            };

        private static CellElement Label(string s, double h = 26, TextAlignment ta = TextAlignment.Left, Brush bg = null)
            => new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Padding = new Thickness(4, 1, 4, 1),
                Content = E.T(s, 11.5, E.P, false, TextWrapping.NoWrap, ta, HorizontalAlignment.Center)
            };

        private static CellElement Cell(Element e, double h = 26, Brush bg = null)
        {
            if (h < 34) e.Compact();   // 单元格较矮：输入框用紧凑样式，否则文本会被裁切
            return new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Content = e
            };
        }

        // ==================================================================
        //  用户管理 · 用户列表网格 (image105)
        //  数据驱动（行数为 8、每格实时刷新 UserCell），无法写成静态 XAML，
        //  故保留为生成器，供 Pages/maint_user.xaml 以 <vw:D Method="MaintUserGrid"/> 声明式引用。
        // ==================================================================
        public static Element MaintUserGrid()
        {
            var ug = E.Grid().Cols("200", "200", "200", "*");
            ug.Add(Head("User"), 0, 0);
            ug.Add(Head("Pass"), 0, 1);
            ug.Add(Head("Level"), 0, 2);
            ug.Add(Head(""), 0, 3);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int rr = r, cc = c;
                    ug.Add(Cell(E.Live(_ => Ops.UserCell(rr, cc), null, 190, E.CardBg, E.P, 11.5), 26), r + 1, c);
                }
                ug.Add(Label("", 26), r + 1, 3);
            }
            return ug;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
