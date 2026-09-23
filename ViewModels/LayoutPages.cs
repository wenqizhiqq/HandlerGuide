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

        /// <summary>彩盘。</summary>
        private static readonly Brush TrayColor = new SolidColorBrush(Color.FromRgb(0x3E, 0x8E, 0xD8));

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

                // ---- 主画布 ----
                canvas,

                // ---- 图例 ----
                BuildLegend(),

                // ---- 模组动作说明 ----
                E.T("模组动作对照（每个方框内都能看到动的零件）", 12, E.P, true),
                BuildModuleTable(),

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
                ("上料模组",     "料盘传送臂（轴 30）",     "料盘升降台在槽位间上下走位，底部顶升气缸伸出"),
                ("下料模组",     "料盘传送臂（轴 30）",     "料盘升降台反向走位，分盘气缸把满盘顶出"),
                ("移盘模组",     "小车 A / B（轴 28 / 29）", "横梁上的夹爪小车左右穿梭，到位后下压取放料盘"),
                ("上料流水线",   "时间驱动（走料节拍）",     "白盘沿导轨向上流动，到位后停一格"),
                ("黑盘流水线",   "时间驱动（走料节拍）",     "黑盘沿导轨向下流动"),
                ("彩盘流水线",   "时间驱动（走料节拍）",     "彩盘沿导轨向下流动"),
                ("下料流水线",   "时间驱动（走料节拍）",     "已测料盘沿导轨向下流出"),
                ("手工盘模组",   "时间驱动（扫描节拍）",     "5×4 手工盘格位逐格点亮，表示人工放料位置"),
                ("压测模组",     "测试位下压 Z1 / Z2（轴 2 / 3）", "压头按真实 Z 位置下压 / 抬起，压到位后保持"),
                ("料枪模组",     "上料取放臂 Y 轴（轴 5）",   "料枪沿 Y 方向伸出 / 缩回，伸出量随轴位置变化"),
                ("加热盘模组",   "加热使能输出（OUT15）",     "加热盘通电极亮并冒热流；未使能时为冷盘青色")
            };

            var list = new List<object>();
            foreach (var r in rows)
            {
                list.Add(E.H(
                    E.T(r.Mod, 11, E.P, true).Wd(96),
                    E.T(r.Driver, 10.5, E.Sec).Wd(210),
                    E.T(r.Motion, 10.5, E.Sec, false, TextWrapping.Wrap).Wd(640)));
            }
            return E.V(list.ToArray());
        }

        // ==================================================================
        //  平面图画布（逐帧重绘）
        // ==================================================================

        /// <summary>构建整机平面仿真画布，并把逐帧重绘逻辑挂到 CanvasElement.Draw 上。</summary>
        private static CanvasElement BuildLayoutCanvas()
        {
            const double CW = 1420;
            const double CH = 700;

            // ---- 机台外框 ----
            const double FrameX = 20, FrameY = 14, FrameW = 1380, FrameH = 668;

            // ---- 左 / 右立柱（跨上下两带） ----
            const double ColY = 36, ColH = 624;
            const double LeftX = 36, LeftW = 92;
            const double RightX = 1290, RightW = 92;
            const double ColInnerTop = ColY + 30;

            // 立柱料盘槽：12 格，竖直排布
            const int ColSlotN = 12;
            const double ColSlotH = 28, ColSlotGap = 11;
            const double ColStep = ColSlotH + ColSlotGap;
            const double ColTotal = ColSlotN * ColStep;

            // ---- 上排流水线 ----
            const double LaneY = 36, LaneH = 240, LaneW = 74;
            const int LaneN = 6;
            const double LaneSlotH = 22, LaneSlotGap = 8;
            const double LaneStep = LaneSlotH + LaneSlotGap;
            const double LaneTotal = LaneN * LaneStep;
            const double LaneInnerTop = LaneY + 30;

            // ---- 中部两条导轨 ----
            const double RailX0 = 146, RailX1 = 1250;
            const double ArmRailY = 294;
            const double CarRailY = 324;

            // ---- 下排 ----
            const double MpX = 146, MpY = 348, MpW = 306, MpH = 312;
            const double MvX = 498, MvY = 36, MvW = 400, MvH = 240;
            const double PtX = 498, PtY = 348, PtW = 400, PtH = 148;
            const double GnX = 498, GnY = 512, GnW = 400, GnH = 148;
            const double HtX = 920, HtY = 348, HtW = 340, HtH = 312;

            var cv = new CanvasElement { CanvasWidth = CW, CanvasHeight = CH };

            // ---------------- 机台外框 ----------------
            cv.Items.Add(new RectItem
            {
                X = FrameX, Y = FrameY, W = FrameW, H = FrameH, Radius = 14,
                Fill = BodyFill, Stroke = BodyStroke, StrokeThickness = 1.5
            });

            // ---------------- 模组框（外框 + 标题） ----------------
            RectItem ModBox(string title, double x, double y, double w, double h)
            {
                var b = new RectItem
                {
                    X = x, Y = y, W = w, H = h, Radius = 8,
                    Fill = ModFill, Stroke = ModStroke, StrokeThickness = 1
                };
                cv.Items.Add(b);
                cv.Items.Add(new TextItem
                {
                    Text = title, X = x, Y = y + 7, TextSize = 11.5, Foreground = Ink,
                    TextWidth = w, Align = TextAlignment.Center
                });
                return b;
            }

            // =============================================================
            //  左立柱：下料模组
            // =============================================================
            ModBox("下料模组", LeftX, ColY, LeftW, ColH);
            var colSlotsL = new RectItem[ColSlotN];
            for (int i = 0; i < ColSlotN; i++)
            {
                colSlotsL[i] = new RectItem
                {
                    X = LeftX + 12, Y = ColInnerTop + i * ColStep, W = LeftW - 24, H = ColSlotH,
                    Radius = 3, Fill = TrayWhite, Stroke = ModStroke, StrokeThickness = 0.5
                };
                cv.Items.Add(colSlotsL[i]);
            }
            var colLiftL = new RectItem
            {
                X = LeftX + 6, Y = ColInnerTop, W = LeftW - 12, H = 11, Radius = 3, Fill = Blue
            };
            cv.Items.Add(colLiftL);
            var colCylL = new RectItem
            {
                X = LeftX + 14, Y = ColY + ColH - 52, W = LeftW - 28, H = 40,
                Radius = 5, Fill = Red, Text = "分盘\n气缸", TextSize = 9.5, TextColor = Brushes.White
            };
            cv.Items.Add(colCylL);

            // =============================================================
            //  右立柱：上料模组
            // =============================================================
            ModBox("上料模组", RightX, ColY, RightW, ColH);
            var colSlotsR = new RectItem[ColSlotN];
            for (int i = 0; i < ColSlotN; i++)
            {
                colSlotsR[i] = new RectItem
                {
                    X = RightX + 12, Y = ColInnerTop + i * ColStep, W = RightW - 24, H = ColSlotH,
                    Radius = 3, Fill = TrayWhite, Stroke = ModStroke, StrokeThickness = 0.5
                };
                cv.Items.Add(colSlotsR[i]);
            }
            var colLiftR = new RectItem
            {
                X = RightX + 6, Y = ColInnerTop, W = RightW - 12, H = 11, Radius = 3, Fill = Blue
            };
            cv.Items.Add(colLiftR);
            var colCylR = new RectItem
            {
                X = RightX + 14, Y = ColY + ColH - 52, W = RightW - 28, H = 40,
                Radius = 5, Fill = Red, Text = "顶升\n气缸", TextSize = 9.5, TextColor = Brushes.White
            };
            cv.Items.Add(colCylR);

            // =============================================================
            //  上排：四条流水线
            // =============================================================
            var lanes = new (string Name, double X, Brush Tray, int Dir)[]
            {
                ("下料流水线", 146, TrayWhite, +1),
                ("彩盘流水线", 234, TrayColor, -1),
                ("黑盘流水线", 322, TrayBlack, -1),
                ("上料流水线", 410, TrayWhite, +1)
            };

            var laneSlots = new List<(RectItem Slot, int Idx, int Dir)>();
            for (int li = 0; li < lanes.Length; li++)
            {
                var lane = lanes[li];
                ModBox(lane.Name, lane.X, LaneY, LaneW, LaneH);

                cv.Items.Add(new RectItem
                {
                    X = lane.X + 8, Y = LaneInnerTop, W = LaneW - 16, H = LaneTotal - LaneSlotGap,
                    Radius = 4, Fill = LaneFill, Stroke = null, StrokeThickness = 0
                });

                for (int i = 0; i < LaneN; i++)
                {
                    var s = new RectItem
                    {
                        X = lane.X + 12, Y = LaneInnerTop + i * LaneStep, W = LaneW - 24, H = LaneSlotH,
                        Radius = 3, Fill = lane.Tray, Stroke = ModStroke, StrokeThickness = 0.5
                    };
                    cv.Items.Add(s);
                    laneSlots.Add((s, i, lane.Dir));
                }
            }

            // =============================================================
            //  移盘模组
            // =============================================================
            ModBox("移盘模组", MvX, MvY, MvW, MvH);

            // 横向导轨
            cv.Items.Add(new RectItem
            {
                X = MvX + 16, Y = MvY + 92, W = MvW - 32, H = 6, Radius = 3, Fill = RailB
            });

            // 两个工位
            const double StW = 84, StH = 60;
            double stLeftX = MvX + 40;
            double stRightX = MvX + MvW - 40 - StW;
            cv.Items.Add(new RectItem
            {
                X = stLeftX, Y = MvY + 158, W = StW, H = StH, Radius = 5,
                Fill = TrayWhite, Stroke = ModStroke, StrokeThickness = 1
            });
            cv.Items.Add(new RectItem
            {
                X = stRightX, Y = MvY + 158, W = StW, H = StH, Radius = 5,
                Fill = TrayWhite, Stroke = ModStroke, StrokeThickness = 1
            });
            cv.Items.Add(new TextItem
            {
                Text = "上料位", X = stLeftX, Y = MvY + 220, TextSize = 9.5, Foreground = Ink2,
                TextWidth = StW, Align = TextAlignment.Center
            });
            cv.Items.Add(new TextItem
            {
                Text = "下料位", X = stRightX, Y = MvY + 220, TextSize = 9.5, Foreground = Ink2,
                TextWidth = StW, Align = TextAlignment.Center
            });

            // 穿梭横梁 + 夹爪
            const double MvBeamW = 68;
            var mvBeam = new RectItem
            {
                X = MvX + 24, Y = MvY + 74, W = MvBeamW, H = 36, Radius = 6,
                Fill = Green, Text = "移盘", TextSize = 10, TextColor = Brushes.White
            };
            cv.Items.Add(mvBeam);
            var mvGrip = new RectItem
            {
                X = MvX + 40, Y = MvY + 112, W = 36, H = 28, Radius = 4, Fill = Steel
            };
            cv.Items.Add(mvGrip);

            // =============================================================
            //  手工盘模组（5 × 4）
            // =============================================================
            ModBox("手工盘模组", MpX, MpY, MpW, MpH);
            const int MpCols = 5, MpRows = 4;
            const double MpCell = 44, MpGap = 10;
            var mpCells = new RectItem[MpCols * MpRows];
            for (int r = 0; r < MpRows; r++)
            {
                for (int c = 0; c < MpCols; c++)
                {
                    var cell = new RectItem
                    {
                        X = MpX + 18 + c * (MpCell + MpGap),
                        Y = MpY + 48 + r * (MpCell + MpGap),
                        W = MpCell, H = MpCell, Radius = 5,
                        Fill = TrayWhite, Stroke = ModStroke, StrokeThickness = 1
                    };
                    cv.Items.Add(cell);
                    mpCells[r * MpCols + c] = cell;
                }
            }

            // =============================================================
            //  压测模组
            // =============================================================
            ModBox("压测模组", PtX, PtY, PtW, PtH);
            const double PtSeatW = 110, PtHeadW = 74;
            double ptMid = PtX + (PtW - PtSeatW) / 2;
            cv.Items.Add(new RectItem
            {
                X = ptMid, Y = PtY + PtH - 34, W = PtSeatW, H = 22, Radius = 4, Fill = Steel
            });
            var ptHead = new RectItem
            {
                X = PtX + (PtW - PtHeadW) / 2, Y = PtY + 46, W = PtHeadW, H = 24, Radius = 4, Fill = Purple
            };
            cv.Items.Add(ptHead);
            var ptRod = new RectItem
            {
                X = PtX + PtW / 2 - 4, Y = PtY + 34, W = 8, H = 14, Radius = 3, Fill = Steel
            };
            cv.Items.Add(ptRod);
            var ptText = new TextItem
            {
                Text = "下压 0.0mm", X = PtX + 14, Y = PtY + PtH - 22, TextSize = 10,
                Foreground = Ink2, TextWidth = 200, Align = TextAlignment.Left
            };
            cv.Items.Add(ptText);

            // =============================================================
            //  料枪模组
            // =============================================================
            ModBox("料枪模组", GnX, GnY, GnW, GnH);
            const double GnBodyW = 80, GnBodyH = 46;
            double gnBodyY = GnY + (GnH - GnBodyH) / 2;
            cv.Items.Add(new RectItem
            {
                X = GnX + 18, Y = gnBodyY, W = GnBodyW, H = GnBodyH, Radius = 5, Fill = Steel
            });
            double gnBarX = GnX + 18 + GnBodyW;
            double gnBarY = gnBodyY + GnBodyH / 2 - 9;
            var gnBar = new RectItem
            {
                X = gnBarX, Y = gnBarY, W = 24, H = 18, Radius = 4, Fill = Steel
            };
            cv.Items.Add(gnBar);
            var gnTip = new RectItem
            {
                X = gnBarX + 24, Y = gnBarY + 5, W = 12, H = 8, Radius = 2, Fill = Red
            };
            cv.Items.Add(gnTip);
            // 伸出方向终点标记
            cv.Items.Add(new RectItem
            {
                X = GnX + GnW - 26, Y = gnBarY + 8, W = 4, H = 4, Radius = 2, Fill = ModStroke
            });
            var gnText = new TextItem
            {
                Text = "伸出 0.0mm", X = GnX + 14, Y = GnY + GnH - 22, TextSize = 10,
                Foreground = Ink2, TextWidth = 240, Align = TextAlignment.Left
            };
            cv.Items.Add(gnText);

            // =============================================================
            //  加热盘模组
            // =============================================================
            ModBox("加热盘模组", HtX, HtY, HtW, HtH);
            const double HtPlateW = 260, HtPlateH = 110;
            double htPlateX = HtX + (HtW - HtPlateW) / 2;
            double htPlateY = HtY + 116;
            var htPlate = new RectItem
            {
                X = htPlateX, Y = htPlateY, W = HtPlateW, H = HtPlateH, Radius = 10,
                Fill = Cool, Stroke = ModStroke, StrokeThickness = 1
            };
            cv.Items.Add(htPlate);
            var htWaves = new RectItem[3];
            for (int i = 0; i < 3; i++)
            {
                htWaves[i] = new RectItem
                {
                    X = htPlateX + 60 + i * 70, Y = htPlateY - 34, W = 16, H = 30, Radius = 8,
                    Fill = HeatHaze
                };
                cv.Items.Add(htWaves[i]);
            }
            var htText = new TextItem
            {
                Text = "加热 关闭", X = HtX + 12, Y = HtY + HtH - 46, TextSize = 10.5,
                Foreground = Ink2, TextWidth = HtW - 24, Align = TextAlignment.Center
            };
            cv.Items.Add(htText);
            cv.Items.Add(new TextItem
            {
                Text = "OUT15 加热使能", X = HtX + 12, Y = HtY + HtH - 26, TextSize = 9.5,
                Foreground = Ink2, TextWidth = HtW - 24, Align = TextAlignment.Center
            });

            // =============================================================
            //  中部两条导轨：取放臂走廊 + 小车导轨
            // =============================================================
            cv.Items.Add(new RectItem
            {
                X = RailX0, Y = ArmRailY, W = RailX1 - RailX0, H = 6, Radius = 3, Fill = RailB
            });
            cv.Items.Add(new TextItem
            {
                Text = "取放臂走廊 0~900mm", X = RailX0 - 140, Y = ArmRailY - 7, TextSize = 10,
                Foreground = Ink2, TextWidth = 134, Align = TextAlignment.Right
            });
            cv.Items.Add(new RectItem
            {
                X = RailX0, Y = CarRailY, W = RailX1 - RailX0, H = 6, Radius = 3, Fill = RailB
            });
            cv.Items.Add(new TextItem
            {
                Text = "小车导轨 0~1200mm", X = RailX0 - 140, Y = CarRailY - 7, TextSize = 10,
                Foreground = Ink2, TextWidth = 134, Align = TextAlignment.Right
            });

            var armLoad = new RectItem
            {
                X = RailX0, Y = ArmRailY - 28, W = 58, H = 22, Radius = 5,
                Fill = Blue, Text = "上料臂", TextSize = 10, TextColor = Brushes.White
            };
            var armUnload = new RectItem
            {
                X = RailX0, Y = ArmRailY + 8, W = 58, H = 22, Radius = 5,
                Fill = Orange, Text = "下料臂", TextSize = 10, TextColor = Brushes.White
            };
            var carA = new RectItem
            {
                X = RailX0, Y = CarRailY - 26, W = 50, H = 20, Radius = 5,
                Fill = Green, Text = "小车A", TextSize = 9.5, TextColor = Brushes.White
            };
            var carB = new RectItem
            {
                X = RailX0, Y = CarRailY + 8, W = 50, H = 20, Radius = 5,
                Fill = GreenDark, Text = "小车B", TextSize = 9.5, TextColor = Brushes.White
            };
            cv.Items.Add(armLoad);
            cv.Items.Add(armUnload);
            cv.Items.Add(carA);
            cv.Items.Add(carB);

            // 顶部读数条
            var readout = new TextItem
            {
                Text = "", X = 36, Y = 0, TextSize = 11.5, Foreground = Ink,
                TextWidth = FrameW - 32, Align = TextAlignment.Left
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
                // ---- 时间推进 ----
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;
                t += dt;

                var m = MotionManager.Instance;

                // ---- 打开本页后自动启动安全演示节拍（等约 1 秒让界面先画出来） ----
                if (!demoKicked)
                {
                    kickDelay++;
                    if (kickDelay > 20)
                    {
                        demoKicked = true;
                        if (!m.EStop && !m.AnyAlarm() && !m.AutoCycle) m.StartDemo();
                    }
                }

                const double ArmScale = (RailX1 - RailX0) / 900.0;    // 900mm 走廊
                const double CarScale = (RailX1 - RailX0) / 1200.0;   // 1200mm 小车导轨

                // ---- 上 / 下料臂（真实轴位置） ----
                double lp = m.Position(4);
                double up = m.Position(16);
                armLoad.X = RailX0 + lp * ArmScale - 29;
                armUnload.X = RailX0 + up * ArmScale - 29;

                // ---- 小车 A / B（真实轴位置） ----
                double sa = m.Position(28);
                double sb = m.Position(29);
                carA.X = RailX0 + sa * CarScale - 25;
                carB.X = RailX0 + sb * CarScale - 25;

                // ---- 流水线走料（时间驱动） ----
                double flow = t * 30;
                for (int i = 0; i < laneSlots.Count; i++)
                {
                    var (slot, idx, dir) = laneSlots[i];
                    slot.Y = LaneInnerTop + Mod(idx * LaneStep + dir * flow, LaneTotal);
                }

                // ---- 料盘升降台（时间驱动，两条立柱反相） ----
                double liftSpan = ColTotal - 14;
                double tri = Triangle(t * 0.26);
                colLiftL.Y = ColInnerTop + (1 - tri) * liftSpan;
                colLiftR.Y = ColInnerTop + tri * liftSpan;
                for (int i = 0; i < ColSlotN; i++)
                {
                    double sy = ColInnerTop + i * ColStep;
                    colSlotsL[i].Fill = Math.Abs(sy - colLiftL.Y) < ColSlotH ? TrayColor : TrayWhite;
                    colSlotsR[i].Fill = Math.Abs(sy - colLiftR.Y) < ColSlotH ? TrayColor : TrayWhite;
                }
                // 分盘 / 顶升气缸：升降台到位时顶出
                colCylL.Fill = tri > 0.94 ? Red : Steel;
                colCylR.Fill = tri < 0.06 ? Red : Steel;

                // ---- 移盘模组：穿梭横梁 ----
                double mvSpan = MvW - 48 - MvBeamW;
                double mvT;
                if (m.State(28).Busy || m.State(29).Busy)
                    mvT = Math.Min(1, Math.Max(0, sa / 1200.0));
                else
                    mvT = Triangle(t * 0.42);
                double mvX = MvX + 24 + mvT * mvSpan;
                mvBeam.X = mvX;
                mvGrip.X = mvX + 16;
                bool overStation = mvX < stLeftX + StW || mvX + MvBeamW > stRightX;
                mvGrip.Y = overStation ? MvY + 122 : MvY + 112;
                mvGrip.Fill = overStation ? Orange : Steel;

                // ---- 手工盘模组：逐格扫描点亮 ----
                int scan = (int)(t * 7) % mpCells.Length;
                for (int i = 0; i < mpCells.Length; i++)
                {
                    int d = (i - scan + mpCells.Length) % mpCells.Length;
                    mpCells[i].Fill = d == 0 ? Green : d < 4 ? TrayColor : TrayWhite;
                }

                // ---- 压测模组：压头随真实 Z 位置（轴 2） ----
                double zp = m.Position(2);
                double zMax = Math.Max(1, m.Axes[2].MaxMm);
                double zT = Math.Min(1, Math.Max(0, zp / zMax));
                double headTop = PtY + 46;
                double headBottom = PtY + PtH - 74;
                ptHead.Y = headTop + (1 - zT) * (headBottom - headTop);
                ptRod.Y = ptHead.Y - 12;
                ptRod.H = Math.Max(6, ptHead.Y - (PtY + 34));
                bool pressed = zT > 0.85;
                ptHead.Fill = pressed ? Red : Purple;
                ptText.Text = "下压 " + zp.ToString("0.0") + "mm" + (pressed ? "（压到位）" : "");

                // ---- 料枪模组：枪管随真实 Y 轴（轴 5） ----
                double gy = m.Position(5);
                double gyMax = Math.Max(1, m.Axes[5].MaxMm);
                double gT = Math.Min(1, Math.Max(0, gy / gyMax));
                double gunLen = 24 + gT * 210;
                gnBar.W = gunLen;
                gnTip.X = gnBarX + gunLen;
                gnBar.Fill = gT > 0.7 ? Orange : Steel;
                gnText.Text = "伸出 " + gy.ToString("0.0") + "mm";

                // ---- 加热盘模组：OUT15 加热使能 ----
                bool heatOn = m.ReadOut(15);
                if (heatOn)
                {
                    double pulse = 0.5 + 0.5 * Math.Sin(t * 4);
                    htPlate.Fill = LerpBrush(Cool, Red, 0.45 + 0.55 * pulse);
                    htText.Text = "加热 开启 · 盘温上升中";
                    htText.Foreground = Red;
                    for (int i = 0; i < htWaves.Length; i++)
                    {
                        double ph = Mod(t * 0.9 + i * 0.33, 1.0);
                        htWaves[i].H = 14 + ph * 30;
                        htWaves[i].Y = htPlateY - htWaves[i].H - 4;
                    }
                }
                else
                {
                    htPlate.Fill = Cool;
                    htText.Text = "加热 关闭 · 盘温 = 室温";
                    htText.Foreground = Ink2;
                    for (int i = 0; i < htWaves.Length; i++) htWaves[i].H = 1;
                }

                // ---- 顶部读数条 ----
                readout.Text = "卡：" + m.CardName + (m.IsSim ? "（仿真）" : "（真卡）")
                    + "　|　上料臂 X=" + lp.ToString("0.0") + "mm"
                    + "　下料臂 X=" + up.ToString("0.0") + "mm"
                    + "　间距=" + Math.Abs(lp - up).ToString("0.0") + "mm"
                    + "　|　小车A=" + sa.ToString("0.0") + "　小车B=" + sb.ToString("0.0")
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