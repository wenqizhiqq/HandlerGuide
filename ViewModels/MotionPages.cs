// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  ViewModels/MotionPages.cs — 运动仿真页 / 控制卡设置页
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：以纯声明式 Element 树构建两个新页面：
//        · motion_sim  运动仿真（轴控制 / IO 监视 / 安全互锁 / 轨迹预览）
//        · motion_card 控制卡设置（卡型 / 脉冲当量 / 软限位 / 加减速 / 回零）
//        页面内所有实时读数都通过 LiveSignals / LiveValues 的具名信号取值，
//        不在此文件里动态创建控件。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MotionPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_MotionPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · MotionPages。</summary>
        internal const string WenQiZhiFile = "MotionPages";
    }

    /// <summary>运动控制相关页面的声明式构建。</summary>
    public static class MotionPages
    {
        // ---------------- 局部配色（图形用，不参与全局调色板） ----------------

        /// <summary>导轨底色。</summary>
        private static readonly Brush RailBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xD5, 0xDA));

        /// <summary>上料臂颜色。</summary>
        private static readonly Brush LoadBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xD6));

        /// <summary>下料臂颜色。</summary>
        private static readonly Brush UnloadBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x1E));

        /// <summary>小车 A 颜色。</summary>
        private static readonly Brush ShuttleABrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x5B));

        /// <summary>小车 B 颜色。</summary>
        private static readonly Brush ShuttleBBrush = new SolidColorBrush(Color.FromRgb(0x1B, 0x6E, 0x40));

        /// <summary>干涉/危险色。</summary>
        private static readonly Brush DangerBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x5D, 0x55));

        /// <summary>干涉预警带（半透明）。</summary>
        private static readonly Brush DangerBand = new SolidColorBrush(Color.FromArgb(0x38, 0xE8, 0x5D, 0x55));

        /// <summary>Z 轴柱状色。</summary>
        private static readonly Brush ZBrush = new SolidColorBrush(Color.FromRgb(0x8F, 0x6A, 0xD6));

        /// <summary>静态构造：注册具名实时信号（只做一次）。</summary>
        static MotionPages()
        {
            RegisterLive();
        }

        /// <summary>信号是否已注册。</summary>
        private static bool _liveRegistered;

        /// <summary>
        /// 确保具名信号已注册。
        /// 静态构造函数只在「首次访问静态成员」时执行，而 PageCatalog 里只是取方法组
        /// （不触发静态构造），因此这里显式提供一个入口，由 MainViewModel 启动时调用。
        /// </summary>
        public static void EnsureRegistered()
        {
            // 调用本方法即会先执行静态构造函数，无需其它逻辑。
        }

        // ==================================================================
        //  具名实时信号注册
        // ==================================================================

        /// <summary>把轴 / IO / 卡状态注册成具名信号，供 XAML 与页面元素引用。</summary>
        private static void RegisterLive()
        {
            if (_liveRegistered) return;
            _liveRegistered = true;

            var m = MotionManager.Instance;

            // ---- 每根轴 ----
            foreach (var a in m.Axes)
            {
                int i = a.Index;
                LiveValues.Register("ax_pos_" + i, _ => Math.Round(MotionManager.Instance.Position(i), 3));
                LiveValues.Register("ax_tgt_" + i, _ => Math.Round(MotionManager.Instance.State(i).TargetMm, 3));
                LiveValues.Register("ax_vel_" + i, _ => Math.Round(MotionManager.Instance.State(i).Velocity, 1));
                LiveValues.Register("ax_tq_" + i, _ => Math.Round(MotionManager.Instance.State(i).Torque, 1));
                LiveValues.Register("ax_rej_" + i, _ => MotionManager.Instance.State(i).LastReject);
                LiveValues.Register("ax_lim_" + i, _ => a.MinMm.ToString("0.###") + " ~ " + a.MaxMm.ToString("0.###"));

                LiveSignals.Register("ax_busy_" + i, _ => MotionManager.Instance.State(i).Busy);
                LiveSignals.Register("ax_homed_" + i, _ => MotionManager.Instance.State(i).Homed);
                LiveSignals.Register("ax_alm_" + i, _ => MotionManager.Instance.State(i).Alarm);
                LiveSignals.Register("ax_inp_" + i, _ => MotionManager.Instance.State(i).InPosition);
                LiveSignals.Register("ax_on_" + i, _ => MotionManager.Instance.State(i).ServoOn);
                LiveSignals.Register("ax_org_" + i, _ => MotionManager.Instance.State(i).Org);
            }

            // ---- 每路 IO ----
            foreach (var p in m.Inputs)
            {
                int b = p.Bit;
                LiveSignals.Register("in_" + b, _ => MotionManager.Instance.ReadIn(b));
            }
            foreach (var p in m.Outputs)
            {
                int b = p.Bit;
                LiveSignals.Register("out_" + b, _ => MotionManager.Instance.ReadOut(b));
            }

            // ---- 卡 / 汇总 ----
            LiveValues.Register("card_name", _ => MotionManager.Instance.CardName);
            LiveValues.Register("card_info", _ => MotionManager.Instance.Info);
            LiveValues.Register("card_mode", _ => MotionManager.Instance.IsSim ? "仿真模式" : "真卡模式");
            LiveValues.Register("card_axes", _ => MotionManager.Instance.Axes.Count);
            LiveValues.Register("card_io", _ => MotionManager.Instance.Inputs.Count + " 入 / " + MotionManager.Instance.Outputs.Count + " 出");
            LiveValues.Register("move_count", _ => MotionManager.Instance.MoveCount);
            LiveValues.Register("reject_count", _ => MotionManager.Instance.RejectCount);
            LiveValues.Register("last_reject", _ => MotionManager.Instance.LastReject);
            LiveValues.Register("demo_step", _ => MotionManager.Instance.DemoStepText);
            LiveValues.Register("demo_cycles", _ => MotionManager.Instance.DemoCycles);
            LiveValues.Register("estop_text", _ => MotionManager.Instance.EStop ? "急停中" : "正常");

            LiveSignals.Register("card_connected", _ => MotionManager.Instance.Connected);
            LiveSignals.Register("card_sim", _ => MotionManager.Instance.IsSim);
            LiveSignals.Register("estop", _ => MotionManager.Instance.EStop);
            LiveSignals.Register("demo_run", _ => MotionManager.Instance.AutoCycle);
            LiveSignals.Register("any_alarm", _ => MotionManager.Instance.AnyAlarm());
            LiveSignals.Register("any_busy", _ => MotionManager.Instance.AnyBusy());
        }

        // ==================================================================
        //  出厂轴参数快照
        // ==================================================================

        /// <summary>
        /// 出厂轴参数（首次构建控制卡设置页时抓一次）。
        /// 注意：以前的「恢复出厂参数」是从**当前**轴配置回写模型 —— 而「应用」刚刚才把
        /// 模型写进轴配置，所以那个按钮实际什么也没恢复。这里改成真正的出厂快照。
        /// </summary>
        private static List<Motion.AxisConfig> _factoryAxes;

        private static void CaptureFactoryAxes()
        {
            if (_factoryAxes != null) return;
            _factoryAxes = MotionManager.Instance.Axes
                .Select(a => new Motion.AxisConfig
                {
                    Index = a.Index, Name = a.Name, Group = a.Group, Unit = a.Unit,
                    PulseEquivalent = a.PulseEquivalent,
                    MinMm = a.MinMm, MaxMm = a.MaxMm, HomeMm = a.HomeMm,
                    MaxVel = a.MaxVel, Acc = a.Acc, Dec = a.Dec,
                    IsVertical = a.IsVertical, ZUpMm = a.ZUpMm,
                    RailId = a.RailId, ClearanceMm = a.ClearanceMm, AutoCycle = a.AutoCycle
                })
                .ToList();
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>轴的一行控制条。</summary>
        private static Element AxisRow(AxisConfig a)
        {
            int i = a.Index;
            var m = MotionManager.Instance;

            var row = E.H();
            row.Children.Add(E.T(a.Name, 12, E.P, false, TextWrapping.NoWrap).Wd(126));
            row.Children.Add(Live("ax_pos_" + i, "0.###", 74));
            row.Children.Add(Live("ax_tgt_" + i, "0.###", 74));

            // 状态灯：使能 / 回零 / 到位 / 报警 / 运动
            row.Children.Add(E.LiveLamp(_ => m.State(i).ServoOn, false, true, 11));
            row.Children.Add(E.LiveLamp(_ => m.State(i).Homed, false, true, 11));
            row.Children.Add(E.LiveLamp(_ => m.State(i).InPosition, false, true, 11));
            row.Children.Add(E.LiveLamp(_ => m.State(i).Busy, false, true, 11));
            row.Children.Add(E.LiveLamp(_ => m.State(i).Alarm, true, true, 11));

            // 点动 / 回零 / 停止：全部走 Ops，这样急停、自动运行中的禁动、日志都统一
            double[] steps = { -10, -1, -0.1, 0.1, 1, 10 };
            foreach (var s in steps)
            {
                double d = s;
                row.Children.Add(E.MiniBtn(s.ToString("0.###"), () => Ops.JogAxisBy(i, d), 42));
            }
            // 定位到目标框里填的值
            row.Children.Add(E.Num("ax_go_" + i, a.HomeMm, 68, "0.###", a.MinMm, a.MaxMm));
            row.Children.Add(E.MiniBtn("定位",
                () => Ops.MoveAxisTo(i, MachineModel.Instance.GetNum("ax_go_" + i, a.HomeMm)), 44));
            row.Children.Add(E.MiniBtn("回零", () => Ops.HomeAxis(i), 44));
            row.Children.Add(E.MiniBtn("停止", () => Ops.StopAxis(i), 44));
            // 单轴伺服使能 / 断使能
            row.Children.Add(E.MiniBtn("使能", () => Ops.ServoAxis(i, true), 44));
            row.Children.Add(E.MiniBtn("断使", () => Ops.ServoAxis(i, false), 44));
            return row;
        }

        /// <summary>带命名信号的只读显示框（Signal 会从 LiveValues 注册表取源）。</summary>
        private static LiveElement Live(string signal, string fmt, double w,
                                        Brush bg = null, Brush fg = null, double fs = 11.5)
        {
            var e = new LiveElement
            {
                Format = fmt,
                Width = w,
                Background = bg ?? E.BandGrey,
                Foreground = fg ?? E.P,
                FontSize = fs
            };
            e.Signal = signal;
            return e;
        }

        /// <summary>输入信号芯片（只读灯 + 名称 + 位号）。</summary>
        private static Element InChip(IoPoint p)
        {
            int b = p.Bit;
            return E.H(
                E.LiveLamp(_ => MotionManager.Instance.ReadIn(b), p.ActiveLow, true, 11),
                E.T("#" + b.ToString("00"), 10, E.Sec).Wd(24),
                E.T(p.Name, 11, E.P, false, TextWrapping.NoWrap).Wd(104),
                // 仿真时手动造信号：模拟料到位 / 气缸到位，便于单独验证互锁
                E.MiniBtn("模拟", () => Ops.ToggleInput(b), 40))
                .Marg(new Thickness(0, 1, 12, 1));
        }

        /// <summary>输出信号芯片（只读灯 + 名称 + 翻转按钮）。</summary>
        private static Element OutChip(IoPoint p)
        {
            int b = p.Bit;
            return E.H(
                E.LiveLamp(_ => MotionManager.Instance.ReadOut(b), false, true, 11),
                E.T("#" + b.ToString("00"), 10, E.Sec).Wd(24),
                E.T(p.Name, 11, E.P, false, TextWrapping.NoWrap).Wd(104),
                E.MiniBtn("⇄", () => Ops.ToggleOut(b), 26))
                .Marg(new Thickness(0, 1, 12, 1));
        }

        /// <summary>安全规则的一行（灯红 = 规则不满足 = 存在风险）。</summary>
        private static Element RuleRow(SafetyRule r)
        {
            return E.H(
                E.T(r.Id, 11.5, E.Sec, true).Wd(52),
                E.LiveLamp(_ => !r.IsOk(), true, true, 12),
                E.T(r.Name, 12, E.P, false, TextWrapping.NoWrap).Wd(190),
                E.T(r.Blocking ? "阻塞" : "提示", 11, r.Blocking ? E.OnRed : E.Sec).Wd(44),
                E.T(r.Detail, 10.5, E.Sec, false, TextWrapping.Wrap)).Marg(new Thickness(0, 2, 0, 2));
        }

        // ==================================================================
        //  页面 1：运动仿真
        // ==================================================================

        /// <summary>运动仿真页（motion_sim）。</summary>
        public static Element SimPage()
        {
            var root = E.V();

            // ---------------- 控制卡状态头 ----------------
            root.Children.Add(E.Band("运动控制卡状态", E.BandBlue,
                E.H(
                    E.T("卡型", 11.5, E.Sec).Wd(40), Live("card_name", null, 210),
                    E.T("模式", 11.5, E.Sec).Wd(40), Live("card_mode", null, 84),
                    E.T("轴/IO", 11.5, E.Sec).Wd(48), Live("card_io", null, 104),
                    E.T("连接", 11.5, E.Sec).Wd(40),
                    E.LiveLamp(_ => MotionManager.Instance.Connected, false, false, 13),
                    E.T("急停", 11.5, E.Sec).Wd(40),
                    E.LiveLamp(_ => MotionManager.Instance.EStop, true, false, 13),
                    E.T("报警", 11.5, E.Sec).Wd(40),
                    E.LiveLamp(_ => MotionManager.Instance.AnyAlarm(), true, false, 13)),
                E.H(
                    E.Btn("连接雷赛真卡", Ops.ConnectCard, "PrimaryButton", 12),
                    E.MiniBtn("使用仿真卡", Ops.UseSimCard, 104),
                    E.MiniBtn("断开", Ops.DisconnectCard, 56),
                    E.SpacerW(10),
                    E.Btn("急停", Ops.EmergencyStop, "DangerButton", 12),
                    E.Btn("急停复位", Ops.ResetEmergency, "PrimaryButton", 12),
                    E.MiniBtn("清除报警", Ops.ClearAlarm, 84),
                    E.MiniBtn("全部回零", () => Ops.HomeAll(), 84),
                    E.MiniBtn("全部停止", Ops.StopAllAxes, 84),
                    E.SpacerW(10),
                    E.T("指令", 11.5, E.Sec).Wd(36), Live("move_count", null, 64),
                    E.T("拒绝", 11.5, E.Sec).Wd(36), Live("reject_count", null, 64)),
                E.H(
                    E.T("最近一次安全裁决：", 11.5, E.Sec, true).Wd(124),
                    Live("last_reject", null, 700, Palette.WarningBg, E.P, 11.5))));

            // ---------------- 四个标签页 ----------------
            root.Children.Add(E.Tabs(
                ("轴控制", () => TabAxis()),
                ("IO 监视", () => TabIo()),
                ("安全互锁", () => TabSafety()),
                ("轨迹预览", () => TabTrace())));

            return root;
        }

        /// <summary>标签页 1：轴控制。</summary>
        private static Element TabAxis()
        {
            var m = MotionManager.Instance;
            var body = E.V();

            body.Children.Add(E.H(
                E.T("轴名", 11.5, E.Sec, true).Wd(126),
                E.T("位置(mm)", 11.5, E.Sec, true).Wd(74),
                E.T("目标(mm)", 11.5, E.Sec, true).Wd(74),
                E.T("使能", 11, E.Sec, true).Wd(24),
                E.T("回零", 11, E.Sec, true).Wd(24),
                E.T("到位", 11, E.Sec, true).Wd(24),
                E.T("运动", 11, E.Sec, true).Wd(24),
                E.T("报警", 11, E.Sec, true).Wd(24),
                E.T("点动 -10 / -1 / -0.1 / +0.1 / +1 / +10（mm）", 11, E.Sec, true)));

            var inner = E.V();
            foreach (var g in m.Groups())
            {
                var grp = E.Group(g + "（" + m.GroupAxes(g).Count + " 轴）");
                foreach (var a in m.GroupAxes(g)) grp.Children.Add(AxisRow(a));
                inner.Children.Add(grp);
            }
            body.Children.Add(E.Scroll(inner, 430,
                ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto));
            return body;
        }

        /// <summary>标签页 2：IO 监视。</summary>
        private static Element TabIo()
        {
            var m = MotionManager.Instance;

            var inWrap = new WrapElement();
            foreach (var p in m.Inputs) inWrap.Children.Add(InChip(p));

            var outWrap = new WrapElement();
            foreach (var p in m.Outputs) outWrap.Children.Add(OutChip(p));

            // 点数 / 位号区间都取自真实 IO 表（交接资料 303 入 + 兼容点 / 123 出 + 兼容点），
            // 不再写死「32 点 IN0~IN31」。
            int inMax = m.Inputs.Count == 0 ? 0 : m.Inputs.Max(x => x.Bit);
            int outMax = m.Outputs.Count == 0 ? 0 : m.Outputs.Max(x => x.Bit);
            return E.V(
                E.T("输入信号（" + m.Inputs.Count + " 点，实时）—— 绿色 = 激活；红边表示低电平有效（急停 / 门禁类）", 11.5, E.Sec),
                E.Group("输入信号 全局位号 0 ~ " + inMax + "（真表 U0~U10 + 兼容段）", inWrap),
                E.T("输出信号（" + m.Outputs.Count + " 点）—— 点击 ⇄ 可手动翻转输出；带联动的输出会同时刷新对应输入反馈", 11.5, E.Sec),
                E.Group("输出信号 全局位号 0 ~ " + outMax + "（真表 U20~U24 + 兼容段）", outWrap),
                E.H(
                    E.MiniBtn("全部输出复位", Ops.ResetOutputs, 124),
                    E.T("输出→输入反馈映射：真空电磁阀1~8 → 真空反馈1~8；测试臂下压气缸 → 测试座下压到位；加热使能 → 加热器就绪",
                        10.5, E.Sec, false, TextWrapping.Wrap)));
        }

        /// <summary>标签页 3：安全互锁。</summary>
        private static Element TabSafety()
        {
            var m = MotionManager.Instance;

            var ruleBox = E.V();
            foreach (var r in m.Rules) ruleBox.Children.Add(RuleRow(r));

            // 软限位表
            var lim = E.V();
            foreach (var a in m.Axes)
            {
                lim.Children.Add(E.H(
                    E.T(a.Name, 11.5, E.P, false, TextWrapping.NoWrap).Wd(126),
                    E.T(a.MinMm.ToString("0.###"), 11.5, E.Sec).Wd(70),
                    E.T("~", 11.5, E.Sec).Wd(16),
                    E.T(a.MaxMm.ToString("0.###"), 11.5, E.Sec).Wd(70),
                    E.T(a.Unit, 11, E.Sec).Wd(30),
                    E.T("当前", 11, E.Sec).Wd(34),
                    Live("ax_pos_" + a.Index, "0.###", 74),
                    E.T(a.IsVertical ? "垂直轴" : (string.IsNullOrEmpty(a.RailId) ? "水平轴" : "水平轴 · 导轨 " + a.RailId),
                        10.5, E.Sec).Wd(150)));
            }

            return E.V(
                E.T("所有运动指令下发前都会先通过以下互锁裁决；任一条阻塞项不满足，指令被直接拒绝并写入信息记录。",
                    11.5, E.Sec, false, TextWrapping.Wrap),
                E.Group("安全互锁规则（SR-01 ~ SR-10）", ruleBox),
                E.Group("软限位与轴归属", E.Scroll(lim, 260, ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto)));
        }

        /// <summary>标签页 4：轨迹预览（动画画布 + 演示节拍）。</summary>
        private static Element TabTrace()
        {
            var canvas = BuildTraceCanvas();
            return E.V(
                E.H(
                    E.T("共享走廊：上料X轴 / 下料X轴（0~900mm）", 11, E.Sec).Wd(300),
                    E.T("小车导轨：小车 A / B（0~1200mm）", 11, E.Sec).Wd(260),
                    E.T("Z 轴高度（0~30mm）", 11, E.Sec)),
                canvas,
                E.H(
                    E.Btn("启动生产", () => Ops.StartProduction(), "PrimaryButton", 12),
                    E.Btn("暂停", () => Ops.PauseProduction(), "PrimaryButton", 12),
                    E.Btn("继续", () => Ops.ResumeProduction(), "PrimaryButton", 12),
                    E.Btn("停止", () => Ops.StopProduction(), "DangerButton", 12),
                    E.MiniBtn("单步", Ops.SingleStep, 56),
                    E.SpacerW(12),
                    E.T("当前步骤：", 11.5, E.Sec, true).Wd(72),
                    Live("demo_step", null, 300),
                    E.T("完成循环：", 11.5, E.Sec, true).Wd(76),
                    Live("demo_cycles", null, 60)),
                E.T("红色半透明带 = 上料臂的安全间隙区（80mm）；下料臂一旦进入该区域即触发 SR-08 互锁，"
                    + "运动指令被拒绝。演示节拍每一步都走同一套互锁，因此不会撞机。",
                    10.5, E.Sec, false, TextWrapping.Wrap));
        }

        // ==================================================================
        //  轨迹画布（逐帧重绘）
        // ==================================================================

        /// <summary>构建轨迹预览画布，并把逐帧重绘逻辑挂到 CanvasElement.Draw 上。</summary>
        private static CanvasElement BuildTraceCanvas()
        {
            const double W = 1000;
            const double H = 340;

            // 走廊几何：X 轴 0~900mm 映射到 px 60~940
            const double CorridorX0 = 60, CorridorX1 = 940, CorridorMm = 900;
            const double CorridorY = 62;
            double cScale = (CorridorX1 - CorridorX0) / CorridorMm;

            // 小车几何：0~1200mm 映射到 px 60~940
            const double ShuttleY = 176;
            const double ShuttleMm = 1200;
            double sScale = (CorridorX1 - CorridorX0) / ShuttleMm;

            // Z 柱几何
            const double ZBaseY = 322, ZMaxPx = 78, ZMm = 30;

            var cv = new CanvasElement { CanvasWidth = W, CanvasHeight = H };

            // ---- 静态底图 ----
            // 走廊底轨
            cv.Items.Add(new RectItem
            {
                X = CorridorX0, Y = CorridorY - 2, W = CorridorX1 - CorridorX0, H = 4,
                Fill = RailBrush, Radius = 2
            });
            cv.Items.Add(new TextItem
            {
                Text = "900mm", X = CorridorX1 - 60, Y = CorridorY - 22, TextSize = 10,
                Foreground = E.Sec, TextWidth = 60, Align = TextAlignment.Right
            });

            // 小车导轨底轨
            cv.Items.Add(new RectItem
            {
                X = CorridorX0, Y = ShuttleY - 2, W = CorridorX1 - CorridorX0, H = 4,
                Fill = RailBrush, Radius = 2
            });
            cv.Items.Add(new TextItem
            {
                Text = "1200mm", X = CorridorX1 - 64, Y = ShuttleY - 22, TextSize = 10,
                Foreground = E.Sec, TextWidth = 64, Align = TextAlignment.Right
            });

            // 干涉预警带（跟随上料臂，逐帧改 X）
            var danger = new RectItem
            {
                X = CorridorX0, Y = CorridorY - 34, W = 1, H = 68,
                Fill = DangerBand, Stroke = null, StrokeThickness = 0, Radius = 2
            };
            cv.Items.Add(danger);

            // 上料臂 / 下料臂标记
            var load = new RectItem
            {
                X = CorridorX0, Y = CorridorY - 34, W = 58, H = 30, Radius = 5,
                Fill = LoadBrush, Text = "上料臂", TextSize = 10.5, TextColor = Brushes.White
            };
            var unload = new RectItem
            {
                X = CorridorX0, Y = CorridorY + 4, W = 58, H = 30, Radius = 5,
                Fill = UnloadBrush, Text = "下料臂", TextSize = 10.5, TextColor = Brushes.White
            };
            cv.Items.Add(load);
            cv.Items.Add(unload);

            // 小车 A / B 标记
            var shA = new RectItem
            {
                X = CorridorX0, Y = ShuttleY - 30, W = 46, H = 26, Radius = 5,
                Fill = ShuttleABrush, Text = "小车A", TextSize = 10, TextColor = Brushes.White
            };
            var shB = new RectItem
            {
                X = CorridorX0, Y = ShuttleY + 6, W = 46, H = 26, Radius = 5,
                Fill = ShuttleBBrush, Text = "小车B", TextSize = 10, TextColor = Brushes.White
            };
            cv.Items.Add(shA);
            cv.Items.Add(shB);

            // Z 轴柱（上料 Z1 / 下料 Z1 / 测试 Z1）
            var zBars = new RectItem[3];
            var zLabels = new TextItem[3];
            string[] zNames = { "上料Z1", "下料Z1", "测试Z1" };
            // 轴号按真表名解析（旧写法硬编码 8 / 20 / 2）
            int[] zAxes =
            {
                MotionManager.Instance.AxisIndex("上料吸嘴1Z轴"),
                MotionManager.Instance.AxisIndex("下料吸嘴1Z轴"),
                MotionManager.Instance.AxisIndex("前压头Z轴")
            };
            for (int k = 0; k < 3; k++)
            {
                double x = 200 + k * 240;
                zBars[k] = new RectItem
                {
                    X = x, Y = ZBaseY, W = 46, H = 1, Radius = 3, Fill = ZBrush
                };
                cv.Items.Add(zBars[k]);
                zLabels[k] = new TextItem
                {
                    Text = zNames[k] + " 0.0mm", X = x - 14, Y = ZBaseY + 6, TextSize = 10.5,
                    Foreground = E.Sec, TextWidth = 74, Align = TextAlignment.Center
                };
                cv.Items.Add(zLabels[k]);
            }

            // 底部读数
            var readout = new TextItem
            {
                Text = "", X = 60, Y = 8, TextSize = 11.5, Foreground = E.P,
                TextWidth = 900, Align = TextAlignment.Left
            };
            cv.Items.Add(readout);

            // ---- 逐帧重绘 ----
            cv.Draw = c =>
            {
                var m = MotionManager.Instance;
                double lp = m.Position(m.AxisIndex("上料X轴")), up = m.Position(m.AxisIndex("下料X轴"));
                double sa = m.Position(m.AxisIndex("前梭车X轴")), sb = m.Position(m.AxisIndex("后梭车X轴"));
                double clearance = 80;

                load.X = CorridorX0 + lp * cScale - 29;
                unload.X = CorridorX0 + up * cScale - 29;

                // 干涉带跟随上料臂；下料臂落入则整体变红
                double half = clearance * cScale;
                danger.X = CorridorX0 + lp * cScale - half;
                danger.W = Math.Max(4, half * 2);
                bool conflict = Math.Abs(lp - up) < clearance;
                danger.Fill = conflict ? DangerBrush : DangerBand;

                shA.X = CorridorX0 + sa * sScale - 23;
                shB.X = CorridorX0 + sb * sScale - 23;

                for (int k = 0; k < 3; k++)
                {
                    int ax = zAxes[k];
                    double zp = m.Position(ax);
                    double h = Math.Max(1, zp / ZMm * ZMaxPx);
                    zBars[k].H = h;
                    zBars[k].Y = ZBaseY - h;
                    // 垂直轴数值越大越靠上；≥ ZUpMm 视为已抬起（安全）
                    zBars[k].Fill = zp >= m.Cfg(ax).ZUpMm ? ZBrush : DangerBrush;
                    zLabels[k].Text = zNames[k] + " " + zp.ToString("0.0") + "mm";
                }

                readout.Text = "上料臂 X = " + lp.ToString("0.0") + "mm　|　下料臂 X = " + up.ToString("0.0")
                               + "mm　|　间距 = " + Math.Abs(lp - up).ToString("0.0") + "mm（安全间隙 80mm）"
                               + "　|　小车A = " + sa.ToString("0.0") + "mm　|　小车B = " + sb.ToString("0.0") + "mm";
            };

            // 立即画出第一帧，保证静态截图（自检）里也有内容
            cv.Refresh();

            return cv;
        }

        // ==================================================================
        //  页面 2：控制卡设置
        // ==================================================================

        /// <summary>控制卡设置页（motion_card）：卡型 / 脉冲当量 / 软限位 / 加减速 / 回零。</summary>
        public static Element CardPage()
        {
            var m = MotionManager.Instance;
            CaptureFactoryAxes();   // 首次打开本页时抓一次出厂参数

            // 卡型选择（纯展示 + 提示，真实卡型由 LTDMC.dll 探测结果决定）
            var cardCmb = E.Cmb("mc_cardtype",
                new[] { "Leadshine DMC2410（4 轴）", "Leadshine DMC5480（8 轴）", "Leadshine DMC5800（32 轴）", "内置仿真卡" },
                260, 2);

            var head = E.Band("控制卡", E.BandBlue,
                E.H(
                    E.T("卡型", 12, E.Sec).Wd(46), cardCmb,
                    E.T("实际连接", 12, E.Sec).Wd(70), Live("card_name", null, 220),
                    E.T("状态", 12, E.Sec).Wd(40),
                    E.LiveLamp(_ => MotionManager.Instance.Connected, false, false, 13),
                    E.T("模式", 12, E.Sec).Wd(40), Live("card_mode", null, 84)),
                E.H(
                    E.Btn("连接 / 探测控制卡", Ops.ConnectCard, "PrimaryButton", 12),
                    E.MiniBtn("强制仿真", Ops.UseSimCard, 84),
                    E.MiniBtn("断开", Ops.DisconnectCard, 56),
                    E.T("说明：优先通过 LTDMC.dll 探测雷赛 DMC 卡；探测失败自动降级为内置仿真卡。",
                        11, E.Sec, false, TextWrapping.Wrap)));

            // 轴参数表（脉冲当量 / 软限位 / 加减速 / 回零位置）
            var rows = E.V();
            rows.Children.Add(E.H(
                E.T("轴名", 11.5, E.Sec, true).Wd(126),
                E.T("脉冲当量", 11.5, E.Sec, true).Wd(74),
                E.T("下限", 11.5, E.Sec, true).Wd(62),
                E.T("上限", 11.5, E.Sec, true).Wd(62),
                E.T("最大速度", 11.5, E.Sec, true).Wd(70),
                E.T("加速度", 11.5, E.Sec, true).Wd(70),
                E.T("回零位", 11.5, E.Sec, true).Wd(62),
                E.T("当前", 11.5, E.Sec, true).Wd(70),
                E.T("归属", 11.5, E.Sec, true)));

            foreach (var a in m.Axes)
            {
                int idx = a.Index;
                var cfg = m.Cfg(idx);
                rows.Children.Add(E.H(
                    E.T(a.Name, 11.5, E.P, false, TextWrapping.NoWrap).Wd(126),
                    E.Num("mc_eq_" + idx, cfg.PulseEquivalent, 70, "0", 0, 100000),
                    E.Num("mc_min_" + idx, cfg.MinMm, 58, "0.###"),
                    E.Num("mc_max_" + idx, cfg.MaxMm, 58, "0.###"),
                    E.Num("mc_vel_" + idx, cfg.MaxVel, 66, "0"),
                    E.Num("mc_acc_" + idx, cfg.Acc, 66, "0"),
                    E.Num("mc_home_" + idx, cfg.HomeMm, 58, "0.###"),
                    Live("ax_pos_" + idx, "0.###", 70),
                    E.T(cfg.Group + (cfg.IsVertical ? " · 垂直" : ""), 10.5, E.Sec).Wd(110)));
            }

            var apply = E.H(
                E.Btn("应用轴参数", () => ApplyAxisParams(), "PrimaryButton", 12),
                E.MiniBtn("恢复出厂参数", () => RestoreAxisParams(), 116),
                E.T("说明：应用后会把脉冲当量 / 软限位 / 速度曲线重新下发到控制卡（d1000_set_equiv、"
                    + "d1000_set_soft_limit、d1000_set_profile）。",
                    10.5, E.Sec, false, TextWrapping.Wrap));

            // 回零设置
            var homeBox = E.Group("回零设置",
                E.Row("回零方式", E.Cmb("mc_homemode", new[] { "负向回零（原点在负方向）", "正向回零（原点在正方向）", "回到预设回零位置" }, 240, 2), 96),
                E.Row("回零低速", E.Num("mc_homevel", 20, 90, "0"), 96, "mm/s"),
                E.Row("回零加速时间", E.Num("mc_hometacc", 0.1, 90, "0.##"), 96, "s"),
                E.H(E.MiniBtn("执行全部归零", () => Ops.HomeAll(), 116),
                      E.MiniBtn("取消归零", Ops.CancelHome, 96)));

            // 诊断
            var diag = E.Group("控制卡诊断",
                E.H(E.T("指令计数", 11.5, E.Sec).Wd(70), Live("move_count", null, 70),
                    E.T("安全拒绝", 11.5, E.Sec).Wd(70), Live("reject_count", null, 70),
                    E.T("急停状态", 11.5, E.Sec).Wd(70), Live("estop_text", null, 70),
                    E.T("最近裁决", 11.5, E.Sec).Wd(70), Live("last_reject", null, 520, Palette.WarningBg, E.P, 11.5)));

            return E.V(
                head,
                E.Group("轴参数（脉冲当量 / 软限位 / 速度曲线 / 回零位置）",
                    E.Scroll(rows, 420, ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto)),
                apply,
                E.H(homeBox, E.SpacerW(12), diag));
        }

        /// <summary>把界面上的轴参数应用到控制卡。</summary>
        private static void ApplyAxisParams()
        {
            var m = MotionManager.Instance;
            var mm = MachineModel.Instance;

            // 运行中不允许改轴参数（软限位/速度曲线改了会直接影响正在跑的轨迹）
            if (Ops.IsRunning) { Ops.Warn("运行中不允许修改轴参数，请先停止"); return; }

            int fixedSwaps = 0, badHome = 0;
            foreach (var a in m.Axes)
            {
                int i = a.Index;
                double oldMin = a.MinMm, oldMax = a.MaxMm;

                a.PulseEquivalent = Math.Max(1, mm.GetNum("mc_eq_" + i, a.PulseEquivalent));
                a.MinMm = mm.GetNum("mc_min_" + i, a.MinMm);
                a.MaxMm = mm.GetNum("mc_max_" + i, a.MaxMm);
                a.MaxVel = Math.Max(1, mm.GetNum("mc_vel_" + i, a.MaxVel));
                a.Acc = Math.Max(1, mm.GetNum("mc_acc_" + i, a.Acc));
                a.Dec = a.Acc;
                a.HomeMm = mm.GetNum("mc_home_" + i, a.HomeMm);

                // 限位写反是现场最常见的低级错误：直接对调并提示
                if (a.MinMm > a.MaxMm)
                {
                    double t = a.MinMm; a.MinMm = a.MaxMm; a.MaxMm = t;
                    mm.SetNum("mc_min_" + i, a.MinMm);
                    mm.SetNum("mc_max_" + i, a.MaxMm);
                    fixedSwaps++;
                    Ops.Warn(a.Name + " 软限位上下限写反（" + oldMin.ToString("0.###")
                             + " > " + oldMax.ToString("0.###") + "），已自动对调");
                }

                // 回零位必须落在软限位区间内，否则归零会立刻被互锁拒绝
                if (a.HomeMm < a.MinMm || a.HomeMm > a.MaxMm)
                {
                    a.HomeMm = Math.Clamp(a.HomeMm, a.MinMm, a.MaxMm);
                    mm.SetNum("mc_home_" + i, a.HomeMm);
                    badHome++;
                    Ops.Warn(a.Name + " 回零位超出软限位，已夹到 " + a.HomeMm.ToString("0.###"));
                }

                m.Card?.Configure(a);
            }
            Ops.PullAxisConfigsToModel();
            mm.Log("信息", "轴参数已应用到控制卡（" + m.Axes.Count + " 轴"
                   + (fixedSwaps > 0 ? "，修正 " + fixedSwaps + " 处限位反接" : "")
                   + (badHome > 0 ? "，修正 " + badHome + " 处回零位越界" : "") + "）");
            Ops.SaveParams();
        }

        /// <summary>恢复出厂轴参数：回到出厂快照，并重新下发到控制卡。</summary>
        private static void RestoreAxisParams()
        {
            CaptureFactoryAxes();
            var mm = MachineModel.Instance;
            var m = MotionManager.Instance;

            foreach (var a in m.Axes)
            {
                var f = _factoryAxes.FirstOrDefault(x => x.Index == a.Index);
                if (f == null) continue;
                int i = a.Index;
                a.PulseEquivalent = f.PulseEquivalent;
                a.MinMm = f.MinMm;
                a.MaxMm = f.MaxMm;
                a.HomeMm = f.HomeMm;
                a.MaxVel = f.MaxVel;
                a.Acc = f.Acc;
                a.Dec = f.Dec;

                mm.SetNum("mc_eq_" + i, a.PulseEquivalent);
                mm.SetNum("mc_min_" + i, a.MinMm);
                mm.SetNum("mc_max_" + i, a.MaxMm);
                mm.SetNum("mc_vel_" + i, a.MaxVel);
                mm.SetNum("mc_acc_" + i, a.Acc);
                mm.SetNum("mc_home_" + i, a.HomeMm);

                m.Card?.Configure(a);
            }
            // 同步到 Ops 的 axN_* 参数域，保持两套键名一致
            Ops.PullAxisConfigsToModel();
            mm.Log("信息", "已恢复出厂轴参数并重新下发控制卡（" + m.Axes.Count + " 轴）");
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
