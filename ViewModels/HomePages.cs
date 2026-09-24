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
    //  温启志 18719361399 · 源码署名标记（HomePages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_HomePages
    internal static class WenQiZhiTag_HomePages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · HomePages。</summary>
        internal const string WenQiZhiFile = "HomePages";
    }

    // 主界面（复刻 image37：三列布局 + 底部版权条）
    // 温启志 18719361399 · 类 public static class HomePages
    public static class HomePages
    {
        public static Element MainScreen()
        {
            var m = E.M;
            var root = E.Grid().Cols("460", "*", "260").Rows("*");
            root.VAlign = VerticalAlignment.Stretch;

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
            recipeCb.OnChanged = i => Ops.SelectRecipeByIndex(i);
            var remoteBtn = E.MiniBtn("远程品种", Ops.DownloadRemoteRecipe);
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
                E.MiniBtn("自动通道映射检查", () => Ops.CheckChannelMap(true)),
                E.MiniBtn("手动通道映射检查", () => Ops.CheckChannelMap(false))));
            left.Children.Add(E.H(
                E.MiniBtn("自动清洁", () => Ops.RunClean(true)),
                E.MiniBtn("手动清洁", () => Ops.RunClean(false))));

            var leftBox = E.Box(left, E.CardBg, null, 0, 12, new Thickness(10), new Thickness(0, 0, 12, 0));
            leftBox.VAlign = VerticalAlignment.Stretch;
            root.Add(leftBox, col: 0);

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
            // 中间列只放标题+批状态徽章，开批/结批/清零 已经统一在命令中心第二行；
            // 否则同一个动作两处都有，按钮互相重复，用户记不清该点哪一个。
            stats.Children.Add(E.H(
                E.T("统计数据", 13, E.Sec).Margin(0, 0, 8, 0),
                E.LiveBadge(x => LotOps.LotStateText, x => Ops.RunStateBrush(x.RunState), 110, 12.5)));
            // 温启志 18719361399：批号 / 子批号 / 客户 / 操作员 四个录入框排成 2x2。
            // 原先每个字段各占一整行、且把「定宽的 TextBox」直接放进 RowFill 的 * 列里，
            // 由于 WPF 中「显式 Width + Stretch」会退化为居中，标签与输入框之间被拉开一大段空白。
            // 现在用 TwoCol 分成左右两栏、并各自用 E.H 包一层，输入框紧贴标签左对齐。
            Func<string, string, Element> batchField = (label, key) =>
                E.RowFill(label, E.H(E.Text(key, m.GetText(key, key == "f_Operator" ? "OP-001" : ""), 150)), 74);
            stats.Children.Add(E.TwoCol(
                new object[] { batchField("批号MO", "f_BatchNo"), batchField("子批号", "f_SubBatchNo") },
                new object[] { batchField("客户", "f_Customer"), batchField("操作员", "f_Operator") }));
            // 批量 / 班别 / 输出介质（《ASTI Hexa Maxx》4.1 Open Lot 的三项必填信息）
            stats.Children.Add(E.TwoCol(
                new object[] { E.RowFill("批量(LotSize)", E.H(E.Num("f_LotSize", 0, 150, "0", 1, 10000000)), 74),
                               E.RowFill("班别(Shift)", E.H(E.Text("f_Shift", "A", 150)), 74) },
                new object[] { E.RowFill("输出介质", E.H(E.Cmb("f_output_type", new[] { "Tray", "Tape" }, 150)), 74),
                               E.RowFill("批次进度", E.H(E.Live(x => LotOps.ProgressPercent, "0.0'%'", 96, E.BandGrey)), 74) }));
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
            stats.Children.Add(E.RowFill("StartMode", E.H(E.Cmb("start_mode",
                new[] { "初始模式", "连续模式", "复测-初始模式", "复测-连续模式" }, 150)), 74));

            mid.Children.Add(stats);
            var midBox = E.Box(mid, E.CardBg, null, 0, 12, new Thickness(10), new Thickness(0, 0, 12, 0));
            midBox.VAlign = VerticalAlignment.Stretch;
            root.Add(midBox, col: 1);

            // ---------------- 右列：批次数据统计窗口 ----------------
            var right = E.V(E.T("批次数据统计", 13, E.P, true).Margin(0, 0, 0, 8));
            // 温启志 18719361399：Counter / UPH-Tray / Timer 三个按钮原先点了没有任何反应，
            // 现在改成真正的页签（E.Tabs 的内容是「点了才构建」的惰性构建）。
            string[] counters =
            {
                "Load In", "Load Out1(SHT-A)", "Load Out1(SHT-B)", "ShuttleA Out", "ShuttleB Out",
                "Offload Out", "Auto1", "Auto2", "Auto3", "Fix1", "Fix2", "Fix3", "Jam Rate", "RuntimeUPH"
            };
            // ---- 页签 1：Counter（各工位计数器） ----
            Func<Element> counterTab = () =>
            {
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
                return cg;
            };

            // ---- 页签 2：UPH/Tray（产能统计，全部取自机台实时运行态） ----
            Func<Element> uphTab = () =>
            {
                var g = E.Grid().Cols("*", "86");
                int rr = 0;
                Action<string, Element> row = (k, v) =>
                {
                    g.Add(E.T(k, 11.5, E.P).Margin(4, 3, 4, 3), rr, 0);
                    g.Add(v, rr, 1);
                    rr++;
                };
                row("RuntimeUPH", E.Live(x => x.UPH, "N0", 80, E.BandGrey));
                row("UPH48", E.Live(x => x.UPH48, "N0", 80, E.BandGrey));
                row("良品率", E.Live(x => x.Yield, "0.00'%'", 80, E.BandGrey));
                row("累计处理", E.Live(x => x.Accumulated, "N0", 80, E.BandGrey));
                row("好品数", E.Live(x => x.GoodCount, "N0", 80, E.BandGreen));
                row("次品数", E.Live(x => x.BadCount, "N0", 80, E.BandAmber));
                row("处理数", E.Live(x => x.TotalCount, "N0", 80, E.BandGrey));
                row("已运行", E.Live(x => x.RunTime, null, 80, E.BandGrey));
                row("阶段", E.Live(x => x.Stage, null, 80, E.BandGrey));
                // 批次统计（《ASTI Hexa Maxx》3.1.8 Run Time Data）
                row("Gross PPH", E.Live(x => LotOps.GrossPPH, "N0", 80, E.BandGrey));
                row("Nett PPH", E.Live(x => LotOps.NettPPH, "N0", 80, E.BandGrey));
                row("MTBA(min)", E.Live(x => LotOps.MtbaMin, "0.0", 80, E.BandGrey));
                row("批次时间(min)", E.Live(x => LotOps.LotTimeMin, "0.0", 80, E.BandGrey));
                row("卡料次数", E.Live(x => x.JamCount, "N0", 80, E.BandAmber));
                row("剩余数量", E.Live(x => LotOps.Remaining, "N0", 80, E.BandGrey));
                return g;
            };

            // ---- 页签 3：Timer（节拍 / 计时统计） ----
            Func<Element> timerTab = () =>
            {
                var g = E.Grid().Cols("*", "86");
                int rr = 0;
                Action<string, Element> row = (k, v) =>
                {
                    g.Add(E.T(k, 11.5, E.P).Margin(4, 3, 4, 3), rr, 0);
                    g.Add(v, rr, 1);
                    rr++;
                };
                row("Cycle.T", E.Live(x => x.CycleT, "0.0", 80, E.BandGrey));
                row("Index.T", E.Live(x => x.IndexT, "0.0", 80, E.BandGrey));
                row("Test.T", E.Live(x => x.TestT, "0.0", 80, E.BandGrey));
                row("累计计时", E.Live(x => x.Accumulated, "N0", 80, E.BandGrey));
                row("已运行", E.Live(x => x.RunTime, null, 80, E.BandGrey));
                row("当前时间", E.Live(x => x.Clock, null, 80, E.BandGrey));
                row("运行状态", E.Live(x => x.RunState, null, 80, E.BandGrey));
                row("运行模式", E.Live(x => x.Mode, null, 80, E.BandGrey));
                return g;
            };

            right.Children.Add(E.Tabs(
                ("Counter", counterTab),
                ("UPH/Tray", uphTab),
                ("Timer", timerTab)));
            var rightBox = E.Box(right, E.CardBg, null, 0, 12, new Thickness(10));
            rightBox.VAlign = VerticalAlignment.Stretch;
            root.Add(rightBox, col: 2);

            // ---------------- 底部：拆卸 / SNAP + 信号灯 + 版权 ----------------
            var bottom = E.V(
                E.H(
                    E.MiniBtn("拆卸", Ops.DisassembleArm),
                    E.MiniBtn("SNAP", Ops.SnapMachineState),
                    E.SpacerW(16),
                    E.RedLamp(true, true, 14), E.RedLamp(true, true, 14), E.RedLamp(true, true, 14)),
                E.Box(
                    E.H(E.T("JHT DESIGN CO., LTD", 11, Brushes.White, true), E.SpacerW(20), E.T("©Ume", 11, Brushes.White)),
                    new SolidColorBrush(Color.FromRgb(0x9B, 0x1B, 0x1B)), null, 0, 6,
                    new Thickness(10, 4, 10, 4), new Thickness(0, 8, 0, 0)));

            var outer = E.Grid().Rows("*", "Auto");
            outer.VAlign = VerticalAlignment.Stretch;
            outer.Add(root, 1);
            outer.Add(bottom, 2);
            return outer;
        }

        /// <summary>
        /// 主界面顶部的「命令中心」生产控制条（温启志 18719361399）。
        /// 这组按钮原先在窗口顶部的「命令中心工具条」里（位于页面之上），
        /// 按要求搬到主界面页内，成为主界面本身的一部分。
        /// 按钮的可用性跟着运行状态实时变化：未启动时暂停/继续/停止自动灰显。
        /// </summary>
        public static Element CommandCenter()
        {
            var m = E.M;

            // 统一造按钮：底色/文字色/加粗/可用性/提示一次给全
            Func<string, Action, Brush, Brush, bool, Func<bool>, string, Element> mk =
                (text, act, bg, fg, bold, en, tip) =>
                {
                    var b = E.Btn(text, act, "CmdButton", 12.5, en);
                    if (bg != null) b.Background = bg;
                    if (fg != null) b.Foreground = fg;
                    if (bold) b.FontWeight = FontWeights.SemiBold;
                    b.Padding = new Thickness(14, 5, 14, 5);
                    b.Margin = new Thickness(0, 2, 5, 2);
                    b.ToolTip = tip;
                    return b;
                };

            // 分组之间的细竖线
            Func<Element> sep = () => E.Box(null, E.Div, null, 0, 0, default(Thickness),
                                            new Thickness(5, 4, 5, 4), 1, 20);

            var amber = new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0x30));
            var blue = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xD6));
            var grey = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73));

            var row = E.H();
            row.Children.Add(E.T("命令中心", 12.5, E.Sec).Margin(0, 0, 10, 0));
            row.Children.Add(E.LiveBadge(x => x.RunState, x => Ops.RunStateBrush(x.RunState), 62, 13));

            // ---- 生产控制 ----
            row.Children.Add(mk("启动", () => Ops.StartProduction(), Palette.OnGreen, Brushes.White, true,
                () => Ops.CanStartProduction, "启动生产：先做安全前置判断，再跑取放料节拍并开始计产"));
            row.Children.Add(mk("暂停", () => Ops.PauseProduction(), amber, Brushes.White, true,
                () => Ops.CanPauseProduction, "暂停：保持当前位置，节拍与计产都停"));
            row.Children.Add(mk("继续", () => Ops.ResumeProduction(), blue, Brushes.White, true,
                () => Ops.CanResumeProduction, "从暂停处继续"));
            row.Children.Add(mk("停止", () => Ops.StopProduction(), grey, Brushes.White, true,
                () => Ops.CanStopProduction, "停止生产并停全部轴"));
            row.Children.Add(mk("单步", () => Ops.SingleStep(), null, null, false,
                () => Ops.CanSingleStepProduction, "单步执行一拍，方便观察动作顺序"));

            row.Children.Add(sep());

            // ---- 归零 ----
            row.Children.Add(mk("全部归零", () => Ops.HomeAll(), null, null, false,
                () => Ops.CanHomeAxes, "按《电机归零》页勾选的电机逐个回零（先 Z 抬起再水平轴）"));
            row.Children.Add(mk("取消归零", () => Ops.CancelHome(), null, null, false, null,
                "取消正在进行的归零队列"));
            row.Children.Add(mk("全部停止", () => Ops.StopAllAxes(), null, null, false, null,
                "只停轴，不改运行状态"));

            row.Children.Add(sep());

            // ---- 报警 / 急停 ----
            row.Children.Add(mk("报警解除", () => Ops.ClearAlarm(), null, null, false, null,
                "清伺服报警 + 复位全部输出点"));
            row.Children.Add(mk("急停复位", () => Ops.ResetEmergency(), null, null, false, null,
                "解除急停：恢复伺服使能前请先确认现场安全"));
            row.Children.Add(mk("急停", () => Ops.EmergencyStop(), Palette.OnRed, Brushes.White, true, null,
                "立即断使能、停全部轴、红灯 + 蜂鸣"));

            row.Children.Add(sep());

            // ---- 计数 ----
            row.Children.Add(mk("计数清零", () => Ops.ResetCounters(), null, null, false, null,
                "把好品 / 次品 / 处理数等统计清零"));

            // ---------------- 第 2 行：批次管理 ----------------
            // 温启志 18719361399：开批 / 结批 / 中止批 / 清盘 / 换载带 / 重扫 / KGD / OTF 归零，
            // 全部对应《ASTI Hexa Maxx System Manual》Chapter 4 Operator's Menu 的功能。
            var row2 = E.H();
            row2.Children.Add(E.T("批次管理", 12.5, E.Sec).Margin(0, 0, 10, 0));
            row2.Children.Add(E.LiveBadge(x => LotOps.LotStateText, x => Ops.RunStateBrush(x.RunState), 76, 12.5));

            row2.Children.Add(mk("开批", () => LotOps.OpenLot(), Palette.OnGreen, Brushes.White, true,
                () => !m.BatchOpen && !Ops.IsRunning, "开新批次：校验批号/批量/操作员/归零状态后开始计产"));
            row2.Children.Add(mk("结批", () => LotOps.CloseLot(), grey, Brushes.White, true,
                () => m.BatchOpen && !Ops.IsRunning, "结束当前批次并写批次记录"));
            row2.Children.Add(mk("中止批", () => LotOps.AbortLot(), null, null, false,
                () => m.BatchOpen, "中止批次：机台内器件按默认流程走完后收尾"));
            row2.Children.Add(mk("清盘", () => LotOps.PurgeTrays(), null, null, false,
                () => m.BatchOpen, "清盘：机台内器件跳过检测直接送输出料盘"));
            row2.Children.Add(mk("清料完成", () => LotOps.FinishPurge(), null, null, false,
                () => m.Purging, "清料流程收尾，结束 Purge/Abort 状态"));

            row2.Children.Add(sep());

            row2.Children.Add(mk("换载带#1", () => LotOps.ChangeCarrier(1), null, null, false,
                () => m.OutputType == "Tape", "更换载带 #1 并把计数器清零"));
            row2.Children.Add(mk("换载带#2", () => LotOps.ChangeCarrier(2), null, null, false,
                () => m.OutputType == "Tape", "更换载带 #2 并把计数器清零"));
            row2.Children.Add(mk("重扫模式", () => LotOps.SetRescanMode(!m.RescanMode), null, null, false,
                () => !Ops.IsRunning, "切换重扫模式(Rescan Mode)"));
            row2.Children.Add(mk("KGD补盘", () => LotOps.SetKgdTraySort(!m.KgdTraySort), null, null, false, null,
                "空穴位用 KGD 器件按行/列填满"));
            row2.Children.Add(mk("OTF归零", () => LotOps.OtfHoming(), null, null, false,
                () => !Ops.IsRunning, "不按急停也让各机械臂回到原点"));

            row2.Children.Add(sep());

            row2.Children.Add(mk("打印汇总", () => LotOps.ExportLotSummaryDialog("手动"), null, null, false, null,
                "导出当前批次汇总"));
            row2.Children.Add(mk("自动打印", () => LotOps.SetAutoPrinting(!m.AutoPrinting), null, null, false, null,
                "开批 / 结批时自动输出批次汇总"));

            return E.Box(E.V(row, E.Spacer(6), row2), E.CardBg, E.Div, 1, 12,
                         new Thickness(12, 8, 12, 8), new Thickness(0, 0, 0, 10));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
