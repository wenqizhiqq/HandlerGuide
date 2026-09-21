using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{
    /// <summary>一个软件页面（对应手册里的一个功能窗口）。</summary>
    public class PageVm : ObservableObject
    {
        private Element _content;
        private bool _active;

        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Desc { get; set; }
        public Func<Element> Build { get; set; }
        public RelayCommand OpenCommand { get; set; }

        public bool IsActive { get => _active; set => Set(ref _active, value); }

        /// <summary>页面内容（首次访问时构建并缓存）。</summary>
        public Element Content => _content ?? (_content = Build != null ? Build() : new StackElement());

        public string Display => Icon + "  " + Name;
    }

    /// <summary>命令中心工具条上的一个模块（含下拉子工具）。</summary>
    public class ToolGroupVm : ObservableObject
    {
        private bool _open;

        public string Name { get; set; }
        public string Icon { get; set; }
        public string Desc { get; set; }
        public List<PageVm> Items { get; } = new List<PageVm>();
        public PageVm Direct { get; set; }
        public RelayCommand Command { get; set; }

        public bool HasItems => Items.Count > 0;
        public bool IsOpen { get => _open; set => Set(ref _open, value); }
        private bool _groupActive;
        /// <summary>当组内任一页面（Direct 或子项）为当前页时高亮工具条按钮。</summary>
        public bool IsGroupActive { get => _groupActive; set => Set(ref _groupActive, value); }

        /// <summary>工具条按钮上的两行文字。</summary>
        public string Line1 => Name.Length > 4 ? Name.Substring(0, 2) : Name;
        public string Line2 => Name.Length > 4 ? Name.Substring(2) : "";
        public bool HasLine2 => Line2.Length > 0;
    }

    /// <summary>主窗口视图模型：工具条导航 + 状态栏 + 实时刷新。</summary>
    public class MainViewModel : ObservableObject
    {
        public MachineModel M => MachineModel.Instance;

        public List<ToolGroupVm> Groups { get; private set; }
        public List<PageVm> AllPages { get; } = new List<PageVm>();
        public PageVm HomePage { get; private set; }
        public PageVm ManualPage { get; private set; }
        public ToolGroupVm BoardGroup { get; private set; }

        // 由视图注入的对话框服务（保持视图模型不依赖 Window）
        public Func<string, string, bool> ConfirmPassword { get; set; }
        public Func<string, string, bool> Confirm { get; set; }
        public Action<string, string> Alert { get; set; }

        public MainViewModel()
        {
            Groups = PageCatalog.Toolbar(this);
            HomePage = PageCatalog.Home(this);
            ManualPage = PageCatalog.Manual(this);
            BoardGroup = Groups.FirstOrDefault(g => g.Name == "显示控制板");
            AllPages.Add(HomePage);
            AllPages.Add(ManualPage);
            foreach (var g in Groups)
            {
                if (g.Direct != null) AllPages.Add(g.Direct);
                AllPages.AddRange(g.Items);
            }
            _selectedUserLevel = M.UserLevel;
            Navigate(HomePage);
        }

        // ---------------- 导航 ----------------

        private PageVm _current;
        public PageVm CurrentPage
        {
            get => _current;
            private set
            {
                if (!Set(ref _current, value)) return;
                Raise(nameof(PageTitle));
                Raise(nameof(PageDesc));
                Raise(nameof(CurrentContent));
                Raise(nameof(Breadcrumb));
            }
        }

        public Element CurrentContent => CurrentPage?.Content;
        public string PageTitle => CurrentPage == null ? "" : CurrentPage.Display;
        public string PageDesc => CurrentPage?.Desc ?? "";
        public string Breadcrumb => CurrentPage == null ? "" : "位置：" + CurrentPage.Name;

        public void Navigate(PageVm page)
        {
            if (page == null) return;
            foreach (var g in Groups)
            {
                g.IsOpen = false;
                g.IsGroupActive = ReferenceEquals(g.Direct, page) || g.Items.Any(p => ReferenceEquals(p, page));
                g.Notify(nameof(ToolGroupVm.IsGroupActive));
            }
            foreach (var p in AllPages) p.IsActive = ReferenceEquals(p, page);
            CurrentPage = page;
        }

        // ---------------- 顶栏 / 工具条命令 ----------------

        public RelayCommand GoHomeCommand => new RelayCommand(() => Navigate(HomePage));

        public RelayCommand SaveCommand => new RelayCommand(() => M.Log("信息", "参数已保存"));

        public RelayCommand RecallCommand => new RelayCommand(() => M.Log("信息", "参数已召回"));

        public bool KeyboardLocked
        {
            get => M.KeyboardLocked;
            set
            {
                if (M.KeyboardLocked == value) return;
                M.KeyboardLocked = value;
                Raise();
                M.Log("信息", value ? "键盘已锁定" : "键盘已解锁");
            }
        }

        public RelayCommand CloseCommand => new RelayCommand(() =>
        {
            if (Confirm == null || Confirm("关闭程序", "确认关闭控制程序？"))
                System.Windows.Application.Current.Shutdown();
        });

        public RelayCommand AboutCommand => new RelayCommand(() =>
        {
            if (Alert != null)
                Alert("关于",
                    "EXCEED9000 高速分选机 · 控制软件（界面复刻）\n" +
                    "Version: 2.0.23.709\n\n" +
                    "页面与布局依据《Pick and Place Handler 9000 用户手册》复刻：\n" +
                    "命令中心工具条 · 品种设定 · 单元设定 · 诊断 · 维护配置 ·\n" +
                    "设定模式 · 使能设定 · 温控设定 · 电机归零 · 信息记录 · 显示控制板");
        });

        public RelayCommand ManualCommand => new RelayCommand(() => Navigate(ManualPage));

        // ---------------- 用户等级 ----------------

        public string[] UserLevels { get; } = { "操作员", "工程师", "管理员" };

        private string _selectedUserLevel;
        public string SelectedUserLevel
        {
            get => _selectedUserLevel;
            set
            {
                if (!Set(ref _selectedUserLevel, value)) return;
                if (value == null || value == M.UserLevel) return;
                string pwd = value == "工程师" ? "123456" : value == "管理员" ? "admin" : "";
                if (string.IsNullOrEmpty(pwd))
                {
                    M.UserLevel = value;
                    M.Log("信息", "用户等级切换为：" + value);
                    return;
                }
                if (ConfirmPassword != null && ConfirmPassword("切换到【" + value + "】需要输入密码：", pwd))
                {
                    M.UserLevel = value;
                    M.Log("信息", "用户等级切换为：" + value);
                }
                else
                {
                    M.Log("警告", "密码错误，已取消切换");
                    _selectedUserLevel = M.UserLevel;
                    Raise();
                }
            }
        }

        // ---------------- 语言 ----------------

        private int _languageIndex;
        public int LanguageIndex
        {
            get => _languageIndex;
            set
            {
                if (!Set(ref _languageIndex, value)) return;
                M.Chinese = value == 0;
                Raise(nameof(TitleText));
            }
        }

        public string[] Languages { get; } = { "zh_CH", "en_US" };
        public string TitleText => M.Chinese ? "EXCEED9000 · 控制软件" : "EXCEED9000 · Control Software";

        // ---------------- 状态栏 ----------------

        public string StatusRun => "状态：" + M.RunState;
        public string StatusMode => "模式：" + M.Mode;
        public string StatusRecipe => "品种：" + M.Recipe;
        public string StatusTemp => "温度：" + M.TempMode + " " + M.TempSet.ToString("0") + "°C";
        public string StatusPressure => "压力：" + M.Pressure.ToString("0.0") + "Kgf";
        public string StatusClock => M.Clock;

        public Brush StatusDot => M.RunState == "运行" ? Palette.Success
            : M.RunState == "暂停" ? Palette.Tip
            : Palette.Secondary;

        public string VersionText => "Version: 2.0.23.709";

        /// <summary>由界面定时器驱动：推进仿真 + 刷新实时读数。</summary>
        public void Tick()
        {
            M.Tick();
            M.TickClock();
            LiveRegistry.RefreshAll();
            Raise(nameof(StatusRun));
            Raise(nameof(StatusMode));
            Raise(nameof(StatusRecipe));
            Raise(nameof(StatusTemp));
            Raise(nameof(StatusPressure));
            Raise(nameof(StatusClock));
            Raise(nameof(StatusDot));
        }
    }
}
