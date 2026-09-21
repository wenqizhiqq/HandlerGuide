using System;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // 主界面（复刻 image37：三列布局 + 底部版权条）
    public static class HomePages
    {
        public static Element MainScreen()
        {
            var m = E.M;
            var root = E.Grid().Cols("360", "*", "276");

            // ---------------- 左列 ----------------
            var left = E.V();

            left.Children.Add(E.RowFill("运行状态",
                E.H(E.Live(x => x.RunState, null, 120, E.BandGrey, E.P, 15)), 84));

            left.Children.Add(E.RowFill("运行模式",
                E.H(E.Live(x => x.Mode, "{0}", 0, E.BandGreen, E.Ok, 14),
                    E.SpacerW(6),
                    E.Live(x => x.OnlineText, null, 0, E.BandGreen, E.Ok, 12)), 84));

            var recipeCb = E.Cmb("recipe_sel", m.Recipes, 168);
            recipeCb.SelectedIndex = Array.IndexOf(m.Recipes, m.Recipe);
            recipeCb.OnChanged = i =>
            {
                if (i >= 0 && i < m.Recipes.Length)
                {
                    m.Recipe = m.Recipes[i];
                    m.Log("信息", "品种切换至 [" + m.Recipe + "]");
                }
            };
            var remoteBtn = E.MiniBtn("远程品种", () => m.Log("信息", "下载远程品种（FTP）"));
            left.Children.Add(E.RowFill("品种", E.H(recipeCb, remoteBtn), 84));

            left.Children.Add(E.T("TestSite:  " + m.SiteLayoutName, 12, E.Acc, true).Margin(0, 8, 0, 4));

            // 测试位分布矩阵：列 a~h，行 A~D
            string[] cols = { "a", "b", "c", "d", "e", "f", "g", "h" };
            string[] rows = { "A", "B", "C", "D" };
            var siteGrid = E.Matrix(cols, rows, (r, c) =>
            {
                int ch = r * 8 + c + 1;
                return E.Box(
                    E.T("Ch" + ch, 10.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    E.HeadGreen, Brushes.White, 1, 0, new Thickness(2));
            }, 42, 22, E.HeadBlue, E.HeadBlue, 26, 20);
            left.Children.Add(E.Box(siteGrid, null, E.Div, 1, 8, new Thickness(2), new Thickness(0, 0, 0, 8)));

            // 料盘统计块
            Func<string, Element> tray = key => E.Box(
                E.V(
                    E.T(key, 10.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    E.T("0.00%", 10, E.Sec, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    E.T("0", 12, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    E.T("(0)", 9.5, E.Sec, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)),
                E.BandGrey, E.Div, 1, 6, new Thickness(4), new Thickness(0, 0, 6, 6), 66);

            left.Children.Add(E.H(tray("HotP1"), tray("HotP2"), E.SpacerW(10),
                tray("Fix1"), tray("Fix2"), tray("Fix3")));
            left.Children.Add(E.H(tray("Loader"), tray("Auto1"), tray("Auto2"), tray("Auto3")));

            var gpib = E.Display("GPIB:0.11@", 0, E.HeadGreen, E.P, TextAlignment.Center, 12);
            gpib.Margin = new Thickness(0, 4, 0, 6);
            gpib.HAlign = HorizontalAlignment.Stretch;
            left.Children.Add(gpib);

            left.Children.Add(E.H(
                E.MiniBtn("自动通道映射检查", () => m.Log("信息", "自动通道映射检查")),
                E.MiniBtn("手动通道映射检查", () => m.Log("信息", "手动通道映射检查"))));
            left.Children.Add(E.H(
                E.MiniBtn("自动清洁", () => m.Log("信息", "自动清洁")),
                E.MiniBtn("手动清洁", () => m.Log("信息", "手动清洁"))));

            root.Add(E.Box(E.Scroll(left), E.CardBg, null, 0, 12, new Thickness(14), new Thickness(0, 0, 12, 0)), col: 0);

            // ---------------- 中列 ----------------
            var mid = E.V();

            mid.Children.Add(E.RowFill("当前用户",
                E.H(E.Live(x => x.UserLevel, "{0}", 92), E.Live(x => x.Clock, null, 96)), 74));

            mid.Children.Add(E.RowFill("Temp.(°C)", E.H(
                E.Live(x => x.TempSet, "0", 62, E.BandGreen, E.P, 12.5),
                E.SpacerW(6),
                E.Live(x => x.TempTag, null, 62, E.BandGreen, E.Ok, 12),
                E.SpacerW(6),
                E.Live(x => x.TempMode, "{0}", 74, E.BandGreen, E.Ok, 12)), 74));

            mid.Children.Add(E.RowFill("测试压力(kgf)", E.H(
                E.Live(x => x.Pressure, "0.00", 62), E.SpacerW(6),
                E.T("Rot.", 12, E.Sec), E.SpacerW(4),
                E.RedLamp(true, true), E.RedLamp(true, true)), 86));

            var stats = E.V();
            stats.Children.Add(E.H(
                E.T("统计数据", 12, E.Sec).Margin(0, 0, 8, 0),
                E.MiniBtn("开批", () => { m.BatchOpen = true; m.Log("信息", "开批：" + m.BatchNo); }),
                E.MiniBtn("结批", () => { m.BatchOpen = false; m.Log("信息", "结批，生成 .rpt 报告"); }),
                E.MiniBtn("清零", () => { m.GoodCount = m.BadCount = m.TotalCount = m.Accumulated = 0; m.Log("信息", "统计已清零"); })));
            foreach (var f in new[] { ("批号MO", "BatchNo"), ("子批号", "SubBatchNo"), ("客户", "Customer"), ("操作员", "Operator") })
                stats.Children.Add(E.RowFill(f.Item1,
                    E.Text("f_" + f.Item2, m.GetText("f_" + f.Item2, f.Item2 == "Operator" ? "OP-001" : "")), 74));
            stats.Children.Add(E.Spacer(4));
            stats.Children.Add(E.TwoCol(
                new object[] { E.RowFill("好品数", E.Live(x => x.GoodCount, "N0", 0, E.BandGreen), 60), E.RowFill("阶段", E.Live(x => x.Stage, null, 0, E.BandGrey), 60) },
                new object[] { E.RowFill("次品数", E.Live(x => x.BadCount, "N0", 0, E.BandAmber), 60), E.RowFill("良品率", E.Live(x => x.Yield, "0.00'%'", 0, E.BandGrey), 60) },
                new object[] { E.RowFill("处理数", E.Live(x => x.TotalCount, "N0", 0, E.BandGrey), 60), E.RowFill("UPH", E.Live(x => x.UPH, "N0", 0, E.BandGrey), 60) },
                new object[] { E.RowFill("时间", E.Live(x => x.RunTime, null, 0, E.BandGrey), 60), E.RowFill("UPH48", E.Live(x => x.UPH48, "N0", 0, E.BandGrey), 60) },
                new object[] { E.RowFill("Cycle.T", E.Live(x => x.CycleT, "0.0", 0, E.BandGrey), 60), E.RowFill("Index.T", E.Live(x => x.IndexT, "0.0", 0, E.BandGrey), 60) },
                new object[] { E.RowFill("Test.T", E.Live(x => x.TestT, "0.0", 0, E.BandGrey), 60), E.RowFill("累计", E.Live(x => x.Accumulated, "N0", 0, E.BandGrey), 60) }
            ));
            stats.Children.Add(E.Spacer(6));
            stats.Children.Add(E.RowFill("StartMode", E.Cmb("start_mode",
                new[] { "初始模式", "连续模式", "复测-初始模式", "复测-连续模式" }, 150), 74));

            mid.Children.Add(stats);
            root.Add(E.Box(E.Scroll(mid), E.CardBg, null, 0, 12, new Thickness(14), new Thickness(0, 0, 12, 0)), col: 1);

            // ---------------- 右列：批次数据统计窗口 ----------------
            var right = E.V(E.T("批次数据统计", 13, E.P, true).Margin(0, 0, 0, 8));
            right.Children.Add(E.H(E.MiniBtn("Counter"), E.MiniBtn("UPH/Tray"), E.MiniBtn("Timer")));
            string[] counters =
            {
                "Load In", "Load Out1(SHT-A)", "Load Out1(SHT-B)", "ShuttleA Out", "ShuttleB Out",
                "Offload Out", "Auto1", "Auto2", "Auto3", "Fix1", "Fix2", "Fix3", "Jam Rate", "RuntimeUPH"
            };
            var cg = E.Grid().Cols("*", "64", "62");
            for (int i = 0; i < counters.Length; i++)
            {
                cg.Add(E.T(counters[i], 11.5, E.P).Margin(4, 3, 4, 3), i, 0);
                cg.Add(E.Display("0", 60, E.BandGrey), i, 1);
                if (counters[i].StartsWith("Auto") || counters[i].StartsWith("Fix"))
                    cg.Add(E.Display("0.00%", 58, E.BandGrey), i, 2);
                else if (counters[i] == "Jam Rate")
                    cg.Add(E.Display("0/0", 58, E.BandGrey), i, 2);
            }
            right.Children.Add(cg);
            root.Add(E.Box(E.Scroll(right), E.CardBg, null, 0, 12, new Thickness(14)), col: 2);

            // ---------------- 底部：拆卸 / SNAP + 信号灯 + 版权 ----------------
            var bottom = E.V(
                E.H(
                    E.MiniBtn("拆卸", () => m.Log("信息", "测试臂抬起，ArmA 移至小车上，ArmB 移至测试区中间")),
                    E.MiniBtn("SNAP", () => m.Log("信息", "已生成 log 文件记录机器状态")),
                    E.SpacerW(16),
                    E.RedLamp(true, true, 14), E.RedLamp(true, true, 14), E.RedLamp(true, true, 14)),
                E.Box(
                    E.H(E.T("JHT DESIGN CO., LTD", 11, Brushes.White, true), E.SpacerW(20), E.T("©Ume", 11, Brushes.White)),
                    new SolidColorBrush(Color.FromRgb(0x9B, 0x1B, 0x1B)), null, 0, 6,
                    new Thickness(10, 4, 10, 4), new Thickness(0, 8, 0, 0)));

            var outer = E.Grid().Rows("*", "Auto");
            outer.Add(root, 0);
            outer.Add(bottom, 1);
            return outer;
        }
    }
}
