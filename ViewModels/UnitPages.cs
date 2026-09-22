using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // ==================================================================
    //  “单元设定”工具条：速度 / 上下料臂 / 运料小车 / 测试臂 /
    //  料盘传送 / 进出盘 / 偏移设定 / 应用参数至其他品种
    // ==================================================================
    public static class UnitPages
    {
        // ---------------- 速度 (image59) ----------------
        public static Element UnitSpeed()
        {
            var m = E.M;
            string[] parts = { "所有轴", "上料臂", "上料Z轴", "下料臂", "下料Z轴", "运料小车", "空盘传送臂", "旋转台", "测试臂" };

            var head = E.Grid().Cols("110", "360", "360");
            head.Add(E.Cell("", Brushes.Transparent), 0, 0);
            head.Add(E.Cell("速度比率", Brushes.Transparent, E.P, true, TextAlignment.Center), 0, 1);
            head.Add(E.Cell("加速度/减速度", Brushes.Transparent, E.P, true, TextAlignment.Center), 0, 2);

            var rows = E.V();
            foreach (var p in parts)
            {
                var row = E.Grid().Cols("110", "360", "360").Marg(new Thickness(0, 4, 0, 4));
                row.Add(E.T(p, 12.5, E.P).Margin(0, 0, 10, 0), 0, 0);
                row.Add(E.SldRow("", "sp_v_" + p, 100), 0, 1);
                row.Add(E.SldRow("", "sp_a_" + p, 100), 0, 2);
                rows.Children.Add(row);
            }

            return E.V(
                
                E.Group("", head),
                E.Group("", rows),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "速度设定已应用"))));
        }

        // ---------------- 上下料臂 (image60) ----------------
        public static Element UnitArm()
        {
            var m = E.M;
            Func<string, Element> page = tag => E.V(
                E.Group("吸料重试",
                    E.Row("取料重试次数", E.Num("ua_retry_" + tag, 2, 90, "0"), 130),
                    E.Row("取料叠加深度", E.Num("ua_deep_" + tag, 0.5, 90, "0.###"), 130, "mm"),
                    E.Chk("ua_autoskip_" + tag, "自动跳过（吸取次数超过设定数后自动跳过这颗料）", true),
                    E.Row("吸料失败自动跳过次数", E.Num("ua_skipn_" + tag, 3, 90, "0"), 170)),
                E.Group("计时器",
                    E.Row("真空检查时间", E.Num("ua_vacchk_" + tag, 25, 90, "0"), 130, "ms"),
                    E.Row("真空释放时间", E.Num("ua_vacrel_" + tag, 100, 90, "0"), 130, "ms"),
                    E.Row("真空吸取超时时间", E.Num("ua_vacto_" + tag, 500, 90, "0"), 130, "ms"),
                    E.Row("机械臂稳定时间", E.Num("ua_stable_" + tag, 0, 90, "0"), 130, "ms"),
                    E.Row("丢料检查时间", E.Num("ua_dropchk_" + tag, 100, 90, "0"), 130, "ms")),
                E.Group("自动归零",
                    E.Row("吸头自动归零间隔", E.Num("ua_homeint_" + tag, 0, 90, "0"), 130, "次"),
                    E.Row("当前IC间隔", E.Num("ua_curint_" + tag, 0, 90, "0", null, null, true), 130, "次")),
                E.Group("二速缓降",
                    E.Chk("ua_slow_" + tag, "二速缓降（吸头执行取料动作时按设定进行二级降速）", true),
                    E.Row("Speed", E.Num("ua_slowspeed_" + tag, 50, 90, "0"), 130, "%"),
                    E.Row("Acc/Dec", E.Num("ua_slowacc_" + tag, 50, 90, "0"), 130, "%"),
                    E.Row("Height", E.Num("ua_slowh_" + tag, 3, 90, "0.###"), 130, "mm"),
                    E.Chk("ua_slowonly_" + tag, "仅上料盘有效", true)),
                E.Group("其他",
                    E.Chk("ua_continue_" + tag, "换盘/上料臂丢料跳过后继续取料"),
                    E.Chk("ua_same_" + tag, "下料与上料一致")),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "上下料臂参数已应用"))));

            return E.V(
                E.T("上下料臂设定", 13, E.P, true),
                E.Spacer(8),
                E.Tabs(("上料", () => page("ld")), ("下料", () => page("ul"))));
        }

        // ---------------- 运料小车 (image61) ----------------
        public static Element UnitCart()
        {
            var m = E.M;
            return E.V(
                
                E.Grid().Cols("*", "24", "*")
                    .Add(E.Group("震动小车",
                        E.Chk("uc_shake", "震动小车（浮动传感器检测到料未放置妥帖时自动震动）", true),
                        E.Chk("uc_force", "强制（无论浮动检查是否通过都触发震动）"),
                        E.Row("次数", E.Num("uc_n", 3, 90, "0"), 90),
                        E.Row("加速", E.Num("uc_acc", 100, 90, "0"), 90, "ms"),
                        E.Row("振幅", E.Num("uc_amp", 2, 90, "0.###"), 90, "mm"),
                        E.Row("震动后等待", E.Num("uc_wait", 200, 90, "0"), 90, "ms"),
                        E.Row("器件浮动检查时间", E.Num("uc_float", 100, 90, "0"), 130, "ms")), 0, 0)
                    .Add(E.Group("检查",
                        E.Chk("uc_xchk", "小车X向检查（选装功能）"),
                        E.Spacer(4),
                        E.T("Gate检查：Shuttle 出入口 Gate Sensor 检查", 12, E.P, true),
                        E.Chk("uc_gate_in", "入口横向", true),
                        E.Chk("uc_gate_outx", "出口横向", true),
                        E.Chk("uc_gate_outy", "出口纵向", true),
                        E.Spacer(6),
                        E.T("纵向传感器选择", 12, E.P, true),
                        E.H(E.T("小车A", 12.5, E.P).Margin(0, 0, 8, 0), E.Chk("uc_ar1", "R1", true), E.Chk("uc_ar2", "R2", true)),
                        E.H(E.T("小车B", 12.5, E.P).Margin(0, 0, 8, 0), E.Chk("uc_br1", "R1", true), E.Chk("uc_br2", "R2", true))), 0, 2),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "运料小车参数已应用"))));
        }

        // ---------------- 测试臂 (image62) ----------------
        public static Element UnitTestArm()
        {
            var m = E.M;
            return E.V(
                
                E.Group("计时器",
                    E.Row("真空检查时间", E.Num("ut_vacchk", 25, 90, "0"), 130, "ms"),
                    E.Row("真空释放时间", E.Num("ut_vacrel", 100, 90, "0"), 130, "ms"),
                    E.Row("真空吸取超时", E.Num("ut_vacto", 500, 90, "0"), 130, "ms"),
                    E.Row("机械臂稳定时间", E.Num("ut_stable", 0, 90, "0"), 130, "ms"),
                    E.Row("丢料检查时间", E.Num("ut_dropchk", 100, 90, "0"), 130, "ms")),
                E.Group("压力与重测",
                    E.Chk("ut_aftervac", "放料后真空检查（提起后检查 Nest 是否还有芯片）"),
                    E.Spacer(6),
                    E.Row("压力超限", E.H(E.Num("ut_press", 10, 80, "0"), E.T("%fs", 12, E.Sec).Margin(8, 0, 16, 0),
                        E.T("时间", 12.5, E.P).Margin(0, 0, 8, 0), E.Num("ut_presst", 0, 80, "0"), E.T("ms", 12, E.Sec).Margin(8, 0, 0, 0)), 90),
                    E.Row("重测次数", E.Num("ut_retry", 0, 90, "0"), 130),
                    E.Row("测试座查空偏移", E.Num("ut_skoffset", 0, 90, "0.###"), 130, "mm"),
                    E.Row("叠料传感器失效检查偏移", E.Num("ut_stkoffset", 0, 90, "0.###"), 170, "mm")),
                E.Group("检查与延时",
                    E.Chk("ut_skchk", "测试座查空@连续好品达到阈值"),
                    E.Row("阈值", E.Num("ut_skchk_n", 0, 90, "0"), 130, "颗"),
                    E.Chk("ut_chchk", "测试通道检查", true),
                    E.Chk("ut_skchk2", "测试座检查(传感器)", true),
                    E.Row("放料后延时抬起", E.Num("ut_delayup", 0, 90, "0"), 130, "ms")),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "测试臂参数已应用"))));
        }

        // ---------------- 料盘传送 (image63) ----------------
        public static Element UnitTransfer()
        {
            var m = E.M;
            return E.V(
                
                E.Group("抓盘 / 放盘确认",
                    E.Row("真空检查时间 / 抓盘确认时间", E.Num("uf_grab", 200, 100, "0"), 220, "ms"),
                    E.Row("真空释放时间 / 放盘确认时间", E.Num("uf_release", 200, 100, "0"), 220, "ms"),
                    E.Row("真空吸取超时 / 抓盘失败超时", E.Num("uf_timeout", 2000, 100, "0"), 220, "ms")),
                E.Group("其他",
                    E.Row("机械臂稳定时间", E.Num("uf_stable", 0, 100, "0"), 220, "ms"),
                    E.Row("料盘到位确认时间", E.Num("uf_arrive", 100, 100, "0"), 220, "ms")),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "料盘传送参数已应用"))));
        }

        // ---------------- 进出盘 (image64) ----------------
        public static Element UnitIO()
        {
            var m = E.M;
            return E.V(
                
                E.Grid().Cols("*", "24", "*")
                    .Add(E.Group("检测与超时",
                        E.Row("料盘存在检查时间", E.Num("ui_exist", 300, 100, "0"), 160, "ms"),
                        E.Row("料盘进出传送超时", E.Num("ui_moveto", 5000, 100, "0"), 160, "ms"),
                        E.Row("料盘进出到位确认", E.Num("ui_arrive", 200, 100, "0"), 160, "ms"),
                        E.Row("料盘进出接近确认", E.Num("ui_near", 100, 100, "0"), 160, "ms"),
                        E.Row("顶盘气缸到位超时时间", E.Num("ui_cylto", 1000, 100, "0"), 160, "ms")), 0, 0)
                    .Add(E.Group("气缸动作",
                        E.Row("料盘锁定时间", E.Num("ui_lock", 200, 100, "0"), 160, "ms"),
                        E.Row("分盘气缸动作时间", E.Num("ui_split", 200, 100, "0"), 160, "ms"),
                        E.Row("料盘敲击次数", E.Num("ui_knockn", 0, 100, "0"), 160, "次"),
                        E.Row("料盘敲击时间", E.Num("ui_knockt", 0, 100, "0"), 160, "ms"),
                        E.Row("预温盘敲击次数", E.Num("ui_preknockn", 0, 100, "0"), 160, "次"),
                        E.Row("预温盘敲击时间", E.Num("ui_preknockt", 0, 100, "0"), 160, "ms")), 0, 2),
                E.Group("其他",
                    E.Chk("ui_float", "芯片浮动检查（激活 AUTO1/2/3 的浮动检查功能）", true),
                    E.Row("手自动换盘", E.Cmb("ui_changemode", new[] { "自动", "手动" }, 130), 100)),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "进出盘参数已应用"))));
        }

        // ---------------- 偏移设定 (image65) ----------------
        public static Element UnitOffset()
        {
            var m = E.M;
            var regions = new[]
            {
                "#1-小车A入口", "#2-小车B入口", "#3-上料盘", "#4-加热盘1", "#5-加热盘2",
                "#6-旋转台", "#7-清洁盘", "#8-自动盘1", "#9-自动盘2", "#10-自动盘3",
                "#11-固定盘1", "#12-固定盘2", "#13-固定盘3"
            };

            var list = E.V();
            for (int i = 0; i < regions.Length; i++)
            {
                int idx = i;
                list.Children.Add(E.H(
                    E.T(regions[i], 12, E.P).Margin(0, 0, 6, 0).Wd(150),
                    E.MiniBtn("+", () => m.Log("信息", "切换到区域：" + regions[idx]), 34)));
            }

            var map = E.Grid().Cols("*", "1", "*").Rows("Auto", "Auto");
            map.Add(E.MachineMap(), 0, 0);
            map.Add(E.Box(null, new SolidColorBrush(Color.FromRgb(0xC0, 0x20, 0x2A)), null, 0, 0, default(Thickness), default(Thickness), 1, 400), 0, 1);
            map.Add(E.ArmHead("吸头偏移",
                E.Row("X", E.Num("uo_x", 0, 90, "0.###"), 90, "mm"),
                E.Row("Y", E.Num("uo_y", 0, 90, "0.###"), 90, "mm"),
                E.Row("放料", E.Num("uo_place", -2, 90, "0.###"), 90, "mm"),
                E.Row("吸料", E.Num("uo_pick", 0, 90, "0.###"), 90, "mm"),
                E.Row("吸头间距", E.Num("uo_pitch", 0, 90, "0.###"), 90, "mm"),
                E.Chk("uo_single", "单独设定（可分别单独设定每个吸头）")), 0, 2);
            map.Add(E.Group("区域选择", E.Scroll(list, 300)), 1, 0);

            return E.V(
                E.T("偏移设定", 13, E.P, true),
                E.Spacer(6),
                E.T("本模块定义了上下料取放臂在各个区域取放料的位置偏移参数，用户可根据具体实际取放的位置调整取放臂各个吸头在各个区域的取放偏移值。", 11.5, E.Sec, false, TextWrapping.Wrap),
                E.T("缺省值：X-(0), Y-(0), 单独设定-(否), 放料-(-2), 取料-(0), 吸头间距-(0)", 11, E.Sec, false, TextWrapping.Wrap),
                E.Spacer(10),
                E.Grid().Cols("*", "24", "*")
                    .Add(E.Group("机台分区与吸头偏移", map), 0, 0)
                    .Add(E.Group("专项偏移",
                        E.T("小车A/B：分别设定测试运料小车在入口位和出口位的偏移，调整量为 -2~2mm", 11.5, E.Sec, false, TextWrapping.Wrap),
                        E.Row("小车A 入口", E.Num("uo_ca_in", 0, 90, "0.###"), 110, "mm"),
                        E.Row("小车A 出口", E.Num("uo_ca_out", 0, 90, "0.###"), 110, "mm"),
                        E.Row("小车B 入口", E.Num("uo_cb_in", 0, 90, "0.###"), 110, "mm"),
                        E.Row("小车B 出口", E.Num("uo_cb_out", 0, 90, "0.###"), 110, "mm"),
                        E.Spacer(6),
                        E.T("空盘取放臂：调整空盘取放臂到六个自动进出盘机构的取放位置偏移", 11.5, E.Sec, false, TextWrapping.Wrap),
                        E.Matrix(new[] { "Auto1", "Auto2", "Auto3" }, new[] { "偏移" },
                            (r, c) => E.Num("uo_emp_" + c, 0, 90, "0.###"), 110, 26, E.HeadBlue, E.HeadBlue, 80, 24),
                        E.Spacer(6),
                        E.T("测试Y：分别设定测试 Y 轴在取/放料位置的偏移，调整量为 -2~2mm", 11.5, E.Sec, false, TextWrapping.Wrap),
                        E.Row("测试Y 取料", E.Num("uo_ty_pick", 0, 90, "0.###"), 110, "mm"),
                        E.Row("测试Y 放料", E.Num("uo_ty_place", 0, 90, "0.###"), 110, "mm"),
                        E.Spacer(6),
                        E.Chk("uo_rot_sep", "旋转台取/放分别设定")), 0, 2),
                E.H(E.MiniBtn("恢复"), E.Btn("Apply", () => m.Log("信息", "偏移设定已应用"))));
        }

        // ---------------- 应用参数至其他品种 ----------------
        public static Element UnitApplyOthers()
        {
            var m = E.M;
            var items = E.V();
            foreach (var r in m.Recipes)
                items.Children.Add(E.Chk("uao_" + r, r, r != m.Recipe));

            return E.V(
                
                E.Group("当前品种", E.Display(m.Recipe, 240, E.BandGreen, E.P, TextAlignment.Center, 13)),
                E.Group("选择单元参数", E.V(
                    E.Chk("uao_speed", "速度", true),
                    E.Chk("uao_arm", "上下料臂", true),
                    E.Chk("uao_cart", "运料小车", true),
                    E.Chk("uao_testarm", "测试臂", true),
                    E.Chk("uao_transfer", "料盘传送", true),
                    E.Chk("uao_io", "进出盘", true),
                    E.Chk("uao_offset", "偏移设定"))),
                E.Group("目标品种", items),
                E.H(E.MiniBtn("全选", () => { foreach (var r in m.Recipes) m.SetToggle("uao_" + r, true); }),
                    E.MiniBtn("全不选", () => { foreach (var r in m.Recipes) m.SetToggle("uao_" + r, false); }),
                    E.Btn("应用", () => m.Log("信息", "单元参数已应用到所选品种"))));
        }
    }
}
