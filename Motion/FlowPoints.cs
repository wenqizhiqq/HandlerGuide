// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/FlowPoints.cs — 流程点位表（点位 / 速度 / 阈值集中管理）
//  项目：HandlerGuide（三温测试分选机上位机复刻）
//  职责：FlowEngine 的 16 条工艺线程只从这里取数值，不许在 if 里散落硬编码。
//
//  数值口径（每个常量后面都标了来源，现场照标记校核）：
//    [demo]  现有 MotionManager.BuildDemo() 22 步节拍里用过的数值，原样搬过来；
//    [表]    交接资料轴表 / IO 表直接给的（软限位、回零位、点号、轴号）；
//    [推导]  资料里没有、按机台几何或轴软限位推出来的 —— 这些必须现场逐点校核，
//            FlowEngine.Start() 会把「推导项」的数量与清单写一条日志，便于对表。
//
//  重要：本表只给「目标位置」，速度 / 软限位仍由 MotionManager.AxisRules 决定，
//        所有点位都在各自轴的软限位区间内（越界会被 SafetyCheck 直接拒绝）。
// ============================================================================
using System;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（FlowPoints）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_FlowPoints
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · FlowPoints。</summary>
        internal const string WenQiZhiFile = "FlowPoints";
    }

    /// <summary>
    /// 流程点位表。分Bin 映射、满盘阈值、仓走位、梭车带位、吸嘴高度都在这里，
    /// 改数值只改这一处（FlowEngine 线程体只引用本表常量）。
    /// </summary>
    public static class FlowPoints
    {
        // ==================================================================
        //  1. 批次 / 料盘口径
        // ==================================================================

        /// <summary>吸嘴根数（上料 / 下料各 8 根独立真空吸嘴）[表]。</summary>
        public const int NozzleCount = 8;

        /// <summary>
        /// 一批颗数 = 一排放 8 颗。
        /// [demo] 现有节拍按 1 颗演，产量统计 recipe_per_cycle 默认 8 颗/循环
        ///        （Ops.OnCycleCompleted），流程侧沿用 8（与 8 根吸嘴一致）。
        /// </summary>
        public const int BatchSize = 8;

        /// <summary>一盘待测芯片数。[LayoutPages 需求 48 注释「一盘 40 颗」]。</summary>
        public const int TrayChips = 40;

        /// <summary>满盘阈值（颗/盘）。[LayoutPages「出料盘满 32 颗 = 满盘回仓换新盘」]。</summary>
        public const int TrayCapacity = 32;

        /// <summary>
        /// 仓内一次分盘的升降步距（mm）。
        /// [推导] 资料只给了仓 Z 的软限位 0~500 与回零位 500，没给盘厚；
        ///        按 500 − 40 = 460 作为「下降一个盘位」的分盘高度。
        /// </summary>
        public const double TrayPitchMm = 40;

        /// <summary>进料仓初始盘数（待测满盘）。[推导] 资料没有盘塔容量，先给 20 盘。</summary>
        public const int FeedTrayStack = 20;

        /// <summary>空盘仓初始盘数。[推导] 先给 20 盘。</summary>
        public const int EmptyTrayStack = 20;

        /// <summary>每座 Auto 收料仓预置的空盘数。[推导] 先给 2 片，避免开机即等盘。</summary>
        public const int AutoEmptyBuffer = 2;

        /// <summary>出盘仓可堆盘数上限。[推导] 先给 30 盘（120s 仿真不会顶满）。</summary>
        public const int OutTrayCapacity = 30;

        // ==================================================================
        //  2. 上料臂（上料X/Y、变距X/Y、旋转U、吸嘴1~8Z）
        // ==================================================================

        /// <summary>上料X 停放位（= 回零位 0）。[demo「上料臂 X → 停放位 0mm」] [表]。</summary>
        public const double LdrXHome = 0;
        /// <summary>上料X 取料位（待测盘）。[demo「上料臂 X → 取料位 200mm」]。</summary>
        public const double LdrXPick = 200;
        /// <summary>上料X 放料位（梭车交接）。[demo「上料臂 X → 放料位 620mm」]。</summary>
        public const double LdrXPlace = 620;

        /// <summary>上料Y 待测盘行。[推导] 上料Y 软限位 0~300，取料行给到停放侧 0。</summary>
        public const double LdrYPick = 0;
        /// <summary>上料Y 放料行（梭车在中线）。[推导] 取 0~300 的中位 150。</summary>
        public const double LdrYPlace = 150;

        /// <summary>吸嘴 Z 抬起高度。[demo「上料臂 Z1 抬起 30」] [表 软限位 0~30]。</summary>
        public const double LdrZUp = 30;
        /// <summary>吸嘴 Z 取放料高度。[demo「上料臂 Z1 下降取料 5」]。</summary>
        public const double LdrZDown = 5;

        /// <summary>上料旋转U 取料朝向（度）。[推导] 回零位 0 即取料朝向。</summary>
        public const double LdrUPick = 0;
        /// <summary>上料旋转U 放料朝向（度）。[推导] 换向 90°，软限位 ±180 内。</summary>
        public const double LdrUPlace = 90;

        /// <summary>上料变距X/Y 取料间距（mm）。[推导] 变距软限位 ±20，取料收到 0。</summary>
        public const double LdrPitchPick = 0;
        /// <summary>上料变距X/Y 放料间距（mm）。[推导] 放到 10（区间内、离取料位有区分度）。</summary>
        public const double LdrPitchPlace = 10;

        // ==================================================================
        //  3. 下料臂（下料X/Y、变距X/Y、旋转U、吸嘴1~8Z）
        // ==================================================================

        /// <summary>下料X 停放位（= 回零位 900）。[demo「下料臂 X → 停放位 900mm」] [表]。</summary>
        public const double UldrXHome = 900;
        /// <summary>下料X 取料位（梭车交接）。[demo「下料臂 X → 取料位 620mm」]。</summary>
        public const double UldrXPick = 620;
        /// <summary>下料X 出料位（收料盘上方）。[demo「下料臂 X → 出料位 880mm」]。</summary>
        public const double UldrXPlace = 880;

        /// <summary>下料Y 梭车取料行（mm）。[推导] 与上料Y 放料行同线 = 150。</summary>
        public const double UldrYShuttle = 150;
        /// <summary>下料Y NG 料仓行（mm）。[推导] 软限位 0~300，给到 240。</summary>
        public const double UldrYNg = 240;

        /// <summary>吸嘴 Z 抬起高度。[demo「下料臂 Z1 抬起 30」]。</summary>
        public const double UldrZUp = 30;
        /// <summary>吸嘴 Z 取放料高度。[demo「下料臂 Z1 下降 5」]。</summary>
        public const double UldrZDown = 5;

        /// <summary>下料旋转U 取料朝向（度）。[推导] 0。</summary>
        public const double UldrUPick = 0;
        /// <summary>下料旋转U 放料朝向（度）。[推导] 反向 −90。</summary>
        public const double UldrUPlace = -90;

        /// <summary>下料变距X/Y 间距（mm）。[推导] 全程收在 0（资料没给盘距）。</summary>
        public const double UldrPitch = 0;

        /// <summary>
        /// 第 n（1..3）座 Auto 仓对应的下料Y 行（mm）。
        /// [推导] 下料Y 软限位 0~300，三仓均分 60 / 120 / 180，NG 另给 240。
        /// </summary>
        public static double UldrYStore(int n) => 60.0 * Math.Clamp(n, 1, 3);

        // ==================================================================
        //  4. 梭车（前 / 后共一条 SHUTTLE 导轨，安全间隙 120 [表 AxisRules]）
        // ==================================================================

        /// <summary>前梭车停放位（= 回零位 0）。[demo「小车 A → 回位 0mm」] [表]。</summary>
        public const double ShFHome = 0;
        /// <summary>前梭车接料位。[demo「小车 A → 接料位 300mm」]。</summary>
        public const double ShFPick = 300;
        /// <summary>
        /// 前梭车测试位。
        /// 口径变化（同一个数给两个口径）：
        ///   [demo 旧口径] 900mm —— 那是单台小车时代的「测试位」；
        ///   [新口径]      500mm —— 真表前后梭车共一条导轨、安全间隙 120，
        ///                后梭车要在 700~1200 带内活动，前梭车到 900 会被 SR-08 直接拒。
        ///                故前测试位收到 500，与后测试位 700 之间留 200 &gt; 120。
        /// </summary>
        public const double ShFTest = 500;

        /// <summary>后梭车停放位（= 回零位 1200）[表]。</summary>
        public const double ShBHome = 1200;
        /// <summary>后梭车下料交接位（mm）。[推导] 取 demo 的 900（后梭车带内，镜像前梭车接料位）。</summary>
        public const double ShBHand = 900;
        /// <summary>后梭车测试位（mm）。[推导] 700 = 前测试位 500 + 200（> 导轨间隙 120）。</summary>
        public const double ShBTest = 700;

        // ==================================================================
        //  5. 压头测试工位（前 / 后各一 Y 一走位、一 Z 下压；同组共 4 轴 [表]）
        // ==================================================================

        /// <summary>压头Y 停放位（= 回零位 160）[表]。</summary>
        public const double PressYHome = 160;
        /// <summary>压头Y 测试位1（mm）。[推导] 软限位 0~320，给 100。</summary>
        public const double PressYSite1 = 100;
        /// <summary>压头Y 测试位2（mm）。[推导] 软限位 0~320，给 220。</summary>
        public const double PressYSite2 = 220;

        /// <summary>压头Z 抬起位（= 回零位 25，抬起阈值 24）[表]。</summary>
        public const double PressZUp = 25;
        /// <summary>
        /// 压头Z 下压位（mm）。
        /// [推导] 资料只给软限位 0~25，没给下压深度；按「留 2mm 余量不压死」取 3。
        /// </summary>
        public const double PressZDown = 3;

        // ==================================================================
        //  6. 四个料盘仓（进料仓 / 空盘仓 / 出盘仓 / Auto1~3）
        // ==================================================================

        /// <summary>仓 Z 上位（= 回零位 500，抬起阈值 470）[表]。</summary>
        public const double StoreZUp = 500;
        /// <summary>仓 Z 分盘位（500 − 盘距 40）[推导]。</summary>
        public const double StoreZDivide = 460;

        /// <summary>仓 Y 回位（= 回零位 0）[表]。</summary>
        public const double StoreYHome = 0;
        /// <summary>仓 Y 交接位（mm）。[推导] 软限位 0~240，取中位 120。</summary>
        public const double StoreYSend = 120;

        // ==================================================================
        //  7. 料盘搬运臂（搬运X：0~700，回零 0 [表]）
        // ==================================================================
        //  [推导] 资料《工作基点》只给了机械基值（maint_base.xaml 的 mb_ta_*）：
        //      Safe −722.990 / Loader −1274.880 / Empty −950.520 / Buffer −722.990
        //      Auto1 −400.100 / Auto2 −208.750 / Auto3 −18.330
        //  搬运X 只有一根行程轴（0~700），故按「距 Safe 的绝对距离」线性映射到
        //  0~690（比例 690 / 704.66 ≈ 0.9792，留 10mm 不贴软限位）。
        //  这条先后次序 = 距停放位的远近，不代表真实左右方向；现场要用实际
        //  机械基值重新标一遍。Buffer 与 Safe 在资料里基值相同（都 −722.990），
        //  这里给 60 让两个工位有区分度。

        /// <summary>
        /// 搬运X 停放位 / 安全位 = 轴表里的回零位（需求 40 定的 530.2px ↔ 442.3mm）。
        /// ⚠ 必须与轴表一致，不能写死 0：X 走廊三臂分时互斥的判据是
        /// <c>HomeMm ± 40</c>，搬运臂停在 0 时会被判成「已离开停放位」，
        /// 上料X / 下料X 会被永久拒绝（实测上料X 连续被拒 800+ 次、一颗都投不进去）。
        /// </summary>
        public static double TfSafe
        {
            get
            {
                var m = Motion.MotionManager.Instance;
                int ax = m.AxisIndex("搬运X轴");
                return ax >= 0 ? m.Cfg(ax).HomeMm : 442.3;
            }
        }
        /// <summary>搬运X 缓存台位（mm）。[推导] 资料基值与 Safe 相同，给 60 区分。</summary>
        public const double TfBuffer = 60;
        /// <summary>搬运X 空盘仓线位（mm）。[推导] |−950.52 − (−722.99)| = 227.6 → 223。</summary>
        public const double TfEmpty = 223;
        /// <summary>搬运X Auto1 仓线位（mm）。[推导] |−400.10 + 722.99| = 322.9 → 316。</summary>
        public const double TfAuto1 = 316;
        /// <summary>搬运X Auto2 仓线位（mm）。[推导] 514.2 → 504。</summary>
        public const double TfAuto2 = 504;
        /// <summary>搬运X 进料仓线位（mm）。[推导] 551.9 → 540。</summary>
        public const double TfLoader = 540;
        /// <summary>搬运X Auto3 仓线位（mm）。[推导] 704.7 → 690。</summary>
        public const double TfAuto3 = 690;

        /// <summary>第 n（1..3）座 Auto 仓的搬运工位。</summary>
        public static double TfAuto(int n)
        {
            switch (n)
            {
                case 1: return TfAuto1;
                case 2: return TfAuto2;
                default: return TfAuto3;
            }
        }

        // ==================================================================
        //  8. 时间常量（全部带 Sleep + 超时，绝不死等）
        // ==================================================================

        /// <summary>单轴到位等待上限（ms）。</summary>
        public const int MoveTimeoutMs = 6000;

        /// <summary>令牌等待上限（ms）。</summary>
        public const int TokenTimeoutMs = 3000;

        /// <summary>真空反馈到位上限（ms）。[推导] 资料没给真空建立时间，先给 1500。</summary>
        public const int VacInTimeoutMs = 1500;

        /// <summary>吸 / 放真空保持时间（ms）。[demo 节拍里 Dwell = 0.4s / 0.3s]。</summary>
        public const int VacuumDwellMs = 300;

        /// <summary>压头测试保压时间（ms）。[推导] sp_test 默认 3.2s 是整机测试节拍，</summary>
        /// <remarks>流程仿真里按 400ms 压缩，否则 120s 跑不出一批；现场按实际测试时间改。</remarks>
        public const int TestDwellMs = 400;

        /// <summary>中转缓存台（气缸）交接时间（ms）。[推导] 气缸行程 600ms。</summary>
        public const int BufferDwellMs = 600;

        /// <summary>NG / OK 仓一次换盘事件时长（ms）。[推导] 气缸换盘 + 人工确认按 5s。</summary>
        public const int NgSwapMs = 5000;

        /// <summary>轮询间隔（ms）。真机那 6 个无 Sleep 忙等在这里全部替换成 20ms 轮询。</summary>
        public const int PollMs = 20;

        // ==================================================================
        //  9. 轴名 / 点名字符串（线程体只通过本表拿名字，改名一处生效）
        // ==================================================================

        /// <summary>上料吸嘴 n（1..8）Z 轴名 [表]。</summary>
        public static string LdrNozzleZ(int n) => "上料吸嘴" + n + "Z轴";
        /// <summary>下料吸嘴 n（1..8）Z 轴名 [表]。</summary>
        public static string UldrNozzleZ(int n) => "下料吸嘴" + n + "Z轴";

        /// <summary>上料吸嘴 n 吸真空输出点 [表 U21_Y16..]。</summary>
        public static string LdrVacOn(int n) => "左上料吸真空" + n;
        /// <summary>上料吸嘴 n 破真空输出点 [表 U21_Y17..]。</summary>
        public static string LdrVacOff(int n) => "左上料破真空" + n;
        /// <summary>上料吸嘴 n 真空反馈输入点 [表 U7_X16..23]。</summary>
        public static string LdrVacOk(int n) => "左上料真空" + n;

        /// <summary>下料吸嘴 n 吸真空输出点 [表 U22_Y0..14]。</summary>
        public static string UldrVacOn(int n) => "右下料吸真空" + n;
        /// <summary>下料吸嘴 n 破真空输出点 [表 U22_Y1..15]。</summary>
        public static string UldrVacOff(int n) => "右下料破真空" + n;
        /// <summary>下料吸嘴 n 真空反馈输入点 [表 U7_X24..31]。</summary>
        public static string UldrVacOk(int n) => "右下料真空" + n;

        /// <summary>压头吸真空输出点（前 1~8 / 后 1~8）[表 U22_Y16.. / U23_Y16..]。</summary>
        public static string PressVacOn(bool front, int n) => (front ? "前压头吸真空" : "后压头吸真空") + n;
        /// <summary>压头破真空输出点（前 / 后各 1~8）[表 U23_Y0.. / U24_Y0..]。</summary>
        public static string PressVacOff(bool front, int n) => (front ? "前压头破真空" : "后压头破真空") + n;

        /// <summary>搬运夹爪输出点 [表 U21_Y0 夹 / Y1 松]。</summary>
        public static string GripperClose => "料盘搬运夹爪夹";
        /// <summary>搬运夹爪松开输出点。</summary>
        public static string GripperOpen => "料盘搬运夹爪松";
        /// <summary>搬运夹爪有无感应输入点 [表 U1_X20]（仿真没接反馈联动，只读不判）。</summary>
        public static string GripperSensor => "料盘搬运夹爪有无感应";

        // ==================================================================
        //  10. 分Bin → 收料仓映射
        // ==================================================================
        //  [照现有代码] Ops.OnCycleCompleted：
        //      良品池 = Bins 里 Index>=1 且 !IsBad 且 !Unused 的 Bin1..Bin12；
        //      不良池 = Bins 里 Index>=1 且 IsBad 的 Bin13..Bin16；
        //  [照现有代码] LayoutPages 需求 48：出料盘 11..13 轮转 = Auto1/2/3，
        //      满 TrayCapacity(32) 颗整盘回仓换新盘。
        //  ⇒ 本表把 12 个良品 Bin 按 4 个一组顺序映射到 Auto1/2/3；
        //    不良 Bin13..16 统一落 NG 料仓（不在 3 座 Auto 仓里）。

        /// <summary>良品 Bin 序号下限 [照 Ops]。</summary>
        public const int GoodBinFirst = 1;
        /// <summary>良品 Bin 序号上限 [照 Ops：Bin1..Bin12]。</summary>
        public const int GoodBinLast = 12;
        /// <summary>不良 Bin 序号下限 [照 Ops：Bin13..Bin16 是 IsBad]。</summary>
        public const int BadBinFirst = 13;
        /// <summary>不良 Bin 序号上限。</summary>
        public const int BadBinLast = 16;
        /// <summary>每座 Auto 仓挂几个良品 Bin。[推导] 12 / 3 = 4。</summary>
        public const int BinsPerStore = 4;
        /// <summary>NG 仓代号（StoreOfBin 返回 0 表示落 NG 料仓，不是 Auto1~3）。</summary>
        public const int NgStore = 0;

        /// <summary>
        /// Bin 序号 → 收料仓：Bin1..12 → Auto1..3（每仓 4 个 Bin）；Bin13..16 → NG 仓(0)。
        /// 越界 / 未启用返回 −1（调用方按「无处落盘」处理，不丢料）。
        /// </summary>
        public static int StoreOfBin(int bin)
        {
            if (bin >= GoodBinFirst && bin <= GoodBinLast)
                return (bin - GoodBinFirst) / BinsPerStore + 1;
            if (bin >= BadBinFirst && bin <= BadBinLast) return NgStore;
            return -1;
        }

        /// <summary>本批里良品落哪个 Bin（顺序轮转 Bin1..12，跳过满盘的仓）。</summary>
        public static int GoodBinAt(int step) => GoodBinFirst + Math.Abs(step) % (GoodBinLast - GoodBinFirst + 1);
        /// <summary>本批里不良品落哪个 Bin（顺序轮转 Bin13..16）。</summary>
        public static int BadBinAt(int step) => BadBinFirst + Math.Abs(step) % (BadBinLast - BadBinFirst + 1);

        /// <summary>
        /// 推导项清单（自检 / 日志用）：资料里没有、按几何或软限位推的关键数值。
        /// </summary>
        public static string DerivedSummary()
        {
            return "分盘步距 " + TrayPitchMm.ToString("0.###") + " / 仓Y交接 " + StoreYSend.ToString("0.###")
                 + " / 前测试位 " + ShFTest.ToString("0.###") + "（旧口径 900）"
                 + " / 后测试位 " + ShBTest.ToString("0.###")
                 + " / 压头下压 " + PressZDown.ToString("0.###")
                 + " / 搬运工位 " + TfBuffer.ToString("0") + "/" + TfEmpty.ToString("0") + "/"
                   + TfAuto1.ToString("0") + "/" + TfAuto2.ToString("0") + "/" + TfLoader.ToString("0") + "/" + TfAuto3.ToString("0")
                 + " / 盘塔 " + FeedTrayStack.ToString("0") + "+" + EmptyTrayStack.ToString("0")
                 + " / 保压 " + TestDwellMs.ToString("0") + "ms";
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
