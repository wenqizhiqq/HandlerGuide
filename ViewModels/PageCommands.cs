using System;

namespace HandlerGuide.ViewModels
{
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
                case "ExportLog":
                    m.Log("信息", "信息记录已导出为文本文件");
                    break;
                default:
                    m.Log("信息", arg ?? name ?? "");
                    break;
            }
        }
    }
}
