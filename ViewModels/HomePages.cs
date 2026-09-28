// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HandlerGuide.Motion;

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
            var root = E.Grid().Cols("430", "*", "260", "360").Rows("*");
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
            // 温启志 18719361399 · Element.VAlign 默认 Center，会把整块内容垂直居中、
            // 与其它卡片顶部起始线不齐 —— 显式顶对齐，让页签贴近顶部。
            right.VAlign = VerticalAlignment.Top;
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

            // ---------------- 最右列：操作按钮面板 ----------------
            var opPanel = OperationPanel();
            root.Add(opPanel, col: 3);

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
        /// 温启志 18719361399 · 主界面右侧「操作按钮面板」：复刻实体操作面板
        /// —— 前面板板块（红框，含上端电源键）+ 后面板板块 + 前/后切换旋钮。
        /// 《手册》要点：每颗按钮上方状态 LED、下方小字说明；
        /// 旋钮内指示灯在后面板有效时亮，前面板指示灯在前面板有效时亮；
        /// 后面板有效时前面板红框内按钮全部无效（电源键在上端不受影响）；
        /// 归零 / 清机 / 步进 / 报警确认进行中指示灯闪亮（400ms 相位）。
        /// </summary>
        public static Element OperationPanel()
        {
            var m = E.M;
            var Mm = MotionManager.Instance;

            // 温启志 18719361399 · 复用命令中心的配色
            Brush grey = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73));
            Brush amber = new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0x30));
            Brush redFrame = new SolidColorBrush(Color.FromRgb(0xC0, 0x20, 0x2A));

            // 闪亮相位：RefreshFast 每 50ms 刷新一次读数，按 400ms 相位取反即可看到闪烁
            Func<Func<MachineModel, bool>, Func<MachineModel, bool>> blink =
                f => mm => f(mm) && (Environment.TickCount64 / 400) % 2 == 0;

            // 面板侧别门禁：前面板键只在前面板有效时可用，后面板键反之
            Func<Func<bool>?, Func<bool>> fen = f => () => !Ops.PanelRear && (f == null || f());
            Func<Func<bool>?, Func<bool>> ren = f => () => Ops.PanelRear && (f == null || f());

            // 「LED + 按钮 + 小字说明」单元：en/bg/fg 允许 null
            Func<string, Action, Func<bool>?, Func<MachineModel, bool>, bool, Brush?, Brush?, string, string, Element> cell =
                (label, act, en, ledGet, ledRed, bg, fg, cap, tip) =>
                {
                    var led = E.LiveLamp(ledGet, ledRed, false, 10);
                    led.HAlign = HorizontalAlignment.Center;
                    led.Margin = new Thickness(0, 0, 0, 3);

                    var b = E.Btn(label, act, "CmdButton", 11.5, en);
                    if (bg != null) b.Background = bg;
                    if (fg != null) b.Foreground = fg;
                    b.FontWeight = FontWeights.SemiBold;
                    b.Padding = new Thickness(2, 4, 2, 4);
                    b.Margin = new Thickness(1, 0, 1, 0);
                    b.HAlign = HorizontalAlignment.Stretch;
                    b.ToolTip = tip;

                    // 温启志 18719361399 · 按钮下方小字说明（手册定义摘要），悬停看完整说明
                    var cap2 = E.T(cap, 8.5, E.Sec, false, TextWrapping.Wrap, TextAlignment.Center, HorizontalAlignment.Center);
                    cap2.Margin = new Thickness(1, 2, 1, 0);

                    var v = E.V(led, b, cap2);
                    v.HAlign = HorizontalAlignment.Stretch;
                    return v;
                };

            // 把按钮定义铺成 cols 列的栅格
            Func<IEnumerable<(string label, Action act, Func<bool>? en, Func<MachineModel, bool> led,
                bool ledRed, Brush? bg, Brush? fg, string cap, string tip)>, int, Element> gridOf =
                (items, cols) =>
                {
                    var list = items.ToArray();
                    int rows = (list.Length + cols - 1) / cols;
                    var g = E.Grid().Cols(string.Join(",", Enumerable.Repeat("1*", cols)))
                                   .Rows(string.Join(",", Enumerable.Repeat("Auto", rows)));
                    for (int i = 0; i < list.Length; i++)
                        g.Add(cell(list[i].label, list[i].act, list[i].en, list[i].led, list[i].ledRed,
                                   list[i].bg, list[i].fg, list[i].cap, list[i].tip), i / cols, i % cols);
                    return g;
                };

            Element SideCap(string text) =>
                E.T(text, 10.5, E.Sec, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center);

            // ---- 前面板红框内 20 键（4×5），后面板有效时全部无效 ----
            var defs = new (string label, Action act, Func<bool>? en,
                Func<MachineModel, bool> led, bool ledRed, Brush? bg, Brush? fg, string cap, string tip)[]
            {
                ("开始运行", () => Ops.StartProduction(), fen(() => Ops.CanStartProduction), mm => Ops.IsRunning, false, Palette.OnGreen, Brushes.White,
                    "无报警时启动自动运行", "当设备无报警状态时，按下该按钮，设备开始自动运行；自动运行中该按钮指示灯亮起"),
                ("停止",     () => Ops.StopProduction(),  fen(() => Ops.CanStopProduction), mm => Ops.IsIdle && !Ops.HomeActive, false, grey, Brushes.White,
                    "停止运行 / 终止归零", "运行中按下停止自动运行；归零中按下也可终止归零；停止状态下该按钮指示灯亮起"),
                ("暂停",     () => Ops.PauseProduction(), fen(() => Ops.CanPauseProduction), mm => Ops.IsPaused, false, amber, Brushes.White,
                    "暂停节拍与计产", "保持当前位置，节拍与计产都停止"),
                ("复位",     () => Ops.ClearAlarm(),      fen(null), mm => Mm.AnyAlarm(), true, null, null,
                    "消除一般性报警", "设备有报警需要清除时指示灯亮起：如安全门关闭后、伺服报警条件消除后，按此键复位报警"),

                ("归零",     () => Ops.HomeAll(),         fen(() => Ops.CanHomeAxes), blink(mm => Ops.HomeActive), false, null, null,
                    "确认后全部轴归零", "停止状态下按下弹出是否归零的确认窗口，再次按下执行归零；归零过程中指示灯闪亮"),
                ("急停",     () => Ops.EmergencyStop(),   fen(null), mm => Mm.EStop, true, Palette.OnRed, Brushes.White,
                    "断使能停全部轴", "立即断使能、停全部轴、红灯 + 蜂鸣"),
                ("报警清除", () => { Ops.ClearAlarm(); Mm.ResetAllOutputs(); Ops.Info("报警已清除，输出已复位"); }, fen(null), mm => Mm.AnyAlarm(), true, null, null,
                    "清报警并复位输出", "清除报警并复位全部输出点"),
                ("全部停止", () => Ops.StopAllAxes(),     fen(null), mm => Mm.AnyBusy(), true, null, null,
                    "只停轴不停生产", "停止全部轴运动，不改变运行状态"),

                ("清机",     () => LotOps.PurgeTrays(),   fen(() => m.BatchOpen), blink(mm => m.Purging), false, null, null,
                    "清机模式，走完即停", "触发清机模式，指示灯闪亮：上料臂停止送料，测试区下料正常工作，机内无料后自动进入停止状态"),
                ("清出",     () => LotOps.AbortLot(),     fen(() => m.BatchOpen), mm => m.Purging, false, null, null,
                    "清出模式，走完自退", "激活清出模式，指示灯点亮：上料盘堆叠不再进盘，机内料全部完成后自动退出并且指示灯熄灭"),
                ("跳过",     () => Ops.Info("跳过：忽略当前报警并继续"), fen(null), blink(mm => Mm.AnyAlarm()), false, null, null,
                    "跳过当前受影响器件", "报警需人工确认时弹窗提示，激活的按钮指示灯闪亮；如吸料失败且该位置无料时按此键跳过"),
                ("重试",     () => Ops.Info("重试：重新执行上次受影响的动作"), fen(null), blink(mm => Mm.AnyAlarm()), false, null, null,
                    "重试上次受影响动作", "报警需人工确认时弹窗提示，激活的按钮指示灯闪亮；如吸料失败且料盘有料时按此键重新吸取"),

                ("结盘",     () => LotOps.FinishPurge(),  fen(() => m.Purging), mm => m.BatchOpen, false, null, null,
                    "结束上料盘取放", "上料盘无料造成报警后按下：结束该盘取放并将空盘传送到上料出盘位置"),
                ("出盘",     () => Ops.TrayOutKey(),      fen(null), mm => Ops.TrayOutRequested, false, null, null,
                    "清出下料自动盘料盘", "运行中直接清出下料自动盘有料料盘；停机时按下激活出盘请求（指示灯亮），再按<开始运行>开始清盘"),
                ("单步",     () => Ops.TestArmStep(),     fen(() => Ops.TestArmCalibrating), mm => Ops.TestArmCalibrating, false, null, null,
                    "校准模式下切换位置", "测试臂「自动校准模式」下激活，控制测试臂到测试位置的切换"),
                ("测试",     () => Ops.ManualTestTrigger(), fen(() => Ops.TestArmCalibrating), mm => Ops.TestArmCalibrating, false, null, null,
                    "校准模式下触发测试", "测试臂「自动校准模式」下激活，手动触发测试信号"),

                ("照明",     () => Mm.ToggleOut(24),      fen(null), mm => Mm.ReadOut(24), false, null, null,
                    "开/关机内照明灯", "按下照明灯亮起，再次按下关闭；照明控制打开时指示灯亮"),
                ("拆卸",     () => Ops.DisassembleArm(),  fen(() => !Ops.IsRunning), mm => false, false, null, null,
                    "测试臂抬起复位", "测试臂抬起：armA 移至小车上方，armB 移至测试区中间"),
                ("SNAP",     () => Ops.SnapMachineState(), fen(null), mm => false, false, null, null,
                    "记录机器状态 log", "异常时生成 log 记录机器状态"),
                ("计数清零", () => Ops.ResetCounters(),   fen(null), mm => false, false, null, null,
                    "统计计数清零", "把好品 / 次品 / 处理数等统计清零"),
            };

            // ---- 前面板上端：电源两键（上端电源区，不受前/后面板侧别禁用影响） ----
            var pwrDefs = new (string label, Action act, Func<bool>? en,
                Func<MachineModel, bool> led, bool ledRed, Brush? bg, Brush? fg, string cap, string tip)[]
            {
                ("电源关闭", () => Ops.PowerKeyOff(), null, mm => Ops.PowerInputOn, false, grey, Brushes.White,
                    "断开整机电源", "机器输入电源开启（后面断路器打开）时指示灯亮；按下断开整机电源"),
                ("电源开启", () => Ops.PowerKeyOn(),  null, mm => Ops.PowerComponentsOn, false, Palette.OnGreen, Brushes.White,
                    "部件上电 / 电脑开机", "第 1 次按下：机器内所有传感器、电机等控制部件上电（指示灯亮）；第 2 次按下：控制电脑开机"),
            };

            // ---- 后面板板块（旋钮切到后面板时有效） ----
            var rearDefs = new (string label, Action act, Func<bool>? en,
                Func<MachineModel, bool> led, bool ledRed, Brush? bg, Brush? fg, string cap, string tip)[]
            {
                ("Step/步进",    () => Ops.RearStepKey(false), ren(null), blink(mm => Ops.StepLowerActive), false, null, null,
                    "下料臂步进走一拍", "启动下料臂步进功能：每次取放后暂停，再按一次继续走一拍；<报警清除>+本键退出"),
                ("T.Start/测试", () => Ops.RearStepKey(true),  ren(null), blink(mm => Ops.StepUpperActive), false, null, null,
                    "上料臂步进走一拍", "启动上料臂步进功能：每次取放后暂停，再按一次继续走一拍；<报警清除>+本键退出"),
                ("报警清除",     () => Ops.RearAlarmClear(),   ren(null), mm => Mm.AnyAlarm(), true, null, null,
                    "清报警，可退步进", "清除报警；按下后 3 秒内按 Step/T.Start 退出对应步进功能"),
                ("报警复位",     () => Ops.ClearAlarm(),       ren(null), mm => Mm.AnyAlarm(), true, null, null,
                    "消除一般性报警", "消除一般性报警（安全门 / 伺服报警等条件消除后）"),

                ("停止",         () => Ops.StopProduction(),   ren(() => Ops.CanStopProduction), mm => Ops.IsIdle && !Ops.HomeActive, false, grey, Brushes.White,
                    "停止 / 终止归零", "停止自动运行；归零中按下终止归零"),
                ("归零",         () => Ops.HomeAll(),          ren(() => Ops.CanHomeAxes), blink(mm => Ops.HomeActive), false, null, null,
                    "全部轴回零", "全部轴回零；归零过程中指示灯闪亮"),
                ("出盘",         () => Ops.TrayOutKey(),       ren(null), mm => Ops.TrayOutRequested, false, null, null,
                    "清出下料自动盘", "运行中直接清盘；停机时激活出盘请求，再按<开始运行>清盘"),
                ("结盘",         () => LotOps.FinishPurge(),   ren(() => m.Purging), mm => m.BatchOpen, false, null, null,
                    "结束上料盘取放", "结束上料盘取放操作，空盘送出到上料出盘位置"),
            };

            // ---- 前 / 后切换旋钮行：旋钮内指示灯（后面板有效时亮）+ 前面板指示灯（前面板有效时亮） ----
            var frontLamp = E.LiveLamp(mm => !Ops.PanelRear, false, false, 11);
            frontLamp.HAlign = HorizontalAlignment.Center;
            var knobLamp = E.LiveLamp(mm => Ops.PanelRear, true, false, 11);
            knobLamp.HAlign = HorizontalAlignment.Center;
            var knob = E.Btn("前/后切换旋钮", Ops.TogglePanelSide, "CmdButton", 11, null);
            knob.FontWeight = FontWeights.SemiBold;
            knob.Padding = new Thickness(4, 6, 4, 6);
            knob.Margin = new Thickness(6, 0, 6, 0);
            knob.HAlign = HorizontalAlignment.Stretch;
            knob.ToolTip = "旋钮：切换前面板 / 后面板。后面板有效时旋钮内指示灯亮，前面板（红框内）按钮无效";
            var knobRow = E.H(
                E.V(frontLamp, SideCap("前面板")),
                E.SpacerW(4),
                E.V(knobLamp, knob),
                E.SpacerW(4),
                E.V(E.Spacer(11), SideCap("后面板")));

            // ---- 组装：旋钮行 + 前面板红框（电源区 + 20 键）+ 后面板板块 ----
            var frontBlock = E.Box(
                E.V(gridOf(pwrDefs, 2), E.Spacer(5), gridOf(defs, 4)),
                null, redFrame, 1.5, 10, new Thickness(6));
            frontBlock.ToolTip = "红框内为前面板按钮：后面板有效时全部无效";

            var rearBlock = E.Box(
                E.V(E.T("后面板板块", 10.5, E.Sec, true).Margin(0, 0, 0, 4), gridOf(rearDefs, 4)),
                null, E.Div, 1, 10, new Thickness(6));

            var panel = E.V(
                E.T("操作面板", 13, E.P, true).Margin(0, 0, 0, 6),
                knobRow,
                E.Spacer(5),
                frontBlock,
                E.Spacer(6),
                rearBlock);
            // 温启志 18719361399 · Element.VAlign 默认 Center 会垂直居中；显式顶对齐与其他卡片同起始线
            panel.VAlign = VerticalAlignment.Top;
            var box = E.Box(panel, E.CardBg, null, 0, 12, new Thickness(8));
            box.VAlign = VerticalAlignment.Stretch;
            return box;
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
                    b.Padding = new Thickness(10, 4, 10, 4);
                    b.Margin = new Thickness(0, 2, 4, 2);
                    b.ToolTip = tip;
                    return b;
                };

            // 分组之间的细竖线
            Func<Element> sep = () => E.Box(null, E.Div, null, 0, 0, default(Thickness),
                                            new Thickness(5, 4, 5, 4), 1, 20);

            var amber = new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0x30));
            var blue = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xD6));
            var grey = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73));

            var row = E.Wrap();
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


            // ---- 归零 ----
            row.Children.Add(mk("全部归零", () => Ops.HomeAll(), null, null, false,
                () => Ops.CanHomeAxes, "按《电机归零》页勾选的电机逐个回零（先 Z 抬起再水平轴）"));
            row.Children.Add(mk("取消归零", () => Ops.CancelHome(), null, null, false, null,
                "取消正在进行的归零队列"));
            row.Children.Add(mk("全部停止", () => Ops.StopAllAxes(), null, null, false, null,
                "只停轴，不改运行状态"));


            // ---- 报警 / 急停 ----
            row.Children.Add(mk("报警解除", () => Ops.ClearAlarm(), null, null, false, null,
                "清伺服报警 + 复位全部输出点"));
            row.Children.Add(mk("急停复位", () => Ops.ResetEmergency(), null, null, false, null,
                "解除急停：恢复伺服使能前请先确认现场安全"));
            row.Children.Add(mk("急停", () => Ops.EmergencyStop(), Palette.OnRed, Brushes.White, true, null,
                "立即断使能、停全部轴、红灯 + 蜂鸣"));


            // ---- 计数 ----
            row.Children.Add(mk("计数清零", () => Ops.ResetCounters(), null, null, false, null,
                "把好品 / 次品 / 处理数等统计清零"));

            // ---------------- 第 2 行：批次管理 ----------------
            // 温启志 18719361399：开批 / 结批 / 中止批 / 清盘 / 换载带 / 重扫 / KGD / OTF 归零，
            // 全部对应《ASTI Hexa Maxx System Manual》Chapter 4 Operator's Menu 的功能。
            var row2 = E.Wrap();
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


            row2.Children.Add(mk("打印汇总", () => LotOps.ExportLotSummaryDialog("手动"), null, null, false, null,
                "导出当前批次汇总"));
            row2.Children.Add(mk("自动打印", () => LotOps.SetAutoPrinting(!m.AutoPrinting), null, null, false, null,
                "开批 / 结批时自动输出批次汇总"));

            return E.Box(E.V(row, E.Spacer(4), row2), E.CardBg, E.Div, 1, 10,
                         new Thickness(10, 6, 10, 6), new Thickness(0, 0, 0, 8));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
