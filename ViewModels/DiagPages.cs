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
    //  温启志 18719361399 · 源码署名标记（DiagPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_DiagPages
    internal static class WenQiZhiTag_DiagPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · DiagPages。</summary>
        internal const string WenQiZhiFile = "DiagPages";
    }

    // ==================================================================
    //  “诊断”工具条：电机状态 / 上料臂 / 下料臂 / 测试区域 / 料盘进出 /
    //  面板IO / 测试系统IO   —— 复刻 image67 ~ image80
    // ==================================================================
    // 温启志 18719361399 · 类 public static class DiagPages
    public static class DiagPages
    {
        // ---------------- 公共 ----------------

        /// <summary>诊断窗口：与手册一致的内嵌标签页（0=电机状态 … 6=测试系统IO）。</summary>
        private static Element Shell(int index)
        {
            var t = E.Tabs(
                ("电机状态", MotorBody),
                ("上料臂", () => ArmBody(true)),
                ("下料臂", () => ArmBody(false)),
                ("测试区域", AreaBody),
                ("料盘进出", TrayIoBody),
                ("面板IO", PanelIoBody),
                ("测试系统IO", TestIoBody));
            if (index > 0 && index < t.Tabs.Count) E.SelectTab(t, t.Tabs[index]);
            return t;
        }

        public static Element DiagMotor() => Shell(0);
        public static Element DiagLoader() => Shell(1);
        public static Element DiagUnloader() => Shell(2);
        public static Element DiagArea() => Shell(3);
        public static Element DiagTrayIO() => Shell(4);
        public static Element DiagPanelIO() => Shell(5);
        public static Element DiagTestIO() => Shell(6);

        // ---------------- 单元格助手 ----------------

        private static CellElement LampCell(Element lamp, double h = 24)
            => new CellElement
            {
                Background = E.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Height = h,
                Content = lamp
            };

        private static CellElement TextCell(string s, Brush bg = null, double h = 24,
            TextAlignment ta = TextAlignment.Center, bool bold = false)
            => new CellElement
            {
                Background = bg ?? E.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Height = h,
                Padding = new Thickness(3, 1, 3, 1),
                Content = E.T(s, 11, E.P, bold, TextWrapping.NoWrap, ta, HorizontalAlignment.Center)
            };

        private static CellElement TwoLineCell(string top, string bottom, Brush bg = null, double h = 36)
            => new CellElement
            {
                Background = bg ?? E.CardBg,
                BorderBrush = Palette.LineGrey,
                BorderThickness = 0.5,
                Height = h,
                Padding = new Thickness(2, 1, 2, 1),
                Content = E.V(
                    E.T(top, 11, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    E.T(bottom, 10.5, E.Sec, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center))
            };

        /// <summary>带标题的坐标按钮（第一行文字，第二行坐标值）。</summary>
        private static Element PosBtn(string text, string value, Action act = null)
        {
            var body = E.V(
                E.T(text, 11, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                E.T(value, 11, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center));
            return E.Box(body, E.BandGrey, Palette.LineGrey, 1, 3,
                new Thickness(2, 3, 2, 3), new Thickness(0, 0, 4, 4), 96, 42);
        }

        /// <summary>“灯 + 说明文字”一行。</summary>
        private static Element IoRow(string label, string key, bool red = true, bool square = false, double lampSize = 12)
        {
            var row = E.H(E.ClickLamp(key, red, square, lampSize), E.T(label, 11, E.P, false, TextWrapping.NoWrap));
            row.Children[1].Margin = new Thickness(4, 0, 0, 0);
            return row;
        }

        private static Element InfoRow(string label, bool on, bool red = true, double lampSize = 12)
        {
            var row = E.H(E.Lamp(on, false, lampSize), E.T(label, 11, E.P, false, TextWrapping.NoWrap));
            row.Children[0].Margin = new Thickness(0, 0, 0, 0);
            row.Children[1].Margin = new Thickness(4, 0, 0, 0);
            return row;
        }

        private static Element GreenRow(string label, string key, bool square = true, double lampSize = 12)
            => IoRow(label, key, false, square, lampSize);

        /// <summary>“说明文字 + 灯”一行（手册 image75“其他”区域用的就是这种顺序）。</summary>
        private static Element LabIo(string label, string key, bool red = true, bool square = false, double w = 104, double lampSize = 12)
        {
            var t = E.T(label, 11, E.P, false, TextWrapping.NoWrap);
            t.Width = w;
            return E.H(t, E.ClickLamp(key, red, square, lampSize));
        }

        private static Element LabInfo(string label, bool on, bool red = true, double w = 104, double lampSize = 12)
        {
            var t = E.T(label, 11, E.P, false, TextWrapping.NoWrap);
            t.Width = w;
            return E.H(t, E.Lamp(on, false, lampSize));
        }

        // ==================================================================
        //  1. 电机状态 (image67)
        // ==================================================================
        private static Element MotorBody()
        {
            var m = E.M;
            string[] heads = { "AxisName", "On", "Homed", "Org", "Ale", "Code", "Busy", "INP",
                               "MMPos", "PulsePos", "PosName", "Torque", "Ens", "SP", "SN", "DetailRecord" };
            string[] cols = { "120", "40", "52", "40", "40", "54", "44", "44", "76", "76", "88", "54", "40", "40", "40", "84" };

            var g = E.Grid().Cols(cols);
            for (int c = 0; c < heads.Length; c++)
                g.Add(TextCell(heads[c], E.HeadBlue, 26, TextAlignment.Center, true), 0, c);

            for (int i = 0; i < m.Motors.Count; i++)
            {
                int idx = i;
                int r = i + 1;
                var mo = m.Motors[i];

                g.Add(TextCell(mo.Name, E.CardBg, 24, TextAlignment.Left), r, 0);
                g.Add(LampCell(E.ClickLamp("dm_on_" + i, false, false, 13)), r, 1);
                g.Add(LampCell(E.LiveLamp(mm => mm.Motors[idx].Homed)), r, 2);
                g.Add(LampCell(E.LiveLamp(mm => mm.Motors[idx].ORG)), r, 3);
                g.Add(LampCell(E.LiveLamp(mm => mm.Motors[idx].ALM, true)), r, 4);
                g.Add(TextCell(mo.Code, E.CardBg), r, 5);
                g.Add(LampCell(E.LiveLamp(mm => mm.Motors[idx].Busy)), r, 6);
                g.Add(LampCell(E.LiveLamp(mm => mm.Motors[idx].INP)), r, 7);
                g.Add(LampCell(E.Live(mm => mm.Motors[idx].Position, "0.000", 0, E.CardBg, E.P, 11)), r, 8);
                g.Add(LampCell(E.Live(mm => mm.Motors[idx].Position, "0.000", 0, E.CardBg, E.P, 11)), r, 9);
                g.Add(TextCell("UnHomed/Stop", E.CardBg), r, 10);
                g.Add(LampCell(E.Live(mm => mm.Motors[idx].Torque, "0.0", 0, E.CardBg, E.P, 11)), r, 11);
                g.Add(LampCell(E.ClickLamp("dm_ens_" + i, false, false, 13)), r, 12);
                g.Add(LampCell(E.ClickLamp("dm_sp_" + i, false, false, 13)), r, 13);
                g.Add(LampCell(E.ClickLamp("dm_sn_" + i, false, false, 13)), r, 14);
                g.Add(TextCell("", E.CardBg), r, 15);
            }

            return E.V(
                
                E.Scroll(g, 430, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto),
                E.Spacer(6),
                E.T("点动 / 定距增量移动：", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.JogBar(),
                E.Spacer(4),
                E.H(
                    E.MiniBtn("Home", () => Ops.HomeAll()),
                    E.MiniBtn("Stop", Ops.StopAllAxes),
                    E.MiniBtn("ClearAlm", Ops.ClearAlarm),
                    E.MiniBtn("ClearBusAlm", Ops.ClearBusAlarm),
                    E.MiniBtn("X", Ops.CancelHome),
                    E.T("（X = 取消归零队列）", 11.5, E.Sec).Margin(6, 0, 0, 0)),
                E.Group("信号说明",
                    E.T("ORG：电机原点传感器状态　ALM：电机报警信号(伺服报警)　Homed：是否进行过归零　Code：报警代码　Busy：是否在动作中\n" +
                        "INP：伺服 INP(In-Position) 信号　MMPos：轴运行当前位置　Torque：扭矩　SP：正向软极限　SN：负向软极限\n" +
                        "On：伺服电机线圈励磁上电(Servo On)，停机时可点击翻转输出，未励磁时可用手推动电机便于排错　Ens：消除软极限报警",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  2/3. 上料臂 / 下料臂 (image70 / image74)
        // ==================================================================
        private static Element ArmBody(bool loader)
        {
            string p = loader ? "ld" : "ul";
            string title = loader ? "上料臂" : "下料臂";
            var m = E.M;

            // ---- 左：吸头选择 + XY ----
            var headSel = E.Grid().Cols("68", "68", "68", "68");
            for (int i = 0; i < 8; i++)
                headSel.Add(new RadioElement(p + "_head", "#" + (i + 1), i, i == 2, 12), i / 4, i % 4);

            var xy = E.Grid().Cols("66", "72", "72", "72", "72");
            xy.Add(E.T("", 12), 0, 0);
            string[] ax = { "ArmX", "ArmY", "ArmPX", "ArmPY" };
            for (int c = 0; c < 4; c++)
                xy.Add(E.T(ax[c], 11.5, E.Sec, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center), 0, c + 1);
            xy.Add(E.T("目标值", 11.5, E.P, false, TextWrapping.NoWrap), 1, 0);
            for (int c = 0; c < 4; c++) xy.Add(E.Num(p + "_tgt" + c, 0, 68), 1, c + 1);
            xy.Add(E.T("当前值", 11.5, E.P, false, TextWrapping.NoWrap), 2, 0);
            for (int c = 0; c < 4; c++)
            {
                int k = c;
                xy.Add(E.Live(mm => mm.Motors[4 + k].Position, "0.000", 68, E.BandGrey, E.P, 11.5), 2, c + 1);
            }
            xy.Add(E.Btn("Go", () => Ops.ArmGo(loader,
                E.M.GetChoice(p + "_head", 2),
                E.M.GetChoice(p + "_zone", 0),
                E.M.GetNum(p + "_tgt0", 0),
                E.M.GetNum(p + "_tgt1", 0),
                E.M.GetNum(p + "_gap", 5)), "PrimaryButton", 12), 3, 0);

            var left = E.V(
                E.Group("吸头选择", headSel),
                E.Group("XY",
                    E.H(
                        E.T("X:", 12, E.P, false, TextWrapping.NoWrap), E.Num(p + "_x", 0, 74),
                        E.T("Y:", 12, E.P, false, TextWrapping.NoWrap), E.Num(p + "_y", 0, 74),
                        E.T("间距:", 12, E.P, false, TextWrapping.NoWrap), E.Num(p + "_gap", 5, 62)),
                    E.Spacer(8),
                    xy)
            ).Wd(348);

            // ---- 右：目标区域 ----
            var zones = E.Grid().Cols("92", "92", "92");
            zones.Add(new RadioElement(p + "_zone", "预热盘1", 0, true, 14), 0, 0);
            zones.Add(new RadioElement(p + "_zone", "预热盘2", 1, false, 14), 1, 0);
            zones.Add(new RadioElement(p + "_zone", "上料盘", 2, false, 14), 2, 0);
            zones.Add(new RadioElement(p + "_zone", "小车A", 3, false, 14), 0, 1);
            zones.Add(new RadioElement(p + "_zone", "小车B", 4, false, 14), 1, 1);
            zones.Add(new RadioElement(p + "_zone", "清洁台", 5, false, 14), 0, 2);
            zones.Add(new RadioElement(p + "_zone", "空闲", 6, false, 14), 1, 2);

            // ---- 右：吸头矩阵 ----
            string[] colSpec = { "66", "72", "72", "72", "72", "72", "72", "72", "72" };
            var hg = E.Grid().Cols(colSpec);
            hg.Add(TextCell("", E.HeadBlue, 24), 0, 0);
            for (int i = 0; i < 8; i++)
                hg.Add(TextCell("吸头" + (i + 1), E.HeadBlue, 24, TextAlignment.Center, true), 0, i + 1);

            hg.Add(TextCell("全高位", E.CardBg, 36), 1, 0);
            hg.Add(TextCell("真空", E.CardBg, 28), 2, 0);
            hg.Add(TextCell("吸取", E.CardBg, 28), 3, 0);
            hg.Add(TextCell("释放", E.CardBg, 28), 4, 0);

            for (int i = 0; i < 8; i++)
            {
                hg.Add(TwoLineCell("高位", "-1.000"), 1, i + 1);
                hg.Add(LampCell(E.ClickLamp(p + "_vac_" + i, true, false, 15), 28), 2, i + 1);
                hg.Add(LampCell(E.ClickLamp(p + "_pick_" + i, false, true, 15), 28), 3, i + 1);
                hg.Add(LampCell(E.ClickLamp(p + "_rel_" + i, false, true, 15), 28), 4, i + 1);
            }

            hg.Add(TextCell("", E.CardBg, 30), 5, 0);
            hg.Add(TextCell("", E.CardBg, 30), 6, 0);
            for (int i = 0; i < 8; i++)
            {
                hg.Add(new CellElement
                {
                    Background = E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 30,
                    Content = E.MiniBtn("全取", () => Ops.HeadAllPick(p, i))
                }, 5, i + 1);
                hg.Add(new CellElement
                {
                    Background = E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 30,
                    Content = E.MiniBtn("全放", () => Ops.HeadAllRelease(p, i))
                }, 6, i + 1);
            }

            var right = E.V(
                E.Group("目标区域", zones),
                E.Group("吸头控制", E.Scroll(hg, 260, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Disabled)),
                E.Group("说明",
                    E.T("方式一：选择吸头 → 选择目标区域 → 输入目标 X / Y 位置 → 点击 [Go] 执行 X-Y 轴运动（吸头 1~8 必须高于飞行高位）。\n" +
                        "方式二：点击目标区域中的预热盘按钮，再打开主界面“显示控制板”里的出入料区域，在区域内用右键选择分布点即可运动到该坐标；" +
                        "选中分布点后按 INSERT 可在该位置插入 IC。\n" +
                        "真空控制：红色为输入状态，绿色为输出状态，暗色为非激活态，亮色为激活态；点击输出图标可切换输出状态，从而控制真空吸取 / 释放。",
                        11.5, E.Sec, false, TextWrapping.Wrap))
            );

            return E.Grid().Cols("352", "*").Add(left, 0, 0).Add(right, 0, 1);
        }

        // ==================================================================
        //  4. 测试区域 (image75)
        // ==================================================================
        private static Element AreaBody()
        {
            var m = E.M;

            Func<string, string, double, double, Element> car = (tag, key, inPos, outPos) =>
                E.Group("运料小车" + tag,
                    E.H(E.Display("0.000", 96),
                        E.MiniBtn("Jog", () => Ops.JogAxisByName("测试运料小车" + tag, 1), 56)),
                    E.Spacer(8),
                    E.H(
                        E.Box(E.V(
                            E.T("至入口", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.T(inPos.ToString("0.000"), 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)),
                            E.BandGrey, Palette.LineGrey, 1, 3, new Thickness(4), new Thickness(0, 0, 8, 0), 128, 42),
                        E.Box(E.V(
                            E.T("至出口", 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                            E.T(outPos.ToString("0.000"), 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)),
                            E.BandGrey, Palette.LineGrey, 1, 3, new Thickness(4), new Thickness(0, 0, 8, 0), 128, 42)),
                    E.Spacer(8),
                    E.LampLine("入口浮动检查", 8, true, false, "sh_" + key + "_float"),
                    E.LampLine("出口检查", 8, true, false, "sh_" + key + "_gate"),
                    E.LampLine("垂直传感器", 8, true, false, "sh_" + key + "_vert"));

            var other = E.Group("其他",
                E.LampLine("Skt空", 4, true, false, "ta_skt", 104),
                LabInfo("比例阀低压检查", true),
                LabInfo("Z1抱闸释放按钮", false),
                LabInfo("Z2抱闸释放按钮", false),
                LabIo("Z1电机抱闸打开", "ta_zb1", false),
                LabIo("Z2电机抱闸打开", "ta_zb2", false),
                LabIo("真空气路更改", "ta_air", false));

            Func<string, string, Element> armIo = (tag, prefix) =>
            {
                string[] heads = { "a", "b", "c", "d", "e", "f", "g", "h",
                                   "AF", "Ag", "Bf", "Bg", "Ae", "Ah", "Be", "Bh" };
                var g = E.Grid().Cols("44", "34", "34", "34", "34", "34", "34", "34", "34",
                                      "34", "34", "34", "34", "34", "34", "34", "34", "72");
                g.Add(TextCell("", E.HeadBlue, 22), 0, 0);
                for (int c = 0; c < 16; c++)
                    g.Add(TextCell(heads[c], E.HeadBlue, 22, TextAlignment.Center, true), 0, c + 1);
                g.Add(TextCell("", E.HeadBlue, 22), 0, 17);

                string[] rows = { "真空", "吸取", "释放" };
                for (int r = 0; r < 3; r++) g.Add(TextCell(rows[r], E.CardBg, 26), r + 1, 0);

                for (int c = 0; c < 16; c++)
                {
                    g.Add(LampCell(E.ClickLamp(prefix + "_vac_" + c, true, false, 14), 26), 1, c + 1);
                    g.Add(LampCell(E.ClickLamp(prefix + "_pick_" + c, false, true, 14), 26), 2, c + 1);
                    g.Add(LampCell(E.ClickLamp(prefix + "_rel_" + c, false, true, 14), 26), 3, c + 1);
                }

                g.Add(new CellElement
                {
                    Background = E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 78,
                    Content = E.V(
                        E.MiniBtn("全关", () => { for (int i = 0; i < 16; i++)
                            { E.M.SetToggle(prefix + "_vac_" + i, false); E.M.SetToggle(prefix + "_pick_" + i, false);
                              E.M.SetToggle(prefix + "_rel_" + i, false); } Ops.Info("测试臂" + tag + " IO：全关"); }),
                        E.MiniBtn("全吸", () => Ops.ArmIoAll(prefix, "vac")),
                        E.MiniBtn("全放", () => Ops.ArmIoAll(prefix, "rel")))
                }, 1, 17, 3, 1);

                return E.Group("测试臂" + tag + "IO",
                    E.Scroll(g, 150, System.Windows.Controls.ScrollBarVisibility.Auto,
                        System.Windows.Controls.ScrollBarVisibility.Disabled));
            };

            var testArm = E.Group("测试臂",
                E.Grid().Cols("112", "112", "112", "112")
                    .Add(TwoLineCell("测试Z1", "0.000", E.BandGrey, 36), 0, 0)
                    .Add(TwoLineCell("测试Z2", "0.000", E.BandGrey, 36), 0, 1)
                    .Add(TwoLineCell("测试位Y1", "0.000", E.BandGrey, 36), 0, 2)
                    .Add(TwoLineCell("测试位Y2", "0.000", E.BandGrey, 36), 0, 3),
                E.Spacer(8),
                E.H(
                    PosBtn("高位", "-131.000", () => Ops.TestArmPos("测试位下压Z1", -131.000)),
                    PosBtn("高位", "-131.000", () => Ops.TestArmPos("测试位下压Z2", -131.000)),
                    PosBtn("A取", "-2.705", () => Ops.TestArmPos("测试位移动Y1", -2.705)),
                    PosBtn("B取", "2.278", () => Ops.TestArmPos("测试位移动Y2", 2.278))),
                E.Spacer(4),
                E.H(
                    PosBtn("取料", "-64.608", () => Ops.MoveAxisByName("测试运料小车A", -64.608)),
                    PosBtn("取料", "-83.406", () => Ops.MoveAxisByName("测试运料小车B", -83.406)),
                    PosBtn("A测", "162.456", () => Ops.TestArmPos("测试位移动Y1", 162.456)),
                    PosBtn("B测", "-162.423", () => Ops.TestArmPos("测试位移动Y2", -162.423))),
                E.Spacer(4),
                E.H(
                    PosBtn("放料", "-87.608", () => Ops.MoveAxisByName("测试运料小车A", -87.608)),
                    PosBtn("放料", "-85.606", () => Ops.MoveAxisByName("测试运料小车B", -85.606)),
                    E.MiniBtn("Jog-Z1", () => Ops.JogAxisByName("测试位下压Z1", 1), 62),
                    E.MiniBtn("Jog-Z2", () => Ops.JogAxisByName("测试位下压Z2", 1), 62)),
                E.Spacer(4),
                E.H(
                    PosBtn("接触位", "-13.421", () => Ops.TestArmPos("测试位下压Z1", -13.421)),
                    PosBtn("接触位", "-12.773", () => Ops.TestArmPos("测试位下压Z2", -12.773)),
                    E.MiniBtn("Jog-Y1", () => Ops.JogAxisByName("测试位移动Y1", 1), 62),
                    E.MiniBtn("Jog-Y2", () => Ops.JogAxisByName("测试位移动Y2", 1), 62)),
                E.Spacer(6),
                E.T("当测试臂 1/2 位于高位以上位置时，测试 Y 才可移动。", 11.5, E.Sec, false, TextWrapping.Wrap));

            return E.V(
                E.Grid().Cols("440", "*")
                    .Add(E.V(car("A", "a", -3.370, 487.790), car("B", "b", -3.410, 487.910)), 0, 0)
                    .Add(testArm, 0, 1),
                E.Grid().Cols("300", "*")
                    .Add(other, 0, 0)
                    .Add(armIo("1", "ta1"), 0, 1),
                armIo("2", "ta2"),
                E.Group("说明",
                    E.T("入口浮动检查：红色指示灯，非激活（暗色）表示传感器被料挡住，即料高度高出预设高度、未放置妥帖，会激活浮动报警；由左到右为 S1~S8。\n" +
                        "出口检查：粉色指示灯显示 Gate Sensor 状态，非激活表示小车内存在 IC；垂直传感器激活表示小车内存在 IC。\n" +
                        "高位：测试臂归零后的位置；安全位：Y 轴移动时 Z 轴必须高于此位置；取料 / 放料：小车入口 / 出口盘的高度；接触位：下降至测试座的位置。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  5. 料盘进出 (image78)
        // ==================================================================
        private static Element TrayIoBody()
        {
            string[] st = { "Safe", "Loader", "Empty", "Buffer", "Auto1", "Auto2", "Auto3" };
            string[] pos = { "-722.990", "-1274.880", "-950.520", "-722.990", "-400.100", "-208.750", "-18.330" };
            string[] baseLines = { "内侧到位", "内侧接近", "斜顶气缸", "落1", "落2", "料盘存在", "分盘销子",
                                   "气缸1上", "气缸1下位", "气缸2上", "气缸2下位", "电机运转", "电机高速" };
            string[] autoLines = { "内侧到位", "敲击气缸", "斜顶气缸", "浮动检查", "料盘满", "外侧接近", "外侧到位",
                                   "气缸1上", "气缸1下位", "气缸2上", "气缸2下位", "电机运转", "电机反转" };

            var cols = E.H();
            for (int i = 0; i < st.Length; i++)
            {
                var col = E.V();
                col.Children.Add(E.Box(E.V(
                        E.T(st[i], 11.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                        E.T(pos[i], 11, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)),
                    E.HeadBlue, Palette.LineGrey, 1, 0, new Thickness(3, 2, 3, 2), new Thickness(0, 0, 0, 3), 150));

                var lines = (i >= 4) ? autoLines : baseLines;
                foreach (var ln in lines)
                    col.Children.Add(IoRow(ln, "tio_" + st[i] + "_" + ln, true));
                col.Wd(154);
                col.Margin = new Thickness(0, 0, 4, 0);
                cols.Children.Add(col);
            }

            // 右侧固定盘
            var fixedCol = E.V(
                E.T("", 11),
                InfoRow("Fix1料盘存在", true),
                InfoRow("Fix2料盘存在", true),
                InfoRow("Fix3料盘存在", true)).Wd(150);

            // 底部：料盘搬臂状态
            var bottom = E.H();
            for (int i = 1; i < st.Length; i++)
            {
                var c = E.V(
                    E.T(st[i], 11.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                    IoRow("有无料盘(逻辑)", "tio_arm_" + st[i] + "_logic", true),
                    IoRow("内侧料盘存在", "tio_arm_" + st[i] + "_inner", true),
                    IoRow("外侧到位存在", "tio_arm_" + st[i] + "_outer", true));
                c.Wd(154);
                c.Margin = new Thickness(0, 0, 4, 0);
                bottom.Children.Add(c);
            }

            return E.V(
                E.H(cols, fixedCol),
                E.Spacer(12),
                E.Group("料盘搬臂状态", bottom),
                E.Group("说明",
                    E.T("输入 / 输出状态：红色为输入状态，绿色为输出状态，暗色为非激活态，亮色为激活态。\n" +
                        "料盘传送臂按钮 [Safe]/[Loader]/[Empty]/[Buffer]/[Auto1~3] 分别控制传送臂运动至对应工作位置，电机移动时料盘吸头应位于上面位置。\n" +
                        "吸头上位 / 下位为输入状态；吸头向下为输出，控制吸盘上下气缸；吸头真空为输入，激活表示料盘被吸住；吸头吸取 / 释放为输出，控制料盘吸取真空阀。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  6. 面板IO (image79)
        // ==================================================================
        private static Element PanelIoBody()
        {
            string[] btn = { "开始", "归零", "暂停", "复位", "清出", "跳过", "重试",
                             "清机", "结盘", "出盘", "报警清除", "单步", "T_Start", "保留" };

            var panel = E.Grid().Cols("146", "146");
            panel.Add(TextCell("按钮", E.HeadBlue, 24, TextAlignment.Center, true), 0, 0);
            panel.Add(TextCell("按钮指示灯", E.HeadBlue, 24, TextAlignment.Center, true), 0, 1);
            for (int i = 0; i < btn.Length; i++)
            {
                panel.Add(LampCell(IoRow(btn[i], "pi_btn_" + i, true, false, 13), 26), i + 1, 0);
                panel.Add(LampCell(IoRow(btn[i], "pi_led_" + i, true, false, 13), 26), i + 1, 1);
            }

            var other = E.V(
                E.Grid().Cols("176", "152")
                    .Add(IoRow("主气路低压", "pi_air_low", true), 0, 0)
                    .Add(GreenRow("加热器电源", "pi_heater"), 0, 1)
                    .Add(IoRow("清洁盘存在检查", "pi_clean", true), 1, 0)
                    .Add(GreenRow("测试区加热", "pi_testheat"), 1, 1)
                    .Add(IoRow("热吹风流量报警", "pi_blow", true), 2, 0)
                    .Add(GreenRow("热吹气", "pi_hotair"), 2, 1)
                    .Add(GreenRow("上料入盘CDA底部清洁", "pi_cda"), 3, 0),
                E.Spacer(10),
                E.T("门关闭", 11.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center),
                E.Spacer(2),
                E.H(
                    E.ClickLamp("pi_door_0", true), E.ClickLamp("pi_door_1", true), E.ClickLamp("pi_door_2", true),
                    E.ClickLamp("pi_door_3", true), E.ClickLamp("pi_door_4", true), E.ClickLamp("pi_door_5", true)),
                E.Spacer(8),
                E.Grid().Cols("150", "150")
                    .Add(E.Group("离子风机",
                        GreenRow("清洁", "pi_ion_clean"),
                        IoRow("#1", "pi_ion_1", true), IoRow("#2", "pi_ion_2", true),
                        IoRow("#3", "pi_ion_3", true), IoRow("#4", "pi_ion_4", true),
                        IoRow("#5", "pi_ion_5", true), IoRow("#6", "pi_ion_6", true),
                        IoRow("#7", "pi_ion_7", true)), 0, 0)
                    .Add(E.V(
                        E.Group("测试位离子风机",
                            IoRow("#TA", "pi_ion_ta", true),
                            GreenRow("#TA停止", "pi_ion_tastop")),
                        E.Group("离子吹气", GreenRow("离子吹气", "pi_ion_blow"))), 0, 1));

            return E.V(
                E.Grid().Cols("330", "*")
                    .Add(E.Group("操作面板",
                        E.H(E.Chk("pi_diag", "诊断", false), E.Lamp(true), E.T("前面板有效", 11.5, E.P)),
                        E.Spacer(6),
                        E.Scroll(panel, 420, System.Windows.Controls.ScrollBarVisibility.Disabled,
                            System.Windows.Controls.ScrollBarVisibility.Auto)), 0, 0)
                    .Add(E.Group("其他IO", other), 0, 1),
                E.Group("信号灯",
                    E.H(
                        E.T("红", 11.5, E.P), E.ClickLamp("pi_lamp_r", true, false, 16),
                        E.T("绿", 11.5, E.P), E.ClickLamp("pi_lamp_g", false, false, 16),
                        E.T("橙", 11.5, E.P), E.ClickLamp("pi_lamp_o", true, false, 16),
                        E.T("蜂鸣器", 11.5, E.P), E.ClickLamp("pi_buzzer", true, false, 16))),
                E.Group("说明",
                    E.T("操作面板：按钮为输入状态，按下时激活信号；按钮指示灯为输出状态，激活时对应按钮指示灯点亮。\n" +
                        "主气路低压：气压传感器输入，检测总输入压缩空气气压是否过低（<0.5MPa），激活表示气压正常。\n" +
                        "门关闭：6 个传感器检测门是否关闭，激活表示门关闭。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  7. 测试系统IO (image80)
        // ==================================================================
        private static Element TestIoBody()
        {
            var m = E.M;
            string[] rowLbl = { "A", "B", "C", "D" };
            string[] colLbl = { "a", "b", "c", "d", "e", "f", "g", "h" };

            var ck = E.Grid().Cols("34", "42", "42", "42", "42", "42", "42", "42", "42");
            ck.Add(E.T("", 11), 0, 0);
            for (int c = 0; c < 8; c++)
                ck.Add(E.T(colLbl[c], 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center), 0, c + 1);
            for (int r = 0; r < 4; r++)
            {
                ck.Add(E.T(rowLbl[r], 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center), r + 1, 0);
                for (int c = 0; c < 8; c++)
                    ck.Add(new CellElement
                    {
                        Background = E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 24,
                        Content = E.Chk("tio_site_" + r + "_" + c, "", false)
                    }, r + 1, c + 1);
            }

            var ttl = E.Grid().Cols("52", "56", "56", "56", "56");
            ttl.Add(TextCell("", E.HeadBlue, 24), 0, 0);
            for (int c = 0; c < 4; c++)
                ttl.Add(TextCell("CS" + (c + 1), E.HeadBlue, 24, TextAlignment.Center, true), 0, c + 1);
            string[] ttlRows = { "Bin0", "Bin1", "Bin2", "Bin3", "Bin4", "Bin5", "Bin6", "Bin7" };
            for (int r = 0; r < ttlRows.Length; r++)
            {
                ttl.Add(TextCell(ttlRows[r], E.CardBg, 24, TextAlignment.Left), r + 1, 0);
                for (int c = 0; c < 4; c++)
                    ttl.Add(LampCell(E.ClickLamp("tio_bin_" + r + "_" + c, true, false, 14), 24), r + 1, c + 1);
            }
            ttl.Add(TextCell("EOT", E.CardBg, 24, TextAlignment.Left), 9, 0);
            ttl.Add(TextCell("DUT", E.CardBg, 24, TextAlignment.Left), 10, 0);
            ttl.Add(TextCell("SOT", E.CardBg, 24, TextAlignment.Left), 11, 0);
            for (int c = 0; c < 4; c++)
            {
                ttl.Add(LampCell(E.ClickLamp("tio_eot_" + c, true, false, 14), 24), 9, c + 1);
                ttl.Add(LampCell(E.ClickLamp("tio_dut_" + c, false, true, 14), 24), 10, c + 1);
                ttl.Add(LampCell(E.ClickLamp("tio_sot_" + c, false, true, 14), 24), 11, c + 1);
            }
            ttl.Add(TextCell("动态刷新", E.CardBg, 26, TextAlignment.Right), 12, 0);
            ttl.Add(LampCell(E.ClickLamp("tio_dyn", false, true, 14), 26), 12, 1);

            // ---- 接口方式选择（与「测试接口」页共用同一个 ti_type 键，改哪边都同步）----
            var waySel = E.H(
                E.T("接口方式：", 12, E.Sec),
                E.SpacerW(4),
                E.Radios("ti_type", new[] { "TTL", "GPIB", "RS232", "TCP" }, false),
                E.SpacerW(12),
                E.T("当前：", 12, E.Sec),
                E.Live(_ => TestInterfaceService.WayName, null, 80, E.BandBlue, E.P, 12.5));

            // ---- ① TTL 调试面板：测试位矩阵 + TTL-接口 灯阵 ----
            var ttlPanel = E.V(
                ck,
                E.Spacer(8),
                E.Btn("SOT", Ops.TestIo_Sot, "PrimaryButton", 12),
                E.Spacer(8),
                E.Group("TTL-接口", ttl),
                E.Spacer(8),
                E.Group("TTL 说明",
                    E.T("SOT 出（OUT 位 40ms 脉冲），EOT / DUT 入（20ms 轮询，跳变才上报）。灯位号在「测试接口」页的 TTL SOT / EOT / DUT 位里设。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));

            // ---- ② RS232 调试面板 ----
            var rs232Panel = E.V(
                E.Group("RS232 串口参数",
                    E.Row("串口端口号", E.Cmb("ti_com", TestInterfaceService.AvailablePorts(), 150), 110),
                    E.Row("波特率", E.Cmb("ti_baud", new[] { "9600", "19200", "38400", "57600", "115200" }, 150), 110),
                    E.Row("数据位", E.Cmb("ti_data", new[] { "7", "8" }, 150), 110),
                    E.Row("停止位", E.Cmb("ti_stop", new[] { "1", "2" }, 150), 110),
                    E.Row("校验值", E.Cmb("ti_parity", new[] { "None", "Odd", "Even" }, 150), 110),
                    E.Spacer(8),
                    E.H(E.Btn("打开串口", TestInterfaceService.Connect, "PrimaryButton", 12),
                        E.Btn("关闭串口", TestInterfaceService.Disconnect, "PrimaryButton", 12))),
                E.Spacer(8),
                E.Group("RS232 说明",
                    E.T("按上面参数打开真实串口，收发内容显示在右侧日志里。改完参数要先「关闭串口」再「打开串口」才会生效。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));

            // ---- ③ TCP 调试面板 ----
            var tcpPanel = E.V(
                E.Group("TCP 连接参数",
                    E.Row("地址", E.Text("ti_ip", "127.0.0.1", 150), 110),
                    E.Row("端口", E.Num("ti_port", 5001, 120, "0", 1, 65535), 110),
                    E.Row("本机角色", E.Cmb("ti_role", new[] { "客户端（主动连测试机）", "服务端（等测试机连入）" }, 230), 110),
                    E.Spacer(8),
                    E.H(E.Btn("连接", TestInterfaceService.Connect, "PrimaryButton", 12),
                        E.Btn("断开", TestInterfaceService.Disconnect, "PrimaryButton", 12))),
                E.Spacer(8),
                E.Group("TCP 说明",
                    E.T("客户端模式由本机主动连测试机的 IP:端口；服务端模式监听本机端口、等测试机连入（建链最长等「超时」设定的毫秒数）。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));

            // ---- ④ GPIB 调试面板 ----
            var gpibPanel = E.V(
                E.Group("GPIB 参数",
                    E.Row("接口地址", E.Num("ti_gpib", 11, 120, "0", 0, 30), 110),
                    E.Spacer(8),
                    E.H(E.Btn("连接", TestInterfaceService.Connect, "PrimaryButton", 12),
                        E.Btn("断开", TestInterfaceService.Disconnect, "PrimaryButton", 12),
                        E.MiniBtn("*IDN? 查询", () => Ops.TestIo_Send("*IDN?")))),
                E.Spacer(8),
                E.Group("GPIB 说明",
                    E.T("资源串按 GPIB0::地址::INSTR 生成；优先用 VISA（visa32.dll），装不到时自动退回 NI-488.2（gpib-32.dll）。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));

            // ---- 按接口方式实时切换（VisibleElement 每 tick 重算条件，切换不用重建页面）----
            var left = E.V(
                waySel,
                E.Spacer(8),
                E.ShowIf(_ => TestInterfaceService.Way == TestInterfaceService.WayTtl, ttlPanel),
                E.ShowIf(_ => TestInterfaceService.Way == TestInterfaceService.WayGpib, gpibPanel),
                E.ShowIf(_ => TestInterfaceService.Way == TestInterfaceService.WayRs232, rs232Panel),
                E.ShowIf(_ => TestInterfaceService.Way == TestInterfaceService.WayTcp, tcpPanel));

            // 温启志 18719361399 · 收发日志：长文本需换行 + 左对齐
            var logEl = E.Live(_ => TestInterfaceService.LogText, null, 330, E.CardBg, E.P, 11.5);
            logEl.Align = TextAlignment.Left;
            logEl.TextHAlign = HorizontalAlignment.Left;
            logEl.Wrap = TextWrapping.Wrap;

            var right = E.V(
                E.H(E.LiveLamp(_ => TestInterfaceService.IsConnected, false, true, 14),
                    E.SpacerW(6),
                    E.Live(_ => TestInterfaceService.StateText, null, 240, E.BandGrey, E.P, 12)),
                E.Spacer(6),
                E.H(E.Btn("连接", TestInterfaceService.Connect, "PrimaryButton", 12),
                    E.Btn("断开", TestInterfaceService.Disconnect, "PrimaryButton", 12),
                    E.MiniBtn("TestInfo", Ops.TestIo_TestInfo)),
                E.Spacer(8),
                E.H(E.T("msg:", 12, E.P), E.Text("tio_msg", "", 240)),
                E.Spacer(6),
                E.H(E.MiniBtn("Send", () => Ops.TestIo_Send(m.GetText("tio_msg"))),
                    E.MiniBtn("Clear", () => { m.SetText("tio_msg", ""); m.Log("信息", "清空发送内容"); })),
                E.Spacer(6),
                E.H(E.Cmb("ti_com", TestInterfaceService.AvailablePorts(), 120),
                    E.MiniBtn("清空日志", TestInterfaceService.ClearLog)),
                E.Spacer(8),
                E.Box(E.Scroll(logEl, 300, System.Windows.Controls.ScrollBarVisibility.Disabled,
                        System.Windows.Controls.ScrollBarVisibility.Auto),
                    E.CardBg, Palette.LineGrey, 1, 3, new Thickness(8)));

            return E.V(
                E.Grid().Cols("*", "400").Add(left, 0, 0).Add(right, 0, 1),
                E.Group("说明",
                    E.T("调试面板跟随上方「接口方式」自动切换：TTL / GPIB / RS232 / TCP 各一套，切换即时生效、不用重进页面。\n"
                        + "[SOT] 按钮可手动激活开始测试信号：① 在需要测试的测试位上选择激活；② 按下 [SOT] 按钮激活测试过程；"
                        + "③ 在测试状态中检查测试反馈，测试成功后测试结果反馈到该框内。\n"
                        + "报文收发走真实通道（TCP / RS232 / GPIB，TTL 走 DIO 位），收发内容与十六进制原样显示在右侧日志中。\n"
                        + "动态刷新：当测试接口板为锁定硬件版本时，关闭动态刷新将显示最后一次刷新的状态，打开则实时显示输入状态。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
