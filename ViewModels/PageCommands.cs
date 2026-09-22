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
                case "Warn":
                    m.Log("警告", arg ?? "");
                    break;
                case "Jog":
                    m.Log("信息", "Jog " + (arg ?? ""));
                    break;
                case "Toggle":
                    if (!string.IsNullOrEmpty(arg))
                        m.SetToggle(arg, !m.GetToggle(arg));
                    break;
                case "Apply":
                    m.Log("信息", "已应用参数" + (arg != null ? "：" + arg : ""));
                    break;
                case "Reset":
                    m.Log("信息", "已重置" + (arg != null ? "：" + arg : ""));
                    break;
                case "Export":
                    m.Log("信息", "已导出" + (arg != null ? "：" + arg : ""));
                    break;
                case "Import":
                    m.Log("信息", "已导入" + (arg != null ? "：" + arg : ""));
                    break;
                case "ApplyTemp":
                {
                    int idx = m.GetChoice("tc_mode");
                    m.TempMode = idx == 1 ? "高温模式" : "常温模式";
                    m.TempSet = idx == 1 ? 50 : 25;
                    m.Log("信息", "温控模式切换为：" + m.TempMode);
                    break;
                }
                case "Home":
                {
                    foreach (var mo in m.Motors) m.SetToggle("mh_" + mo.Name, true);
                    m.Log("信息", "开始归零过程");
                    break;
                }
                case "CancelHome":
                    m.Log("信息", "已取消归零");
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
                    m.Log("信息", "信息记录已更新");
                    break;
                case "SelectAllRecipes":
                    foreach (var r in m.Recipes) m.SetToggle("uao_" + r, true);
                    m.Log("信息", "已全选目标品种");
                    break;
                case "DeselectAllRecipes":
                    foreach (var r in m.Recipes) m.SetToggle("uao_" + r, false);
                    m.Log("信息", "已全不选目标品种");
                    break;                case "ExportLog":
                    m.Log("信息", "信息记录已导出为文本文件");
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
