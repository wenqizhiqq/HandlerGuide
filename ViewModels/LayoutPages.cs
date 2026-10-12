// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  ViewModels/LayoutPages.cs — 三温标准机「模块平面仿真」页
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：把用户提供的整机平面图（11 个模组）画成一张实时动画画布：
//        · 上料模组 / 下料模组 / 手工盘模组 / 压测模组 / 料枪模组 /
//          加热盘模组 / 移盘模组 / 上料流水线 / 黑盘流水线 / 彩盘流水线 /
//          下料流水线
//        每个模组内部都有会动的零件（升降、流水、穿梭、下压、伸缩、加热）。
//        动作分两类来源：
//          (1) 真实轴位置 —— 上/下料臂 X、小车 A/B、测试下压 Z、上料臂 Y；
//          (2) 时间驱动 —— 流水线走料、料盘升降、手工盘扫描、加热脉冲。
//        打开本页会自动启动「安全演示节拍」，每一步都过同一套互锁，不会撞机。
//        本页现在**只显示「整机俯视图」**：2D平面图 / 整机俯视图 / 3D整机图 这套页签选择条
//        已按用户要求整个撤掉（先删 3D，再把选择器也去掉），打开本页直接就是俯视图。
//        画布上的**所有文字**也已按用户要求清掉 —— 分组标签、手臂标注框、行程文字、
//        署名水印全部撤除。现在画布上只剩「真机照片 + 料盘 / 加热盘 / 两根手臂」的
//        图形与动画，一句字都没有；所有文字说明统一收到右侧说明栏（见 LayoutPage()）。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LayoutPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_LayoutPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · LayoutPages。</summary>
        internal const string WenQiZhiFile = "LayoutPages";
    }

    /// <summary>整机模块平面仿真的声明式构建（温启志 18719361399）。</summary>
    public static class LayoutPages
    {
        /// <summary>
        /// 自检用：最近一次构建出来的平面仿真画布。
        /// 生产路径完全不读这个字段，只是让 App 的自检能拿到画布上的包含性守卫
        /// （见 <see cref="CanvasElement.CheckGuards"/>）——料盘循环流动，
        /// 必须在多个相位上校验才抓得住越界。
        /// </summary>
        internal static CanvasElement LastLayoutCanvas;

        // ==================================================================
        //  配色（只用于图形，不参与全局调色板）
        // ==================================================================

        /// <summary>机台外框填充。</summary>
        private static readonly Brush BodyFill = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEF));

        /// <summary>机台外框描边。</summary>
        private static readonly Brush BodyStroke = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xC0));

        /// <summary>模组底色。</summary>
        private static readonly Brush ModFill = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xFA));

        /// <summary>模组描边。</summary>
        private static readonly Brush ModStroke = new SolidColorBrush(Color.FromRgb(0xCB, 0xCB, 0xD2));

        /// <summary>流水线槽底。</summary>
        private static readonly Brush LaneFill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xE9));

        /// <summary>白盘。</summary>
        private static readonly Brush TrayWhite = new SolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xEE));

        /// <summary>黑盘。</summary>
        private static readonly Brush TrayBlack = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x4C));

        /// <summary>
        /// 彩盘。比黑盘偏蓝一档，用来区分「彩盘流水线」。
        /// 原图里四条流水线的料盘其实都是深色的，所以这里只做「偏蓝」而不是饱和蓝，
        /// 否则整页会被这一列抢掉视觉重心。
        /// </summary>
        private static readonly Brush TrayColor = new SolidColorBrush(Color.FromRgb(0x3A, 0x6B, 0xA8));

        /// <summary>红（气缸 / 报警 / 加热）。</summary>
        private static readonly Brush Red = new SolidColorBrush(Color.FromRgb(0xC0, 0x20, 0x2A));

        /// <summary>橙（下料臂）。</summary>
        private static readonly Brush Orange = new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x1E));

        /// <summary>蓝（上料臂）。</summary>
        private static readonly Brush Blue = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xD6));

        /// <summary>绿（小车 A）。</summary>
        private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x5B));

        /// <summary>深绿（小车 B）。</summary>
        private static readonly Brush GreenDark = new SolidColorBrush(Color.FromRgb(0x1B, 0x6E, 0x40));

        /// <summary>紫（Z 轴 / 压头）。</summary>
        private static readonly Brush Purple = new SolidColorBrush(Color.FromRgb(0x8F, 0x6A, 0xD6));

        /// <summary>钢灰（料枪 / 机械件）。</summary>
        private static readonly Brush Steel = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x93));

        /// <summary>导轨底色。</summary>
        private static readonly Brush RailB = new SolidColorBrush(Color.FromRgb(0xD5, 0xD5, 0xDA));

        /// <summary>热流色（半透明）。</summary>
        private static readonly Brush HeatHaze = new SolidColorBrush(Color.FromArgb(0x50, 0xE0, 0x5A, 0x1E));

        /// <summary>冷盘色。</summary>
        private static readonly Brush Cool = new SolidColorBrush(Color.FromRgb(0x5F, 0x9E, 0xC8));

        /// <summary>深导轨色（原图左右两侧那两条蓝竖条）。</summary>
        private static readonly Brush RailDeep = new SolidColorBrush(Color.FromRgb(0x2B, 0x4C, 0x8A));

        /// <summary>青绿（滑台 / 工装）。</summary>
        private static readonly Brush Teal = new SolidColorBrush(Color.FromRgb(0x1F, 0x8E, 0x8E));

        /// <summary>青绿（暗，手工盘未点亮的格位）。</summary>
        private static readonly Brush TealDim = new SolidColorBrush(Color.FromRgb(0x8F, 0xC4, 0xC0));

        /// <summary>黑盘（原图里那些深色料盘）。</summary>
        private static readonly Brush TrayDark = new SolidColorBrush(Color.FromRgb(0x27, 0x2E, 0x3E));

        /// <summary>料盘描边。</summary>
        private static readonly Brush TrayEdge = new SolidColorBrush(Color.FromRgb(0x6E, 0x76, 0x88));

        /// <summary>料盘格纹（暗盘上）。</summary>
        private static readonly Brush TrayLine = new SolidColorBrush(Color.FromRgb(0x55, 0x5F, 0x74));

        /// <summary>料盘格纹（亮盘上）。</summary>
        private static readonly Brush TrayLineOn = new SolidColorBrush(Color.FromRgb(0xD8, 0xE6, 0xF6));

        /// <summary>
        /// 料盘缓存被取放时的「点亮」底色。原图里缓存位全是深色料盘，
        /// 所以这里刻意用暗钢蓝而不是饱和蓝 —— 既有动感又不会把整页颜色带跑。
        /// </summary>
        private static readonly Brush CacheLit = new SolidColorBrush(Color.FromRgb(0x3F, 0x6B, 0xA8));

        /// <summary>文字主色。</summary>
        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1D, 0x1D, 0x1F));

        /// <summary>文字次色。</summary>
        private static readonly Brush Ink2 = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x73));

        /// <summary>
        /// 真机料盘平面布局（温启志 18719361399）。
        /// 数据来源：用户提供的「满盘俯视图」逐像素量测后，按机台外框归一化到 0..1。
        /// 每一项 = (左 u, 上 v, 宽 du, 高 dv, 是否绿色盘)，一共 14 个：
        ///     · 上排 6 个 —— 深色格纹盘（u 0.146..0.785, v 0.048..0.263）
        ///     · 中排 5 个 —— 深色格纹盘 / 料盘缓存位（v 0.357..0.569）
        ///     · 左下 3 个 —— 绿色格纹盘 / 出料盘（v 0.639..0.833）
        /// 「整机俯视图」画布直接乘画布尺寸落点。
        /// </summary>
        /// <remarks>
        /// 注意：这里是 <c>internal</c> 而不是 private —— 量测数据留作公开接口，
        /// 方便以后新增视图（例如再版三维）复用同一份料盘坐标，保证各视图永远一致。
        /// </remarks>
        internal static readonly (double U, double V, double DU, double DV, bool Green)[] TrayPlan =
        {
            // 上排 6 个：深色格纹盘
            (0.14649, 0.04820, 0.08380, 0.21499, false),
            (0.24705, 0.04820, 0.08380, 0.21499, false),
            (0.34947, 0.04820, 0.08132, 0.21499, false),
            (0.46182, 0.04820, 0.08132, 0.21499, false),
            (0.58100, 0.04820, 0.08256, 0.21499, false),
            (0.70329, 0.04820, 0.08194, 0.21499, false),
            // 中排 5 个：深色格纹盘（料盘缓存位）
            (0.15021, 0.35654, 0.07945, 0.21270, false),
            (0.24953, 0.35654, 0.08070, 0.21270, false),
            (0.35009, 0.35654, 0.08070, 0.21270, false),
            (0.46182, 0.35654, 0.08007, 0.21270, false),
            (0.58286, 0.35654, 0.07821, 0.21270, false),
            // 左下 3 个：绿色格纹盘（出料盘）
            (0.13718, 0.63887, 0.07635, 0.19434, true),
            (0.21353, 0.63887, 0.07697, 0.19434, true),
            (0.29050, 0.63887, 0.07697, 0.19434, true),
        };

        // ==================================================================
        //  页面
        // ==================================================================

        /// <summary>整机模块平面仿真页（motion_layout）。</summary>
        public static Element LayoutPage()
        {
            // 本页**只显示「整机俯视图」**：页签选择条（2D平面图 / 整机俯视图）
            // 已按用户要求整个撤掉 —— 打开本页直接就是俯视图，不用再选。
            //
            // 俯视图以 image\handler_top.jpg（空机台照片）为底图，按真机量测坐标现画
            // 14 个料盘（上排 6 深盘 / 中排 5 深盘 / 左下 3 绿盘）+ 2 个加热盘，
            // 并叠上「下料X手臂」「上料X轴」两根手臂（红色 2×4 吸嘴组 + XY 走位）做取放动画。
            //
            // 2D 平面工程图（BuildLayoutCanvas()，11 模组 540×438）现在**没有入口**了，
            // 代码原样保留着。哪天要放回来，把下面这行换成：
            //     Func<Element> f2d  = () => BuildLayoutCanvas();
            //     Func<Element> fTop = () => BuildLayoutCanvasTop();
            //     var topCanvas = E.Tabs(1, ("2D平面图", f2d), ("整机俯视图", fTop));
            // 即可。注意 lambda 必须显式写成 Func<Element>，否则编译器会把公共类型
            // 推成 CanvasElement，返回 Element 的 lambda 转不过去（CS1503）。
            // 另外「3D整机图」页签与 ViewModels/Layout3D.cs 已删除，
            // 要找回那份三维代码：git show 68ec276:ViewModels/Layout3D.cs
            var topCanvas = BuildLayoutCanvasTop();

            return E.V(
                // ---- 主画布 + 右侧说明栏 ----
                // 两列网格：左列 * 吃满剩余宽度放整机俯视图画布，右列固定 490。
                // 俯视图以 image\handler_top.jpg（空机台）为底图，按真机量测坐标
                // 现画 14 个料盘（上排 6 深盘 / 中排 5 深盘 / 左下 3 绿盘）并做取放动画；
                // 画布上还叠了上/下料两根 X 轴手臂（红色 2×4 吸嘴组 + XY 走位）。
                // 右侧命令中心 / 图例 / 模组动作对照与画布并排。
                E.Grid()
                    .Cols("*", "490")
                    .Rows("Auto")
                    .Add(topCanvas, 0, 0)
                    .Add(
                        E.V(
                            // ---- 本页标题行 ----
                            // 原来这行在页顶左侧，和外壳的「页面名 + 页面描述」重复了一遍。
                            // 按用户要求挪到「最右边」= 右栏（固定 490）顶部，排在仿真节拍控制之上。
                            // 用 Wrap 而不是 H：右栏可用宽 = 490 − 栏左边距 12 = 478，
                            // 标题 14.5px + 间距 14 + 副标题 11px 约 451px，一行放得下；
                            // 万一字体度量比估算宽，Wrap 会自己折到第二行，不会像 H 那样被裁掉。
                            E.Wrap(
                                E.T("三温标准机 · 模块平面仿真", 14.5, E.P, true),
                                E.SpacerW(14),
                                E.T("（11 个模组 · 动作随运动控制卡实时变化）", 11, E.Sec)),
                            E.Spacer(6),

                            // ---- 仿真节拍控制 ----
                            // 原来这条是页顶的**通栏**操作条（7 个按钮一行排开）。按用户要求
                            // 挪到「仿真图最右侧」= 右栏顶部，并**分 2 行**显示：
                            // 本栏固定 490 宽，减去栏左边距 12 和 Band 内边距 14×2 只剩约 450，
                            // 7 个按钮一行要 ~570px 塞不下，所以按语义拆成
                            //   第 1 行 = 节拍控制（启动 / 停止）+ 急停 / 急停复位
                            //   第 2 行 = 整机动作（全部回零 / 全部停止 / 清除报警）
                            // 两行实测各约 336px / 252px，都在 450 之内。
                            E.Band("仿真节拍控制", Palette.BandBlue,
                                E.V(
                                    E.H(
                                        E.Btn("启动演示节拍", () => MotionManager.Instance.StartDemo(), "PrimaryButton", 12),
                                        E.Btn("停止演示", () => MotionManager.Instance.StopDemo(), "PrimaryButton", 12),
                                        E.Btn("急停", () => MotionManager.Instance.TriggerEStop(), "DangerButton", 12),
                                        E.Btn("急停复位", () => MotionManager.Instance.ResetEStop(), "PrimaryButton", 12)),
                                    E.Spacer(6),
                                    E.H(
                                        E.Btn("全部回零", () => MotionManager.Instance.HomeAll(), "PrimaryButton", 12),
                                        E.Btn("全部停止", () => MotionManager.Instance.StopAll(), "PrimaryButton", 12),
                                        E.Btn("清除报警", () => MotionManager.Instance.ClearAlarm(), "PrimaryButton", 12))),
                                E.Spacer(4),
                                E.T("打开本页即自动演示节拍；点「停止演示」回到静态。"
                                    + "所有动作经 SafetyCheck 互锁裁决，越界指令会被拒绝，不会撞机。",
                                    10, E.Sec, false, TextWrapping.Wrap)),
                            E.Spacer(8),

                            // ---- 命令中心（生产控制 + 批次管理）从主界面搬到本仿真页右侧 ----
                            HomePages.CommandCenter(),
                            E.Spacer(6),
                            E.T("图例", 12, E.P, true),
                            BuildLegend(),
                            E.Spacer(6),
                            E.T("模组动作对照（每个方框内都能看到动的零件）", 12, E.P, true),
                            BuildModuleTable()).Margin(12, 0, 0, 0),
                        0, 1),

                E.T("温启志 18719361399 · 平面仿真页", 10.5, E.Sec));
        }

        /// <summary>图例：颜色 → 含义（用 Box 画小色块，画布外也能显示）。</summary>
        private static Element BuildLegend()
        {
            object Dot(Brush b, string label)
                => E.H(
                    E.Box(null, b, ModStroke, 0.5, 3, default(Thickness), default(Thickness), 13, 13),
                    E.SpacerW(4),
                    E.T(label, 10.5, E.Sec),
                    E.SpacerW(12));

            return E.Wrap(
                Dot(Blue, "上料臂"),
                Dot(Orange, "下料臂"),
                Dot(Green, "小车 A"),
                Dot(GreenDark, "小车 B"),
                Dot(Purple, "Z 轴 / 压头"),
                Dot(Red, "气缸 / 加热 / 报警"),
                Dot(Steel, "料枪 / 机械件"),
                Dot(TrayBlack, "黑盘"),
                Dot(TrayColor, "彩盘"),
                Dot(TrayWhite, "白盘"));
        }

        /// <summary>模组动作说明表。</summary>
        private static Element BuildModuleTable()
        {
            var rows = new (string Mod, string Driver, string Motion)[]
            {
                ("下料模组",     "料盘传送臂（轴 30）",           "左立柱滑台在导轨上走位，到底后分盘气缸顶出（变红）"),
                ("上料模组",     "料盘传送臂（轴 30）",           "右立柱滑台反向走位，到底后顶升气缸伸出（变红）"),
                ("下料流水线",   "时间驱动（走料节拍）",           "黑盘沿导轨向下流出，格纹随之滚动"),
                ("彩盘流水线",   "时间驱动（走料节拍）",           "彩盘沿导轨向下流动"),
                ("黑盘流水线",   "时间驱动（走料节拍）",           "黑盘沿导轨向下流动"),
                ("上料流水线",   "时间驱动（走料节拍）",           "黑盘沿导轨向上进料"),
                ("移盘模组",     "小车 A / B（轴 28 / 29）",       "横梁上的移盘小车左右穿梭，到位后竖直下压机构伸出取放"),
                ("料盘缓存",     "演示步骤联动",                   "中排 5 个料盘位按节拍逐个点亮，表示移盘取放位置"),
                ("手工盘模组",   "时间驱动（扫描节拍）",           "3×3 手工盘格位逐格点亮，表示人工放料位置"),
                ("压测模组",     "测试位下压 Z1（轴 2）",          "压头按真实 Z 位置下压 / 抬起，压到位后变红保持"),
                ("料枪模组",     "上料取放臂 Y 轴（轴 5）",        "料枪滑座与枪管随轴位置伸缩，伸出量实时显示"),
                ("加热盘模组",   "加热使能输出（OUT15）",          "5 层加热盘通电后变红并冒热流；未使能时为冷盘青色"),
                ("下导轨搬送",   "时间驱动",                       "下排搬送小车沿导轨往返，到位后气缸动作")
            };

            var list = new List<object>();
            foreach (var r in rows)
            {
                list.Add(E.H(
                    E.T(r.Mod, 10.5, E.P, true).Wd(72),
                    E.T(r.Driver, 10, E.Sec).Wd(146),
                    E.T(r.Motion, 10, E.Sec, false, TextWrapping.Wrap).Wd(250)));
            }
            return E.V(list.ToArray());
        }

        // ==================================================================
        //  平面图画布（逐帧重绘）
        // ==================================================================

        /// <summary>构建整机平面仿真画布，并把逐帧重绘逻辑挂到 CanvasElement.Draw 上。</summary>
        /// <remarks>
        /// 坐标全部按用户提供的整机图（540×438 像素）里的原始像素写，
        /// 再用 SX/SY/SW 换算到画布。这样以后把画布截图和原图并排一放，
        /// 就能一眼看出哪个模组画偏了 —— 比手算缩放系数可靠得多。
        /// </remarks>
        private static CanvasElement BuildLayoutCanvas()
        {
            // ---- 原图 540×438 → 画布 1188×964（K=2.2，左侧尽量放大）----
            // 机台外框（原图 66..472 × 62..396）换算后落在 70..882 × 30..698，
            // 四周各留一点边距放读数条与标尺。
            const double K = 2.2;      // 缩放（原图 540×438 → 画布 1188×964，左侧尽量放大）
            const double OX = -68.0;   // 水平偏移（按 K 同比）
            const double OY = -103.0;  // 垂直偏移（按 K 同比）
            const double CW = 1188;
            const double CH = 964;

            // ---- 模组几何（原图像素） ----
            // 下面这组数字不是拍脑袋来的：都是把原图 machine.jpg(540×438) 放大 3 倍逐段量出来的。
            //   上排四条流水线（含机台顶沿）  y 62 .. 152   → 每条是一摞 6 片薄料盘
            //   移盘横梁（细导轨 + 穿梭小车） y 154 .. 168
            //   料盘缓存（5 根高料盘柱）      y 170 .. 256
            //   下导轨（手工盘/料枪搬送）      y 260 .. 272
            //   下排 手工盘/压测/料枪/加热盘    y 258 .. 382
            //   机台外框                      y 62 .. 392
            // 左立柱
            const double M1X = 74, M1Y = 110, M1W = 38, M1H = 286;
            // 上排四条流水线
            const double L1X = 110, L1Y = 64, L1W = 50, L1H = 88;
            const double L2X = 177, L2W = 49;
            const double L3X = 246, L3W = 50;
            const double L4X = 318, L4W = 52;
            // 移盘横梁（细，只有导轨 + 小车，没有外框 —— 原图里它就是一根横梁）
            const double M6X = 108, M6Y = 154, M6W = 317, M6H = 14;
            // 料盘缓存（中排 5 根高料盘柱）
            const double M7X = 118, M7Y = 170, M7W = 218, M7H = 86;
            const int CacheN = 5;
            // 下导轨（手工盘 / 料枪 转移）
            const double LRX = 110, LRY = 260, LRW = 235, LRH = 12;
            // 下排
            const double M8X = 110, M8Y = 278, M8W = 96, M8H = 78;    // 手工盘
            const double M9X = 210, M9Y = 274, M9W = 50, M9H = 86;    // 压测
            const double MAX = 264, MAY = 270, MAW = 66, MAH = 112;   // 料枪
            const double MBX = 334, MBY = 258, MBW = 80, MBH = 124;   // 加热盘
            // 右立柱
            const double M12X = 424, M12Y = 88, M12W = 46, M12H = 308;

            // ---- 机台外框 ----
            const double FrX = 66, FrY = 62, FrW = 406, FrH = 334;

            // 原图 → 画布
            double SX(double px) => px * K + OX;
            double SY(double py) => py * K + OY;
            double SW(double v) => v * K;

            var cv = new CanvasElement { CanvasWidth = CW, CanvasHeight = CH };

            // 自检用：把画布暴露出去，让 App 的自检能跑 CheckGuards()。
            // 生产路径不使用这个字段。
            LastLayoutCanvas = cv;

            // ---- 自检用「包含性守卫」 ------------------------------------
            // 把「必须落在某个箱体里」的图形登记进来。坐标比较统一回到「原图像素」域
            // （图形当前的 X/Y 已经是画布坐标，要用 (X-OX)/K 反算回去）。
            // 这是一层安全网：料盘是循环流动的，很容易在某个相位冲出箱体，
            // 光靠看截图不一定抓得住（前两版就各漏了一处：料盘底部、手工盘最后一行）。
            void Guard(string name, double bx, double by, double bw, double bh, List<RectItem> items)
            {
                cv.Guards.Add(() =>
                {
                    foreach (var it in items)
                    {
                        double ix = (it.X - OX) / K, iy = (it.Y - OY) / K;
                        double iw = it.W / K, ih = it.H / K;
                        double over = Math.Max(Math.Max(bx - ix, by - iy),
                                               Math.Max(ix + iw - (bx + bw), iy + ih - (by + bh)));
                        if (over > 0.5)
                            return name + " 越界 " + over.ToString("0.0") + "px（图形 "
                                 + ix.ToString("0.0") + "," + iy.ToString("0.0") + " "
                                 + iw.ToString("0.0") + "×" + ih.ToString("0.0") + "，箱体 "
                                 + bx.ToString("0.0") + "," + by.ToString("0.0") + " "
                                 + bw.ToString("0.0") + "×" + bh.ToString("0.0") + "）";
                    }
                    return null;
                });
            }

            // =============================================================
            //  机台底盘 + 左右两侧的导轨条（原图里那两条蓝/青竖条）
            // =============================================================
            cv.Items.Add(new RectItem
            {
                X = SX(FrX), Y = SY(FrY), W = SW(FrW), H = SW(FrH), Radius = 10,
                Fill = BodyFill, Stroke = BodyStroke, StrokeThickness = 1.5
            });
            // 左侧竖直导轨条
            cv.Items.Add(new RectItem
            {
                X = SX(84), Y = SY(120), W = SW(5), H = SW(268), Radius = 2, Fill = RailDeep
            });
            // 右侧竖直导轨条
            cv.Items.Add(new RectItem
            {
                X = SX(444), Y = SY(96), W = SW(5), H = SW(292), Radius = 2, Fill = RailDeep
            });

            // =============================================================
            //  模组外框 + 标题
            // =============================================================
            RectItem ModBox(string title, double x, double y, double w, double h, double titleSize = 9.5)
            {
                var b = new RectItem
                {
                    X = SX(x), Y = SY(y), W = SW(w), H = SW(h), Radius = 5,
                    Fill = ModFill, Stroke = ModStroke, StrokeThickness = 1
                };
                cv.Items.Add(b);
                cv.Items.Add(new TextItem
                {
                    Text = title, X = SX(x), Y = SY(y) + 3, TextSize = titleSize, Foreground = Ink,
                    TextWidth = SW(w), Align = TextAlignment.Center
                });
                return b;
            }

            // =============================================================
            //  左立柱：下料模组（竖直升降 + 分盘气缸）
            // =============================================================
            ModBox("下料\n模组", M1X, M1Y, M1W, M1H, 9);
            // 竖直导轨
            cv.Items.Add(new RectItem
            {
                X = SX(M1X + 15), Y = SY(M1Y + 8), W = SW(6), H = SW(M1H - 16),
                Radius = 3, Fill = RailB
            });
            // 升降滑台（上下动）
            var m1Car = new RectItem
            {
                X = SX(M1X + 5), Y = SY(M1Y + 12), W = SW(M1W - 10), H = SW(26),
                Radius = 4, Fill = Teal, Text = "滑台", TextSize = 8, TextColor = Brushes.White
            };
            cv.Items.Add(m1Car);
            // 分盘气缸（红，滑台到底时顶出）
            var m1Cyl = new RectItem
            {
                X = SX(M1X + 4), Y = SY(M1Y + 200), W = SW(M1W - 8), H = SW(40),
                Radius = 4, Fill = Red, Text = "分盘\n气缸", TextSize = 8, TextColor = Brushes.White
            };
            cv.Items.Add(m1Cyl);
            Guard("下料模组", M1X, M1Y, M1W, M1H, new List<RectItem> { m1Car, m1Cyl });

            // =============================================================
            //  上排四条流水线（每条 = 料盘竖直流动）
            // =============================================================
            var lanes = new (string Name, double X, double W, Brush Tray, int Dir)[]
            {
                ("下料\n流水线", L1X, L1W, TrayDark, +1),
                ("彩盘\n流水线", L2X, L2W, TrayColor, -1),
                ("黑盘\n流水线", L3X, L3W, TrayDark, -1),
                ("上料\n流水线", L4X, L4W, TrayDark, +1)
            };

            // 箱高 L1H=88，标题占 ~20 → 可用 68。
            // 关键：料盘是「循环流动」的。取模后的顶端最大值其实是 laneInnerTop+LaneTotal-ε
            // （不是 laneInnerTop+(LaneTotal-LaneStep)），所以约束是
            //     LaneTotal + LaneTrayH ≤ 可用高
            //   5 片 * (10+1.5) = 57.5 → 57.5 + 10 = 67.5 ≤ 68 ✔
            // 上一版用 6 片 * (9+2) = 66 → 66+9 = 75 > 68，最后一片会掉到箱体外面去。
            // 原图里每条流水线就是一摞 5~6 片薄料盘，5 片正好。
            const int LaneTrayN = 5;
            const double LaneTrayH = 10, LaneTrayGap = 1.5;
            const double LaneStep = LaneTrayH + LaneTrayGap;   // 11.5
            const double LaneTotal = LaneTrayN * LaneStep;      // 57.5（循环周期）
            const double LaneInner = LaneTotal - LaneTrayGap;   // 56（槽的可见高度）
            double laneInnerTop = L1Y + 20;

            var laneTrays = new List<(RectItem Body, RectItem[] Lines, int Idx, int Dir)>();
            var laneItems = new List<RectItem>[lanes.Length];
            for (int li = 0; li < lanes.Length; li++)
            {
                var lane = lanes[li];
                var box = new List<RectItem>();
                laneItems[li] = box;
                ModBox(lane.Name, lane.X, L1Y, lane.W, L1H, 8.5);
                // 槽底
                var trough = new RectItem
                {
                    X = SX(lane.X + 5), Y = SY(laneInnerTop), W = SW(lane.W - 10), H = SW(LaneInner),
                    Radius = 3, Fill = LaneFill, Stroke = null, StrokeThickness = 0
                };
                cv.Items.Add(trough);
                box.Add(trough);
                for (int i = 0; i < LaneTrayN; i++)
                {
                    var body = new RectItem
                    {
                        X = SX(lane.X + 8), Y = SY(laneInnerTop + i * LaneStep),
                        W = SW(lane.W - 16), H = SW(LaneTrayH), Radius = 2,
                        Fill = lane.Tray, Stroke = TrayEdge, StrokeThickness = 0.6
                    };
                    cv.Items.Add(body);
                    box.Add(body);
                    // 料盘格纹：3 条横线（2.4 / 5.0 / 7.6，都 < 10 不会溢出盘外）
                    var lines = new RectItem[3];
                    for (int k = 0; k < lines.Length; k++)
                    {
                        lines[k] = new RectItem
                        {
                            X = SX(lane.X + 11), Y = SY(laneInnerTop + i * LaneStep + 2.4 + k * 2.6),
                            W = SW(lane.W - 22), H = 1, Radius = 0, Fill = TrayLine
                        };
                        cv.Items.Add(lines[k]);
                        box.Add(lines[k]);
                    }
                    laneTrays.Add((body, lines, i, lane.Dir));
                }
                Guard(lane.Name.Replace("\n", ""), lane.X, L1Y, lane.W, L1H, box);
            }

            // =============================================================
            //  移盘模组：横梁 + 穿梭小车 + 竖直下压机构
            //  原图里它没有外框，就是一根横梁骑在流水线上方，所以这里不画 ModBox，
            //  只把「移盘模组」四个字放在横梁左端（占 M6X .. M6X+46）。
            // =============================================================
            const double M6LabelW = 46;
            const double M6RailX = M6X + M6LabelW + 2;                 // 156
            const double M6RailW = M6W - M6LabelW - 6;                 // 265 → 156..421
            cv.Items.Add(new TextItem
            {
                Text = "移盘模组", X = SX(M6X), Y = SY(M6Y + 1), TextSize = 8.5,
                Foreground = Ink, TextWidth = SW(M6LabelW), Align = TextAlignment.Left
            });
            // 横梁导轨
            cv.Items.Add(new RectItem
            {
                X = SX(M6RailX), Y = SY(M6Y + 6), W = SW(M6RailW), H = SW(5),
                Radius = 2, Fill = RailB
            });
            // 穿梭小车（沿横梁左右走）
            const double M6CarW = 24;
            double M6CarSpan = M6RailW - M6CarW;
            var m6Car = new RectItem
            {
                X = SX(M6RailX), Y = SY(M6Y + 1), W = SW(M6CarW), H = SW(M6H - 2),
                Radius = 3, Fill = Steel, Text = "移盘", TextSize = 7.5, TextColor = Brushes.White
            };
            cv.Items.Add(m6Car);
            Guard("移盘小车", M6RailX, M6Y, M6RailW, M6H, new List<RectItem> { m6Car });
            // 竖直下压机构（原图右侧那根竖直丝杆）
            cv.Items.Add(new RectItem
            {
                X = SX(366), Y = SY(172), W = SW(16), H = SW(66), Radius = 3, Fill = ModFill,
                Stroke = ModStroke, StrokeThickness = 1
            });
            var m6Rod = new RectItem
            {
                X = SX(371), Y = SY(176), W = SW(6), H = SW(20), Radius = 2, Fill = RailB
            };
            cv.Items.Add(m6Rod);
            var m6Head = new RectItem
            {
                X = SX(368), Y = SY(196), W = SW(12), H = SW(12), Radius = 3, Fill = Teal
            };
            cv.Items.Add(m6Head);

            // =============================================================
            //  料盘缓存（中排 5 根高料盘柱）
            //  原图这里是一整片深色料盘柱：每根柱子又高又窄，面上是很密的格纹。
            // =============================================================
            ModBox("料盘缓存（移盘模组取放位）", M7X, M7Y, M7W, M7H, 8.5);
            // 5 个料盘位：40 宽 + 3 间距 → 5*40+4*3 = 212，起点 120 → 120..332，箱内 118..336 ✔
            // 高度：箱 86 - 标题 18 = 68，料盘柱 188..252 = 64 ✔
            // 每个料盘位画 8 横 + 4 纵格纹，模拟原图里那摞深色料盘的密集格面。
            const double CacheW = 40, CacheGap = 3;
            const double CacheTop = M7Y + 18, CacheH = 64;
            const int CacheHLines = 8, CacheVLines = 4;
            var cacheBodies = new RectItem[CacheN];
            var cacheLines = new RectItem[CacheN][];
            for (int i = 0; i < CacheN; i++)
            {
                double x = M7X + 2 + i * (CacheW + CacheGap);
                cacheBodies[i] = new RectItem
                {
                    X = SX(x), Y = SY(CacheTop), W = SW(CacheW), H = SW(CacheH), Radius = 3,
                    Fill = TrayDark, Stroke = TrayEdge, StrokeThickness = 0.6
                };
                cv.Items.Add(cacheBodies[i]);
                var lines = new RectItem[CacheHLines + CacheVLines];
                for (int k = 0; k < CacheHLines; k++)
                {
                    lines[k] = new RectItem
                    {
                        X = SX(x + 3), Y = SY(CacheTop + 3 + k * 7.5), W = SW(CacheW - 6), H = 1,
                        Radius = 0, Fill = TrayLine
                    };
                    cv.Items.Add(lines[k]);
                }
                for (int k = 0; k < CacheVLines; k++)
                {
                    lines[CacheHLines + k] = new RectItem
                    {
                        X = SX(x + CacheW * (k + 1) / (CacheVLines + 1)), Y = SY(CacheTop + 3),
                        W = 1, H = SW(CacheH - 6), Radius = 0, Fill = TrayLine
                    };
                    cv.Items.Add(lines[CacheHLines + k]);
                }
                cacheLines[i] = lines;
            }
            {
                var all = new List<RectItem>();
                for (int i = 0; i < CacheN; i++)
                {
                    all.Add(cacheBodies[i]);
                    all.AddRange(cacheLines[i]);
                }
                Guard("料盘缓存", M7X, M7Y, M7W, M7H, all);
            }

            // =============================================================
            //  下导轨（手工盘 / 料枪 转移小车）
            // =============================================================
            cv.Items.Add(new RectItem
            {
                X = SX(LRX), Y = SY(LRY + 4), W = SW(LRW), H = SW(5), Radius = 2, Fill = RailB
            });
            var lrCar = new RectItem
            {
                X = SX(LRX + 20), Y = SY(LRY), W = SW(26), H = SW(LRH - 2),
                Radius = 3, Fill = Green, Text = "搬送", TextSize = 7.5, TextColor = Brushes.White
            };
            cv.Items.Add(lrCar);
            var lrCyl = new RectItem
            {
                X = SX(LRX + 52), Y = SY(LRY), W = SW(22), H = SW(LRH - 2),
                Radius = 3, Fill = Red, Text = "气缸", TextSize = 7.5, TextColor = Brushes.White
            };
            cv.Items.Add(lrCyl);

            // =============================================================
            //  手工盘模组（3×3 青绿盘位，逐格扫描）
            // =============================================================
            ModBox("手工盘模组", M8X, M8Y, M8W, M8H, 9);
            // 箱高 M8H=80，标题占 ~16 → 可用 64。
            //   4 行 * (13+3) = 64 - 13 = 51，末行底 = M8Y+16+3*16+13 = M8Y+77 < M8Y+80 ✔
            //   4 列 * (13+3) = 64 - 3 = 61，箱内宽 96-36=60 … 所以列起点取 M8X+18，
            //   末列右缘 = M8X+18+61 = M8X+79 < M8X+96 ✔（居中：格阵中心 = M8X+48.5，箱中心 = M8X+48）
            // 原图里手工盘就是一大片密集的青绿格盘，4×4 比 3×3 更接近。
            const int MpCols = 4, MpRows = 4;
            const double MpCell = 13, MpGap = 3;
            var mpCells = new RectItem[MpCols * MpRows];
            for (int r = 0; r < MpRows; r++)
            {
                for (int c = 0; c < MpCols; c++)
                {
                    var cell = new RectItem
                    {
                        X = SX(M8X + 18 + c * (MpCell + MpGap)),
                        Y = SY(M8Y + 16 + r * (MpCell + MpGap)),
                        W = SW(MpCell), H = SW(MpCell), Radius = 2,
                        Fill = TealDim, Stroke = TrayEdge, StrokeThickness = 0.6
                    };
                    cv.Items.Add(cell);
                    mpCells[r * MpCols + c] = cell;
                }
            }
            Guard("手工盘模组", M8X, M8Y, M8W, M8H, new List<RectItem>(mpCells));

            // =============================================================
            //  压测模组（压头随真实 Z 轴）
            // =============================================================
            ModBox("压测模组", M9X, M9Y, M9W, M9H, 9);
            // 测试座（比读数文字高一档，别让 0.0mm 那行压在座子上）
            cv.Items.Add(new RectItem
            {
                X = SX(M9X + 8), Y = SY(M9Y + M9H - 28), W = SW(M9W - 16), H = SW(12),
                Radius = 3, Fill = Steel
            });
            // 压头（上下动）
            var m9Head = new RectItem
            {
                X = SX(M9X + 13), Y = SY(M9Y + 22), W = SW(M9W - 26), H = SW(14),
                Radius = 3, Fill = Purple
            };
            cv.Items.Add(m9Head);
            var m9Rod = new RectItem
            {
                X = SX(M9X + M9W / 2 - 2), Y = SY(M9Y + 14), W = SW(4), H = SW(8),
                Radius = 2, Fill = Steel
            };
            cv.Items.Add(m9Rod);
            var m9Text = new TextItem
            {
                Text = "0.0mm", X = SX(M9X), Y = SY(M9Y + M9H - 14), TextSize = 8,
                Foreground = Ink2, TextWidth = SW(M9W), Align = TextAlignment.Center
            };
            cv.Items.Add(m9Text);
            Guard("压测模组", M9X, M9Y, M9W, M9H, new List<RectItem> { m9Head, m9Rod });

            // =============================================================
            //  料枪模组（枪管随真实 Y 轴伸缩）
            // =============================================================
            ModBox("料枪模组", MAX, MAY, MAW, MAH, 9);
            // 竖直丝杆
            cv.Items.Add(new RectItem
            {
                X = SX(MAX + 26), Y = SY(MAY + 16), W = SW(7), H = SW(MAH - 30),
                Radius = 3, Fill = RailB
            });
            // 滑座
            var maCar = new RectItem
            {
                X = SX(MAX + 18), Y = SY(MAY + 20), W = SW(24), H = SW(14),
                Radius = 3, Fill = Teal
            };
            cv.Items.Add(maCar);
            // 料枪本体
            cv.Items.Add(new RectItem
            {
                X = SX(MAX + 6), Y = SY(MAY + 62), W = SW(22), H = SW(18), Radius = 3, Fill = Steel
            });
            // 枪管（伸缩）
            var maBar = new RectItem
            {
                X = SX(MAX + 28), Y = SY(MAY + 66), W = SW(8), H = SW(10), Radius = 2, Fill = Steel
            };
            cv.Items.Add(maBar);
            var maTip = new RectItem
            {
                X = SX(MAX + 36), Y = SY(MAY + 68), W = SW(6), H = SW(6), Radius = 1, Fill = Red
            };
            cv.Items.Add(maTip);
            // 底部 V 形机构（原图里那对斜杆，用两个小方块示意）
            cv.Items.Add(new RectItem
            {
                X = SX(MAX + 8), Y = SY(MAY + MAH - 22), W = SW(14), H = SW(10),
                Radius = 2, Fill = Steel
            });
            cv.Items.Add(new RectItem
            {
                X = SX(MAX + 44), Y = SY(MAY + MAH - 22), W = SW(14), H = SW(10),
                Radius = 2, Fill = Steel
            });
            var maText = new TextItem
            {
                Text = "0.0mm", X = SX(MAX), Y = SY(MAY + MAH - 12), TextSize = 8,
                Foreground = Ink2, TextWidth = SW(MAW), Align = TextAlignment.Center
            };
            cv.Items.Add(maText);
            Guard("料枪模组", MAX, MAY, MAW, MAH, new List<RectItem> { maCar, maBar, maTip });

            // =============================================================
            //  加热盘模组（OUT15 加热使能 → 盘面变红 + 热流）
            // =============================================================
            ModBox("加热盘模组", MBX, MBY, MBW, MBH, 9);
            const int PlateN = 5;
            const double PlateH = 14, PlateGap = 4;
            var htPlates = new RectItem[PlateN];
            for (int i = 0; i < PlateN; i++)
            {
                htPlates[i] = new RectItem
                {
                    X = SX(MBX + 8), Y = SY(MBY + 18 + i * (PlateH + PlateGap)),
                    W = SW(MBW - 16), H = SW(PlateH), Radius = 2,
                    Fill = Cool, Stroke = TrayEdge, StrokeThickness = 0.6
                };
                cv.Items.Add(htPlates[i]);
            }
            // 热流（三束，从盘面往上冒）
            var htWaves = new RectItem[3];
            for (int i = 0; i < 3; i++)
            {
                htWaves[i] = new RectItem
                {
                    X = SX(MBX + 18 + i * 20), Y = SY(MBY + 4), W = SW(8), H = 1,
                    Radius = 4, Fill = HeatHaze
                };
                cv.Items.Add(htWaves[i]);
            }
            var mbText = new TextItem
            {
                Text = "加热 关闭", X = SX(MBX), Y = SY(MBY + MBH - 14), TextSize = 8,
                Foreground = Ink2, TextWidth = SW(MBW), Align = TextAlignment.Center
            };
            cv.Items.Add(mbText);
            {
                var all = new List<RectItem>(htPlates);
                all.AddRange(htWaves);
                Guard("加热盘模组", MBX, MBY, MBW, MBH, all);
            }

            // =============================================================
            //  右立柱：上料模组（竖直升降 + 顶升气缸）
            // =============================================================
            ModBox("上料\n模组", M12X, M12Y, M12W, M12H, 9);
            cv.Items.Add(new RectItem
            {
                X = SX(M12X + 18), Y = SY(M12Y + 8), W = SW(6), H = SW(M12H - 16),
                Radius = 3, Fill = RailB
            });
            var m12Car = new RectItem
            {
                X = SX(M12X + 5), Y = SY(M12Y + 12), W = SW(M12W - 10), H = SW(26),
                Radius = 4, Fill = Teal, Text = "滑台", TextSize = 8, TextColor = Brushes.White
            };
            cv.Items.Add(m12Car);
            var m12Cyl = new RectItem
            {
                X = SX(M12X + 4), Y = SY(M12Y + 226), W = SW(M12W - 8), H = SW(44),
                Radius = 4, Fill = Red, Text = "顶升\n气缸", TextSize = 8, TextColor = Brushes.White
            };
            cv.Items.Add(m12Cyl);
            Guard("上料模组", M12X, M12Y, M12W, M12H, new List<RectItem> { m12Car, m12Cyl });

            // =============================================================
            //  顶部读数条
            // =============================================================
            var readout = new TextItem
            {
                Text = "", X = SX(FrX), Y = 2, TextSize = 11, Foreground = Ink,
                TextWidth = SW(FrW), Align = TextAlignment.Left
            };
            cv.Items.Add(readout);

            // =============================================================
            //  逐帧重绘
            // =============================================================
            long last = Environment.TickCount64;
            double t = 0;

            cv.Draw = c =>
            {
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;

                var m = MotionManager.Instance;

                // 只有点击「启动演示节拍」后（AutoCycle=true）才推进动画；否则保持静态初态
                if (m.AutoCycle) t += dt;

                // ---- 流水线走料（时间驱动，料盘沿导轨流动） ----
                double flow = t * 13;      // 原图像素/秒
                for (int i = 0; i < laneTrays.Count; i++)
                {
                    var (body, lines, idx, dir) = laneTrays[i];
                    double p = Mod(idx * LaneStep + dir * flow, LaneTotal);
                    double top = laneInnerTop + p;
                    body.Y = SY(top);
                    for (int k = 0; k < lines.Length; k++) lines[k].Y = SY(top + 2.4 + k * 2.6);
                }

                // ---- 左/右立柱滑台（时间驱动，反相） ----
                double m1Span = M1H - 46;
                double tri = Triangle(t * 0.24);
                m1Car.Y = SY(M1Y + 12 + (1 - tri) * m1Span);
                m12Car.Y = SY(M12Y + 12 + tri * (M12H - 46));
                // 分盘 / 顶升气缸：滑台到位时顶出
                m1Cyl.Fill = tri > 0.93 ? Red : Steel;
                m12Cyl.Fill = tri < 0.07 ? Red : Steel;

                // ---- 移盘模组：穿梭小车（小车 A 在动就跟着走，否则按时间） ----
                double car = m.Position(28);
                double mvT = (m.State(28).Busy || m.State(29).Busy)
                    ? Math.Min(1, Math.Max(0, car / 1200.0))
                    : Triangle(t * 0.38);
                m6Car.X = SX(M6RailX + mvT * M6CarSpan);
                // 竖直下压机构：小车走到两端时下压
                bool atEnd = mvT < 0.08 || mvT > 0.92;
                m6Rod.H = SW(atEnd ? 34 : 20);
                m6Head.Y = SY(atEnd ? 210 : 196);
                m6Head.Fill = atEnd ? Orange : Teal;

                // ---- 料盘缓存：随演示步骤逐个点亮（底色用暗钢蓝，格纹提亮） ----
                int lit = (int)(t * 3) % (CacheN + 2);
                for (int i = 0; i < CacheN; i++)
                {
                    bool on = i == lit;
                    cacheBodies[i].Fill = on ? CacheLit : TrayDark;
                    for (int k = 0; k < cacheLines[i].Length; k++)
                        cacheLines[i][k].Fill = on ? TrayLineOn : TrayLine;
                }

                // ---- 下导轨搬送小车（时间驱动） ----
                lrCar.X = SX(LRX + 20 + Triangle(t * 0.33) * (LRW - 100));
                lrCyl.Fill = Triangle(t * 0.33) > 0.85 ? Red : Steel;

                // ---- 手工盘模组：逐格扫描 ----
                int scan = (int)(t * 6) % mpCells.Length;
                for (int i = 0; i < mpCells.Length; i++)
                {
                    int d = (i - scan + mpCells.Length) % mpCells.Length;
                    mpCells[i].Fill = d == 0 ? Green : d < 3 ? Teal : TealDim;
                }

                // ---- 压测模组：压头随真实 Z 轴（轴 2） ----
                double zp = m.Position(2);
                double zMax = Math.Max(1, m.Axes[2].MaxMm);
                double zT = Math.Min(1, Math.Max(0, zp / zMax));
                double headTop = M9Y + 22;
                double headBottom = M9Y + M9H - 42;   // 压到底时压头下沿正好贴住测试座上沿
                // 始终在「原图像素」域里算，再统一换算到画布 —— 不要拿已经换算过的
                // m9Head.Y 反算回去，那要经过 /K、-OY/K 两次变形，很容易写错。
                double headY = headTop + (1 - zT) * (headBottom - headTop);
                m9Head.Y = SY(headY);
                m9Rod.Y = SY(headY - 8);
                bool pressed = zT > 0.85;
                m9Head.Fill = pressed ? Red : Purple;
                m9Text.Text = zp.ToString("0.0") + "mm" + (pressed ? " 压到位" : "");

                // ---- 料枪模组：枪管随真实 Y 轴（轴 5） ----
                double gy = m.Position(5);
                double gyMax = Math.Max(1, m.Axes[5].MaxMm);
                double gT = Math.Min(1, Math.Max(0, gy / gyMax));
                double gunLen = 8 + gT * 20;
                maBar.W = SW(gunLen);
                maTip.X = SX(MAX + 28 + gunLen);
                maBar.Fill = gT > 0.7 ? Orange : Steel;
                maText.Text = gy.ToString("0.0") + "mm";
                // 滑座随枪管下移（取料行程）
                maCar.Y = SY(MAY + 20 + gT * 46);

                // ---- 加热盘模组：OUT15 加热使能 ----
                bool heatOn = m.ReadOut(15);
                if (heatOn)
                {
                    double pulse = 0.5 + 0.5 * Math.Sin(t * 4);
                    var hot = LerpBrush(Cool, Red, 0.5 + 0.5 * pulse);
                    for (int i = 0; i < htPlates.Length; i++) htPlates[i].Fill = hot;
                    mbText.Text = "加热 开启";
                    mbText.Foreground = Red;
                    for (int i = 0; i < htWaves.Length; i++)
                    {
                        double ph = Mod(t * 0.9 + i * 0.33, 1.0);
                        htWaves[i].H = SW(4 + ph * 12);
                        htWaves[i].Y = SY(MBY + 16) - htWaves[i].H;
                    }
                }
                else
                {
                    for (int i = 0; i < htPlates.Length; i++) htPlates[i].Fill = Cool;
                    mbText.Text = "加热 关闭";
                    mbText.Foreground = Ink2;
                    for (int i = 0; i < htWaves.Length; i++) htWaves[i].H = 1;
                }

                // ---- 顶部读数条 ----
                readout.Text = "卡：" + m.CardName + (m.IsSim ? "（仿真）" : "（真卡）")
                    + "　|　上料臂 X=" + m.Position(4).ToString("0.0")
                    + "　下料臂 X=" + m.Position(16).ToString("0.0")
                    + "　间距=" + Math.Abs(m.Position(4) - m.Position(16)).ToString("0.0")
                    + "　|　小车A=" + car.ToString("0.0")
                    + "　小车B=" + m.Position(29).ToString("0.0")
                    + "　|　" + (m.EStop ? "【急停】" : "运行正常")
                    + "　步骤：" + m.DemoStepText;
            };

            // 先画一帧，保证静态截图 / 首次显示就有内容（不依赖快速通道）。
            cv.Refresh();
            return cv;
        }

        /// <summary>
        /// 构建整机俯视图仿真画布。
        ///
        /// 底图 = image/handler_top.jpg —— 用户提供的**空机台**俯视图（1696×1376，
        /// 机台上一片料盘都没有）。料盘不在底图里，而是由本方法按「满盘俯视图」
        /// 逐像素量测出来的真机坐标**现画**上去，一共 14 个（见 <see cref="TrayPlan"/>）：
        ///     · 上排 6 个 —— 深色格纹盘
        ///     · 中排 5 个 —— 深色格纹盘（料盘缓存位）
        ///     · 左下 3 个 —— 绿色格纹盘（出料盘）
        /// 这样做的好处：料盘数量 / 位置 / 颜色都和真机一致，而且能随演示节拍做动画
        /// （空机台 + 现画料盘 = 料盘可以逐个点亮、横移臂可以走位；焊进底图里就动不了）。
        ///
        /// 画布 1010×819：与底图 1696:1376 ≈ 1.233 的宽高比一致，高度与 3D 画布相同（819），
        /// 刚好嵌进左侧「标签页」区域，不会把页面顶出竖向滚动条。
        /// </summary>
        private static CanvasElement BuildLayoutCanvasTop()
        {
            // 1010×819 —— 与底图 1696×1376（≈1.233）等比，且与 3D 画布同高。
            const double CW = 1010;
            const double CH = 819;

            var cv = new CanvasElement { CanvasWidth = CW, CanvasHeight = CH };

            // ---- 底图：image/handler_top.jpg（空机台；运行时由 .csproj CopyToOutputDirectory
            // 复制到 exe 同级目录，pack://siteoforigin 从那里加载）----
            ImageBrush bgBrush;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri("pack://siteoforigin:,,,/image/handler_top.jpg");
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                bgBrush = new ImageBrush(bmp) { Stretch = Stretch.Fill };
            }
            catch
            {
                bgBrush = new ImageBrush();
            }
            cv.Items.Add(new RectItem { X = 0, Y = 0, W = CW, H = CH, Fill = bgBrush });

            // 找不到图时叠深灰底，绝不黑屏。
            // 这里原来还会在画布正中写一行「image/handler_top.jpg 加载失败」，按用户要求
            // 画布上不留任何文字，于是改成往运行日志里记一条 —— 失败依旧可查（日志页 / 日志文件），
            // 但仿真画面上不会再冒出一行字。
            if (bgBrush.ImageSource == null)
            {
                cv.Items.Add(new RectItem
                {
                    X = 0, Y = 0, W = CW, H = CH,
                    Fill = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x30))
                });
                try { MachineModel.Instance.Log("警告", "整机俯视图底图 image/handler_top.jpg 加载失败，画布以深灰底代替"); }
                catch { }
            }

            // ---- 真机料盘：按 TrayPlan 的平面坐标落点 ----
            // 一张盘 = ①取放高亮底板 ②盘身 ③格子层 ④物料格（每个格子里一颗料）。
            // 两套配色，和真机一致：深盘 = 近黑底 + 灰格；绿盘 = 墨绿底 + 青绿格
            // （颜色取自「满盘俯视图」对应区域的中位色）。
            var darkBody = new SolidColorBrush(Color.FromRgb(0x26, 0x28, 0x2E));
            var darkLine = new SolidColorBrush(Color.FromRgb(0x6E, 0x72, 0x7C));
            var darkEdge = new SolidColorBrush(Color.FromRgb(0x8A, 0x8F, 0x9A));
            var greenBody = new SolidColorBrush(Color.FromRgb(0x1F, 0x31, 0x2D));
            var greenLine = new SolidColorBrush(Color.FromRgb(0x5B, 0x86, 0x7C));
            var greenEdge = new SolidColorBrush(Color.FromRgb(0x7F, 0xA8, 0x9A));

            // 物料配色：待测（深盘里）= 亮青；已测 / 出料（绿盘里）= 亮绿
            var devBlue = new SolidColorBrush(Color.FromRgb(0x3C, 0xC0, 0xF0));

            var devGreen = new SolidColorBrush(Color.FromRgb(0x3A, 0xD3, 0x8E));

            // 每个料盘的「取放高亮」底板（平时全透明，轮到它被取放时点亮）
            var trayHilite = new List<RectItem>();
            // 每个料盘的物料格列表（按「从上到下、从左到右」排好序，动画里逐格增删）
            var trayDevices = new List<List<RectItem>>();

            for (int i = 0; i < TrayPlan.Length; i++)
            {
                var tp = TrayPlan[i];
                double x = tp.U * CW, y = tp.V * CH, w = tp.DU * CW, h = tp.DV * CH;
                var body = tp.Green ? greenBody : darkBody;
                var line = tp.Green ? greenLine : darkLine;
                var edge = tp.Green ? greenEdge : darkEdge;

                // ① 取放高亮先加，压在盘身下面（外扩 3px，动画里改它的 Fill / Stroke）
                var hi = new RectItem
                {
                    X = x - 3, Y = y - 3, W = w + 6, H = h + 6,
                    Radius = 5,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(Color.FromArgb(0, 0xFF, 0xC0, 0x40)),
                    StrokeThickness = 3,
                };
                cv.Items.Add(hi);
                trayHilite.Add(hi);

                // ② 盘身
                cv.Items.Add(new RectItem
                {
                    X = x, Y = y, W = w, H = h,
                    Radius = 3,
                    Fill = body,
                    Stroke = edge,
                    StrokeThickness = 1.2,
                });

                // ③ 格子层：用 DrawingBrush 按「一格」的尺寸平铺 —— 一张盘只占 1 个图元
                //    就画出整片格子（深盘 5×10 格，绿盘 4×8 格，格子接近正方形）。
                int cols = tp.Green ? 4 : 5;
                int rows = tp.Green ? 8 : 10;
                double cw = w / cols, ch = h / rows;
                var pocket = new DrawingBrush
                {
                    TileMode = TileMode.Tile,
                    Viewport = new Rect(0, 0, cw, ch),
                    ViewportUnits = BrushMappingMode.Absolute,
                    Stretch = Stretch.Fill,          // 把「一格」的图形精确铺满一个 Viewport 瓷砖
                    Drawing = new GeometryDrawing
                    {
                        Brush = body,
                        Pen = new Pen(line, 1),
                        Geometry = new RectangleGeometry(new Rect(0.5, 0.5, cw - 1, ch - 1)),
                    },
                };
                cv.Items.Add(new RectItem
                {
                    X = x, Y = y, W = w, H = h,
                    Radius = 3,
                    Fill = pocket,
                    Stroke = body,          // 只为了把 BorderThickness 压到 0，颜色无所谓
                    StrokeThickness = 0,
                });

                // ④ 物料格：每个格子里一颗料（比格子内缩 3px，露出格子的边）
                var devs = new List<RectItem>();
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        const double inset = 3.2;
                        var dv = new RectItem
                        {
                            X = x + c * cw + inset,
                            Y = y + r * ch + inset,
                            W = Math.Max(2, cw - inset * 2),
                            H = Math.Max(2, ch - inset * 2),
                            Radius = 2,
                            Fill = tp.Green ? devGreen : devBlue,
                        };
                        cv.Items.Add(dv);
                        devs.Add(dv);
                    }
                }
                trayDevices.Add(devs);
            }

            // ---- 加热盘（真机俯视图右侧那两块点阵加热盘）----
            // 坐标同样是量测出来的：格子中心 = 盘面上的圆点中心。
            // 原图 1696×1376 上量得：8 列（列间距 16.086px）、两块盘各 11 行（行间距 26.97px），
            // 首行 y 分别是 547.6 / 857.9；列起点 1356.5。换算到画布就是下面这几个数。
            // 每块盘 8×11 = 88 个格子，两块共 176 个。
            const double HP_X0 = 1356.5, HP_DX = 16.086;   // 原图列起点 / 列间距
            const double HP_DY = 26.97;                    // 原图行间距
            const int HP_COLS = 8, HP_ROWS = 11;
            double[] hpY0 = { 547.6, 857.9 };              // 原图两块盘的首行 y
            double hkx = CW / 1696.0, hky = CH / 1376.0;   // 原图 → 画布

            var heatLine = new SolidColorBrush(Color.FromRgb(0x92, 0x98, 0xA4));   // 格子线
            var heatChip = new SolidColorBrush(Color.FromRgb(0xF2, 0xA9, 0x3B));   // 加热中的芯片（琥珀）
            var heatChips = new List<List<RectItem>>();

            for (int hp = 0; hp < hpY0.Length; hp++)
            {
                // 网格左上角：以圆点为中心，往外让半个间距
                double gw = HP_DX * hkx, gh = HP_DY * hky;          // 一个格子（画布）
                double gx0 = HP_X0 * hkx - gw / 2;
                double gy0 = hpY0[hp] * hky - gh / 2;

                // 格子层：DrawingBrush 平铺（一块盘 1 个图元就画出 88 个格子）
                var pocket = new DrawingBrush
                {
                    TileMode = TileMode.Tile,
                    Viewport = new Rect(0, 0, gw, gh),
                    ViewportUnits = BrushMappingMode.Absolute,
                    Stretch = Stretch.Fill,
                    Drawing = new GeometryDrawing
                    {
                        Brush = new SolidColorBrush(Color.FromArgb(0x14, 0x00, 0x00, 0x00)),
                        Pen = new Pen(heatLine, 1),
                        Geometry = new RectangleGeometry(new Rect(0.5, 0.5, gw - 1, gh - 1)),
                    },
                };
                cv.Items.Add(new RectItem
                {
                    X = gx0, Y = gy0, W = gw * HP_COLS, H = gh * HP_ROWS, Radius = 2,
                    Fill = pocket, Stroke = heatLine, StrokeThickness = 0,
                });

                // 芯片：每个点位上放一颗，正对圆点居中。
                // 注意格子是「高瘦」的（原图列距 16px、行距 27px），如果按格子等比铺料，
                // 一颗料会变成一根竖条，完全不像芯片；所以取窄边（列距）的 0.66 当边长，
                // 做成正方形，视觉上就是「一颗料被放到加热盘的点位上」。
                var chips = new List<RectItem>();
                double cs = gw * 0.66;               // 芯片边长（正方形）
                for (int r = 0; r < HP_ROWS; r++)
                {
                    for (int c = 0; c < HP_COLS; c++)
                    {
                        var chip = new RectItem
                        {
                            X = gx0 + c * gw + (gw - cs) / 2,
                            Y = gy0 + r * gh + (gh - cs) / 2,
                            W = cs, H = cs,
                            Radius = 1.5, Fill = heatChip,
                        };
                        cv.Items.Add(chip);
                        chips.Add(chip);
                    }
                }
                heatChips.Add(chips);
            }

            // ---- 分组标注：已按用户要求整组删除 ----
            // 原来这里有四条压在照片上的文字：「上排料盘 ×6（深盘）」「中排料盘 ×5（深盘 · 缓存位）」
            // 「左下绿色料盘 ×3（出料）」「加热盘 ×2（8×11 点位 · 芯片刷新）」。
            // 现在画布上一句字都不留，这些分组说明统一放到右侧说明栏的「图例」里。

            // ---- 横移取放臂：沿上排与中排之间的导轨左右走位，走到哪个盘就点亮哪个盘 ----
            const double armY = 240;                       // 导轨中心线（上排盘底 215 / 中排盘顶 292 之间）
            const double armX0 = 120, armX1 = 874;
            cv.Items.Add(new RectItem
            {
                X = armX0, Y = armY - 4, W = armX1 - armX0, H = 8, Radius = 4,
                Fill = new SolidColorBrush(Color.FromRgb(0x2B, 0x4C, 0x8A)),
            });
            var armHead = new RectItem
            {
                X = armX0, Y = armY - 20, W = 46, H = 40, Radius = 6,
                Fill = new SolidColorBrush(Color.FromArgb(230, 0x2F, 0x7C, 0xD6)),
                Stroke = new SolidColorBrush(Color.FromRgb(0x14, 0x3F, 0x74)),
                StrokeThickness = 1.5,
                // 原来这个方块正中写着一个「臂」字（Text / TextSize / TextColor），
                // 按用户要求画布不留文字，只保留方块本身作为「取放臂头」的图形标记。
            };
            cv.Items.Add(armHead);

            // ==============================================================
            //  下料X手臂（真机量测：X 横梁 + 红色吸嘴 2×4 组合）
            // ==============================================================
            // 量测来源：真机俯视图 image/handler_top.jpg（1696×1376）。
            // 画布坐标 = 照片坐标 × (CW/1696, CH/1376) = × (0.59552, 0.59520)。
            //   · X 横梁外框      照片 x 151..766, y 821..880
            //   · 红色吸嘴组合    照片 x 376..494, y 821..878（2 行 × 4 列 = 8 个吸嘴）
            //   · 绿色滑块 ×2     照片 x 220..263 / 327..370, y 826..874
            //   · 左侧竖直导轨（手臂 Y 行程的导轨）照片 x 97..157, y 870..1260
            //
            // 运动学（用户口述 + 照片印证）：
            //   ① 整根 X 横梁挂在左侧竖直导轨的滑座上，可以**上下平移**（画面上下 = 机器 Y）；
            //   ② 红色吸嘴组合沿横梁**左右平移**（= 机器 X）；
            //   ①×② 叠加 → 吸嘴拿到 XY 定位；2×4 组合一次取放 8 颗料。
            const double UAX0 = 89.9, UAX1 = 456.2;        // X 横梁左右端（画布坐标）
            const double UACY = 506.2;                     // 横梁中心线（照片实测 = 行程上端）
            const double UAH = 35.1;                       // 横梁高
            // Y 向行程：**上端就是照片里的静止位**（横梁正好压在真机导轨上，误差 0），
            // 下端让吸嘴降到三张绿色出料盘的上方 —— 下料手臂够得着出料盘才有意义。
            // 左端那根竖直导轨（照片 y 870..1260 → 画布 y 518..750）印证了「只往下走」：
            // 横梁静止位卡在导轨的**顶端**，所以行程是单向下行，不是绕中线上下摆。
            const double UAY0 = UACY, UAY1 = 660.0;        // Y 向行程：上端 / 下端
            const double UANW = 70.3, UANH = 33.9;         // 吸嘴组合尺寸
            const double UANX0 = 96.0, UANX1 = 386.0;      // 吸嘴左端行程范围
            const int UANC = 4, UANR = 2;                  // 2×4 = 8 个吸嘴

            var uaBlue = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xD6));
            var uaBlueSoft = new SolidColorBrush(Color.FromArgb(0x9A, 0x2F, 0x7C, 0xD6));
            var uaRed = new SolidColorBrush(Color.FromRgb(0xD0, 0x20, 0x20));
            var uaRedEdge = new SolidColorBrush(Color.FromRgb(0x6B, 0x10, 0x10));
            var uaDotBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xDC, 0xDC));
            var uaGreenBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x8E, 0x6E));
            var uaAmberOn = new SolidColorBrush(Color.FromRgb(0xFF, 0xA8, 0x20));
            var uaAmberOff = new SolidColorBrush(Color.FromArgb(0, 0xFF, 0xC0, 0x40));

            // 行程上下限：横梁**外缘**能到的两个极限位置。
            // 早先这里画的是一个半透明圆角「行程槽」，但行程从 ±30 拉长到 154 之后，
            // 那个槽会把三张绿色出料盘整片罩住，像给料盘盖了块灰玻璃。
            // 改成两条细限位线 + 一个带箭头的行程标尺，只标「能走到哪」，不遮料盘。
            // 温启志 18719361399
            double uaSlotTop = UAY0 - UAH / 2 - 5;
            double uaSlotBot = UAY1 + UAH / 2 + 5;
            var uaLimit = new SolidColorBrush(Color.FromArgb(0x8C, 0x2F, 0x7C, 0xD6));
            foreach (double uaLy in new[] { uaSlotTop, uaSlotBot })
            {
                cv.Items.Add(new RectItem
                {
                    X = UAX0 - 4, Y = uaLy, W = (UAX1 - UAX0) + 8, H = 1.4, Fill = uaLimit,
                });
            }

            // Y 行程标尺：竖线 + 上下两个小三角，画在横梁左端**外侧**（x 77..89），
            // 那一条竖带在照片里是空白的，压上去不会盖住任何零件。
            cv.Items.Add(new RectItem
            {
                X = 82, Y = uaSlotTop + 3, W = 2, H = uaSlotBot - uaSlotTop - 6, Fill = uaBlueSoft,
            });
            cv.Items.Add(new PolyItem
            {
                Points = new PointCollection
                {
                    new Point(83, uaSlotTop), new Point(77, uaSlotTop + 9), new Point(89, uaSlotTop + 9),
                },
                Fill = uaBlue,
            });
            cv.Items.Add(new PolyItem
            {
                Points = new PointCollection
                {
                    new Point(83, uaSlotBot), new Point(77, uaSlotBot - 9), new Point(89, uaSlotBot - 9),
                },
                Fill = uaBlue,
            });

            // 出料盘高亮：吸嘴走到哪张绿盘上方就点亮哪张
            var uaTrayX = new List<double>();
            var uaHilite = new List<RectItem>();
            for (int i = 0; i < TrayPlan.Length; i++)
            {
                if (!TrayPlan[i].Green) continue;          // 只有 3 张绿色出料盘
                var tp = TrayPlan[i];
                uaTrayX.Add(tp.U * CW);
                uaTrayX.Add((tp.U + tp.DU) * CW);
                var hl = new RectItem
                {
                    X = tp.U * CW - 3, Y = tp.V * CH - 3,
                    W = tp.DU * CW + 6, H = tp.DV * CH + 6, Radius = 3,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = uaAmberOff, StrokeThickness = 2,
                };
                cv.Items.Add(hl);
                uaHilite.Add(hl);
            }

            // X 横梁（手臂本体）：半透明蓝，压在照片导轨上，看得见「手臂现在在哪」
            var uaBeam = new RectItem
            {
                X = UAX0, Y = UACY - UAH / 2, W = UAX1 - UAX0, H = UAH, Radius = 5,
                Fill = new SolidColorBrush(Color.FromArgb(0x7A, 0x1B, 0x2A, 0x5A)),
                Stroke = uaBlue, StrokeThickness = 2,
            };
            cv.Items.Add(uaBeam);

            // 横梁上的两个绿色滑块（真机照片里就有，跟着横梁一起走）
            var uaGreenSliders = new List<RectItem>();
            double[] uaGx = { 131.0, 156.6, 194.7, 220.3 };
            for (int i = 0; i < 2; i++)
            {
                var gs = new RectItem
                {
                    X = uaGx[i * 2], Y = UACY - UAH / 2 + 3,
                    W = uaGx[i * 2 + 1] - uaGx[i * 2], H = UAH - 6, Radius = 3,
                    Fill = uaGreenBrush,
                };
                cv.Items.Add(gs);
                uaGreenSliders.Add(gs);
            }

            // 红色吸嘴 2×4 组合
            var uaNozzle = new RectItem
            {
                X = UANX0, Y = UACY - UANH / 2, W = UANW, H = UANH, Radius = 5,
                Fill = uaRed, Stroke = uaRedEdge, StrokeThickness = 2,
            };
            cv.Items.Add(uaNozzle);

            var uaDots = new List<EllipseItem>();
            double uaDotD = Math.Min(UANW / UANC, UANH / UANR) * 0.60;
            for (int r = 0; r < UANR; r++)
            {
                for (int q = 0; q < UANC; q++)
                {
                    var dot = new EllipseItem
                    {
                        X = UANX0 + UANW * (q + 0.5) / UANC - uaDotD / 2,
                        Y = UACY - UANH / 2 + UANH * (r + 0.5) / UANR - uaDotD / 2,
                        W = uaDotD, H = uaDotD, Fill = uaDotBrush,
                    };
                    cv.Items.Add(dot);
                    uaDots.Add(dot);
                }
            }

            // ---- 「下料X手臂」标注框：已按用户要求整块删除 ----
            // 原来这里是 316×100 的半透明白底标注框（x 458..774 / y 548..648）+ 标题 +
            // 三行说明 + 一个红色图例小方块 —— 因为照片这一带的机械细节太密，必须垫白底
            // 才读得出来。画布不留文字之后，白底和方块都成了没有内容的浮块，一并撤掉，
            // 让这一片照片恢复完整（红色吸嘴组本身还画在横梁上，图例不再需要）。

            // ==============================================================
            //  上料X轴（真机量测：X 横梁 + 红色吸嘴 2×4 组合）
            // ==============================================================
            // 定位方法：把用户给的截图当模板，在整张真机俯视图上做**归一化互相关**匹配。
            // 最佳命中 photo (1149, 1202)、模板缩放 1.10×、NCC 0.835。
            // （同一套流程回代上一张「下料X手臂」截图 → photo (169, 814)、NCC 0.810，
            //   和当初手工量测完全一致，所以这套定位可信。）
            //
            // 画布坐标 = 照片坐标 × (CW/1696, CH/1376) = × (0.59552, 0.59520)。
            //   · 横梁外框（含两端黄色端板） 照片 x 1148..1550, y 1200..1258
            //   · 红色吸嘴 2×4 组合         照片 x 1297..1413（117 宽 —— 和下料那颗 118 宽同规格）
            //   · 绿色滑块 ×2               照片 x 1419..1431 / 1452..1498
            //   · 横梁内部（吸嘴 X 行程）   照片 x 1167..1533
            //
            // 运动学（用户口述「红色是 2*4 的吸嘴组，可以 xy 移动」＋照片印证）：
            //   ① 红色吸嘴组沿横梁左右走 = 机器 X；
            //   ② 整根横梁挂在两根竖直导杆上上下平移 = 机器 Y；
            //   ①×② = 吸嘴拿到 XY 定位，2×4 一次取放 8 颗。
            // Y 行程（用户口径：「上料的 y 也是这么长」）：
            // 上料横梁的 Y 行程长度**与下料X手臂完全等长**。所以这里不写死数字，
            // 直接取下料的 (UAY1 - UAY0) —— 以后调下料行程，上料自动跟着走，不会走散。
            // 原来这里是「以静止位为中心、上下各 ±26」的对称摆动，总行程只有 52，
            // 比下料的 153.8 短了 2/3，和真机两根同规格的 X 轴不符。
            //
            // 方向：下料是「静止位在行程上端、单向下行」；上料正好反过来 ——
            // 横梁静止位（照片实测 731.5）在**行程下端**，只向上走。
            // 之所以不能继续「以静止位为中心上下对称」：上料横梁已经贴在画布下沿附近
            // （静止位 731.5、梁高 34.5、画布高 819），对称摆到 ±76.9 的话横梁下缘会到
            // 825.6，冲出画布 6.6px 被 ClipToBounds 裁掉。向上走既放得下，
            // 方向上也正好对着上方的料盘 / 加热盘区，动作合理。
            const double U2X0 = 683.7, U2X1 = 922.9;       // 横梁外框左右端（画布坐标）
            const double U2CY = 731.5;                     // 横梁中心线（照片实测 = 静止位 = 行程下端）
            const double U2H = 34.5;                       // 横梁高
            const double U2Y0 = U2CY;                      // Y 行程下端 = 静止位
            const double U2Y1 = U2CY - (UAY1 - UAY0);      // Y 行程上端（与下料 Y 行程等长，= 577.7）
            const double U2NW = 69.1, U2NH = 34.5;         // 红色吸嘴组合尺寸
            const double U2NX0 = 694.9, U2NX1 = 843.3;     // 吸嘴左端行程范围
            const double U2GA0 = 845.0, U2GA1 = 852.2;     // 绿滑块 A
            const double U2GB0 = 864.6, U2GB1 = 892.0;     // 绿滑块 B
            const int U2NC = 4, U2NR = 2;                  // 2×4 = 8 个吸嘴

            // 上料用紫色系，和下料的蓝色区分开 —— 两根 X 轴同规格，
            // 不用颜色区分的话画面上会出现两条一模一样的蓝横梁，看着像画重了。
            // 温启志 18719361399
            var u2Violet = new SolidColorBrush(Color.FromRgb(0x7C, 0x4F, 0xD0));
            var u2VioletSoft = new SolidColorBrush(Color.FromArgb(0x9A, 0x7C, 0x4F, 0xD0));
            var u2GreenBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x8E, 0x6E));
            var u2Red = new SolidColorBrush(Color.FromRgb(0xD0, 0x20, 0x20));
            var u2RedEdge = new SolidColorBrush(Color.FromRgb(0x6B, 0x10, 0x10));
            var u2DotBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xDC, 0xDC));

            // 行程包络 = 横梁**外缘**能到的两个极限位置
            // （上端 = 行程上端再让出半个梁高 + 5；下端 = 静止位再让出半个梁高 + 5）
            double u2EnvTop = U2Y1 - U2H / 2 - 5;
            double u2EnvBot = U2Y0 + U2H / 2 + 5;

            // 行程上下限（横梁外缘能到的两个极限位置）
            var u2Limit = new SolidColorBrush(Color.FromArgb(0x8C, 0x7C, 0x4F, 0xD0));
            foreach (double u2Ly in new[] { u2EnvTop, u2EnvBot })
            {
                cv.Items.Add(new RectItem
                {
                    X = U2X0 - 4, Y = u2Ly, W = (U2X1 - U2X0) + 8, H = 1.4, Fill = u2Limit,
                });
            }

            // Y 行程标尺（画在横梁左端外侧，x 667..679；原来这里是为了避开左边的标注框，
            // 标注框已在需求 35 删除，标尺位置保持不变）
            cv.Items.Add(new RectItem
            {
                X = 672, Y = u2EnvTop + 3, W = 2, H = u2EnvBot - u2EnvTop - 6, Fill = u2VioletSoft,
            });
            cv.Items.Add(new PolyItem
            {
                Points = new PointCollection
                {
                    new Point(673, u2EnvTop), new Point(667, u2EnvTop + 9), new Point(679, u2EnvTop + 9),
                },
                Fill = u2Violet,
            });
            cv.Items.Add(new PolyItem
            {
                Points = new PointCollection
                {
                    new Point(673, u2EnvBot), new Point(667, u2EnvBot - 9), new Point(679, u2EnvBot - 9),
                },
                Fill = u2Violet,
            });

            // X 横梁（上料手臂本体）
            var u2Beam = new RectItem
            {
                X = U2X0, Y = U2CY - U2H / 2, W = U2X1 - U2X0, H = U2H, Radius = 5,
                Fill = new SolidColorBrush(Color.FromArgb(0x7A, 0x2A, 0x18, 0x4A)),
                Stroke = u2Violet, StrokeThickness = 2,
            };
            cv.Items.Add(u2Beam);

            // 横梁上的两个绿色滑块（真机照片里就有，跟着横梁一起走）
            var u2GreenA = new RectItem
            {
                X = U2GA0, Y = U2CY - U2H / 2 + 3, W = U2GA1 - U2GA0, H = U2H - 6, Radius = 3,
                Fill = u2GreenBrush,
            };
            cv.Items.Add(u2GreenA);
            var u2GreenB = new RectItem
            {
                X = U2GB0, Y = U2CY - U2H / 2 + 3, W = U2GB1 - U2GB0, H = U2H - 6, Radius = 3,
                Fill = u2GreenBrush,
            };
            cv.Items.Add(u2GreenB);

            // 红色吸嘴 2×4 组合
            var u2Nozzle = new RectItem
            {
                X = U2NX0, Y = U2CY - U2NH / 2, W = U2NW, H = U2NH, Radius = 5,
                Fill = u2Red, Stroke = u2RedEdge, StrokeThickness = 2,
            };
            cv.Items.Add(u2Nozzle);

            var u2Dots = new List<EllipseItem>();
            double u2DotD = Math.Min(U2NW / U2NC, U2NH / U2NR) * 0.60;
            for (int r = 0; r < U2NR; r++)
            {
                for (int q = 0; q < U2NC; q++)
                {
                    var dot = new EllipseItem
                    {
                        X = U2NX0 + U2NW * (q + 0.5) / U2NC - u2DotD / 2,
                        Y = U2CY - U2NH / 2 + U2NH * (r + 0.5) / U2NR - u2DotD / 2,
                        W = u2DotD, H = u2DotD, Fill = u2DotBrush,
                    };
                    cv.Items.Add(dot);
                    u2Dots.Add(dot);
                }
            }

            // ---- 「上料X轴」标注框：已按用户要求整块删除 ----
            // 原来是 236×98 的半透明白底标注框（x 430..666 / y 654..752）+ 标题 +
            // 三行说明 + 红色图例小方块。撤掉之后这一片照片不再被白底盖住，
            // 上料横梁与红色吸嘴组看得很清楚，不再需要图例指代。

            // ---- 画布署名水印：已按用户要求从画布上撤掉 ----
            // 原来画布左下角有一行「温启志 18719361399 · 整机俯视图仿真（…）」。
            // 画布不留文字之后这里不再画字；署名仍保留在本文件头部注释、类名注释
            // 以及右侧说明栏的页脚里（温启志 18719361399）。

            // ---- 逐帧动画：横移臂在导轨上往返，最近的料盘随之点亮 ----
            long last = Environment.TickCount64;
            double t = 0;

            cv.Draw = c =>
            {
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;

                // 只有点了「启动演示节拍」才推进；否则保持静态初态
                if (MotionManager.Instance.AutoCycle) t += dt;

                // 臂头：0..1 三角波映射到导轨两端
                double headX = armX0 + Triangle(t * 0.11) * (armX1 - armX0);
                armHead.X = headX;

                // ---- 下料X手臂：X 横梁上下平移（Y） ＋ 吸嘴沿横梁左右走（X） ----
                // ① 横梁整体 Y 平移：从照片里的静止位（行程上端）单向下行到出料盘上方。
                //    相位不加偏移 ⇒ t = 0 时横梁正好压在真机导轨上，静止状态与照片完全重合。
                double uaCYn = UAY0 + Triangle(t * 0.13) * (UAY1 - UAY0);
                uaBeam.Y = uaCYn - UAH / 2;
                for (int k = 0; k < uaGreenSliders.Count; k++)
                    uaGreenSliders[k].Y = uaCYn - UAH / 2 + 3;

                // ② 吸嘴组合沿横梁 X 向走位（另一条相位，避免和 Y 同步显得很假）
                double uaNx = UANX0 + Triangle(t * 0.09 + 0.22) * (UANX1 - UANX0);
                uaNozzle.X = uaNx;
                uaNozzle.Y = uaCYn - UANH / 2;

                // 8 个吸嘴点位跟着红色方块走（2 行 × 4 列）
                for (int r = 0; r < UANR; r++)
                {
                    // 注意：外层 cv.Draw 的 lambda 形参就叫 c，这里不能再叫 c（CS0136）
                    for (int q = 0; q < UANC; q++)
                    {
                        var dot = uaDots[r * UANC + q];
                        dot.X = uaNx + UANW * (q + 0.5) / UANC - dot.W / 2;
                        dot.Y = uaCYn - UANH / 2 + UANH * (r + 0.5) / UANR - dot.H / 2;
                    }
                }

                // 吸嘴中心离哪张出料盘**最近**，就点亮哪张（琥珀色描边）。
                // 早先用的是「中心落在 ±8 判定窗内就点亮」，可是吸嘴组合有 70px 宽、
                // 出料盘才 84px 宽，中心一旦落在两张盘的公共边上，**两张会同时点亮**，
                // 看着像画错了。改成「最近的那张独占点亮」，一次只有一张，而且逐张推进。
                double uaNcx = uaNx + UANW / 2;
                int uaBest = -1;
                double uaBestD = double.MaxValue;
                for (int i = 0; i < uaHilite.Count; i++)
                {
                    double uaCx = (uaTrayX[i * 2] + uaTrayX[i * 2 + 1]) / 2.0;
                    double uaD = Math.Abs(uaNcx - uaCx);
                    if (uaD < uaBestD) { uaBestD = uaD; uaBest = i; }
                }
                for (int i = 0; i < uaHilite.Count; i++)
                    uaHilite[i].Stroke = (i == uaBest) ? uaAmberOn : uaAmberOff;

                // ---- 上料X轴：横梁上下平移（Y） ＋ 红色吸嘴组沿横梁左右走（X） ----
                // ① 横梁 Y 平移。相位不加偏移 ⇒ t = 0 时三角波为 0，横梁停在**行程下端**
                //    （= 照片里的静止位），静止状态与照片完全重合 —— 和下料同一套约定，
                //    只是方向相反：下料从静止位往下走，上料从静止位往上走。
                //    行程长度 = (U2Y0 - U2Y1) = 下料的 (UAY1 - UAY0)，两根轴等长。
                double u2CYn = U2Y0 - Triangle(t * 0.15) * (U2Y0 - U2Y1);
                u2Beam.Y = u2CYn - U2H / 2;
                u2GreenA.Y = u2CYn - U2H / 2 + 3;
                u2GreenB.Y = u2CYn - U2H / 2 + 3;

                // ② 红色吸嘴组沿横梁 X 向走位。
                //    照片静止位：吸嘴左端 photo x1297 ⇒ 画布 772.39，
                //    在行程里的比例 = (772.39-694.9)/(843.3-694.9) = 0.522。
                //    三角波相位要写成「比例的一半」= 0.261（上升沿，tri(0.261)=0.522），
                //    不能直接把比例 0.522 当相位 —— 那是下降沿上的另一个点，
                //    tri(0.522)=0.956 会把吸嘴送到 836.8，静止位对不上照片。
                //    （下料手臂用的是同一套算法：比例 0.441 ⇒ 相位 0.22。）
                double u2Nx = U2NX0 + Triangle(t * 0.085 + 0.261) * (U2NX1 - U2NX0);
                u2Nozzle.X = u2Nx;
                u2Nozzle.Y = u2CYn - U2NH / 2;

                // 8 个吸嘴点位跟着红色方块走（2 行 × 4 列）
                for (int r = 0; r < U2NR; r++)
                {
                    for (int q = 0; q < U2NC; q++)
                    {
                        var dot = u2Dots[r * U2NC + q];
                        dot.X = u2Nx + U2NW * (q + 0.5) / U2NC - dot.W / 2;
                        dot.Y = u2CYn - U2NH / 2 + U2NH * (r + 0.5) / U2NR - dot.H / 2;
                    }
                }

                // 离臂头最近的料盘点亮（超过 90px 就都不亮）
                int best = -1;
                double bestD = double.MaxValue;
                for (int i = 0; i < TrayPlan.Length; i++)
                {
                    double cx = (TrayPlan[i].U + TrayPlan[i].DU / 2) * CW;
                    double d = Math.Abs(cx - (headX + 23));
                    if (d < bestD) { bestD = d; best = i; }
                }
                for (int i = 0; i < trayHilite.Count; i++)
                {
                    bool on = i == best && bestD < 90;
                    ((SolidColorBrush)trayHilite[i].Fill).Color =
                        on ? Color.FromArgb(70, 0xFF, 0xC0, 0x40) : Colors.Transparent;
                    ((SolidColorBrush)trayHilite[i].Stroke).Color =
                        on ? Color.FromArgb(255, 0xFF, 0xA8, 0x20) : Color.FromArgb(0, 0xFF, 0xC0, 0x40);
                }

                // ---- 格子里的物料变化 ----
                // 每个料盘按自己的相位在「满 / 空」之间循环，逐格地取走或累积：
                //   · 上排 6 个 = 待测盘：满 → 空（物料被逐格取走）
                //   · 中排 5 个 = 缓存盘：空 → 满 → 空
                //   · 左下 3 个 = 出料盘：空 → 满（逐格累积已测品）
                // 填充顺序统一「从上到下、从左到右」，所以看起来是从下往上被清空的。
                // 只有真的发生变化的格子才会触发属性通知（Set 里做了相等判断），
                // 所以每帧实际刷新的图元只有一两个，不会拖慢界面。
                for (int i = 0; i < trayDevices.Count; i++)
                {
                    var devs = trayDevices[i];
                    int n = devs.Count;
                    if (n == 0) continue;

                    bool green = TrayPlan[i].Green;
                    var devBrush = green ? devGreen : devBlue;
                    double k = Triangle(t * 0.08 + i * 0.19);
                    int target = i < 6
                        ? (int)Math.Round(n * (1.0 - k))   // 待测盘：满 → 空
                        : (int)Math.Round(n * k);          // 缓存盘 / 出料盘：空 → 满

                    for (int j = 0; j < n; j++)
                        devs[j].Fill = j < target ? devBrush : null;
                }

                // ---- 加热盘上的芯片刷新 ----
                // 斜向扫过的波：同一时刻约一半格子有芯片，形成一条从左下往右上走的料带，
                // 看起来就是一颗颗芯片被放上加热盘、加热完再被取走。
                for (int hp = 0; hp < heatChips.Count; hp++)
                {
                    var chips = heatChips[hp];
                    for (int r = 0; r < HP_ROWS; r++)
                    {
                        // 注意：外层 cv.Draw 的 lambda 形参就叫 c，这里不能再叫 c（CS0136）
                        for (int cc = 0; cc < HP_COLS; cc++)
                        {
                            int j = r * HP_COLS + cc;
                            if (j >= chips.Count) break;
                            double ph = t * 0.55 + hp * 0.5 - (r + cc) * 0.13;
                            chips[j].Fill = Triangle(ph) > 0.5 ? heatChip : null;
                        }
                    }
                }
            };

            // 先画一帧，保证静态截图 / 首次显示就有内容（不依赖快速通道）
            cv.Refresh();
            return cv;
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>始终为正的取模。</summary>
        private static double Mod(double a, double b)
        {
            if (b <= 0) return 0;
            double r = a % b;
            return r < 0 ? r + b : r;
        }

        /// <summary>0..1 三角波（周期 1）。</summary>
        private static double Triangle(double x)
        {
            double p = Mod(x, 1.0);
            return p < 0.5 ? p * 2 : (1 - p) * 2;
        }

        /// <summary>两色线性插值（t = 0 → a，t = 1 → b）。</summary>
        private static Brush LerpBrush(Brush a, Brush b, double t)
        {
            t = Math.Min(1, Math.Max(0, t));
            var ca = ((SolidColorBrush)a).Color;
            var cb = ((SolidColorBrush)b).Color;
            return new SolidColorBrush(Color.FromRgb(
                (byte)(ca.R + (cb.R - ca.R) * t),
                (byte)(ca.G + (cb.G - ca.G) * t),
                (byte)(ca.B + (cb.B - ca.B) * t)));
        }

    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------