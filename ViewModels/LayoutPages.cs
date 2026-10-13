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
//        署名水印全部撤除。现在画布上只剩「真机照片 + 料盘 / 加热盘 / 手臂」的
//        图形与动画，一句字都没有；所有文字说明统一收到右侧说明栏（见 LayoutPage()）。
//        俯视图里的三根手臂全部用**真机照片抠图**绘制（温启志 18719361399）：
//          · 料盘搬运臂  image/tray_arm.png        （只有 X 一个自由度，动画里只改 X）
//          · 下料手臂    image/unload_arm_head.png （红色滑座沿横梁走 = 机器 X）
//          · 上料手臂    image/load_arm_head.png   （同上）
//        另外两根**蓝色横梁**也是照片抠图（image/beam_down.png / image/beam_up.png）——
//        用户明确：「蓝色的可以带动红色的 Y 移动」「整根蓝色横梁上下移动」：
//          · 横梁做 Y（俯视图里上下）平移，红色滑座跟着梁一起升降；
//          · 红色滑座自己仍沿梁左右走（X）；两根梁用不同相位，画面不整齐划一；
//          · 每个红滑座上面画了 **2×4 吸嘴组合**（沿梁 4 个 × 垂直 2 行 = 8 个）。
//        底图相应换成抹掉这三根臂 + 两根梁的 image/handler_top_clean.png —— 原位已用
//        梁外侧确认为纯底板的那一行（下料梁取上方 y=812、上料梁取下方 y=1292，都做中位
//        滤波 + 中位钳位）按行补平，横梁升降时原地不会留下不动的重影。
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

            // 上排 6 个深盘改成「叠层」表示层数后的图例（温启志 18719361399）
            var legendLayer = new SolidColorBrush(Color.FromRgb(0xC9, 0xCE, 0xD6));
            var legendLayerWarn = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x00));
            // 三根手臂现在都是真机照片抠图（上料 / 下料 / 料盘搬运），统一用灰蓝色标注。
            // 原来「上料臂 = 蓝方块 / 下料臂 = 橙方块」那张是 2D 合成图的图例，已随抠图改造撤掉。
            var legendPhoto = new SolidColorBrush(Color.FromRgb(0x6E, 0x86, 0xA8));

            return E.Wrap(
                Dot(legendPhoto, "上料 / 下料手臂（真机照片抠图）"),
                Dot(legendPhoto, "料盘搬运臂（真机照片抠图）"),
                Dot(Green, "小车 A"),
                Dot(GreenDark, "小车 B"),
                Dot(Purple, "Z 轴 / 压头"),
                Dot(Red, "气缸 / 加热 / 报警"),
                Dot(Steel, "料枪 / 机械件"),
                Dot(TrayBlack, "黑盘"),
                Dot(TrayColor, "彩盘"),
                Dot(TrayWhite, "白盘"),
                Dot(legendLayer, "上排料盘层数（叠层，最多 20 层）"),
                Dot(legendLayerWarn, "层数不足 3 层：需增加料盘"));
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

            // 上排料盘堆叠层数的数据源（温启志 18719361399）：静态时读它，
            // 演示节拍里逐帧回写，保证「俯视图」与其它页面看到的是同一份层数。
            var mm = MachineModel.Instance;

            // ---- 底图：优先用「已抹掉搬运臂」的修补版 image/handler_top_clean.png ----
            // 温启志 18719361399：搬运臂单独抠成 image/tray_arm.png 后，底图上原来的
            // 位置必须先抹平，否则手臂一走位就会在原地留下一个重影。找不到修补版时
            // 退回原始照片（此时手臂留在底图上不动，功能不中断）。
            ImageBrush bgBrush = new ImageBrush();
            foreach (var bgUri in new[]
                     {
                         "pack://siteoforigin:,,,/image/handler_top_clean.png",
                         "pack://siteoforigin:,,,/image/handler_top.jpg",
                     })
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(bgUri);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    bgBrush = new ImageBrush(bmp) { Stretch = Stretch.Fill };
                    break;
                }
                catch
                {
                    // 换下一个候选底图
                }
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
                try { MachineModel.Instance.Log("警告", "整机俯视图底图（handler_top_clean.png / handler_top.jpg）加载失败，画布以深灰底代替"); }
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

            // ---- 料盘堆叠层数配色（温启志 18719361399）----------------------------
            // 上排 6 个深盘不再画格子，改成「一摞薄盘」表示层数：正常 = 灰白薄盘，
            // 层数 < MinTrayAlarmLayers 时整摞与文字转黄，并在盘内写「需增加料盘」。
            var layerWell = new SolidColorBrush(Color.FromRgb(0x12, 0x14, 0x18));      // 盘内槽底
            var layerPlate = new SolidColorBrush(Color.FromRgb(0xC9, 0xCE, 0xD6));     // 正常层（灰白薄盘）
            var layerWarnPlate = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x00)); // 报警层（黄）
            var layerCountInk = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0));  // 层数文字
            var layerWarnInk = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x00));   // 报警文字（黄）
            var layerWarnWash = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xC0, 0x00));
            var layerClear = new SolidColorBrush(Colors.Transparent);

            // 每个料盘的「取放高亮」底板（平时全透明，轮到它被取放时点亮）
            var trayHilite = new List<RectItem>();
            // 每个料盘的物料格列表（按「从上到下、从左到右」排好序，动画里逐格增删）
            var trayDevices = new List<List<RectItem>>();
            // 以下 4 个列表与 TrayPlan 一一对齐（下标 = 料盘序号）：
            // 上排 6 个深盘放叠层数据，其余盘放 null（它们走原来的格子 + 物料路径）。
            var trayLayers = new List<List<RectItem>>();   // 叠层图元（下标 0 = 最底层）
            var trayLayerLabel = new List<TextItem>();     // 盘内层数文字
            var trayLayerWarn = new List<TextItem>();      // 盘内「需增加料盘」
            var trayLayerWash = new List<RectItem>();      // 报警黄色底光

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

                // ③ 上排 6 个深盘 = 料盘堆叠：**不画格子**，改画「叠层」表示层数。
                //    温启志 18719361399 ·「这里不需要显示格子，改成多少层的料盘就好了，
                //    最多 20 层料盘，如果少于 3 层就需要黄色提示需要增加料盘。」
                //    · 正常：灰白薄盘按「自下而上」叠 N 片（N = 当前层数），盘内上方写「N 层」；
                //    · N < MinTrayAlarmLayers：整摞与数字转黄，盘内再加一行「需增加料盘」。
                //    其余 8 个盘（中排 5 缓存盘 / 左下 3 出料盘）仍走原来的「格子 + 物料」路径。
                if (i < 6 && !tp.Green)
                {
                    const double pad = 5;      // 盘内留边
                    const double bandH = 30;   // 顶部文字带（层数 + 报警两行）
                    double wellX = x + pad, wellW = w - pad * 2;
                    // 盘内槽底
                    cv.Items.Add(new RectItem
                    {
                        X = wellX, Y = y + pad, W = wellW, H = h - pad * 2,
                        Radius = 2, Fill = layerWell, Stroke = null, StrokeThickness = 0,
                    });
                    // 报警黄色底光（压在叠层之下，正常时全透明）
                    var wash = new RectItem
                    {
                        X = wellX, Y = y + pad, W = wellW, H = h - pad * 2,
                        Radius = 2, Fill = layerClear, Stroke = null, StrokeThickness = 0,
                    };
                    cv.Items.Add(wash);

                    double areaTop = y + pad + bandH;               // 叠层区顶
                    double areaH = (y + h - pad) - areaTop;         // 叠层区高
                    double step = areaH / MachineModel.MaxTrayLayers;
                    double plateH = Math.Max(2.6, step - 1.6);
                    double plateW = wellW - 6;
                    var plates = new List<RectItem>();
                    for (int j = 0; j < MachineModel.MaxTrayLayers; j++)
                    {
                        // j = 0 是最底层（贴盘底），往上依次叠 —— 层数少时只有底部几片亮，
                        // 一眼就能看出「这摞盘快用完了」。
                        var plate = new RectItem
                        {
                            X = wellX + 3, Y = y + h - pad - plateH - j * step,
                            W = plateW, H = plateH,
                            Radius = 1.2, Fill = layerPlate, Stroke = null, StrokeThickness = 0,
                        };
                        cv.Items.Add(plate);
                        plates.Add(plate);
                    }
                    // 层数文字（盘内顶部居中）
                    var label = new TextItem
                    {
                        Text = "", X = wellX, Y = y + pad + 2, TextSize = 11,
                        Foreground = layerCountInk, TextWidth = wellW, Align = TextAlignment.Center,
                    };
                    cv.Items.Add(label);
                    // 「需增加料盘」黄色提示（层数不足时才写字，否则留空）
                    var warn = new TextItem
                    {
                        Text = "", X = wellX, Y = y + pad + 16, TextSize = 9.5,
                        Foreground = layerWarnInk, TextWidth = wellW, Align = TextAlignment.Center,
                    };
                    cv.Items.Add(warn);

                    // 四个对齐列表都补位：叠层盘写数据、物料格留 null
                    trayDevices.Add(null);
                    trayLayers.Add(plates);
                    trayLayerLabel.Add(label);
                    trayLayerWarn.Add(warn);
                    trayLayerWash.Add(wash);
                    continue;
                }
                // 非叠层盘：叠层相关的四个列表留 null，保证下标与 TrayPlan 一一对齐
                trayLayers.Add(null);
                trayLayerLabel.Add(null);
                trayLayerWarn.Add(null);
                trayLayerWash.Add(null);

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

            // ---- 料盘搬运臂（真机照片抠图）：只能沿 X 走位，走到哪个盘就点亮哪个盘 ----
            // 温启志 18719361399 ·「这个搬运料盘的手臂，只能 x 移动，你需要抠图」
            // 该模组在真机上吊在这条横导轨下面（照片 y 424 起、x 1166..1342），运动学
            // 只有 X 一个自由度，所以这里把它从整机俯视图里**抠**成透明底图
            // image/tray_arm.png（176×386 px），逐帧**只改 X、Y 恒定**。
            // 底图同步换成抹掉该模组的 image/handler_top_clean.png —— 否则手臂一走位，
            // 原地会留下一个不动的重影。
            const double armY = 240;                       // 导轨中心线（上排盘底 215 / 中排盘顶 292 之间）
            const double armX0 = 120, armX1 = 874;
            cv.Items.Add(new RectItem
            {
                X = armX0, Y = armY - 4, W = armX1 - armX0, H = 8, Radius = 4,
                Fill = new SolidColorBrush(Color.FromRgb(0x2B, 0x4C, 0x8A)),
            });

            // 抠图手臂的画布几何：照片框 (1166,424)-(1342,810) × (0.59552, 0.59520)
            const double armW = 104.8;      // 手臂宽（照片 176 px）
            const double armH = 229.7;      // 手臂高（照片 386 px）
            // 需求 40（用户：「抓取料盘手臂的默认位置应该靠左2个料盘宽度」）：
            // 默认停放位不再是照片原位 694.4，而是靠左 2 个料盘宽度。
            // 一个料盘宽度 = TrayPlan 深盘 DU 均值 0.08126 × CW 1010 ≈ 82.1 px。
            const double TRAY_W = 82.1;                    // 一个料盘宽度（画布 px）
            const double armRestX = 694.4 - 2 * TRAY_W;    // = 530.2，静态首屏就停这儿
            const double armRestY = 252.4;  // 照片原位 y（只能 X 移动 ⇒ Y 永远是这个值）
            // 抠图加载失败时退回原来的蓝色方块标记，画面不会空一块。
            Brush armBrush = new SolidColorBrush(Color.FromArgb(230, 0x2F, 0x7C, 0xD6));
            try
            {
                var ab = new BitmapImage();
                ab.BeginInit();
                ab.UriSource = new Uri("pack://siteoforigin:,,,/image/tray_arm.png");
                ab.CacheOption = BitmapCacheOption.OnLoad;
                ab.EndInit();
                ab.Freeze();
                armBrush = new ImageBrush(ab) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持蓝色方块兜底
            }
            var trayArm = new RectItem
            {
                X = armRestX, Y = armRestY, W = armW, H = armH,
                Fill = armBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(trayArm);

            // ==============================================================
            //  下料X手臂（真机照片抠图：红色滑座 + 上下蓝导杆 + 白色法兰）
            // ==============================================================
            // 温启志 18719361399 ·「下料手臂和上料手臂，你也需要抠图」
            // 抠图来源：真机俯视图 image/handler_top.jpg。红色滑座在照片 x376..493 / y822..876，
            // 上下各两根蓝色导杆 + 白色法兰 + 左下白夹头跟滑座连成一体，一并抠成
            // image/unload_arm_head.png（RGBA 190×192，照片框 x334..524 / y750..942）。
            // 底图同步换成 image/handler_top_clean.png —— 该臂原位已用「同一批 y 行上的干净
            // 底板 / 干净导轨」按行补平（左右镜像平铺），否则滑座一走位，原地会留下一个
            // 不动的红块重影。
            // 运动学（照片印证）：**红色滑座沿横梁左右平移 = 机器 X**。横梁本身这一版不再
            // 单独上下平移 —— 真机照片里横梁上下都是一整片有细节的机台底板，把横梁整体
            // 挪走会露出一大块需要凭空捏造的区域，所以只做 X（详见文件头 motion_layout 说明）。
            const double UA_X0 = 72.0, UA_X1 = 362.0;      // 抠图左沿的 X 行程（红块始终留在梁上）
            const double UA_W = 113.15, UA_H = 114.28;     // 抠图尺寸（画布）：需求 38 避让要按它算脚印
            const double UA_REST = 198.9;                  // 照片原位（静态首屏与真机照片重合）
            const double UA_CX_OFF = 59.8;                 // 红块中心相对抠图左沿的偏移（画布）

            var uaAmberOn = new SolidColorBrush(Color.FromRgb(0xFF, 0xA8, 0x20));
            var uaAmberOff = new SolidColorBrush(Color.FromArgb(0, 0xFF, 0xC0, 0x40));

            // 抠图加载失败时退回一个半透明蓝方块，画面不会空一块（与料盘搬运臂同一套兜底）。
            Brush uaBrush = new SolidColorBrush(Color.FromArgb(0x7A, 0x1B, 0x2A, 0x5A));
            try
            {
                var ub = new BitmapImage();
                ub.BeginInit();
                ub.UriSource = new Uri("pack://siteoforigin:,,,/image/unload_arm_head.png");
                ub.CacheOption = BitmapCacheOption.OnLoad;
                ub.EndInit();
                ub.Freeze();
                uaBrush = new ImageBrush(ub) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持蓝色方块兜底
            }

            // ---- 下料横梁（真机照片抠图）：整根梁做「上下（Y）平移」------------
            // 温启志 18719361399 · 用户明确：「蓝色的可以带动红色的 Y 移动」「整根蓝色横梁上下移动」。
            // image/beam_down.png = 照片框 (142,820)-(796,880)：深蓝导轨 + 上下黄描边 +
            // 两端黄色端头支架 + 两个绿滑块；红滑座那一段已用「同一行的干净导轨列」平铺补平，
            // 所以梁里没有红块 —— 红滑座由单独的 image/unload_arm_head.png 叠在梁上画。
            // 底图上这条带（820..879）已用梁上方 y=812 的纯底板补平，梁升起来时原地露的是底板。
            const double BD_Y = 488.07;                    // 静止时梁顶的画布 y（t = 0 与真机照片重合）
            Brush bdBrush = new SolidColorBrush(Color.FromArgb(0x7A, 0x1B, 0x2A, 0x5A));
            try
            {
                var bb = new BitmapImage();
                bb.BeginInit();
                bb.UriSource = new Uri("pack://siteoforigin:,,,/image/beam_down.png");
                bb.CacheOption = BitmapCacheOption.OnLoad;
                bb.EndInit();
                bb.Freeze();
                bdBrush = new ImageBrush(bb) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持深蓝方块兜底
            }
            // ---- 下料侧「红色滑座」（真机照片抠图）：与下料横梁一起沿竖直导轨升降 ----
            // 温启志 18719361399 · 用户指出「红色滑座是横梁的安装座，行程一放大，梁走开就会
            // 跟滑座脱开」，要求把它从底图上抠出来、跟着梁一起走。
            // image/unload_carriage.png = 照片框 (80,664)-(180,1004)（RGBA 101×341）：
            // 红滑座 + 下方白色支架。底图上这一块（连导轨一起）已用**上料侧同一 y 行的导轨列**
            // 补平 —— 两根导轨的绿滑块相位完全相同（照片 y 322/458/592/726），逐行平移
            // 1446px 就能严丝合缝接上，补完是一条完整的导轨（含被红滑座压住的那块绿滑块）。
            // 画布几何 = 照片 × (0.595518, 0.595203)：47.64 / 395.22 / 60.15 / 202.96。
            const double CD_Y = 395.22;                    // 静止时滑座顶的画布 y（t = 0 与照片重合）
            Brush cdBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0x10, 0x14));
            try
            {
                var cbb = new BitmapImage();
                cbb.BeginInit();
                cbb.UriSource = new Uri("pack://siteoforigin:,,,/image/unload_carriage.png");
                cbb.CacheOption = BitmapCacheOption.OnLoad;
                cbb.EndInit();
                cbb.Freeze();
                cdBrush = new ImageBrush(cbb) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持红色方块兜底
            }
            var carriageDown = new RectItem
            {
                X = 47.64, Y = CD_Y, W = 60.15, H = 202.96,
                Fill = cdBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(carriageDown);
            var beamDown = new RectItem
            {
                X = 84.56, Y = BD_Y, W = 389.47, H = 35.71,
                Fill = bdBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(beamDown);

            // 画布几何：照片框 (334,750)-(524,942) × (0.59552, 0.59520)
            const double UA_Y = 446.4;                     // 红滑座静止时的画布 y
            var uaArm = new RectItem
            {
                X = UA_REST, Y = UA_Y, W = UA_W, H = UA_H,
                Fill = uaBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(uaArm);

            // 横梁的 Y 行程不再另画标尺：梁本身就是真机照片，行程靠画面直接看，
            // 再叠一条「能走到哪」的 Y 包络反而会把照片覆盖掉（用户要求画布上不放标注）。
            // 温启志 18719361399

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

            // 原来这里是合成画的「半透明蓝横梁 + 两个绿滑块 + 红色吸嘴 2×4 方块 + 8 个白点」
            // 以及一块 316×100 的白底标注框 —— 现在整根手臂已经是真机照片抠图
            // （image/unload_arm_head.png，见上方），这些合成图元全部撤掉，
            // 让照片这一片恢复完整、也不再出现「照片 + 合成方块」两层重叠的观感。
            // 温启志 18719361399

            // ==============================================================
            //  上料X手臂（真机照片抠图：红色滑座 + 上下蓝导杆 + 白色法兰）
            // ==============================================================
            // 温启志 18719361399 · 与下料臂同一套做法。
            // 定位方法：把用户给的截图当模板，在整张真机俯视图上做**多尺度归一化互相关**：
            //   上料臂截图 → photo (1145,1201)、缩放 1.12×、NCC 0.952
            //   下料臂截图 → photo (158, 823)、缩放 1.12×、NCC 0.968
            // （两张同源截图缩放一致，互证可信。）
            // 抠图：红色滑座在照片 x1300..1411 / y1204..1257，上下导杆 + 白法兰一并抠成
            // image/load_arm_head.png（RGBA 142×174，照片框 x1282..1424 / y1144..1318）。
            // 运动学：红色滑座沿横梁左右平移 = 机器 X；横梁本体另有 Y（上下）平移，见下方
            //         image/beam_up.png —— 与下料梁同一套做法。
            // 画布几何 = 照片 × (0.59552, 0.59520)：763.46 / 680.91 / 84.56 / 103.56。
            const double U2_X0 = 674.1, U2_X1 = 845.6;     // 抠图左沿的 X 行程（红块始终留在梁上）
            const double U2_W = 84.56, U2_H = 103.56;      // 抠图尺寸（画布）：需求 38 避让要按它算脚印
            const double U2_REST = 763.5;                  // 照片原位（静态首屏与真机照片重合）
            const double U2_Y = 680.9;                     // 红滑座静止时的画布 y

            // 抠图加载失败时退回一个半透明紫方块兜底（上料原先用紫色系，便于区分两根轴）。
            Brush u2Brush = new SolidColorBrush(Color.FromArgb(0x7A, 0x7C, 0x4F, 0xD0));
            try
            {
                var ub2 = new BitmapImage();
                ub2.BeginInit();
                ub2.UriSource = new Uri("pack://siteoforigin:,,,/image/load_arm_head.png");
                ub2.CacheOption = BitmapCacheOption.OnLoad;
                ub2.EndInit();
                ub2.Freeze();
                u2Brush = new ImageBrush(ub2) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持紫色方块兜底
            }

            // ---- 上料横梁（真机照片抠图）：与下料梁同一套，整根梁做上下（Y）平移 ------
            // 温启志 18719361399 · image/beam_up.png = 照片框 (1141,1201)-(1555,1263)：
            // 深蓝导轨 + 上下黄描边 + 两端黄色端头支架 + 两个绿滑块（右侧一个偏大）；
            // 红滑座那一段同样用「同一行的干净导轨列」补平成导轨。
            // 底图上这条带（1201..1262）用梁下方 y=1292 的纯底板补平。
            // 画布 (679.49, 714.84, 246.54, 36.90) = 照片 × (0.595518, 0.595203)。
            const double BU_Y = 714.84;                    // 静止时梁顶的画布 y
            Brush buBrush = new SolidColorBrush(Color.FromArgb(0x7A, 0x1B, 0x2A, 0x5A));
            try
            {
                var bu = new BitmapImage();
                bu.BeginInit();
                bu.UriSource = new Uri("pack://siteoforigin:,,,/image/beam_up.png");
                bu.CacheOption = BitmapCacheOption.OnLoad;
                bu.EndInit();
                bu.Freeze();
                buBrush = new ImageBrush(bu) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持深蓝方块兜底
            }
            // ---- 上料侧「红色滑座」（真机照片抠图）：与上料横梁一起升降 ----
            // 温启志 18719361399 · 与下料侧同一套做法。
            // image/load_carriage.png = 照片框 (1519,1042)-(1621,1318)（RGBA 103×277）：
            // 红滑座 + 下方白色支架；底图同一块已用下料侧同一 y 行的导轨列补平。
            // 画布几何 = 照片 × (0.595518, 0.595203)：904.59 / 620.20 / 61.34 / 164.87。
            const double CU_Y = 620.20;                    // 静止时滑座顶的画布 y
            Brush cuBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0x10, 0x14));
            try
            {
                var cub = new BitmapImage();
                cub.BeginInit();
                cub.UriSource = new Uri("pack://siteoforigin:,,,/image/load_carriage.png");
                cub.CacheOption = BitmapCacheOption.OnLoad;
                cub.EndInit();
                cub.Freeze();
                cuBrush = new ImageBrush(cub) { Stretch = Stretch.Fill };
            }
            catch
            {
                // 保持红色方块兜底
            }
            var carriageUp = new RectItem
            {
                X = 904.59, Y = CU_Y, W = 61.34, H = 164.87,
                Fill = cuBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(carriageUp);
            var beamUp = new RectItem
            {
                X = 679.49, Y = BU_Y, W = 246.54, H = 36.90,
                Fill = buBrush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(beamUp);

            var u2Arm = new RectItem
            {
                X = U2_REST, Y = U2_Y, W = U2_W, H = U2_H,
                Fill = u2Brush, Stroke = null, StrokeThickness = 0,
            };
            cv.Items.Add(u2Arm);

            // 原来这里是合成画的「半透明紫横梁 + 两个绿滑块 + 红色吸嘴方块 + 8 个白点」、
            // 两条 Y 行程限位线 + 一个 Y 行程标尺，以及 236×98 的白底标注框 ——
            // 现在整根手臂已是真机照片抠图，这些合成图元全部撤掉。
            // 温启志 18719361399

            // ---- 画布署名水印：已按用户要求从画布上撤掉 ----
            // 原来画布左下角有一行「温启志 18719361399 · 整机俯视图仿真（…）」。
            // 画布不留文字之后这里不再画字；署名仍保留在本文件头部注释、类名注释
            // 以及右侧说明栏的页脚里（温启志 18719361399）。

            // ---- 逐帧动画：横移臂在导轨上往返，最近的料盘随之点亮 ----
            // 温启志 18719361399 · Y 行程按**竖直导轨的实测高度**取。
            // 用户：「这个是下料 y，固定的，高度是下料手臂的 y 移动范围」→ 整根导轨全行程。
            //   下料导轨：照片 y 266..1277 → 画布 y 158.32..760.07，高 601.75。
            //   骑在导轨上的滑座：照片 y 664..1004 → 画布高 202.96。
            //   ⇒ 真正能走的行程 = 导轨高 601.75 − 滑座高 202.96 = 398.79，
            //     滑座顶边正好走到导轨顶端 158.32、底边正好走到导轨底端 760.07，
            //     两头都不越界 —— 这才是「整根导轨全行程」。
            //   静止位（滑座顶 395.22）在行程内偏上：向上 236.89 / 向下 161.90。
            // 上料按下料**同一个行程值**走（用户：「跟下料同一个行程值」），
            // 它的静止位正好在行程下端，所以只向上走。
            // 底图上整条导轨都在（没有需要补的区域），滑座抠走以后露出来的是导轨本体，
            // 不会再像 30.95 那版一样「一动就露出没补过的底板」。
            const double BEAM_STROKE = 398.79;              // 两根梁 / 两个滑座共用的 Y 行程
            const double BD_UP = 236.89;                    // 下料：静止位 → 行程上端（向上）
            const double BD_PHASE = 0.29701;                // = (BD_UP / BEAM_STROKE) / 2，使 t = 0 落在静止位

            // ==============================================================
            //  需求 38 · 三臂分时轮流 + 硬避让（温启志 18719361399）
            // ==============================================================
            // 用户：「搬运料盘手臂需要和上料下料手臂不能冲突了，怕撞击」
            // 画布几何实测：三条臂的扫掠包络在 X 与 Y 两个方向都相交 ——
            //   搬运臂 X[120, 874]  Y[252.4, 482.1]
            //   下料臂 X[ 72, 475.15] Y[209.5, 722.6]
            //   上料臂 X[674.1, 930.16] Y[282.1, 784.5]
            //   ⇒ 真实 2D 重叠 355×230 px（搬运∩下料）与 200×200 px（搬运∩上料）。
            // 靠缩小行程解决不了：两个取放臂都按满行程扫掠时，搬运臂左沿只剩 94 px 窗口。
            // 所以按用户选定的方案做**分时轮流 + 硬避让**两件事：
            //   · 分时轮流：主时钟把一轮切成 3 个等长时隙（各 SLOT_SEC 秒），每个时隙
            //     只动**一组机构**（横梁 Y + 滑座 Y + 手臂 X 绑在一起动），另外两组原地
            //     不动 —— 从时间上杜绝「两组机构同时对穿」。时隙归属：
            //       0 = 下料组（下料横梁 + 下料滑座 + 下料臂 X）
            //       1 = 上料组（上料横梁 + 上料滑座 + 上料臂 X）
            //       2 = 搬运臂 X
            //     横梁必须一起冻结：否则搬运臂的时隙里横梁还在升，会升进搬运臂的高度层。
            //   · 硬避让：搬运臂的 X 每帧被钳位，使它的脚印 [X, X+armW] 与取放臂脚印
            //     留够 ARM_CLR 间隙 —— 但**只在 Y 向高度区间真的相交时**才钳。
            //     静止位（照片）里：下料臂 Y 相交但 X 不碰、上料臂 X 相交但 Y 不碰，
            //     所以一次钳位都不触发 ⇒ 静态首屏与真机照片逐像素重合、启动不瞬移。
            //     这是**兜底的安全保证**：即使分时上出现边界重合，几何上也不会压上去。
            // 间隙换算：画布 1010 px 对应机台横向 ~1205 mm ⇒ 0.838 px/mm，40mm ≈ 34 px。
            // ------------------------------------------------------------------
            // 需求 39（方案 A · 用户：「搬运臂运行时，上/下料臂先回橙色框停放位让出走廊」）
            // 在需求 38 基础上改三件事：
            //   · **每组时隙内相位推进 1.0**（三角波周期 1 ⇒ 一个完整来回），时隙结束
            //     正好回到各自停放位 —— 轮到搬运臂时两根取放臂必定已回停放位让出走廊，
            //     与真机 SR-11「别组都回停放位才允许离位 / 回停放位永远放行」同一套语义。
            //   · 时隙 4s → 8s：速率保持 0.125/s（画面速度不变），8s × 0.125 = 1.0 相位，
            //     一轮 = 3 × 8 = 24s。
            //   · 相位改由 t **直接算出**（不再逐帧累加）：时隙内进度 u ∈ [0,1)，
            //     本组相位 = 初值 + (轮到本组 ? u : 0) —— 时隙边界没有累加误差，
            //     停放位永远精确落位；未启动时 t = 0：两根取放臂在照片原位，
            //     搬运臂在默认停放位 530.2（需求 40，照片原位 694.4 靠左 2 个料盘宽度）。
            // 硬避让随之**按时隙门控**：搬运臂只对「正在干活的那组」让位（slot 0 让下料 /
            // slot 1 让上料）；slot 2 里两组都在停放位 = 走廊已让出，不再钳位，搬运臂
            // 全行程走位（俯视图里停放的下料臂头与搬运臂包络还剩 ~36px 名义相交，
            // 真机上两臂不同高度 —— 与 SR-11「停放位不占走廊」一致）。
            // 搬运臂实际位置 trayX 对目标做**限速跟随**（上限 ≈ 三角波最大扫速）：
            // 让位钳位释放时从让位点平滑滑回停放位，不会瞬移。
            const double ARM_CLR = 34.0;                    // 安全间隙（画布 px，≈40mm）
            const double SLOT_SEC = 8.0;                    // 每个时隙秒数（一轮 = 3 × 8 = 24s）
            long last = Environment.TickCount64;
            double t = 0;
            double trayX = armRestX;                        // 搬运臂实际渲染位置（限速跟随目标）

            cv.Draw = c =>
            {
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;

                // 只有点了「启动演示节拍」才推进；否则保持静态初态。
                // 需求 39：相位由 t 直接算出（见上方说明），不再逐帧累加 ——
                // 每组只在自己的时隙里从初值走一个完整来回（+1.0 相位，三角波周期 1），
                // 时隙结束精确回到停放位，其余时间停在停放位。
                bool running = MotionManager.Instance.AutoCycle;
                if (running) t += dt;
                double cyc = Mod(t, 3.0 * SLOT_SEC);
                int slot = (int)(cyc / SLOT_SEC);
                if (slot > 2) slot = 2;
                double u = (cyc - slot * SLOT_SEC) / SLOT_SEC;  // 时隙内进度 0..1
                // 需求 38 时隙归属：0 = 下料组 / 1 = 上料组 / 2 = 搬运臂。
                double phBeamD = BD_PHASE + (slot == 0 ? u : 0.0);  // 下料横梁/滑座（t = 0 → lift 0）
                double phBeamU = (slot == 1 ? u : 0.0);             // 上料横梁/滑座（t = 0 → lift 0）
                double phDown = 0.2188 + (slot == 0 ? u : 0.0);     // 下料臂（初值 = 照片原位 198.9）
                double phUp = 0.2605 + (slot == 1 ? u : 0.0);       // 上料臂（初值 = 照片原位 763.5）
                double phTray = 0.31593 + (slot == 2 ? u : 0.0);    // 搬运臂（初值 = 默认停放位 530.2，需求 40）

                // 搬运臂的 X 分两块算：① 它自己相位给出的**目标位**；
                // ② 等两个取放臂的 X 算完之后，再统一做硬避让钳位（见下方）。
                // 需求 40：相位 0.31593 ⇒ 未启动时正好停在默认停放位 530.2
                // （= 照片原位 694.4 靠左 2 个料盘宽度）。时隙内走位顺序不变：
                // 先右到 769.2、再左到 120、最后回 530.2。

                // ---- 两根蓝色横梁 + 两个红滑座的 Y 平移（整组沿竖直导轨升降）----
                // 相位初值都取成「t = 0 时 lift = 0」⇒ 静态首屏与真机照片逐像素重合。
                // 需求 38 起改由**时隙门控**推进（见上方），不再直接吃 t —— 这样搬运臂
                // 的时隙里横梁是冻结的，不会升进搬运臂的高度层。
                double beamLiftD = Triangle(phBeamD) * BEAM_STROKE - BD_UP;
                beamDown.Y = BD_Y + beamLiftD;
                carriageDown.Y = CD_Y + beamLiftD;
                double beamLiftU = -Triangle(phBeamU) * BEAM_STROKE;
                beamUp.Y = BU_Y + beamLiftU;
                carriageUp.Y = CU_Y + beamLiftU;

                // ---- 下料X手臂（照片抠图）：红色滑座沿横梁左右走 = 机器 X ----
                // 抠图只改 X；Y 跟着横梁一起升降（红滑座坐在梁上，梁升它就升）。
                // 相位 0.2188 ⇒ t = 0 时 tri = 0.4376，落在行程 72..362 的比例 0.4376 上，
                // 正好是照片原位 198.9，静态首屏与真机照片逐像素重合。
                double uaX = UA_X0 + Triangle(phDown) * (UA_X1 - UA_X0);
                uaArm.X = uaX;
                uaArm.Y = UA_Y + beamLiftD;

                // 吸嘴中心离哪张出料盘**最近**，就点亮哪张（琥珀色描边）。
                // 早先用的是「中心落在 ±8 判定窗内就点亮」，可是吸嘴组合有 70px 宽、
                // 出料盘才 84px 宽，中心一旦落在两张盘的公共边上，**两张会同时点亮**，
                // 看着像画错了。改成「最近的那张独占点亮」，一次只有一张，而且逐张推进。
                // 滑座中心 = 抠图左沿 + UA_CX_OFF（红块中心相对抠图左沿的固定偏移）。
                double uaNcx = uaX + UA_CX_OFF;
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

                // ---- 上料X手臂（照片抠图）：红色滑座沿横梁左右走 = 机器 X ----
                // 与下料同一套：抠图只改 X；Y 跟着上料横梁一起升降。
                // 相位 0.2605 ⇒ t = 0 时 tri = 0.521，正好是照片原位 763.5
                // （行程 674.1..845.6 的比例 0.521），首屏与照片重合。
                // 需求 39：上料组只在上料时隙里走一个完整来回（+1.0 相位），
                // 时隙结束回到停放位 763.5，把走廊让给搬运臂。
                double u2X = U2_X0 + Triangle(phUp) * (U2_X1 - U2_X0);
                u2Arm.X = u2X;
                u2Arm.Y = U2_Y + beamLiftU;

                // ---- 搬运臂（抠图）：**只改 X，Y 不动** + 需求 38/39 硬避让 ----
                // 目标位由自己的相位给出；随后把它的脚印 [X, X+armW] 从**正在干活的
                // 那组**取放臂的「脚印 + ARM_CLR」禁区里推出去 —— 推到离目标更近的
                // 一侧，且只在 Y 向高度区间真的相交时才钳。
                // 需求 39（方案 A）：避让**按时隙门控** —— slot 0 只避下料、slot 1 只避
                // 上料；轮到搬运臂（slot 2）时两组都已回停放位 = 走廊已让出，不再钳位，
                // 搬运臂全行程走位（与真机 SR-11「停放位不占走廊」同语义）。
                // 实际渲染位置 trayX 对目标**限速跟随**（上限 ≈ 三角波最大扫速，自由
                // 扫掠时逐帧贴满目标）：让位钳位释放的突跳被限速成平滑滑移，不会瞬移。
                double trayTarget = armX0 + Triangle(phTray) * (armX1 - armW - armX0);
                double headX = trayTarget;
                if (running)
                {
                    if (slot == 0 && YOverlap(armRestY, armH, UA_Y + beamLiftD, UA_H))
                        headX = PushOut(headX, armW, uaX - ARM_CLR, uaX + UA_W + ARM_CLR,
                                        armX0, armX1 - armW);
                    if (slot == 1 && YOverlap(armRestY, armH, U2_Y + beamLiftU, U2_H))
                        headX = PushOut(headX, armW, u2X - ARM_CLR, u2X + U2_W + ARM_CLR,
                                        armX0, armX1 - armW);
                    double trayV = 2.1 * 0.125 * (armX1 - armW - armX0) * dt;
                    double trayStep = headX - trayX;
                    if (trayStep > trayV) trayStep = trayV;
                    else if (trayStep < -trayV) trayStep = -trayV;
                    trayX += trayStep;
                }
                trayArm.X = trayX;
                headX = trayX;      // 点亮判据按实际渲染位置算

                // 离臂头最近的料盘点亮（超过 90px 就都不亮）
                int best = -1;
                double bestD = double.MaxValue;
                for (int i = 0; i < TrayPlan.Length; i++)
                {
                    double cx = (TrayPlan[i].U + TrayPlan[i].DU / 2) * CW;
                    double d = Math.Abs(cx - (headX + armW / 2));
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

                // ---- 上排 6 个深盘：料盘堆叠层数（叠层图元） ----
                // 温启志 18719361399 · 演示节拍里这一摞盘被逐层取走，见底后补新盘
                // （回到满 20 层），每盘各走各的相位，画面不会整齐划一。
                // 层数 < MinTrayAlarmLayers 时：整摞转黄 + 盘内写「需增加料盘」。
                // 静态（未启动演示节拍）时用模型里的层数，首屏就能看到各盘实际层数与报警。
                for (int i = 0; i < trayLayers.Count; i++)
                {
                    var plates = trayLayers[i];
                    if (plates == null) continue;          // 非叠层盘

                    int layers;
                    if (running)
                    {
                        double p = Mod(t * 0.055 + i * 0.19, 1.0);
                        layers = MachineModel.MaxTrayLayers - (int)(p * MachineModel.MaxTrayLayers);
                        if (layers < 1) layers = 1;
                        mm.TopTrayLayers[i] = layers;      // 回写模型，其它页面可直接读
                    }
                    else
                    {
                        layers = mm.TopTrayLayers[i];
                    }
                    if (layers > MachineModel.MaxTrayLayers) layers = MachineModel.MaxTrayLayers;
                    if (layers < 1) layers = 1;

                    bool warn = layers < MachineModel.MinTrayAlarmLayers;
                    var plateBrush = warn ? layerWarnPlate : layerPlate;
                    for (int j = 0; j < plates.Count; j++)
                        plates[j].Fill = j < layers ? plateBrush : null;   // 只亮到第 layers 片

                    trayLayerWash[i].Fill = warn ? layerWarnWash : layerClear;
                    trayLayerLabel[i].Text = layers + "层";
                    trayLayerLabel[i].Foreground = warn ? layerWarnInk : layerCountInk;
                    trayLayerWarn[i].Text = warn ? "需增加料盘" : "";
                }

                // ---- 格子里的物料变化 ----
                // 每个料盘按自己的相位在「满 / 空」之间循环，逐格地取走或累积：
                //   · 中排 5 个 = 缓存盘：空 → 满 → 空
                //   · 左下 3 个 = 出料盘：空 → 满（逐格累积已测品）
                // 填充顺序统一「从上到下、从左到右」，所以看起来是从下往上被清空的。
                // 只有真的发生变化的格子才会触发属性通知（Set 里做了相等判断），
                // 所以每帧实际刷新的图元只有一两个，不会拖慢界面。
                for (int i = 0; i < trayDevices.Count; i++)
                {
                    var devs = trayDevices[i];
                    if (devs == null) continue;            // 上排 6 深盘已改成叠层，没有物料格
                    int n = devs.Count;
                    if (n == 0) continue;

                    bool green = TrayPlan[i].Green;
                    var devBrush = green ? devGreen : devBlue;
                    double k = Triangle(t * 0.08 + i * 0.19);
                    int target = (int)Math.Round(n * k);   // 缓存盘 / 出料盘：空 → 满

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

        /// <summary>
        /// 需求 38 · 两个 Y 区间 [aY, aY+aH] 与 [bY, bY+bH] 是否相交。
        /// 用来判断「取放臂是否真的升/降到了搬运臂所在的高度层」——
        /// 静止位两根臂高度不同，X 走廊虽然重叠，几何上却不冲突。
        /// </summary>
        private static bool YOverlap(double aY, double aH, double bY, double bH)
            => aY < bY + bH && bY < aY + aH;

        /// <summary>
        /// 需求 38 · 把宽度 w、左沿在 x 的矩形脚印 [x, x+w] 推出禁区 [bl, br]：
        /// 取离当前位更近的一侧推出去，并夹在 [xmin, xmax] 内。
        /// 本来就不相交时原样返回；两侧都推不动时也原样返回（上层兜底，不会出现这种布局）。
        /// </summary>
        private static double PushOut(double x, double w, double bl, double br, double xmin, double xmax)
        {
            if (x + w <= bl || x >= br) return x;      // 不相交，不用动
            double left = bl - w;                       // 推到禁区左边（脚印右沿贴 bl）
            double right = br;                          // 推到禁区右边（脚印左沿贴 br）
            bool okL = left >= xmin;
            bool okR = right <= xmax;
            if (okL && okR) return (x - left <= right - x) ? left : right;
            if (okL) return left;
            if (okR) return right;
            return x;
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