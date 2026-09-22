// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（RecipePages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_RecipePages
    internal static class WenQiZhiTag_RecipePages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · RecipePages。</summary>
        internal const string WenQiZhiFile = "RecipePages";
    }

    // ==================================================================
    //  “品种设定”工具条：品种维护 / 测试模式 / 料盘 / 测试臂 / 测试接口 /
    //  Bin设定 / 温度 / 视觉 / 清洁盘
    // ==================================================================
    // 温启志 18719361399 · 类 public static class RecipePages
    public static class RecipePages
    {
        private static string[] SiteCols => new[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        private static string[] SiteRows => new[] { "A", "B", "C", "D" };
        private static string[] BinCols => new[] { "NT", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "NG", "ER" };

        // ---------------- 品种维护 (image40) ----------------
        public static Element RecipeMaint()
        {
            var m = E.M;
            var list = E.V();
            foreach (var r in m.Recipes)
            {
                bool cur = r == m.Recipe;
                list.Children.Add(E.Box(
                    E.T(r, 12.5, cur ? Brushes.Goldenrod : E.P, cur),
                    cur ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE4)) : Brushes.White,
                    E.Div, 1, 4, new Thickness(8, 5, 8, 5), new Thickness(0, 0, 0, 2)));
            }

            return E.V(
                
                E.Row("品种名", E.Text("rc_name", "NewRecipe", 220), 90),
                E.Spacer(6),
                E.H(
                    E.Btn("新建", () => m.Log("信息", "新建品种：" + m.GetText("rc_name"))),
                    E.Btn("删除", () => m.Log("信息", "删除品种（当前品种不可删除）")),
                    E.Btn("复制", () => m.Log("信息", "复制品种")),
                    E.Btn("重命名", () => m.Log("信息", "重命名品种")),
                    E.Btn("导入", () => m.Log("信息", "导入品种参数文件")),
                    E.Btn("导出", () => m.Log("信息", "导出品种参数文件"))),
                E.Spacer(10),
                E.Group("品种列表", E.Scroll(list, 320)),
                E.Group("说明",
                    E.T("操作方法：在目标文本框中输入新品种的名称，点击[新建]按钮即可新建；在列表中选择欲操作的品种后点击[删除]/[复制]/[重命名]。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---------------- 测试模式 (image41) ----------------
        public static Element TestMode()
        {
            var m = E.M;
            var layouts = new[]
            {
                "ThirtyTwo-Sites_R", "ThirtyTwo-Sites_L", "Sixteen-Sites_A", "Sixteen-Sites_B",
                "Eight-Sites_A", "Eight-Sites_B", "Four-Sites_A", "Four-Sites_B",
                "Double-Sites_A", "Double-Sites_B", "Single-Site_A", "Single-Site_B",
                "SixtyFour-Sites", "TwentyFour-Sites", "Twelve-Sites", "Six-Sites", "Two-Sites"
            };

            var layout = E.V();
            foreach (var l in layouts)
            {
                bool cur = l == m.SiteLayoutName;
                layout.Children.Add(E.Box(E.T(l, 12, cur ? Brushes.Goldenrod : E.P, cur),
                    cur ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE4)) : Brushes.White,
                    E.Div, 1, 3, new Thickness(8, 4, 8, 4), new Thickness(0, 0, 0, 2)));
            }

            // 测试通道映射：a~h 列，A~D 行，每格一个通道下拉
            var chans = new[] { "—" }.Concat(Enumerable.Range(1, 32).Select(i => i.ToString())).ToArray();
            var mapGrid = E.Grid().Cols("26", "72", "72", "72", "72", "72", "72", "72", "72");
            mapGrid.Add(E.Cell("", E.HeadBlue, E.P, true), 0, 0);
            for (int c = 0; c < SiteCols.Length; c++) mapGrid.Add(E.Cell(SiteCols[c], E.HeadBlue, E.P, true), 0, c + 1);
            for (int r = 0; r < SiteRows.Length; r++)
            {
                mapGrid.Add(E.Cell(SiteRows[r], E.HeadBlue, E.P, true), r + 1, 0);
                for (int c = 0; c < SiteCols.Length; c++)
                {
                    int ch = r * 8 + c + 1;
                    var cb = E.Cmb("tm_ch_" + r + "_" + c, chans, 68);
                    cb.SelectedIndex = ch;   // 默认 Ch1..Ch32
                    mapGrid.Add(cb, r + 1, c + 1);
                }
            }

            return E.V(
                
                E.Grid().Cols("300", "*")
                    .Add(E.Group("测试座布局",
                        E.Scroll(layout, 300),
                        E.Spacer(6),
                        E.Row("间距X", E.Num("tm_pitch_x", 7, 90, "0.###"), 70, "mm"),
                        E.Row("间距Y", E.Num("tm_pitch_y", 7, 90, "0.###"), 70, "mm")), 0, 0)
                    .Add(E.Group("测试通道映射", E.Scroll(mapGrid, 300, System.Windows.Controls.ScrollBarVisibility.Auto, System.Windows.Controls.ScrollBarVisibility.Auto)), 0, 1),
                E.Group("运料小车工作模式",
                    E.Radios("tm_shuttle", new[] { "正常模式", "小车A", "小车B" }, false)),
                E.Group("测试位检查",
                    E.Chk("tm_site_chk", "激活测试位检查功能", true),
                    E.Row("气缸数量", E.Num("tm_cyl", 4, 80, "0"), 90),
                    E.Spacer(4),
                    E.Matrix(SiteCols, new[] { "电平" }, (r, c) => E.Cmb("tm_lvl_" + c, new[] { "High", "Low" }, 60), 66, 26, E.HeadBlue, E.HeadBlue, 44, 24)),
                E.Group("浮动检查传感器",
                    E.Chk("tm_float_in", "入口位置浮动检查传感器", true),
                    E.Chk("tm_float_out", "出口位置浮动检查传感器", true)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "测试模式设定已应用"))));
        }

        // ---------------- 料盘 (image42~image45) ----------------
        public static Element TrayPage()
        {
            var m = E.M;

            Func<int, Element> typeTab = idx => E.V(
                E.Row("名称", E.Text("tp_name" + idx, "SGS_QFP64_10X" + idx, 200), 70),
                E.Spacer(6),
                E.Grid().Cols("150", "150", "150", "150")
                    .Add(E.Row("X起点", E.Num("tp_x0_" + idx, 13, 80), 60), 0, 0)
                    .Add(E.Row("X间距", E.Num("tp_dx_" + idx, 15.7, 80), 60), 0, 1)
                    .Add(E.Row("X数量", E.Num("tp_nx_" + idx, 8, 80, "0"), 60), 0, 2)
                    .Add(E.Row("X伸缩", E.Num("tp_sx_" + idx, 0, 80), 60), 0, 3)
                    .Add(E.Row("Y起点", E.Num("tp_y0_" + idx, 13.1, 80), 60), 1, 0)
                    .Add(E.Row("Y间距", E.Num("tp_dy_" + idx, 15.2, 80), 60), 1, 1)
                    .Add(E.Row("Y数量", E.Num("tp_ny_" + idx, 20, 80, "0"), 60), 1, 2)
                    .Add(E.Row("Y伸缩", E.Num("tp_sy_" + idx, 0, 80), 60), 1, 3)
                    .Add(E.Row("厚度", E.Num("tp_th_" + idx, 0, 80), 60), 2, 0)
                    .Add(E.Row("吸取位", E.Num("tp_pick_" + idx, 0, 80), 60), 2, 1),
                E.Spacer(8),
                E.H(
                    E.TrayDiagram((int)m.GetNum("tp_nx_" + idx, 8), (int)m.GetNum("tp_ny_" + idx, 20)),
                    E.SpacerW(14),
                    E.TraySide(),
                    E.SpacerW(14),
                    E.PadGrid(8, 14)),
                E.Spacer(8),
                E.Row("选择需要复制的料盘类型", E.Cmb("tp_copy_" + idx, new[] { "料盘类型1", "料盘类型2", "料盘类型3" }, 160), 170),
                E.H(E.MiniBtn("复制", () => m.Log("信息", "复制料盘类型")), E.Btn("应用", () => m.Log("信息", "料盘参数已应用"))));

            var defList = new[] { "上料盘", "上料空盘", "下料空盘", "自动盘1", "自动盘2", "自动盘3", "固定盘1", "固定盘2", "固定盘3", "清洁盘" };
            var defPanel = E.V(
                E.T("料盘定义：可以定义各个区域使用料盘的类型及取放方式（快速 / X跳过 / 正向 / 反向）。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(8),
                E.Grid().Cols("160", "160", "160", "160")
                    .Add(E.Cell("区域", E.HeadBlue, E.P, true), 0, 0)
                    .Add(E.Cell("料盘类型", E.HeadBlue, E.P, true), 0, 1)
                    .Add(E.Cell("取放方式", E.HeadBlue, E.P, true), 0, 2)
                    .Add(E.Cell("器件旋转", E.HeadBlue, E.P, true), 0, 3),
                BuildDefRows(defList, 1),
                E.Spacer(8),
                E.H(E.MiniBtn("更改", () => m.Log("信息", "已更改所选区域料盘定义")),
                    E.Btn("应用", () => m.Log("信息", "料盘定义已应用"))));

            return E.V(
                E.T("料盘设定", 13, E.P, true),
                E.Spacer(8),
                E.Tabs(
                    ("料盘类型1", () => typeTab(1)),
                    ("料盘类型2", () => typeTab(2)),
                    ("料盘类型3", () => typeTab(3)),
                    ("小车", () => E.V(
                        E.T("小车所用 KIT 的参数设置。", 11.5, E.Sec, false, TextWrapping.Wrap),
                        E.Spacer(8),
                        E.Row("X起点", E.Num("ct_x0", 0, 90), 80),
                        E.Row("X间距", E.Num("ct_dx", 15, 90), 80),
                        E.Row("X数量", E.Num("ct_nx", 8, 90, "0"), 80),
                        E.Row("Y起点", E.Num("ct_y0", 0, 90), 80),
                        E.Row("Y间距", E.Num("ct_dy", 15, 90), 80),
                        E.Row("Y数量", E.Num("ct_ny", 4, 90, "0"), 80),
                        E.Spacer(6),
                        E.H(E.TrayDiagram(8, 4), E.SpacerW(16), E.PadGrid(8, 4)))),
                    ("加热/致冷板", () => E.V(
                        E.Chk("hb_use1", "使用板#1", true),
                        E.Chk("hb_use2", "使用板#2", true),
                        E.Spacer(8),
                        E.Row("X起点", E.Num("hb_x0", 0, 90), 80),
                        E.Row("X间距", E.Num("hb_dx", 15, 90), 80),
                        E.Row("X数量", E.Num("hb_nx", 8, 90, "0"), 80),
                        E.Row("Y起点", E.Num("hb_y0", 0, 90), 80),
                        E.Row("Y间距", E.Num("hb_dy", 15, 90), 80),
                        E.Row("Y数量", E.Num("hb_ny", 4, 90, "0"), 80),
                        E.Spacer(6),
                        E.H(E.TrayDiagram(8, 4), E.SpacerW(16), E.PadGrid(8, 4)))),
                    ("料盘定义", () => defPanel)));
        }

        private static Element BuildDefRows(string[] areas, int startRow)
        {
            var g = E.Grid().Cols("160", "160", "160", "160");
            var types = new[] { "料盘类型1", "料盘类型2", "料盘类型3", "小车", "加热/致冷板" };
            var modes = new[] { "快速", "X跳过", "正向", "反向" };
            var rots = new[] { "无", "Rot90", "Rotm90", "Rot180" };
            for (int i = 0; i < areas.Length; i++)
            {
                int r = startRow + i;
                g.Add(E.Cell(areas[i], i % 2 == 0 ? E.BandBlue : E.BandGrey, E.P), r, 0);
                g.Add(E.Cmb("td_t_" + i, types, 150), r, 1);
                g.Add(E.Cmb("td_m_" + i, modes, 150), r, 2);
                g.Add(E.Cmb("td_r_" + i, rots, 150), r, 3);
            }
            return g;
        }

        // ---------------- 测试臂 (image46) ----------------
        public static Element TestArmPage()
        {
            var m = E.M;
            var contactModes = new[]
            {
                "直接-接触", "落料-接触", "直接-软接触", "落料-软接触",
                "顶端开口", "落料模式", "正常工作"
            };

            return E.V(
                
                E.Grid().Cols("*", "24", "*")
                    .Add(E.Group("高度位置",
                        E.Row("吸取位", E.Num("ta_pick", 0, 100), 90, "mm"),
                        E.Row("放置位", E.Num("ta_place", 0, 100), 90, "mm"),
                        E.Row("测试高度", E.Num("ta_test", 0, 100), 90, "mm"),
                        E.Row("落料偏移", E.Num("ta_drop", -2, 100, "0.###", -15, 0), 90, "mm"),
                        E.Row("接触位", E.Num("ta_contact", 0, 100), 90, "mm"),
                        E.Row("校准偏移", E.Num("ta_cal", 0, 100), 90, "mm"),
                        E.Spacer(4),
                        E.Chk("ta_vac", "真空模式（下压测试时提前打开真空）", true),
                        E.Row("落料等待", E.Num("ta_wait", 0, 100, "0"), 90, "ms"),
                        E.Row("IC尺寸(Max-X)", E.Num("ta_icx", 7, 100), 110, "mm"),
                        E.Row("IC尺寸(Max-Y)", E.Num("ta_icy", 7, 100), 110, "mm"),
                        E.Row("Socket查空@接触数", E.Num("ta_skchk", 0, 100, "0"), 130)), 0, 0)
                    .Add(E.Group("接触模式",
                        E.Radios("ta_mode", contactModes),
                        E.Spacer(8),
                        E.T("直接-接触（最常用）：", 12, E.P, true),
                        E.T("Z高位 → Y移动到小车位置 → Z吸取位 → Z高位 → Y移动到测试位置 → Z接触位(测试) → Z高位 → Y移动到小车位置 → Z放置位。", 11, E.Sec, false, TextWrapping.Wrap).Margin(0, 2, 0, 8),
                        E.T("落料-接触：", 12, E.P, true),
                        E.T("测试臂先将 IC 释放到测试座中再下压进行测试，适用于对测试接触有特殊要求的状况。", 11, E.Sec, false, TextWrapping.Wrap)), 0, 2),
                E.Group("测试座压力设定",
                    E.Row("管腿数量", E.Num("ta_pins", 32, 90, "0"), 100),
                    E.Row("每管腿力", E.Num("ta_pinforce", 12, 90, "0.###"), 100, "kgf"),
                    E.Row("接触力", E.Num("ta_force", 384, 110, "0.###", null, null, true), 100, "kgf"),
                    E.Chk("ta_bigforce", "大压力模式（选配，可支持超过 480KG 力量的测试）", false)),
                E.Group("设定方式",
                    E.T("自动校准模式：确认机器中测试臂及小车入口托盘内无料，点击进入自动校准模式，设定校准偏移值等各项参数后，点击应用按钮。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.T("在小车入料托盘内放入 IC，根据对应的使能设定在对应的位置放入，进入该模式后测试臂尝试吸取使能位置的产品。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.T("实验模式：通过自动模式调整完测试臂接触位置后进入，可验证接触高度。按下 <步进/Step> 后两个测试臂会切换压到测试座上并自动启动测试过程。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.Spacer(6),
                    E.H(
                        E.Btn("自动校准模式", () => m.Log("信息", "进入测试臂自动高度校准模式")),
                        E.MiniBtn("步进/Step", () => m.Log("信息", "测试臂切换")),
                        E.MiniBtn("测试/T.Start", () => m.Log("信息", "手动触发测试信号")),
                        E.MiniBtn("退出高度校准", () => m.Log("信息", "退出高度校准模式")))),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "测试臂参数已应用"))));
        }

        // ---------------- 测试接口 (image47) ----------------
        public static Element TestInterface()
        {
            var m = E.M;
            return E.V(
                
                E.Group("接口方式",
                    E.Radios("ti_type", new[] { "TTL", "GPIB", "RS232", "TCP" }, false),
                    E.Spacer(6),
                    E.Chk("ti_binerr", "Type 不停机@BinErr（勾选后测到 ErrBin 不停机报警）", true)),
                E.Grid().Cols("*", "24", "*")
                    .Add(E.Group("GPIB / TTL",
                        E.Row("GPIB 接口地址", E.Num("ti_gpib", 11, 90, "0", 0, 30), 120),
                        E.Spacer(6),
                        E.Row("TTL 接口类型", E.Cmb("ti_ttl", Enumerable.Range(1, 16).Select(i => "DIO Type " + i).ToArray(), 160), 120),
                        E.T("（在“维护配置”->“DIO设定”中定义，总共可定义 16 种）", 11, E.Sec, false, TextWrapping.Wrap)), 0, 0)
                    .Add(E.Group("RS232",
                        E.Row("波特率", E.Cmb("ti_baud", new[] { "9600", "19200", "38400", "57600", "115200" }, 130), 80),
                        E.Row("数据位", E.Cmb("ti_data", new[] { "7", "8" }, 130), 80),
                        E.Row("停止位", E.Cmb("ti_stop", new[] { "1", "2" }, 130), 80),
                        E.Row("校验值", E.Cmb("ti_parity", new[] { "None", "Odd", "Even" }, 130), 80)), 0, 2),
                E.Group("定时设定 (ms)",
                    E.Grid().Cols("160", "130", "40", "160", "130")
                        .Add(E.T("超时", 12.5, E.P, ta: TextAlignment.Right).Margin(0, 0, 10, 0), 0, 0)
                        .Add(E.Num("ti_timeout", 5000, 120, "0"), 0, 1)
                        .Add(E.T("ms", 12, E.Sec).Margin(8, 0, 20, 0), 0, 2)
                        .Add(E.T("开测延时", 12.5, E.P, ta: TextAlignment.Right).Margin(0, 0, 10, 0), 0, 3)
                        .Add(E.Num("ti_sotdelay", 10, 120, "0"), 0, 4)),
                E.Group("说明",
                    E.T("“测试接口”窗口中的 Bin F% 报警 “IC数量” 设定：当数量为零时，关闭对 Bin F% 的报警。", 11.5, E.Sec, false, TextWrapping.Wrap)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "测试接口设定已应用"))));
        }

        // ---------------- Bin设定 (image48~image50) ----------------
        public static Element BinSetting()
        {
            var m = E.M;
            Func<string, Element> page = tag => E.V(
                E.Group("属性",
                    E.Matrix(BinCols, new[]
                    {
                        "连续次品", "连续好品", "复测", "良率报警1", "良率报警2", "良率报警3",
                        "次品", "旋转台", "未使用"
                    }, (r, c) => E.CheckCell("bin_" + tag + "_" + r + "_" + c, r >= 6), 52, 24, E.HeadBlue, E.HeadBlue, 110, 24)),
                E.Group("阈值",
                    E.Matrix(BinCols, new[] { "Bin F%", "Bin数量", "测试座Bin%差别", "测试臂Bin%差别" },
                        (r, c) => E.Num("binv_" + tag + "_" + r + "_" + c, 0, 48, "0.###"), 52, 24, E.HeadBlue, E.HeadBlue, 130, 24)),
                E.Group("Bin颜色",
                    E.Matrix(BinCols, new[] { "颜色" },
                        (r, c) => E.Cmb("binc_" + tag + "_" + c, new[] { "绿", "红", "蓝", "黄", "灰" }, 48), 52, 26, E.HeadBlue, E.HeadBlue, 110, 24)),
                E.Group("出料（Soft Bin → 下料料盘映射）",
                    E.Matrix(BinCols, new[] { "自动盘1", "自动盘2", "自动盘3", "固定盘1", "固定盘2", "固定盘3" },
                        (r, c) => E.Cmb("bint_" + tag + "_" + r + "_" + c, BinCols, 52), 52, 24, E.HeadBlue, E.HeadBlue, 90, 24)),
                E.H(
                    E.T("Pass Bin", 12.5, E.P).Margin(0, 0, 8, 0), E.Cmb("bin_pass", new[] { "NO", "NT", "0", "1", "2" }, 100),
                    E.SpacerW(20),
                    E.T("Open Bin", 12.5, E.P).Margin(0, 0, 8, 0), E.Cmb("bin_open", new[] { "NO", "NT", "0", "1", "2" }, 100),
                    E.SpacerW(20),
                    E.Chk("bin_retest", "重测(Double Contact)", true)),
                E.H(
                    E.Btn("Bin Yield Ctrl", () => m.Log("信息", "打开 Bin 良率卡控窗口")),
                    E.SpacerW(30),
                    E.MiniBtn("恢复"),
                    E.Btn("应用", () => m.Log("信息", "Bin 设定已应用"))));

            var yieldCtrl = E.V(
                E.T("Bin 良率卡控：根据产品的要求设定需要的良率卡控。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(8),
                E.Matrix(new[] { "限定值%", "IC数量", "统计范围" }, Enumerable.Range(1, 8).Select(i => "Bin" + i).ToArray(),
                    (r, c) => c == 2
                        ? E.Cmb("byc_scope_" + r, new[] { "测试臂单个位置", "单个测试臂总体", "测试座单个位置", "测试座总体" }, 140)
                        : E.Num("byc_" + r + "_" + c, 0, 80, "0.###"), 150, 26, E.HeadBlue, E.HeadBlue, 80, 26),
                E.H(E.Btn("应用", () => m.Log("信息", "Bin 良率卡控已应用"))));

            return E.V(
                
                E.Tabs(
                    ("Online", () => page("on")),
                    ("Online-Retest", () => page("onr")),
                    ("Offline", () => page("off")),
                    ("Bin良率卡控", () => yieldCtrl)),
                E.Group("说明",
                    E.T("Bin NT 为 No Test（未测试）；Bin NG 为 No Good（视觉系统检测 Fail）；Bin ERR 为错误 Bin（测试系统传送过来的测试结果为无效分类，Not in Use 未用属性为真时）。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.T("设定方法：直接用鼠标在网格内点击即可设定，点击每行第二列可对整行进行设定（Fix1~3 右侧小方格）。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---------------- 温度 (image51 / image52) ----------------
        public static Element TempRecipe()
        {
            var m = E.M;
            var hot = E.V(
                E.Group("高温设定",
                    E.Row("加热时间", E.Num("tr_h_time", 60, 100, "0"), 130, "s"),
                    E.Row("卡料加热时间", E.Num("tr_h_jam", 0, 100, "0"), 130, "s"),
                    E.Row("初始等待时间", E.Num("tr_h_initwait", 0, 100, "0"), 130, "s"),
                    E.Row("初始超时", E.Num("tr_h_initto", 0, 100, "0"), 130, "s"),
                    E.Row("超限超时", E.Num("tr_h_overto", 0, 100, "0"), 130, "s"),
                    E.Row("下次报警延时", E.Num("tr_h_nextalarm", 0, 100, "0"), 130, "s"),
                    E.Row("温度范围", E.H(E.Num("tr_h_tmin", 45, 80, "0"), E.T("~", 12, E.Sec).Margin(8, 0, 8, 0), E.Num("tr_h_tmax", 55, 80, "0")), 130, "°C")),
                E.Group("开关选项",
                    E.Chk("tr_h_fan", "离子风机自动打开/关闭", true),
                    E.Chk("tr_h_heater", "测试位加热器打开", true),
                    E.Chk("tr_h_blow", "热风吹气"),
                    E.Chk("tr_h_skipdis", "使能关闭停止通道"),
                    E.Chk("tr_h_preheat", "上料臂卡料返回预热盘", true),
                    E.Chk("tr_h_idle_to_ch", "超时报警—通道停止"),
                    E.Chk("tr_h_all_to", "超时报警—断开所有"),
                    E.Chk("tr_h_blow_to", "超时报警—（热风）吹气")),
                E.Group("接触时间",
                    E.Row("接触空闲", E.Num("tr_h_idle", 0, 100, "0"), 110, "ms"),
                    E.Row("接触加热", E.Num("tr_h_heat", 0, 100, "0"), 110, "ms")),
                E.Group("高温偏移值设定",
                    E.Radios("tr_pt", new[] { "1点", "2点", "3点" }, false, 2),
                    E.Spacer(8),
                    E.Grid().Cols("120", "110", "120", "110", "120", "110")
                        .Add(E.T("基点#1", 12.5, E.P, ta: TextAlignment.Right).Margin(0, 0, 10, 0), 0, 0)
                        .Add(E.Num("tr_base1", 25, 100, "0"), 0, 1)
                        .Add(E.T("基点#2", 12.5, E.P, ta: TextAlignment.Right).Margin(0, 0, 10, 0), 0, 2)
                        .Add(E.Num("tr_base2", 60, 100, "0"), 0, 3)
                        .Add(E.T("基点#3", 12.5, E.P, ta: TextAlignment.Right).Margin(0, 0, 10, 0), 0, 4)
                        .Add(E.Num("tr_base3", 95, 100, "0"), 0, 5),
                    E.Spacer(6),
                    E.Matrix(new[] { "预温盘#1", "预温盘#2", "小车A", "小车B", "测试板1", "测试板2" },
                        new[] { "偏移值" },
                        (r, c) => E.Num("tr_off_" + c, 0, 100, "0.###"), 110, 26, E.HeadBlue, E.HeadBlue, 90, 24),
                    E.Row("高温设定", E.Num("tr_hotset", 95, 100, "0"), 130, "°C"),
                    E.Row("Group", E.Text("tr_group", "Group1", 160), 130)));

            var room = E.V(
                E.Group("常温设定",
                    E.Chk("tr_r_check", "常温检查（常温模式下检查温度是否到达设定值以下）", true),
                    E.Row("温度范围", E.H(E.Num("tr_r_tmin", 20, 80, "0"), E.T("~", 12, E.Sec).Margin(8, 0, 8, 0), E.Num("tr_r_tmax", 30, 80, "0")), 130, "°C"),
                    E.Row("初始超时", E.Num("tr_r_initto", 0, 100, "0"), 130, "s"),
                    E.Row("超限超时", E.Num("tr_r_overto", 0, 100, "0"), 130, "s"),
                    E.Row("下次报警延时", E.Num("tr_r_nextalarm", 0, 100, "0"), 130, "s"),
                    E.Chk("tr_r_blow", "（热风）吹气（高温切换为常温时按设定时间持续吹气）"),
                    E.Spacer(6),
                    E.Row("吹气时间", E.Num("tr_r_blowtime", 0, 100, "0"), 130, "s")),
                E.Group("说明",
                    E.T("该窗口只有在设备安装了加热控制之后才会出现。该模块设定品种相关的加热条件及工作模式：常温模式 / 高温模式 / 低温模式。", 11.5, E.Sec, false, TextWrapping.Wrap)));

            return E.V(
                E.T("温度设定", 13, E.P, true),
                E.Spacer(8),
                E.Group("温度模式", E.Radios("tr_mode", new[] { "常温模式", "高温模式", "低温模式" }, false)),
                E.Tabs(("高温", () => hot), ("室温", () => room)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "温度设定已应用"))));
        }

        // ---------------- 视觉 (image53 / image54) ----------------
        public static Element VisionPage()
        {
            var m = E.M;
            var func = E.V(
                E.Group("2D 功能",
                    E.Chk("vs_2d", "2D（勾选后 2D 功能生效）", true),
                    E.Chk("vs_err", "误码报警", true),
                    E.Chk("vs_dup", "重码检查", true),
                    E.Row("重试次数", E.Num("vs_retry", 2, 90, "0"), 90),
                    E.Row("最小码长", E.Num("vs_minlen", 4, 90, "0"), 90),
                    E.Row("最大码长", E.Num("vs_maxlen", 64, 90, "0"), 90),
                    E.Row("误码标识", E.Text("vs_errtag", "ERROR", 130), 90),
                    E.Row("误码BIN", E.Cmb("vs_errbin", new[] { "NT", "NG", "ERR", "0", "1", "2" }, 130), 90),
                    E.Spacer(6),
                    E.Row("通讯方式", E.Cmb("vs_comm", new[] { "TCP", "RS232" }, 130), 90),
                    E.Row("串口号", E.Cmb("vs_port", new[] { "COM1", "COM2", "COM3", "COM4" }, 130), 90),
                    E.Row("串口波特率", E.Cmb("vs_baud", new[] { "9600", "19200", "38400", "115200" }, 130), 90),
                    E.Row("TCP读码器IP", E.Text("vs_ip", "192.168.0.10", 160), 100),
                    E.Chk("vs_reread", "重读全部（重读小车内的所有芯片）")),
                E.Group("说明",
                    E.T("2D：勾选后 2D 功能生效；误码报警：当收到的结果为 ERROR 时弹出报警提示；重码检查：当收到的结果重复时弹出报警提示。", 11.5, E.Sec, false, TextWrapping.Wrap)));

            var bottom = E.V(
                E.Group("底部视觉",
                    E.Chk("bv_enable", "视觉检查功能打开", true),
                    E.Chk("bv_multi", "多拍（每颗 IC 根据设定进行多次不同位置的拍照）"),
                    E.Chk("bv_random", "随机结果（非正常模式）"),
                    E.Row("随机良率", E.Num("bv_yield", 100, 90, "0"), 90, "%"),
                    E.Row("随机时间", E.Num("bv_time", 300, 90, "0"), 90, "ms"),
                    E.Row("NG重试", E.Num("bv_ngretry", 0, 90, "0"), 90),
                    E.Chk("bv_tip", "提示（每收到一次 NG 结果就弹出提示）"),
                    E.Chk("bv_ngfirst", "分料NGbin优先", true),
                    E.Row("检测超时", E.Num("bv_timeout", 2000, 90, "0"), 90, "ms")),
                E.Group("报警设定",
                    E.Chk("bv_socktotal", "Alarm By Sockets Total"),
                    E.Row("次数", E.Num("bv_socktotal_n", 0, 90, "0"), 90),
                    E.Chk("bv_sockconti", "Alarm By Sockets Conti."),
                    E.Row("次数", E.Num("bv_sockconti_n", 0, 90, "0"), 90),
                    E.Chk("bv_sitetotal", "Alarm By Site Total"),
                    E.Row("次数", E.Num("bv_sitetotal_n", 0, 90, "0"), 90),
                    E.Chk("bv_siteconti", "Alarm By Site Conti."),
                    E.Row("次数", E.Num("bv_siteconti_n", 0, 90, "0"), 90),
                    E.Chk("bv_autoreset", "自动重置报警计数器", true)),
                E.Group("调试",
                    E.T("可通过复选框选择对应的吸头，点击“检测”按钮，吸头就会移动到拍照位。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.Spacer(6),
                    E.LampLine("吸头", 8, true, false, "bv_head"),
                    E.Spacer(6),
                    E.H(E.Btn("检测", () => m.Log("信息", "吸头移动到拍照位")))));

            return E.V(
                E.T("视觉设定", 13, E.P, true),
                E.Spacer(8),
                E.Tabs(("功能配置", () => func), ("Bottom Vision", () => bottom)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "视觉设定已应用"))));
        }

        // ---------------- 清洁盘 (image55 / image56 / image57) ----------------
        public static Element CleanPage()
        {
            var m = E.M;
            var main = E.V(
                E.Group("测试座清洁",
                    E.Chk("cp_enable", "测试座清洁（需要清洁功能时勾选）", true),
                    E.Spacer(4),
                    E.T("条件：清洁功能的触发条件", 12, E.P, true),
                    E.Chk("cp_init", "初始开始", true),
                    E.Chk("cp_retest", "复测初始开始"),
                    E.Chk("cp_end", "结束（执行出盘时触发）"),
                    E.Chk("cp_manual", "手动清洁"),
                    E.Chk("cp_alarm", "测试座报警", true),
                    E.Row("间隔", E.Num("cp_interval", 1000, 100, "0"), 90, "次"),
                    E.Row("计数器", E.Num("cp_counter", 0, 100, "0", null, null, true), 90, "次")),
                E.Group("清洁片",
                    E.Row("清洁片数量", E.Num("cp_qty", 500, 100, "0"), 110),
                    E.Row("厚度差别", E.Num("cp_thick", 0.2, 100, "0.###"), 110, "mm"),
                    E.Row("报警", E.Num("cp_alarmn", 500, 100, "0"), 110, "次"),
                    E.Row("计数器", E.Num("cp_usecnt", 0, 100, "0", null, null, true), 110, "次")),
                E.Group("接触参数",
                    E.Radios("cp_mode", new[] { "直接接触", "落料模式" }, false),
                    E.Row("接触时间", E.Num("cp_ctime", 200, 100, "0"), 110, "ms"),
                    E.Row("接触次数", E.Num("cp_cnt", 1, 100, "0"), 110),
                    E.Row("落料高度", E.Num("cp_droph", 0, 100, "0"), 110, "mm")),
                E.Group("测试臂高度调整",
                    E.Row("小车释放", E.Num("cp_z_rel", 0, 100, "0.###"), 110, "mm"),
                    E.Row("小车吸取", E.Num("cp_z_pick", 0, 100, "0.###"), 110, "mm"),
                    E.Row("测试座接触", E.Num("cp_z_cont", 0, 100, "0.###"), 110, "mm"),
                    E.Row("速度", E.Num("cp_speed", 100, 100, "0"), 110, "%"),
                    E.Row("测试座查空", E.Cmb("cp_chk", new[] { "清洁前", "清洁后", "不检查" }, 130), 110)));

            var offset = E.V(
                E.T("可通过此界面调整清洁盘功能的单独偏移参数。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(8),
                E.H(
                    E.MachineMap(),
                    E.SpacerW(16),
                    E.ArmHead("清洁盘偏移",
                        E.Row("X", E.Num("cpo_x", 0, 90, "0.###"), 70, "mm"),
                        E.Row("Y", E.Num("cpo_y", 0, 90, "0.###"), 70, "mm"),
                        E.Row("放料", E.Num("cpo_place", -2, 90, "0.###"), 70, "mm"),
                        E.Row("取料", E.Num("cpo_pick", 0, 90, "0.###"), 70, "mm"),
                        E.Row("吸头间距", E.Num("cpo_pitch", 0, 90, "0.###"), 70, "mm"))),
                E.H(E.MiniBtn("默认", () => m.Log("信息", "已恢复清洁盘默认偏移")),
                    E.MiniBtn("恢复"),
                    E.Btn("应用", () => m.Log("信息", "清洁盘偏移已应用"))));

            var tray = E.V(
                E.T("可以设定所用清洁盘的料盘信息。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(8),
                E.Row("名称", E.Text("cpt_name", "CleanPlate", 200), 70),
                E.Spacer(6),
                E.Grid().Cols("150", "150", "150", "150")
                    .Add(E.Row("X起点", E.Num("cpt_x0", 0, 80), 60), 0, 0)
                    .Add(E.Row("X间距", E.Num("cpt_dx", 15, 80), 60), 0, 1)
                    .Add(E.Row("X数量", E.Num("cpt_nx", 8, 80, "0"), 60), 0, 2)
                    .Add(E.Row("Y起点", E.Num("cpt_y0", 0, 80), 60), 0, 3)
                    .Add(E.Row("Y间距", E.Num("cpt_dy", 15, 80), 60), 1, 0)
                    .Add(E.Row("Y数量", E.Num("cpt_ny", 4, 80, "0"), 60), 1, 1),
                E.Spacer(8),
                E.H(E.TrayDiagram(8, 4, 30), E.SpacerW(16), E.PadGrid(8, 4)));

            var qa = E.V(
                E.Chk("qa_enable", "QA取样", true),
                E.Row("数量", E.Num("qa_qty", 10, 100, "0"), 90),
                E.Row("取样盘", E.Cmb("qa_tray", new[] { "Fix1", "Fix2", "Fix3", "Auto1", "Auto2", "Auto3" }, 130), 90),
                E.T("取到设定数量后，不再取 IC，将所有 IC 测试完成后，放到设定料盘，退盘，弹出报警提示送检。", 11.5, E.Sec, false, TextWrapping.Wrap).Margin(0, 8, 0, 0));

            return E.V(
                E.T("清洁盘设定", 13, E.P, true),
                E.Spacer(8),
                E.Tabs(("清洁盘", () => main), ("偏移调整", () => offset), ("Tray", () => tray), ("QA取样", () => qa)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "清洁盘设定已应用"))));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
