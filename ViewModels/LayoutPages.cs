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
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // ==================================================================
        //  页面
        // ==================================================================

        /// <summary>整机模块平面仿真页（motion_layout）。</summary>
        public static Element LayoutPage()
        {
            var canvas = BuildLayoutCanvas();

            return E.V(
                E.H(
                    E.T("三温标准机 · 模块平面仿真", 14.5, E.P, true),
                    E.SpacerW(14),
                    E.T("（11 个模组 · 动作随运动控制卡实时变化）", 11, E.Sec)),

                // ---- 操作条 ----
                E.Band("仿真节拍控制", Palette.BandBlue,
                    E.H(
                        E.Btn("启动演示节拍", () => MotionManager.Instance.StartDemo(), "PrimaryButton", 12),
                        E.Btn("停止演示", () => MotionManager.Instance.StopDemo(), "PrimaryButton", 12),
                        E.SpacerW(10),
                        E.Btn("急停", () => MotionManager.Instance.TriggerEStop(), "DangerButton", 12),
                        E.Btn("急停复位", () => MotionManager.Instance.ResetEStop(), "PrimaryButton", 12),
                        E.SpacerW(10),
                        E.Btn("全部回零", () => MotionManager.Instance.HomeAll(), "PrimaryButton", 12),
                        E.Btn("全部停止", () => MotionManager.Instance.StopAll(), "PrimaryButton", 12),
                        E.Btn("清除报警", () => MotionManager.Instance.ClearAlarm(), "PrimaryButton", 12)),
                    E.T("本页打开后会自动启动演示节拍，便于观察模组动作；点「停止演示」即回到静态。"
                        + "所有动作都经 SafetyCheck 互锁裁决，越界或干涉指令会被直接拒绝，因此不会撞机。",
                        10.5, E.Sec, false, TextWrapping.Wrap)),

                // ---- 主画布 + 右侧说明栏 ----
                // 整机图本身接近正方形，而页面可用区域是宽屏；
                // 并排放反而能让画布保持足够大，又不浪费右边的空白。
                E.H(
                    canvas,
                    E.SpacerW(20),
                    E.V(
                        // ---- 命令中心（生产控制 + 批次管理）从主界面搬到本仿真页右侧 ----
                        HomePages.CommandCenter(),
                        E.Spacer(10),
                        E.T("图例", 12, E.P, true),
                        BuildLegend(),
                        E.Spacer(10),
                        E.T("模组动作对照（每个方框内都能看到动的零件）", 12, E.P, true),
                        BuildModuleTable())),

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
            // ---- 原图 540×438 → 画布 952×728 ----
            // 机台外框（原图 66..472 × 62..396）换算后落在 70..882 × 30..698，
            // 四周各留一点边距放读数条与标尺。
            const double K = 2.0;      // 缩放
            const double OX = -62.0;   // 水平偏移
            const double OY = -94.0;   // 垂直偏移
            const double CW = 952;
            const double CH = 728;

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
            bool demoKicked = false;
            int kickDelay = 0;

            cv.Draw = c =>
            {
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;
                t += dt;

                var m = MotionManager.Instance;

                // 打开本页后自动启动安全演示节拍（等约 1 秒让界面先画出来）
                if (!demoKicked)
                {
                    kickDelay++;
                    if (kickDelay > 20)
                    {
                        demoKicked = true;
                        if (!m.EStop && !m.AnyAlarm() && !m.AutoCycle) m.StartDemo();
                    }
                }

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