// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（PageCommands）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_PageCommands
    internal static class WenQiZhiTag_PageCommands
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · PageCommands。</summary>
        internal const string WenQiZhiFile = "PageCommands";
    }

    // 温启志 18719361399 · 类 public static class PageCommands
    /// <summary>
    /// 页面内按钮的命令桥：XAML 通过 &lt;vw:Btn Cmd="Log" Arg="..."/&gt; 声明式引用，
    /// 这里只做“名字→动作”的映射，不构造任何界面元素。
    /// </summary>
    public static class PageCommands
    {
        public static RelayCommand Command(string name, string arg)
        {
            return new RelayCommand(() => Run(name, arg));
        }

        public static void Run(string name, string arg)
        {
            var m = MachineModel.Instance;
            switch (name)
            {
                case "Log":
                    m.Log("信息", arg ?? "");
                    break;

                // ---- 下面这些是 XAML 页面真正用到的按钮命令（温启志 18719361399）----
                // Cmd="Apply"/"Reset" 的 Arg 是「页 id」，对应 Ops 里的按页表单。
                case "Apply":
                    if (Ops.HasPageForm(arg)) Ops.ApplyPageForm(arg);
                    else m.Log("信息", "已应用参数" + (arg != null ? "：" + arg : ""));
                    break;
                case "Reset":
                    if (Ops.HasPageForm(arg)) Ops.RestorePageForm(arg);
                    else m.Log("信息", "已重置" + (arg != null ? "：" + arg : ""));
                    break;
                case "Warn":
                    m.Log("警告", arg ?? "");
                    break;
                case "Jog":
                {
                    // Arg 形如 "轴名|方向" 或 "轴名|+步长"
                    string a = arg ?? "";
                    int bar = a.IndexOf('|');
                    if (bar <= 0) { m.Log("信息", "Jog " + a); break; }
                    string ax = a.Substring(0, bar);
                    string d = a.Substring(bar + 1).Trim();
                    if (d == "+" || d == "-") Ops.JogAxisByName(ax, d == "+" ? 1 : -1);
                    else if (double.TryParse(d, out double step)) Ops.JogAxisBy(Ops.FindAxis(ax)?.Index ?? -1, step);
                    else Ops.JogAxisByName(ax, 1);
                    break;
                }
                case "Toggle":
                    if (!string.IsNullOrEmpty(arg))
                        m.SetToggle(arg, !m.GetToggle(arg));
                    break;
                case "Export":
                    if (arg == "recipe") Ops.ExportRecipeDialog();
                    else if (arg == "log") Ops.ExportLogDialog();
                    else if (arg == "user") Ops.ExportUsersDialog();
                    else if (arg == "alarm") Ops.ExportAlarmStatsDialog();
                    else m.Log("信息", "已导出" + (arg != null ? "：" + arg : ""));
                    break;
                case "Import":
                    if (arg == "recipe") Ops.ImportRecipeDialog();
                    else if (arg == "user") Ops.ImportUsersDialog();
                    else m.Log("信息", "已导入" + (arg != null ? "：" + arg : ""));
                    break;
                case "ApplyTemp":
                    Ops.ApplyTempControl();
                    break;
                case "Home":
                    Ops.ConfirmHome();
                    break;
                case "CancelHome":
                    Ops.CancelHome();
                    break;
                case "SelectAll":
                {
                    foreach (var mo in m.Motors) m.SetToggle("mh_" + mo.Name, true);
                    m.Log("信息", "已全选电机");
                    break;
                }
                case "DeselectAll":
                {
                    foreach (var mo in m.Motors) m.SetToggle("mh_" + mo.Name, false);
                    m.Log("信息", "已全不选");
                    break;
                }
                case "UpdateLog":
                    Ops.UpdateLog();
                    break;
                case "SelectAllRecipes":
                    foreach (var r in m.Recipes) m.SetToggle("uao_" + r, true);
                    m.Log("信息", "已全选目标品种");
                    break;
                case "DeselectAllRecipes":
                    foreach (var r in m.Recipes) m.SetToggle("uao_" + r, false);
                    m.Log("信息", "已全不选目标品种");
                    break;
                case "ExportLog":
                    Ops.ExportLogDialog();
                    break;
                case "ExportAlarm":
                    Ops.ExportAlarmStatsDialog();
                    break;
                // 「偏移设定」页区域列表里的 + 按钮：Arg 是区域编号（1~13）
                case "OffsetRegion":
                    {
                        int ri;
                        if (int.TryParse(arg, out ri)) Ops.SelectOffsetRegion(ri);
                        else m.Log("警告", "区域编号无效：" + arg);
                    }
                    break;

                // ---- 维护配置：接触力校准 / 工作基点 / 用户管理 等固定表单页（温启志 18719361399）----
                case "DefaultContactForce":
                    Ops.DefaultContactForce();
                    break;
                case "StartTestArmCalibration":
                    Ops.StartTestArmCalibration();
                    break;
                // maint_base 的「应用」要同时做表单应用 + 写基准点
                case "ApplyBase":
                    Ops.ApplyPageForm(arg);
                    Ops.ApplyBasePoints();
                    break;
                case "CapturePos":
                    {
                        if (!string.IsNullOrEmpty(arg))
                        {
                            int bar = arg.IndexOf('|');
                            string key = bar > 0 ? arg.Substring(0, bar) : arg;
                            string axis = bar > 0 ? arg.Substring(bar + 1).Trim() : null;
                            Ops.CapturePosTo(key, axis);
                        }
                        break;
                    }
                case "CaptureArmXY":
                    Ops.CaptureArmXY(arg == "True" || arg == "1");
                    break;
                case "ApplyArmXYChange":
                    Ops.ApplyArmXYChange(arg == "True" || arg == "1");
                    break;
                case "ArmZPos":
                    {
                        var p = (arg ?? "").Split('|');
                        if (p.Length >= 3 && int.TryParse(p[1], out int head))
                            Ops.ArmZPos(p[0] == "True" || p[0] == "1", head, p[2] == "True" || p[2] == "1");
                        break;
                    }
                case "ArmZAll":
                    {
                        var p = (arg ?? "").Split('|');
                        if (p.Length >= 2)
                            Ops.ArmZAll(p[0] == "True" || p[0] == "1", p[1] == "True" || p[1] == "1");
                        break;
                    }
                case "AddOrUpdateUser":
                    Ops.AddOrUpdateUser();
                    break;
                case "DeleteUser":
                    Ops.DeleteUser();
                    break;
                case "SetPrivilegeLevel":
                    Ops.SetPrivilegeLevel();
                    break;
                case "ApplyUsers":
                    Ops.ApplyUsers();
                    break;
                default:
                    m.Log("信息", arg ?? name ?? "");
                    break;
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
