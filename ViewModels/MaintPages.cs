using System;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // ==================================================================
    //  “维护配置”工具条：工作基点 / 安全设定 / 温度偏移 / 接触力校准 /
    //  用户管理 / 信号灯设定   —— 复刻 image82 ~ image106
    // ==================================================================
    public static class MaintPages
    {
        private static CellElement Head(string s, double w = 0)
            => new CellElement
            {
                Background = E.HeadBlue, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = 26,
                Width = w > 0 ? w : double.NaN,
                Padding = new Thickness(3, 1, 3, 1),
                Content = E.T(s, 11.5, E.P, true, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center)
            };

        private static CellElement Label(string s, double h = 26, TextAlignment ta = TextAlignment.Left, Brush bg = null)
            => new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Padding = new Thickness(4, 1, 4, 1),
                Content = E.T(s, 11.5, E.P, false, TextWrapping.NoWrap, ta, HorizontalAlignment.Center)
            };

        private static CellElement Cell(Element e, double h = 26, Brush bg = null)
        {
            if (h < 34) e.Compact();   // 单元格较矮：输入框用紧凑样式，否则文本会被裁切
            return new CellElement
            {
                Background = bg ?? E.CardBg, BorderBrush = Palette.LineGrey, BorderThickness = 0.5, Height = h,
                Content = e
            };
        }

        /// <summary>数值框 + 取当前位置按钮（手册中的 [&lt;]）。</summary>
        private static Element Val(string key, double def, double w = 86)
            => E.H(E.Num(key, def, w), E.MiniBtn("<", () => E.M.Log("信息", "获取当前位置 → " + key), 28));

        private static Element ValCell(string key, double def, double w = 86, double h = 28)
            => Cell(E.H(E.Num(key, def, w), E.MiniBtn("<", () => E.M.Log("信息", "获取当前位置 → " + key), 28)), h);

        // ==================================================================
        //  1. 工作基点 (image82 / image84 / image93)
        // ==================================================================
        public static Element MaintBase()
        {
            return E.Tabs(
                ("工作基点设定-测试区域", TestAreaBase),
                ("工作基点设定-上料臂", () => ArmBase(true)),
                ("工作基点设定-下料臂", () => ArmBase(false)),
                ("工作基点设定-料盘搬运臂", TrayArmBase));
        }

        // ---- 测试区域 ----
        private static Element TestAreaBase()
        {
            var car = E.Grid().Cols("86", "112", "46", "112", "46", "112", "46");
            car.Add(Label("", 26, TextAlignment.Left, E.CardBg), 0, 0);
            car.Add(Label("入口", 26, TextAlignment.Center, E.CardBg), 0, 1);
            car.Add(Label("", 26, TextAlignment.Left, E.CardBg), 0, 2);
            car.Add(Label("读码器(中心)", 26, TextAlignment.Center, E.CardBg), 0, 3);
            car.Add(Label("", 26, TextAlignment.Left, E.CardBg), 0, 4);
            car.Add(Label("出口", 26, TextAlignment.Center, E.CardBg), 0, 5);
            car.Add(Label("", 26, TextAlignment.Left, E.CardBg), 0, 6);
            string[] cars = { "小车A", "小车B" };
            double[] inPos = { -2.11, -2.11 }, bcr = { 202.28, 204.62 }, outPos = { 408.03, 407.94 };
            for (int r = 0; r < 2; r++)
            {
                car.Add(Label(cars[r]), r + 1, 0);
                car.Add(ValCell("mb_car_in_" + r, inPos[r]), r + 1, 1);
                car.Add(Label("", 26), r + 1, 2);
                car.Add(ValCell("mb_car_bcr_" + r, bcr[r]), r + 1, 3);
                car.Add(Label("", 26), r + 1, 4);
                car.Add(ValCell("mb_car_out_" + r, outPos[r]), r + 1, 5);
                car.Add(Label("", 26), r + 1, 6);
            }

            var gate = E.Grid().Cols("86", "112", "46", "112", "46", "112", "46");
            gate.Add(Label("", 26), 0, 0);
            gate.Add(Label("入口(中心)", 26, TextAlignment.Center, E.CardBg), 0, 1);
            gate.Add(Label("", 26), 0, 2);
            gate.Add(Label("出口横向(中心)", 26, TextAlignment.Center, E.CardBg), 0, 3);
            gate.Add(Label("", 26), 0, 4);
            gate.Add(Label("出口纵向(中心)", 26, TextAlignment.Center, E.CardBg), 0, 5);
            gate.Add(Label("", 26), 0, 6);
            double[] g1 = { 172.26, 171.86 }, g2 = { 237.06, 236.42 }, g3 = { 239.18, 237.66 };
            for (int r = 0; r < 2; r++)
            {
                gate.Add(Label(cars[r]), r + 1, 0);
                gate.Add(ValCell("mb_gate_in_" + r, g1[r]), r + 1, 1);
                gate.Add(Label("", 26), r + 1, 2);
                gate.Add(ValCell("mb_gate_h_" + r, g2[r]), r + 1, 3);
                gate.Add(Label("", 26), r + 1, 4);
                gate.Add(ValCell("mb_gate_v_" + r, g3[r]), r + 1, 5);
                gate.Add(Label("", 26), r + 1, 6);
            }

            var arm = E.Grid().Cols("86", "112", "46", "118", "46", "118", "46", "100", "46");
            arm.Add(Label("", 26), 0, 0);
            arm.Add(Label("至小车", 26, TextAlignment.Center, E.CardBg), 0, 1);
            arm.Add(Label("", 26), 0, 2);
            arm.Add(Label("至测试位(单臂压测)", 26, TextAlignment.Center, E.CardBg), 0, 3);
            arm.Add(Label("", 26), 0, 4);
            arm.Add(Label("至测试位(双臂压测)", 26, TextAlignment.Center, E.CardBg), 0, 5);
            arm.Add(Label("", 26), 0, 6);
            arm.Add(Label("至安全位", 26, TextAlignment.Center, E.CardBg), 0, 7);
            arm.Add(Label("", 26), 0, 8);
            double[] y1 = { -2.347, 232.642, 162.142, 0 }, y2 = { 2.03, -232.75, -162.25, 0 };
            for (int r = 0; r < 2; r++)
            {
                arm.Add(Label("Y" + (r + 1)), r + 1, 0);
                arm.Add(ValCell("mb_y" + r + "_car", r == 0 ? y1[0] : y2[0]), r + 1, 1);
                arm.Add(Label("", 26), r + 1, 2);
                arm.Add(ValCell("mb_y" + r + "_1", r == 0 ? y1[1] : y2[1]), r + 1, 3);
                arm.Add(Label("", 26), r + 1, 4);
                arm.Add(ValCell("mb_y" + r + "_2", r == 0 ? y1[2] : y2[2]), r + 1, 5);
                arm.Add(Label("", 26), r + 1, 6);
                arm.Add(ValCell("mb_y" + r + "_safe", 0), r + 1, 7);
                arm.Add(Label("", 26), r + 1, 8);
            }
            arm.Add(Label("Z1基准点", 28, TextAlignment.Right), 3, 3);
            arm.Add(ValCell("mb_z1_base", -148.864), 3, 5);
            arm.Add(Label("", 28), 3, 6);
            arm.Add(Label("Z2基准点", 28, TextAlignment.Right), 4, 3);
            arm.Add(ValCell("mb_z2_base", -148.265), 4, 5);
            arm.Add(Label("", 28), 4, 6);

            return E.V(
                
                E.Group("运料小车(mm)", car),
                E.Group("Gate基点(以第一列为基准)", gate),
                E.Group("测试臂(mm)", arm),
                E.H(E.MiniBtn("恢复", () => E.M.Log("信息", "恢复测试区域工作基点")),
                    E.Btn("应用", () => E.M.Log("信息", "测试区域工作基点已应用"))),
                E.Group("说明",
                    E.T("运料小车入口：小车运行到上料区域接料位置，同时下料运料小车 1/2 位于测试臂放料位置；出口：小车运行到下料区域出料位置，同时上料运料小车 1/2 位于测试臂取料位置；读码器为选装功能，获取小车第一个槽为拍照基点。\n" +
                        "Gate 基点：获取小车 A/B 的入口、出口、纵向检测位置的基点。\n" +
                        "Z1/Z2 基准点：测试臂及小车未上 Kit 时，设定测试臂 Kit 安装基面与小车入料托盘安装基面重合时 Z 轴坐标为零点，此位置也是“高位”位置。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---- 上料臂 / 下料臂 ----
        private static Element ArmBase(bool loader)
        {
            string p = loader ? "mb_ld" : "mb_ul";
            string head = loader ? "#3" : "#2";
            string title = loader ? "上料臂" : "下料臂";

            string[] xyRows = loader
                ? new[] { "小车A入口(中心)", "小车B入口(中心)", "上料盘入口(中心)", "上料入盘CDA底部清洁(中心)",
                         "预热盘1(右下角)", "预热盘2(右下角)", "上料旋转台(中心)", "清洁盘(左上角)", "入料盘(左上角)" }
                : new[] { "#0-空闲位", "#1-小车A出口(中心)", "#2-小车B出口(中心)", "#3-自动盘1-B0点(左上角)",
                         "#6-自动盘2-B0点(左上角)", "#9-自动盘3-B0点(左上角)", "#12-固定盘1-B0点(左上角)",
                         "#15-固定盘2-B0点(左上角)", "#18-固定盘3-B0点(左上角)", "#21-旋转台(左上角)" };
            double[,] xyVal = loader
                ? new[,] { { 354.717, 32.25 }, { 355.811, 503.13 }, { 0.0, 0.0 }, { 0.0, 0.0 },
                           { 173.907, 430.905 }, { 174.078, 840.413 }, { 370.125, 201.713 }, { 270.756, 222.165 }, { 259.056, 642.69 } }
                : new[,] { { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 }, { 0.0, 0.0 } };

            var xy = E.Grid().Cols("190", "112", "112");
            xy.Add(Head("基点描述"), 0, 0);
            xy.Add(Head("X(mm)"), 0, 1);
            xy.Add(Head("Y(mm)"), 0, 2);
            for (int r = 0; r < xyRows.Length; r++)
            {
                xy.Add(Label(xyRows[r]), r + 1, 0);
                xy.Add(Cell(E.Num(p + "_x_" + r, xyVal[r, 0], 104)), r + 1, 1);
                xy.Add(Cell(E.Num(p + "_y_" + r, xyVal[r, 1], 104)), r + 1, 2);
            }

            string[] zRows = loader
                ? new[] { "小车A入口", "小车B入口", "预热盘1", "预热盘2", "上料旋转台", "清洁盘", "上料TrayLoader" }
                : new[] { "小车A出口", "小车B出口", "预热盘1", "预热盘2", "自动盘1", "自动盘2", "自动盘3",
                          "固定盘1", "固定盘2", "固定盘3", "旋转台" };

            var zg = E.Grid();
            zg.Cols("170", "78", "78", "78", "78", "78", "78", "78", "78");
            zg.Add(Head("基点描述", 170), 0, 0);
            for (int c = 0; c < 8; c++) zg.Add(Head("Z" + (c + 1) + "(mm)"), 0, c + 1);
            for (int r = 0; r < zRows.Length; r++)
            {
                zg.Add(Label(zRows[r]), r + 1, 0);
                for (int c = 0; c < 8; c++)
                    zg.Add(Cell(E.Num(p + "_z" + (c + 1) + "_" + r, 0, 72)), r + 1, c + 1);
            }

            // Z1~Z8 输入 + Top / TZ
            int zBase = zRows.Length + 1;
            for (int c = 0; c < 8; c++)
            {
                zg.Add(Cell(E.H(E.T("Z" + (c + 1), 11.5, E.P, false, TextWrapping.NoWrap), E.Num(p + "_zin" + c, 0, 66)), 28), zBase, c + 1);
                zg.Add(Cell(E.H(E.MiniBtn("Top", null, 40), E.MiniBtn("TZ", null, 40)), 28), zBase + 1, c + 1);
            }

            var zBox = E.V(
                E.Scroll(zg, 250, System.Windows.Controls.ScrollBarVisibility.Auto,
                    System.Windows.Controls.ScrollBarVisibility.Auto),
                E.Spacer(6),
                E.Chk(p + "_quick", "Get Quickly", false),
                E.Spacer(6),
                E.H(
                    E.T("归零后间距PX", 11.5, E.P, false, TextWrapping.NoWrap), E.Num(p + "_px", loader ? 39.25 : 0, 86),
                    E.T("归零后间距PY", 11.5, E.P, false, TextWrapping.NoWrap), E.Num(p + "_py", loader ? 58.5 : 0, 86)),
                E.Spacer(8),
                E.H(
                    E.MiniBtn("更改", () => E.M.Log("信息", title + "：更改基点")),
                    E.MiniBtn("获取", () => E.M.Log("信息", title + "：获取当前位置")),
                    E.MiniBtn("恢复", null), E.MiniBtn("恢复全部", null),
                    E.MiniBtn("Top-All", null), E.MiniBtn("TZ-All", null),
                    E.Btn("应用", () => E.M.Log("信息", title + "工作基点已应用"))),
                E.T("当前选中位置为：" + (loader ? "上料TrayLoader" : "自动盘1-B0点(左上角)"), 11.5, E.Sec));

            return E.V(
                E.T(title + "工作基点设定（以吸头 " + head + " 为基准，方向以站在机器操作员位置从上往下看为准）。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(8),
                E.Grid().Cols("500", "*")
                    .Add(E.Group("X-Y轴(mm)",
                        E.Scroll(xy, 300, System.Windows.Controls.ScrollBarVisibility.Auto,
                            System.Windows.Controls.ScrollBarVisibility.Auto),
                        E.Spacer(6),
                        E.H(
                            E.T("X:", 12, E.P, false, TextWrapping.NoWrap), E.Num(p + "_cx", 259.056, 96),
                            E.T("Y:", 12, E.P, false, TextWrapping.NoWrap), E.Num(p + "_cy", 642.69, 96)),
                        E.Spacer(6),
                        E.H(E.MiniBtn("获取", null), E.MiniBtn("更改", null),
                            E.MiniBtn("恢复", null), E.MiniBtn("恢复全部", null)),
                        E.Spacer(4),
                        E.T("当前选中位置为：入料盘(左上角)", 11.5, E.Sec)), 0, 0)
                    .Add(E.Group("Z1/Z2/Z3/Z4/Z5/Z6/Z7/Z8(mm)", zBox), 0, 1),
                E.Group("说明",
                    E.T("X/Y 文本框：当上方 X-Y 轴基点列表框中的当前激活项改变时，此文本框内的值自动变更为激活项的值；" +
                        "点击[获取]按钮将当前 X-Y 轴坐标值自动填入 X/Y 文本框内；点击[更改]按钮时，使用当前 X/Y 文本框内值替换更改列表框内激活项的值。\n" +
                        "Get Quickly：吸头高度基点快速获取功能。机台机械位置调整过后，机台内所有位置的平面和料臂之间是相对水平的，先不勾选 Get Quickly 正常获取小车A位置的所有吸头基点并应用；" +
                        "之后勾选 Get Quickly，选择其他位置，点击【基准吸头】基点的获取，其他吸头的基点位置将自动计算，之后更改应用。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---- 料盘搬运臂 ----
        private static Element TrayArmBase()
        {
            var g = E.Grid().Cols("150", "112", "46");
            g.Add(Head("位置"), 0, 0);
            g.Add(Head("基点(mm)"), 0, 1);
            g.Add(Head(""), 0, 2);
            string[] pos = { "Safe", "Loader", "Empty", "Buffer", "Auto1", "Auto2", "Auto3" };
            double[] val = { -722.990, -1274.880, -950.520, -722.990, -400.100, -208.750, -18.330 };
            for (int r = 0; r < pos.Length; r++)
            {
                g.Add(Label(pos[r]), r + 1, 0);
                g.Add(Cell(E.Num("mb_ta_" + pos[r], val[r], 104)), r + 1, 1);
                g.Add(Cell(E.MiniBtn("<", () => E.M.Log("信息", "获取当前位置 → " + pos[r]), 28)), r + 1, 2);
            }

            return E.V(
                
                E.Group("料盘传送臂", g),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "料盘传送臂工作基点已应用"))),
                E.Group("说明",
                    E.T("Safe：料盘传送臂的安全位基点。Loader / Empty / Buffer / Auto1 / Auto2 / Auto3：料盘传送臂在对应位置的基点。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  2. 安全设定
        // ==================================================================
        public static Element MaintSafe()
        {
            return E.Tabs(
                ("通用", SafeGeneral),
                ("测试区域", SafeTestArea),
                ("上/下料臂", SafeArm),
                ("进出盘", SafeTray));
        }

        private static Element SafeGeneral()
        {
            return E.V(
                E.Group("登出与权限",
                    E.Chk("sf_auto_logout", "是否自动登出", false),
                    E.H(E.T("自动登出时间(分钟)", 11.5, E.P, false, TextWrapping.NoWrap), E.Num("sf_logout_min", 30, 86)),
                    E.Chk("sf_only_normal", "只在正常模式下登出", true),
                    E.Chk("sf_switch_normal", "切换至正常模式登出", true),
                    E.Chk("sf_bcr_only", "用户登录BCR限定", false)),
                E.Group("启动与循环",
                    E.Row("慢速启动速度", E.Num("sf_slow_spd", 20, 86), 160, "%"),
                    E.Row("循环周期", E.Num("sf_loop_cnt", 3, 86, "0"), 160, "次"),
                    E.Chk("sf_batch_check", "开批输入检查（检查 PN 或 OP 是否为空）", true),
                    E.Chk("sf_lock_bin", "生产时禁止修改BIN设定和运行模式", true),
                    E.Chk("sf_bin_legal", "BIN合法检查（Online 时好品次品不能放同一个盘）", true)),
                E.Group("物料与报警",
                    E.Chk("sf_ion_check", "离子风机检测", true),
                    E.Chk("sf_door_ion", "开门自动关闭离子风机", true),
                    E.Chk("sf_skip_retry", "Skipped IC复检", true),
                    E.Chk("sf_heat_empty", "检查加热盘空", false),
                    E.Chk("sf_fixed_offset", "固定偏移（切换品种时偏移参数不变动）", false),
                    E.Row("QA取样后续提示时间", E.Num("sf_qa_hint", 0, 86, "0"), 170, "min（0 为关闭）"),
                    E.Row("定时提示时间", E.Num("sf_timer_hint", 0, 86, "0"), 170, "min（0 为关闭）"),
                    E.Row("离子风机清洁提示间隔", E.Num("sf_ion_clean", 720, 86, "0"), 170, "h")),
                E.Group("FTP Server",
                    E.Row("FTP 地址", E.Text("sf_ftp_ip", "192.168.0.100", 190), 110),
                    E.Row("Server path", E.Text("sf_ftp_path", "/handler/data", 260), 110),
                    E.Row("Userid", E.Text("sf_ftp_user", "handler", 190), 110),
                    E.Row("Password", E.Text("sf_ftp_pwd", "", 190), 110),
                    E.Row("Upload", E.Text("sf_ftp_upload", "/handler/upload", 260), 110)),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "安全设定（通用）已应用"))));
        }

        private static Element SafeTestArea()
        {
            return E.V(
                E.Group("报警与处理",
                    E.Row("开门处理时间", E.Num("st_door_t", 30, 86, "0"), 170, "s"),
                    E.Chk("st_skt_empty", "测试座空检查", true),
                    E.Chk("st_skt_blow", "测试座检查失败吹掉料", false),
                    E.Chk("st_ch_recheck", "强制取消“测试通道检查”@复测-连续模式", false),
                    E.Chk("st_auto_clear", "自动清机@良率报警（先清机，后报警）", true),
                    E.Chk("st_pick_fail_skip", "取料失败跳过料空确认", false),
                    E.Chk("st_arm_drop_home", "测试臂掉料自动归零", true)),
                E.Group("限定值",
                    E.Row("管腿力限定", E.Num("st_leg_force", 15, 86), 170, "kgf"),
                    E.Row("校准高度调整限定", E.Num("st_cal_h", 5, 86), 170, "mm"),
                    E.Row("连续次品判定-相同BIN 阈值", E.Num("st_cont_bin", 5, 86, "0"), 200, "次"),
                    E.Chk("st_cont_same_bin", "连续次品判定-相同BIN", true)),
                E.Group("加热与温度",
                    E.Chk("st_arm_hold", "测试臂提起等待（加热模式保持 IC 温度稳定）", false)),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "安全设定（测试区域）已应用"))));
        }

        private static Element SafeArm()
        {
            return E.V(
                E.Group("偏移限定",
                    E.Row("允许最小偏移", E.Num("sa_off_min", -5, 86), 150, "mm"),
                    E.Row("允许最大偏移", E.Num("sa_off_max", 5, 86), 150, "mm")),
                E.Group("吸取重试限定",
                    E.Row("重试次数", E.Num("sa_retry_cnt", 3, 86, "0"), 150, "次"),
                    E.Row("每次下行距离", E.Num("sa_retry_dz", 0.2, 86), 150, "mm")),
                E.Group("其它",
                    E.Chk("sa_last_seq", "最后料盘顺序放置IC@清出", false)),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "安全设定（上/下料臂）已应用"))));
        }

        private static Element SafeTray()
        {
            return E.V(
                E.Group("进出盘设定",
                    E.Chk("sr_manual_tray", "手动取盘（跳过IC）：Loader 上有【取料失败跳过】报警，盘空后需手动取走，不会自动换盘", false),
                    E.Chk("sr_knock_loader", "启用上料盘敲击（选装）", false),
                    E.Chk("sr_knock_auto", "启用自动盘敲击（选装）", false),
                    E.Chk("sr_fwd_pick", "删料后正向取料@入料盘", true)),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "安全设定（进出盘）已应用"))));
        }

        // ==================================================================
        //  3. 温度偏移
        // ==================================================================
        public static Element MaintTempOffset()
        {
            string[] zones = { "预热盘 #1", "预热盘 #2", "小车 A", "小车 B",
                               "测试臂 A1", "测试臂 A2", "测试臂 A3", "测试臂 A4",
                               "测试臂 A5", "测试臂 A6", "测试臂 A7", "测试臂 A8",
                               "测试臂 B1", "测试臂 B2", "测试臂 B3", "测试臂 B4",
                               "测试臂 B5", "测试臂 B6", "测试臂 B7", "测试臂 B8",
                               "测试板 1", "测试板 2" };

            var g = E.Grid().Cols("190", "112", "112");
            g.Add(Head("目标区域"), 0, 0);
            g.Add(Head("偏移值(°C)"), 0, 1);
            g.Add(Head("实测值(°C)"), 0, 2);
            for (int r = 0; r < zones.Length; r++)
            {
                g.Add(Label(zones[r]), r + 1, 0);
                g.Add(Cell(E.Num("mt_off_" + r, 0, 104)), r + 1, 1);
                g.Add(Cell(E.Live(mm => mm.TempSet, "0.0", 104, E.BandGrey, E.P, 11.5)), r + 1, 2);
            }

            return E.V(
                E.T("温度偏移值校准参数：设定高温模式下各个目标区域的温度偏差值。当目标温度比设定温度低时，设定值为正值，否则为负值。" +
                    "如目标区域实际测定值为 80 度而设定值为 95 度，则在该区域的偏移值处输入 +15。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(10),
                E.Group("校准方式",
                    E.Radios("mt_mode", new[] { "1点", "2点", "3点" }, false, 0),
                    E.Spacer(6),
                    E.T("1点：温度控制器在加热模式时对温度的校准只使用基点#3的偏移值。\n" +
                        "2点：根据基点#1和基点#3的偏移值计算目标温度偏移值。\n" +
                        "3点：分为 2 个范围，目标温度小于基点#2时根据基点#1和#2计算，大于基点#2时根据基点#2和#3计算。",
                        11.5, E.Sec, false, TextWrapping.Wrap)),
                E.Group("基点设定（要求 基点#1 < 基点#2 < 基点#3）",
                    E.Grid().Cols("150", "112", "150", "112", "150", "112")
                        .Add(E.Row("基点#1", E.Num("mt_b1", 25, 96), 100), 0, 0)
                        .Add(E.Row("基点#2", E.Num("mt_b2", 60, 96), 100), 0, 2)
                        .Add(E.Row("基点#3", E.Num("mt_b3", 95, 96), 100), 0, 4)
                        .Add(E.Row("基点#4", E.Num("mt_b4", 125, 96), 100), 1, 0)),
                E.Group("目标区域偏移值", E.Scroll(g, 380, System.Windows.Controls.ScrollBarVisibility.Disabled,
                    System.Windows.Controls.ScrollBarVisibility.Auto)),
                E.H(E.MiniBtn("恢复", null), E.Btn("应用", () => E.M.Log("信息", "温度偏移参数已应用"))));
        }

        // ==================================================================
        //  4. 接触力校准 (image104)
        // ==================================================================
        public static Element MaintForce()
        {
            double[] force = { 0.00, 0.58, 1.16, 1.74, 2.32, 2.90, 3.48, 4.06, 4.64, 5.22,
                               5.80, 6.38, 6.96, 7.54, 8.12, 8.70, 9.28, 9.86, 10.44, 11.02 };
            double[] a1 = { 2.000, 2.000, 2.000, 2.000, 1.900, 2.000, 2.100, 2.300, 2.300, 2.300,
                            2.300, 2.300, 2.300, 2.300, 2.400, 2.400, 2.500, 2.600, 2.800, 2.800 };
            double[] a2 = { 1.600, 1.500, 1.500, 1.500, 1.500, 1.500, 2.300, 2.700, 2.700, 2.400,
                            2.500, 2.500, 2.700, 2.600, 2.900, 2.800, 3.300, 3.300, 3.700, 3.700 };

            var g = E.Grid().Cols("170", "170", "170");
            g.Add(Head("接触力(kgf)"), 0, 0);
            g.Add(Head("测试臂#1"), 0, 1);
            g.Add(Head("测试臂#2"), 0, 2);
            for (int r = 0; r < force.Length; r++)
            {
                g.Add(Cell(E.T(force[r].ToString("0.00"), 11.5, E.P, false, TextWrapping.NoWrap, TextAlignment.Center, HorizontalAlignment.Center), 24), r + 1, 0);
                g.Add(Cell(E.Num("mf_a1_" + r, a1[r], 158, "0.000"), 24), r + 1, 1);
                g.Add(Cell(E.Num("mf_a2_" + r, a2[r], 158, "0.000"), 24), r + 1, 2);
            }

            var right = E.V(
                E.H(E.MiniBtn("Default", null, 96)),
                E.Spacer(8),
                E.Grid().Cols("120", "86", "56", "96", "56")
                    .Add(E.T("", 11), 0, 0)
                    .Add(Head("Socket"), 0, 1)
                    .Add(E.T("kgf", 11.5, E.P, false, TextWrapping.NoWrap), 0, 2)
                    .Add(Head("Shuttle"), 0, 3)
                    .Add(E.T("kgf", 11.5, E.P, false, TextWrapping.NoWrap), 0, 4)
                    .Add(E.T("压力极限值", 11.5, E.P, false, TextWrapping.NoWrap), 1, 0)
                    .Add(Cell(E.Num("mf_lim_skt", 120, 78)), 1, 1)
                    .Add(E.T("", 11), 1, 2)
                    .Add(Cell(E.Num("mf_lim_sht", 20, 78)), 1, 3)
                    .Add(E.T("", 11), 1, 4)
                    .Add(E.T("校高最小值", 11.5, E.P, false, TextWrapping.NoWrap), 2, 0)
                    .Add(Cell(E.Num("mf_min_h", 20, 78)), 2, 1)
                    .Add(E.T("kgf", 11.5, E.P, false, TextWrapping.NoWrap), 2, 2)
                    .Add(E.T("参考值kgf", 11.5, E.P, false, TextWrapping.NoWrap), 2, 3, 1, 2)
                    .Add(E.T("结束MPa", 11.5, E.P, false, TextWrapping.NoWrap), 3, 0)
                    .Add(Cell(E.Num("mf_end_mpa", 0.209, 78, "0.000")), 3, 1)
                    .Add(E.T("MPa", 11.5, E.P, false, TextWrapping.NoWrap), 3, 2)
                    .Add(Cell(E.Display("121.22", 86, E.BandGrey, E.P, TextAlignment.Center, 11.5)), 3, 3)
                    .Add(E.T("", 11), 3, 4)
                    .Add(E.T("延时(ms)", 11.5, E.P, false, TextWrapping.NoWrap), 4, 0)
                    .Add(Cell(E.Num("mf_delay", 500, 78, "0")), 4, 1)
                    .Add(E.T("ms", 11.5, E.P, false, TextWrapping.NoWrap), 4, 2)
                    .Add(Cell(E.Display("120.60", 86, E.BandGrey, E.P, TextAlignment.Center, 11.5)), 4, 3),
                E.Spacer(10),
                E.T("压力极限值：测试臂最大可设定的压力值。自动校准时测试臂会缓慢下降到小车上试探吸取 IC，如果吸取到产品或压力反馈达到设定值则停止向下试探。",
                    11.5, E.Sec, false, TextWrapping.Wrap));

            return E.V(
                E.T("该窗口用于校正测试臂下压反馈值，当未校准时所有校准值为 0。" +
                    "只有正确校准测试臂下压力矩反馈值之后，才可正常使用测试臂接触位置校准功能。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(10),
                E.Grid().Cols("540", "*")
                    .Add(E.Scroll(g, 430, System.Windows.Controls.ScrollBarVisibility.Disabled,
                        System.Windows.Controls.ScrollBarVisibility.Auto), 0, 0)
                    .Add(right, 0, 1),
                E.Spacer(8),
                E.H(
                    E.V(E.Radios("mf_arm", new[] { "臂1", "臂2" }, true, 0),
                        E.Btn("自动校准", () => E.M.Log("信息", "启动测试臂接触力自动校准过程（约 4 分钟）")),
                        E.Spacer(6),
                        E.Btn("应用", () => E.M.Log("信息", "接触力校准数据已应用"))),
                    E.Group("自动校准步骤",
                        E.T("① 在“品种 → 测试模式”中正确设定当前测试制具对应的测试模式。\n" +
                            "② 在“品种 → 测试臂”中正确设定测试臂制具缓冲气缸直径。\n" +
                            "③ 在“结束MPa”中填入结束压力值，使其小于机器输入压力值，并使换算出的测试臂最大压力小于“压力极限值”。\n" +
                            "④ 手动或通过诊断界面移动测试臂到测试位接触位置，使气缸压缩量约 1mm，并让电机保持在该位置（Servo On）。\n" +
                            "⑤ 选中已就位（接触到测试位）的测试臂。\n" +
                            "⑥ 点击[自动校准]按钮启动校准，测试臂气缸依次加载 0~255 级压力并检测电机输出力矩反馈填入列表，约 4 分钟。\n" +
                            "⑦ 完成后切换到另外一支测试臂，重复 ④~⑥。两支测试臂均完成且参考值相同时，点击[应用]保存。",
                            11.5, E.Sec, false, TextWrapping.Wrap))));
        }

        // ==================================================================
        //  5. 用户管理 (image105)
        // ==================================================================
        public static Element MaintUser()
        {
            string[,] users = { { "Admin", "333", "Admin" }, { "Engineer", "222", "Engineer" }, { "Operator", "111", "Operator" } };
            var ug = E.Grid().Cols("200", "200", "200", "*");
            ug.Add(Head("User"), 0, 0);
            ug.Add(Head("Pass"), 0, 1);
            ug.Add(Head("Confirm"), 0, 2);
            ug.Add(Head(""), 0, 3);
            for (int r = 0; r < 3; r++)
            {
                ug.Add(Label(users[r, 0]), r + 1, 0);
                ug.Add(Label(users[r, 1]), r + 1, 1);
                ug.Add(Label(users[r, 2]), r + 1, 2);
                ug.Add(Label(""), r + 1, 3);
            }
            for (int r = 3; r < 6; r++)
                for (int c = 0; c < 4; c++) ug.Add(Label("", 26), r + 1, c);

            string[,] priv =
            {
                { "0", "20", "品种设定", "Technician" },
                { "1", "21", "单元设定", "Technician" },
                { "2", "22", "诊断", "Operator" },
                { "3", "23", "维护配置", "Technician" },
                { "4", "24", "运行模式", "Technician" },
                { "5", "28", "使能设定", "Operator" },
                { "6", "25", "温控模式", "Operator" },
                { "7", "26", "电机归零", "Technician" },
                { "8", "29", "信息记录", "Technician" },
                { "9", "32", "设定语言", "Operator" },
                { "10", "27", "关闭程序", "Technician" },
                { "11", "140", "删除IC", "Technician" }
            };
            string[] lv = { "Operator", "Engineer", "Technician", "Administrator" };

            var pg = E.Grid().Cols("110", "130", "300", "160");
            pg.Add(Head("Serial Number"), 0, 0);
            pg.Add(Head("Identity Number"), 0, 1);
            pg.Add(Head("Privilege Description"), 0, 2);
            pg.Add(Head("Level Set"), 0, 3);
            for (int r = 0; r < priv.GetLength(0); r++)
            {
                pg.Add(Label(priv[r, 0], 24, TextAlignment.Center), r + 1, 0);
                pg.Add(Label(priv[r, 1], 24, TextAlignment.Center), r + 1, 1);
                pg.Add(Label(priv[r, 2], 24), r + 1, 2);
                pg.Add(Cell(E.Cmb("mu_lv_" + r, lv, 150, Array.IndexOf(lv, priv[r, 3]) < 0 ? 2 : Array.IndexOf(lv, priv[r, 3])), 24), r + 1, 3);
            }

            return E.V(
                E.Group("用户管理",
                    E.Scroll(ug, 170, System.Windows.Controls.ScrollBarVisibility.Auto,
                        System.Windows.Controls.ScrollBarVisibility.Auto),
                    E.Spacer(6),
                    E.H(
                        E.Text("mu_user", "", 150), E.Text("mu_pass", "", 150),
                        E.Cmb("mu_level", lv, 130, 0),
                        E.MiniBtn("删除", () => E.M.Log("信息", "删除选中用户（内置用户不可删除）")),
                        E.MiniBtn("修改", () => E.M.Log("信息", "修改 / 添加用户")),
                        E.MiniBtn("导出", null), E.MiniBtn("导入", null))),
                E.Group("权限管理",
                    E.Scroll(pg, 300, System.Windows.Controls.ScrollBarVisibility.Disabled,
                        System.Windows.Controls.ScrollBarVisibility.Auto),
                    E.Spacer(6),
                    E.H(E.Cmb("mu_priv_level", lv, 130, 0),
                        E.MiniBtn("设定", () => E.M.Log("信息", "设定选中权限等级"))),
                    E.Spacer(4),
                    E.H(E.MiniBtn("导出", null), E.MiniBtn("导入", null))),
                E.H(E.Btn("应用", () => E.M.Log("信息", "用户 / 权限设定已应用"))),
                E.Group("说明",
                    E.T("该窗口用于对本机用户进行维护。用户 “Administrator” 和 “Engineer” 为内置用户，无法被删除；只有 “Administrator” 可进入此界面。\n" +
                        "添加用户：在“用户名”和“口令”文本框中输入信息后点击[修改]按钮即可添加新用户，用户名不分大小写，不可与内置用户名相同，前后空字符会被自动截断去除。\n" +
                        "删除用户 / 更改信息：在列表中选择欲操作对象后操作。导入 / 导出：支持导出和导入用户管理及权限管理文件。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ==================================================================
        //  6. 信号灯设定 (image106)
        // ==================================================================
        public static Element MaintLight()
        {
            string[] states = { "运行", "卡料", "暂停", "警告" };
            string[] opts = { "OFF", "BLINK", "ON" };
            int[,] def = { { 0, 0, 2, 0 }, { 1, 0, 0, 2 }, { 0, 2, 0, 0 }, { 0, 1, 0, 2 } };

            var g = E.Grid().Cols("120", "130", "130", "130", "130");
            g.Add(Head("状态"), 0, 0);
            g.Add(Head("红色"), 0, 1);
            g.Add(Head("黄色"), 0, 2);
            g.Add(Head("绿色"), 0, 3);
            g.Add(Head("蜂鸣器"), 0, 4);
            for (int r = 0; r < states.Length; r++)
            {
                g.Add(Label(states[r], 28), r + 1, 0);
                for (int c = 0; c < 4; c++)
                    g.Add(Cell(E.Cmb("ml_" + r + "_" + c, opts, 120, def[r, c]), 28), r + 1, c + 1);
            }

            return E.V(
                
                E.Group("信号灯定义", g),
                E.H(E.Btn("应用", () => E.M.Log("信息", "信号灯设定已应用"))),
                E.Group("说明",
                    E.T("状态：表示设备处于某种状态时，包括运行、卡料、暂停和警告。\n" +
                        "红色 / 黄色 / 绿色：可定义信号灯的三种颜色灯在对应状态下所触发的状态，包括长亮(ON)、闪烁(BLINK)以及不激活(OFF)。\n" +
                        "蜂鸣器：可定义信号灯蜂鸣器的状态，是否蜂鸣。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }
    }
}
