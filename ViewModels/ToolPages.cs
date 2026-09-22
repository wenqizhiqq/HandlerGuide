using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    // 命令中心工具条上的直达页面（复刻 image107 ~ image112）+ 语言 / 键盘 / 关闭 / 说明书
    public static class ToolPages
    {
        // ---------------- 模式设定 (image107 / image108) ----------------
        public static Element ModePage()
        {
            var m = E.M;
            var modes = new[]
            {
                ("正常模式（Normal）", "设备正常生产工作模式，正常取放产品并从测试系统接收测试结果，根据结果对产品进行分类。"),
                ("练习模式（Exercise）", "设备正常取放产品，测试结果可选择从测试系统接收或者由程序自动随机生成。"),
                ("空跑模式（Dry1）", "设备中放入料盘，不用放入产品，设备仿真取放产品进行运动，测试结果可选择从测试系统接收或由程序自动随机生成。"),
                ("空跑模式2（Dry2）", "运行状态同空跑模式，只是料盘不更换，上料盘空后自动置满，下料盘满后自动置空。"),
                ("清除模式", "切换为该模式时不再执行测试动作，将设备内所有 IC 清出机器，适用于测试过程中测试机出现异常的情况。"),
                ("维护模式", "切换为该模式时，“开始运行”按钮失效，设备将无法启动。"),
            };

            var body = E.V(
                E.H(E.T("运行模式", 12.5, E.P).Margin(0, 0, 10, 0),
                    E.Cmb("mode_sel", modes.Select(x => x.Item1).ToArray(), 200)),
                E.Spacer(6),
                E.Chk("mode_ic_chk", "启用料的相关检查", true),
                E.Chk("mode_tray_chk", "启用料盘的相关检查", true),
                E.Spacer(4),
                E.Group("随机生成",
                    E.Chk("mode_random", "随机生成"),
                    E.Row("测试时间", E.H(E.Num("mode_rtime", 500, 90), E.T("ms", 12, E.Sec).Margin(8, 0, 0, 0)), 80),
                    E.Row("良品率", E.H(E.Num("mode_ryield", 100, 90), E.T("%", 12, E.Sec).Margin(8, 0, 0, 0)), 80)),
                E.H(E.Btn("应用", () => m.Log("信息", "工作模式：" + m.GetText("mode_name", "正常模式")))));

            var desc = E.Group("工作模式说明",
                modes.Select(x => (object)E.V(
                    E.T(x.Item1, 12.5, E.P, true),
                    E.T(x.Item2, 11.5, E.Sec, false, TextWrapping.Wrap).Margin(0, 2, 0, 8))).ToArray());

            return E.V(
                E.T("工作模式设定", 13, E.P, true),
                E.Spacer(8),
                body,
                desc,
                E.Group("附注",
                    E.T("系统默认当前用户切换到操作员时，工作模式自动切换到正常模式。", 11.5, E.Sec, false, TextWrapping.Wrap),
                    E.T("当选择模式为“空跑模式”或“空跑模式2”时，可设定测试结果由程序自动随机生成，并设定随机生成时的良率及假定的测试时间。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---------------- 使能设定 (image109) ----------------
        public static Element EnablePage()
        {
            var m = E.M;
            string[] cols = { "a", "b", "c", "d", "e", "f", "g", "h" };

            Func<string, string, Element> block = (title, prefix) => E.Group(title,
                E.Matrix(cols, new[] { "A", "B" }, (r, c) => E.CheckCell(prefix + "_" + r + "_" + c, true),
                    44, 30, E.HeadBlue, E.HeadBlue, 34, 24));

            var units = E.Group("单元使能",
                E.H(
                    E.V(E.Chk("en_loader", "上料臂", true), E.Chk("en_unloader", "下料臂", true),
                         E.Chk("en_cart", "运料小车", true), E.Chk("en_transfer", "料盘传送", true)),
                    E.SpacerW(40),
                    E.V(E.Chk("en_heat", "加热", true), E.Chk("en_vis", "视觉", true),
                         E.Chk("en_clean", "清洁盘", true), E.Chk("en_rotator", "旋转台", true))));

            return E.V(
                
                units,
                block("测试臂A使能", "en_armA"),
                block("测试臂B使能", "en_armB"),
                E.H(E.MiniBtn("恢复"), E.Btn("应用", () => m.Log("信息", "使能设定已应用"))));
        }

        // ---------------- 温控设定 (image110) ----------------
        public static Element TempControlPage()
        {
            var m = E.M;
            return E.V(
                E.T("温控模式", 13, E.P, true),
                E.Spacer(8),
                E.Group("加热器工作条件",
                    E.Radios("tc_mode", new[] { "室温(25°C)", "高温(50°C)" }),
                    E.Spacer(10),
                    E.H(E.Btn("应用", () =>
                    {
                        int idx = m.GetChoice("tc_mode");
                        m.TempMode = idx == 1 ? "高温模式" : "常温模式";
                        m.TempSet = idx == 1 ? 50 : 25;
                        m.Log("信息", "温控模式切换为：" + m.TempMode);
                    }))),
                E.Group("说明",
                    E.T("该窗口用于选择加热器的工作条件。选择“高温”后，主界面 Temp 区域会显示高温模式与温度范围。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---------------- 电机归零 (image111) ----------------
        public static Element MotorHomePage()
        {
            var m = E.M;
            var rows = E.V();
            foreach (var mo in m.Motors)
            {
                rows.Children.Add(E.H(
                    E.Chk("mh_" + mo.Name, "", true),
                    E.T(mo.Name, 12, E.P).Margin(0, 0, 20, 0),
                    E.Display("Safe", 90),
                    E.Display("HomedStop", 110),
                    E.Display("-------√", 90)));
            }

            var ok = E.Btn("OK", () => m.Log("信息", "开始归零过程"));
            ok.StyleKey = null;
            ok.Width = 90; ok.Height = 46;
            ok.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x8E, 0x3E));
            ok.Foreground = Brushes.White;
            ok.FontWeight = FontWeights.SemiBold;
            ok.BorderThickness = new Thickness(0);
            ok.Margin = new Thickness(0, 0, 10, 0);

            var cancel = E.Btn("Cancel", () => m.Log("信息", "已取消归零"));
            cancel.StyleKey = null;
            cancel.Width = 90; cancel.Height = 46;
            cancel.Background = new SolidColorBrush(Color.FromRgb(0xC0, 0x20, 0x2A));
            cancel.Foreground = Brushes.White;
            cancel.FontWeight = FontWeights.SemiBold;
            cancel.BorderThickness = new Thickness(0);

            return E.V(
                
                E.Scroll(rows, 380),
                E.H(
                    E.V(E.MiniBtn("全选", () => { foreach (var mo in m.Motors) m.SetToggle("mh_" + mo.Name, true); m.Log("信息", "已全选电机"); }),
                         E.MiniBtn("全不选", () => { foreach (var mo in m.Motors) m.SetToggle("mh_" + mo.Name, false); m.Log("信息", "已全不选"); })),
                    E.SpacerW(20),
                    ok,
                    cancel));
        }

        // ---------------- 信息记录 (image112) ----------------
        public static Element InfoLogPage()
        {
            var m = E.M;
            string[] heads = { "pidx", "UnitName", "SubArea", "Code", "Message", "DateTime", "Recover", "StopTime", "RunMode", "LotId", "MesId" };
            double[] widths = { 46, 76, 70, 100, 320, 150, 60, 70, 80, 60, 60 };

            var host = E.V();
            var head = E.H();
            for (int i = 0; i < heads.Length; i++)
            {
                var h = E.Display(heads[i], widths[i], E.HeadBlue, E.P, TextAlignment.Left, 11);
                h.Margin = new Thickness(0, 0, 1, 1);
                head.Children.Add(h);
            }
            host.Children.Add(head);

            var sample = new (string, string, string, string, string)[]
            {
                ("92", "HMI_Main", "EVT060054", "品种切换至[SGS_QFP32_7X7_32_1]", "2023-07-14 08:51:23.622"),
                ("93", "System", "EVT000050", "程序开始-2.0.23.709", "2023-07-14 08:51:23.757"),
                ("94", "HMI_Main", "EVT060055", "用户登录成功- [Admin]", "2023-07-14 08:51:38.390"),
                ("95", "HMI_Main", "EVT060056", "用户等级变为- [Admin]", "2023-07-14 08:51:38.397"),
                ("96", "System", "EVT000000", "用户进行操作：登录成功] [Admin][Admin]", "2023-07-14 08:51:38.403"),
                ("97", "Machine", "EVT040059", "机台开始归零", "2023-07-14 11:38:34.435"),
                ("98", "Machine", "EVT040060", "机台归零成功", "2023-07-14 11:38:43.041"),
            };
            foreach (var s in sample)
            {
                var row = E.H();
                row.Children.Add(E.Display(s.Item1, widths[0], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display(s.Item2, widths[1], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("", widths[2], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display(s.Item3, widths[3], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display(s.Item4, widths[4], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display(s.Item5, widths[5], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("0", widths[6], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("", widths[7], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("Normal", widths[8], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("", widths[9], Brushes.White, E.P, TextAlignment.Left, 11));
                row.Children.Add(E.Display("", widths[10], Brushes.White, E.P, TextAlignment.Left, 11));
                foreach (var e in row.Children) e.Margin = new Thickness(0, 0, 1, 1);
                host.Children.Add(row);
            }

            return E.V(
                E.T("信息历史记录", 13, E.P, true),
                E.Spacer(8),
                E.Group("查询条件",
                    E.H(
                        E.V(E.H(E.T("开始日期", 12, E.P).Margin(0, 0, 8, 0), E.Text("il_from", DateTime.Now.ToString("yyyy/M/d"), 130)),
                             E.H(E.T("结束日期", 12, E.P).Margin(0, 0, 8, 0), E.Text("il_to", DateTime.Now.ToString("yyyy/M/d"), 130))),
                        E.SpacerW(30),
                        E.V(E.H(E.T("类型", 12, E.P).Margin(0, 0, 8, 0), E.Cmb("il_type", new[] { "Dummy", "Machine", "System", "HMI_Main" }, 120)),
                             E.H(E.T("单元", 12, E.P).Margin(0, 0, 8, 0), E.Cmb("il_unit", new[] { "全部", "上料臂", "下料臂" }, 120))),
                        E.SpacerW(20),
                        E.V(E.H(E.T("子模块", 12, E.P).Margin(0, 0, 8, 0), E.Cmb("il_sub", new[] { "全部", "Shuttle" }, 120)),
                             E.H(E.T("运行模式", 12, E.P).Margin(0, 0, 8, 0), E.Cmb("il_run", new[] { "Normal", "Exercise", "Dry1", "Dry2" }, 120))),
                        E.SpacerW(20),
                        E.V(E.H(E.T("代码", 12, E.P).Margin(0, 0, 8, 0), E.Text("il_code", "", 120)),
                             E.H(E.MiniBtn("更新", () => m.Log("信息", "信息记录已更新")),
                                  E.MiniBtn("导出", () => m.Log("信息", "信息记录已导出为文本文件")))))),
                E.Spacer(8),
                E.Scroll(host, 320, ScrollBarVisibility.Auto, ScrollBarVisibility.Auto),
                E.Group("说明",
                    E.T("该窗口用于显示设备运行过程中的事件记录，事件记录在本机的 SQL 数据库中，可通过此界面进行查询，输入开始、结束时间，并可限定类型和单元，并可将查询结果导出为文本文件。", 11.5, E.Sec, false, TextWrapping.Wrap)));
        }

        // ---------------- 中/英文切换 ----------------
        public static Element LangPage()
        {
            return E.V(
                E.T("语言设定", 13, E.P, true),
                E.Spacer(8),
                E.Group("界面语言",
                    E.H(E.T("中文界面", 12.5, E.P).Margin(0, 0, 14, 0), E.Switch("lang_cn", true)),
                    E.T("切换后在标题栏与菜单生效（演示）。", 11.5, E.Sec, false, TextWrapping.Wrap).Margin(0, 8, 0, 0)));
        }

        // ---------------- 键盘锁定 ----------------
        public static Element LockPage()
        {
            return E.V(
                E.T("键盘锁定", 13, E.P, true),
                E.Spacer(8),
                E.Group("操作输入",
                    E.H(E.T("锁定键盘输入", 12.5, E.P).Margin(0, 0, 14, 0), E.Switch("kbd_lock")),
                    E.T("锁定后禁用参数编辑输入，防止误操作。", 11.5, E.Sec, false, TextWrapping.Wrap).Margin(0, 8, 0, 0)));
        }

        // ---------------- 关闭程序 ----------------
        public static Element ClosePage()
        {
            return E.V(
                E.T("关闭程序", 13, E.P, true),
                E.Spacer(8),
                E.Group("确认",
                    E.T("确认关闭控制程序？关闭前请先正常停止设备运行，并按顺序：关闭控制程序 → 关闭控制电脑 → 按下电源关闭按钮 → 关断主开关。", 12.5, E.P, false, TextWrapping.Wrap),
                    E.Spacer(8),
                    E.H(E.Btn("关闭控制程序", () =>
                    {
                        if (MessageBox.Show("确定要关闭控制程序吗？", "关闭程序", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            Application.Current.Shutdown();
                    }, "PrimaryButton", 13))));
        }

        // ---------------- 说明书（复用 DocxReader 渲染原手册） ----------------
        public static Element ManualPage()
        {
            var sp = E.V();
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Pick and Place Handler 9000 User Manual_CN.docx");
                if (!System.IO.File.Exists(path))
                    path = "Pick and Place Handler 9000 User Manual_CN.docx";
                var blocks = DocxReader.Read(path);
                int shown = 0;
                foreach (var blk in blocks)
                {
                    if (blk is ParaBlock pb)
                    {
                        var tb = E.T(pb.P.Text, pb.P.Title ? 15 : 12.5, pb.P.Title ? E.P : E.Sec,
                            pb.P.Title, TextWrapping.Wrap);
                        tb.Margin = new Thickness(0, 4, 0, 4);
                        sp.Children.Add(tb);
                        if (++shown > 600) break;
                    }
                    else if (blk is TableBlock tbk)
                    {
                        sp.Children.Add(BuildManualTable(tbk.T));
                        if (++shown > 600) break;
                    }
                }
                if (blocks.Count == 0)
                    sp.Children.Add(E.T("手册内容为空或解析失败。", 12.5, E.Sec));
            }
            catch (Exception ex)
            {
                sp.Children.Add(E.T("无法加载手册：" + ex.Message, 12.5, E.Sec, false, TextWrapping.Wrap));
            }
            return sp;
        }

        private static Element BuildManualTable(DocTable t)
        {
            var grid = E.Grid().Marg(new Thickness(0, 6, 0, 10));
            for (int c = 0; c < t.Cols; c++) grid.Col("*");
            for (int r = 0; r < t.Rows.Count; r++)
            {
                grid.Row("Auto");
                var cells = t.Rows[r];
                for (int c = 0; c < cells.Count; c++)
                {
                    var text = string.Concat(cells[c].Select(run => run.Text));
                    grid.Add(E.Box(E.T(text, 11, E.P, false, TextWrapping.Wrap), null, E.Div, 0.5, 0, new Thickness(6, 4, 6, 4)), r, c);
                }
            }
            return grid;
        }
    }
}
