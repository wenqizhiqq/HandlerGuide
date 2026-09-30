// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（BoardPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_BoardPages
    internal static class WenQiZhiTag_BoardPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · BoardPages。</summary>
        internal const string WenQiZhiFile = "BoardPages";
    }

    // ==================================================================
    //  “显示控制板”工具条：料状态 / 统计 / 监测 共 11 个状态窗口
    //  复刻 image113 ~ image123
    // ==================================================================
    // 温启志 18719361399 · 类 public static class BoardPages
    public static class BoardPages
    {
        private static CellElement Head(string s, double w = 0, double h = 24)
            => new CellElement
            {
                Background = E.HeadBlue, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Width = w > 0 ? w : double.NaN,
                Padding = new Thickness(2, 0, 2, 0),
                Content = E.T(s, 11, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)
            };

        private static CellElement Lab(string s, double w = 0, double h = 24, TextAlignment ta = TextAlignment.Left, Brush bg = null)
            => new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Width = w > 0 ? w : double.NaN,
                Padding = new Thickness(4, 0, 4, 0),
                Content = E.T(s, 11, E.P, false, TextWrapping.NoWrap, ta, HorizontalAlignment.Center)
            };

        private static CellElement Num(string s, double w = 0, double h = 24, Brush bg = null, Brush fg = null)
            => new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Width = w > 0 ? w : double.NaN,
                Padding = new Thickness(2, 0, 2, 0),
                Content = E.T(s, 11, fg ?? E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)
            };


        // ==================================================================
        //  4. 批次数据统计 (image119)
        // ==================================================================
        public static Element BoardBatch()
        {
            Func<Element> counterTab = () =>
            {
                var g = E.Grid().Cols("220", "110", "110");
                g.Add(Head("Counter", 220, 28), 0, 0);
                g.Add(Head("", 110, 28), 0, 1);
                g.Add(Head("", 110, 28), 0, 2);

                var rows = new (string label, string v, string? p)[]
                {
                    ("Load In", "0", null),
                    ("Load Out1(SHT-A)", "0", "0"),
                    ("Load Out1(SHT-B)", "0", null),
                    ("ShuttleA Out", "0", "0"),
                    ("ShuttleB Out", "0", null),
                    ("Offload Out", "0", null),
                    ("Auto1", "0", "0.00%"),
                    ("Auto2", "0", "0.00%"),
                    ("Auto3", "0", "0.00%"),
                    ("Fix1", "0", "0.00%"),
                    ("Fix2", "0", "0.00%"),
                    ("Fix3", "0", "0.00%"),
                    ("Jam Rate", "0/0", null),
                    ("RuntimeUPH", "0", null)
                };
                for (int r = 0; r < rows.Length; r++)
                {
                    g.Add(Lab(rows[r].label, 220, 28, TextAlignment.Center, E.BandGrey), r + 1, 0);
                    g.Add(Num(rows[r].v, 110, 28), r + 1, 1);
                    g.Add(Num(rows[r].p ?? "", 110, 28), r + 1, 2);
                }
                return g;
            };

            Func<string, Element> simpleTab = title =>
                E.Group(title,
                    E.T("（" + title + "统计视图，按各部分分别进行统计。）", 11.5, E.Sec, false, TextWrapping.Wrap));

            return E.V(
                
                E.Tabs(("Counter", counterTab),
                       ("UPH/Tray", () => simpleTab("UPH/Tray")),
                       ("Timer", () => simpleTab("Timer"))),
                E.Group("说明",
                    E.T("Load in：上料臂从上料小车吸取的产品数量；Load out1(SHT-A)/Load out2(SHT-B)：上料臂放到运料小车 A/B 上的产品数量；" +
                        "Offload out：下料臂放置到下料小车的产品数量；ShuttleA/B out：下料臂从运料小车 A/B 上吸取的产品数量。\n" +
                        "Auto1/2/3、Fix1/2/3：下料臂放置到对应盘的产品数量；Jam Rate：Jam 报警造成的停机次数，分母为所处理的产品数量；" +
                        "Runtime UPH：运行时间内经过测试并重新收纳至料盘中的产品数量。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  5. 测试工位统计-Site (image116)
        // ==================================================================
        public static Element BoardSite()
        {
            string[] cols = { "a", "b", "c", "d", "e", "f", "g", "h" };
            string[] rows = { "A", "B", "C", "D" };

            var g = E.Grid();
            g.Cols("56", "150", "150", "150", "150", "150", "150", "150", "150");
            g.Add(Head("", 56, 28), 0, 0);
            g.Add(Head("测试位(0.00%)", 0, 28), 0, 1, 1, 8);
            for (int c = 0; c < cols.Length; c++)
                g.Add(Head(cols[c], 150, 30), 1, c + 1);
            for (int r = 0; r < rows.Length; r++)
            {
                g.Add(Head(rows[r], 56, 30), r + 2, 0);
                for (int c = 0; c < cols.Length; c++)
                    g.Add(Num("0.00%", 150, 30), r + 2, c + 1);
            }

            return E.V(
                
                E.Group("测试工位统计-Site", E.Scroll(g, 300,
                    System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto)),
                E.T("附注：当该窗口处于输入焦点时，可以通过 PgUp/PgDn 按钮对该窗口缩放大小。", 11.5, E.Sec));
        }

        // ==================================================================
        //  6. 测试工位统计-Bin (image117)
        // ==================================================================
        public static Element BoardBin()
        {
            string[] first = { "Aa", "Ca", "Ab", "Cb", "Ac", "Cc", "Ad", "Cd", "Ae", "Ce", "Af", "Cf", "Ag", "Cg", "Ah", "Ch" };
            string[] second = { "Ba", "Da", "Bb", "Db", "Bc", "Dc", "Bd", "Dd", "Be", "De", "Bf", "Df", "Bg", "Dg", "Bh", "Dh" };

            var colSpec = new List<string> { "96" };
            for (int i = 0; i < 32; i++) colSpec.Add("50");
            colSpec.Add("120");

            var g = E.Grid().Cols(colSpec.ToArray());
            g.Add(Head("Socket", 96, 24), 0, 0);
            for (int c = 0; c < 16; c++)
            {
                g.Add(Head(first[c], 50, 24), 0, c + 1);
                g.Add(Head(second[c], 50, 24), 0, c + 17);
            }
            g.Add(Head("Category Total", 120, 24), 0, 33);

            g.Add(Lab("Test Site", 96, 22, TextAlignment.Center, E.BandGrey), 1, 0);
            for (int c = 0; c < 16; c++)
            {
                g.Add(Num("Site" + (2 * c + 1), 50, 22, E.BandGrey), 1, c + 1);
                g.Add(Num("Site" + (2 * c + 2), 50, 22, E.BandGrey), 1, c + 17);
            }
            g.Add(Num("", 120, 22, E.BandGrey), 1, 33);

            g.Add(Lab("Chuck", 96, 22, TextAlignment.Center, E.BandGrey), 2, 0);
            for (int c = 0; c < 16; c++)
            {
                g.Add(Num("A", 50, 22, E.BandGrey), 2, c + 1);
                g.Add(Num("B", 50, 22, E.BandGrey), 2, c + 17);
            }
            g.Add(Num("", 120, 22, E.BandGrey), 2, 33);

            string[] bins = { "NT", "00", "01", "02", "03", "04", "05", "06", "07", "08",
                              "09", "10", "11", "12", "13", "14", "15", "16" };
            for (int r = 0; r < bins.Length; r++)
            {
                bool bad = r > 0;
                g.Add(Lab(bins[r], 96, 20, TextAlignment.Center), r + 3, 0);
                for (int c = 0; c < 32; c++)
                    g.Add(Num("0", 50, 20, E.CardBg, bad ? Palette.OnRed : Palette.OnGreen), r + 3, c + 1);
                g.Add(Num("0", 120, 20, E.CardBg, bad ? Palette.OnRed : Palette.OnGreen), r + 3, 33);
            }

            return E.V(
                
                E.Scroll(g, 460, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto),
                E.T("附注：双击窗口标题可在数字 / 百分比两种显示风格间切换。", 11.5, E.Sec));
        }

        // ==================================================================
        //  7. 结果历史 (image123)
        // ==================================================================
        public static Element BoardHistory()
        {
            string[] rows = { "Aa", "Ba", "Ca", "Da", "Ab", "Bb", "Cb", "Db", "Ac", "Bc", "Cc", "Dc",
                              "Ad", "Bd", "Cd", "Dd", "Ae", "Be", "Ce", "De", "Af", "Bf", "Cf", "Df",
                              "Ag", "Bg", "Cg", "Dg", "Ah", "Bh", "Ch", "Dh" };

            var colSpec = new List<string> { "66" };
            for (int i = 0; i < 32; i++) colSpec.Add("46");

            var g = E.Grid().Cols(colSpec.ToArray());
            g.Add(Head("Socket", 66, 24), 0, 0);
            for (int c = 0; c < 16; c++)
            {
                g.Add(Head("TA", 46, 22), 0, 2 * c + 1);
                g.Add(Head("TB", 46, 22), 0, 2 * c + 2);
            }

            for (int r = 0; r < rows.Length; r++)
            {
                g.Add(Lab(rows[r], 66, 20, TextAlignment.Center, E.BandBlue), r + 1, 0);
                for (int c = 0; c < 32; c++)
                    g.Add(Num("NT", 46, 20, E.BandBlue), r + 1, c + 1);
            }

            int tr = rows.Length + 1;
            g.Add(Num("T.Tm(ms)", 66, 20, E.HeadBlue), tr, 0);
            for (int c = 0; c < 32; c++) g.Add(Num("0", 46, 20, E.HeadBlue), tr, c + 1);
            g.Add(Num("Idx(ms)", 66, 20, E.HeadBlue), tr + 1, 0);
            for (int c = 0; c < 32; c++) g.Add(Num("0", 46, 20, E.HeadBlue), tr + 1, c + 1);

            return E.V(
                E.T("该窗口显示测试座或测试臂各个位置测试结果的历史，可以显示最近的 16 个结果。最下面两行分别为测试时间和 Index 时间，" +
                    "最后一列为连续次品的计数(分子)和阈值(分母)，最后一列最下两行为测试时间和 Index 时间的平均值。",
                    11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(10),
                E.Scroll(g, 480, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto));
        }


        // ==================================================================
        //  9. 料盘分类 (image118)
        // ==================================================================
        public static Element BoardTrayClass()
        {
            Func<string, Brush, string, Brush, Element> cell = (title, headBg, body, bodyColor) =>
                E.V(
                    E.Box(E.T(title, 12, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                        headBg, Palette.LineGrey, 1, 0, new Thickness(4, 3, 4, 3), new Thickness(0, 0, 3, 3), 150, 30),
                    E.Box(E.T(body, 12, bodyColor, true, TextWrapping.Wrap, TextAlignment.Center, HorizontalAlignment.Center),
                        E.CardBg, Palette.LineGrey, 1, 0, new Thickness(6), new Thickness(0, 0, 3, 3), 150, 96));

            var fix1 = cell("TrayFix1", E.HeadAmber, "NT 00 NG ER", Palette.OnRed);
            var fix2 = cell("TrayFix2", E.HeadAmber, "02", Palette.OnRed);
            var fix3 = cell("TrayFix3", E.HeadAmber, "", E.Sec);
            ((StackElement)fix3).Children[1].Margin = new Thickness(0, 0, 3, 3);

            return E.V(
                
                E.H(fix1, fix2, fix3),
                E.H(cell("TrayAuto1", E.HeadGreen, "01", Palette.OnGreen),
                    cell("TrayAuto2", E.HeadGreen, "04 06 17 18\n19 20 21 22\n23 24 25 26\n27 28 29 30\n31 32", Palette.OnRed),
                    cell("TrayAuto3", E.HeadGreen, "08", Palette.OnGreen)),
                E.Group("说明",
                    E.T("固定盘（TrayFix1~3）与自动盘（TrayAuto1~3）分别对应下料区域的 6 个物理料盘；" +
                        "单元格中列出的数字为该盘接收的 Bin 分类号，红色表示其中包含次品分类，绿色表示全部为好品分类。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  10. SPC 统计 (image122)
        // ==================================================================
        public static Element BoardSpc()
        {
            string[] lines =
            {
                "SPC", "",
                "Date:                2023/7/17 9:19:05",
                "Machine ID:          JHTNeoceed",
                "Total Alarm:         0",
                "Power On:            0:00:31:00",
                "Production:          0:00:00:35",
                "Operating:           0:00:00:35",
                "Idle:                0:00:00:00",
                "Unplanned:           0:00:30:24",
                "Total Down:          0:00:30:24",
                "MTBA:                0:00:31:00",
                "MTTA:                0:00:00:00",
                "MTBF:                0:00:00:00",
                "MTTR:                0:00:00:00",
                "MUBA:                0:00:00:00",
                "OEE:                 100.00%", "",
                "Auto1:               0",
                "Auto2:               0",
                "Auto3:               0",
                "Fix1:                0",
                "Fix2:                0",
                "Fix3:                0",
                "Total:               0",
                "Jam Statistics:",
                "------------------------------------------",
                "Jam rate:            0/0",
                "Alarm Occurence:     0",
                "Stoppage(s):         0:00:31:00",
                "Err Stoppage(s):     0:00:00:00", "",
                "Statistic from:      2023/7/17 8:48:05"
            };

            var sp = E.V();
            foreach (var ln in lines)
                sp.Children.Add(E.T(ln, 12, E.P, false, TextWrapping.NoWrap));

            return E.V(
                E.T("该窗口显示 SPC 的统计数据，数据的统计时间以天为单位，即从当天的 00:00 到当前的时间，每次到第二天的 00:00 时自动生成一个文件并计数清零。" +
                    "自动生成文件的位置位于：软件(D:)\\pnp\\report\\spc。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(10),
                E.Group("SPC", E.Scroll(sp, 470, System.Windows.Controls.ScrollBarVisibility.Disabled,
                    System.Windows.Controls.ScrollBarVisibility.Auto)),
                E.T("附注：如果“维护配置 → 安全设定”中的自动参数备份路径设定有效，该 SPC 文件也会自动保存到该远程路径下，具体位置为：设定路径\\report\\，文件名称为：(计算机名称)_SPC_YYYY-MM-DD.spcrp。",
                    11.5, E.Sec, false, TextWrapping.Wrap));
        }

        // ==================================================================
        //  11. 报警统计 (image120)
        // ==================================================================
        public static Element BoardAlarm()
        {
            var g = E.Grid().Cols("90", "80", "90", "110", "130", "*");
            g.Add(Head("Code", 90), 0, 0);
            g.Add(Head("Count", 80), 0, 1);
            g.Add(Head("Time(s)", 90), 0, 2);
            g.Add(Head("Unit", 110), 0, 3);
            g.Add(Head("SubModule", 130), 0, 4);
            g.Add(Head("Message"), 0, 5);

            string[,] data =
            {
                { "1001", "0", "0", "上料臂", "X 轴", "上料臂 X 轴伺服报警" },
                { "1002", "0", "0", "上料臂", "Z 轴", "上料臂吸头吸取失败" },
                { "1003", "0", "0", "测试区域", "小车", "运料小车入口浮动检查报警" },
                { "1004", "0", "0", "测试区域", "测试臂", "测试臂接触压力超限" },
                { "1005", "0", "0", "下料臂", "X 轴", "下料臂 X 轴伺服报警" },
                { "1006", "0", "0", "进出盘", "Loader", "料盘下行丢失报警" },
                { "1007", "0", "0", "进出盘", "Auto1", "料盘满未取出" },
                { "1008", "0", "0", "面板", "门", "安全门打开报警" }
            };
            for (int r = 0; r < data.GetLength(0); r++)
                for (int c = 0; c < 6; c++)
                    g.Add(Num(data[r, c], 0, 22, r % 2 == 0 ? E.CardBg : E.BandGrey,
                        c == 0 ? Palette.OnRed : E.P), r + 1, c);

            var toolbar = E.V(
                E.H(E.Chk("ba_batch", "当前批次", true), E.Chk("ba_date", "选择日期", false)),
                E.Spacer(6),
                E.H(
                    E.V(
                        E.H(E.T("开始日期", 11.5, E.P, false, TextWrapping.NoWrap), E.Text("ba_from", "2023/7/17", 110)),
                        E.H(E.T("结束日期", 11.5, E.P, false, TextWrapping.NoWrap), E.Text("ba_to", "2023/7/17", 110))),
                    E.SpacerW(24),
                    E.V(
                        E.H(E.T("类型", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.Cmb("ba_type", new[] { "JAM", "ALL", "ERROR" }, 120, 0)),
                        E.H(E.T("类型", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.Cmb("ba_type2", new[] { "Normal", "Detail" }, 120, 0))),
                    E.V(
                        E.H(E.T("单元", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.Cmb("ba_unit", new[] { "ALL", "上料臂", "测试区域", "下料臂", "进出盘" }, 130, 0)),
                        E.H(E.T("代码", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.Text("ba_code", "", 130))),
                    E.V(
                        E.H(E.T("子模块", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.Cmb("ba_sub", new[] { "ALL", "X 轴", "Z 轴", "小车", "测试臂" }, 130, 0)),
                        E.Spacer(2)),
                    E.V(E.Btn("更新", Ops.RefreshAlarmStats),
                        E.MiniBtn("导出", Ops.ExportAlarmStatsDialog))));

            return E.V(
                
                E.Group("报警日志统计", toolbar),
                E.Spacer(8),
                E.Scroll(g, 380, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto),
                E.T("附注：在窗口网格中点击鼠标右键的弹出菜单可以切换“详细” / “简单”显示模式。", 11.5, E.Sec));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
