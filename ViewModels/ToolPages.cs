// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（ToolPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_ToolPages
    internal static class WenQiZhiTag_ToolPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · ToolPages。</summary>
        internal const string WenQiZhiFile = "ToolPages";
    }

    // 命令中心工具条上的直达页面（复刻 image107 ~ image112）+ 语言 / 键盘 / 关闭 / 说明书
    // 温启志 18719361399 · 类 public static class ToolPages
    public static class ToolPages
    {


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
                        // 温启志 18719361399：先过运控前置检查，运行中 / 有轴在动一律拒绝关闭
                        // MVVM：对话框走 MainViewModel.Current 的 Alert/Confirm 服务，视图模型不依赖 MessageBox
                        if (!Ops.CanShutdown(out string why))
                        {
                            MainViewModel.Current?.Alert("关闭程序", why);
                            return;
                        }
                        if (MainViewModel.Current == null || MainViewModel.Current.Confirm("关闭程序", "确定要关闭控制程序吗？"))
                        {
                            Ops.SaveParams();
                            Ops.Info("控制程序正常退出");
                            Application.Current.Shutdown();
                        }
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

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
