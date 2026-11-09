using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MainViewModel）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_MainViewModel
    internal static class WenQiZhiTag_MainViewModel
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · MainViewModel。</summary>
        internal const string WenQiZhiFile = "MainViewModel";
    }

    // 温启志 18719361399 · 类 public class PageVm : ObservableObject
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

        /// <summary>固定屏页面（如主界面）：内容填满视口高度，底部条贴底。</summary>
        public bool Fill { get; set; }

        /// <summary>
        /// 单屏页面（如模块平面仿真）：外层不显示竖向滚动条。
        /// 配合页面内的等比缩放容器，整页内容自动缩进一屏，不会出现滚动条。
        /// </summary>
        public bool NoScroll { get; set; }

        /// <summary>页面内容（首次访问时构建并缓存；优先加载 Views/Pages/{id}.xaml）。</summary>
        public Element Content
        {
            get
            {
                if (_content == null)
                {
                    _content = MakeContent();
                    // 页面构建完成 → 冻住本页参数表单的初值（「恢复」回到打开页面那一刻）
                    Ops.FreezePageForms();
                }
                return _content;
            }
        }

        private Element MakeContent()
        {
            // XAML 页面覆盖了同名 C# 构建函数，它的「应用 / 恢复」按钮只能走
            // PageCommands → Ops.ApplyPageForm/RestorePageForm。这里先把本页表单
            // 建好并挂进「待冻结」队列，页面构建完成后由 FreezePageForms() 记初值。
            Ops.PageFormById(Id);

            var fromXaml = PageLoader.Load(Id);
            if (fromXaml != null)
            {
                // Element.VAlign 默认是 Center：XAML 根不显式撑满的话，
                // 整页内容会在视口里垂直居中，顶部留出一大块空白。
                // C# 分支本来就有这行，XAML 分支必须保持一致。
                fromXaml.VAlign = System.Windows.VerticalAlignment.Stretch;
                return fromXaml;
            }
            var body = Build != null ? Build() : new StackElement();
            body.VAlign = System.Windows.VerticalAlignment.Stretch;
            var wrap = body;
            wrap.VAlign = System.Windows.VerticalAlignment.Stretch;
            return wrap;
        }

        public string Display => Icon + "  " + Name;
    }

    // 温启志 18719361399 · 类 public class ToolGroupVm : ObservableObject
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

    // 温启志 18719361399 · 类 public class MainViewModel : ObservableObject
    /// <summary>主窗口视图模型：工具条导航 + 状态栏 + 实时刷新。</summary>
    public class MainViewModel : ObservableObject
    {
        public MachineModel M => MachineModel.Instance;

        public List<ToolGroupVm> Groups { get; private set; }
        public List<PageVm> AllPages { get; } = new List<PageVm>();
        public PageVm HomePage { get; private set; }
        public PageVm ManualPage { get; private set; }
        /// <summary>SECS/GEM 通讯页（工具条「通讯」组，位于设定模式左侧）。温启志 18719361399</summary>
        public PageVm SecsGemPage { get; private set; }
        public ToolGroupVm BoardGroup { get; private set; }

        // 由视图注入的对话框服务（保持视图模型不依赖 Window）
        public Func<string, string, bool> ConfirmPassword { get; set; }
        public Func<string, string, bool> Confirm { get; set; }
        public Action<string, string> Alert { get; set; }

        // 文件对话框服务：由视图注入，视图模型不引用 System.Windows.Forms / Microsoft.Win32。
        //   PickOpenFiles(标题, 过滤器) → 选中文件路径数组（取消返回 null/空）
        //   PickSaveFile (标题, 过滤器, 默认文件名) → 目标路径（取消返回 null）
        public Func<string, string, string[]> PickOpenFiles { get; set; }
        public Func<string, string, string, string> PickSaveFile { get; set; }

        /// <summary>弹出「打开」对话框并导入品种参数。</summary>
        public RelayCommand ImportRecipeCommand => new RelayCommand(() =>
        {
            var files = PickOpenFiles?.Invoke("导入品种参数", "品种参数文件 (*.json)|*.json|所有文件 (*.*)|*.*");
            if (files == null || files.Length == 0) return;
            foreach (var f in files) Ops.ImportRecipeFrom(f);
        });

        /// <summary>弹出「另存为」对话框并导出当前品种参数。</summary>
        public RelayCommand ExportRecipeCommand => new RelayCommand(() =>
        {
            string def = M.Recipe + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".json";
            string path = PickSaveFile?.Invoke("导出品种参数", "品种参数文件 (*.json)|*.json", def);
            if (string.IsNullOrEmpty(path)) return;
            Ops.ExportRecipeTo(M.Recipe, path);
        });

        /// <summary>弹出「另存为」对话框并导出信息记录。</summary>
        public RelayCommand ExportLogToCommand => new RelayCommand(() =>
        {
            string def = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            string path = PickSaveFile?.Invoke("导出信息记录", "文本文件 (*.txt)|*.txt", def);
            if (string.IsNullOrEmpty(path)) return;
            Ops.ExportLogTo(path);
        });

        public MainViewModel()
        {
            // 先注册运动页的具名实时信号（静态构造函数不会因方法组引用而触发）
            MotionPages.EnsureRegistered();

            // SECS/GEM 通讯页先建，供工具条「通讯」组（设定模式左侧）直接引用，避免出现重复页面实例
            SecsGemPage = PageCatalog.SecsGem(this);
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

            // 运动控制卡：优先探测雷赛 LTDMC.dll / DMC 硬件，失败自动降级为内置仿真卡。
            Motion.MotionManager.Instance.Connect();

            // 操作层：订阅运控循环回调、装载上次参数、并入磁盘上的配方。
            // 必须显式调用 —— 静态类的方法组引用不会触发静态构造函数（技能文档 Pitfall 7）。
            Current = this;
            Ops.Init();
            // SECS/GEM 通讯：若上次勾选了「启用通讯」则开机自动连接（默认不勾选 → 不启动）。温启志 18719361399
            SecsGemService.TryAutoStart();
            // 把控制卡的轴参数回填到界面模型，保证《单元设定 / 电机归零》页显示的是真值
            Ops.PullAxisConfigsToModel();
            Ops.RefreshTimingReadouts();

            // 主动恢复「用户最后一次显示的页面」：下次开机直接回到那一页，
            // 导航栏上对应的工具条按钮 / 下拉项高亮由 Navigate 一并还原。
            // 从没记录过、或记录的页面已不存在（例如版本升级把页面删了 / 改了 Id）
            // → 退回主界面，并留一条日志说明，免得用户以为「记忆功能坏了」。
            string lastId = UiState.LastPageId;
            var lastPage = FindPageById(lastId);
            if (lastPage == null && !string.IsNullOrEmpty(lastId))
                M.Log("信息", "上次停留的页面已不存在（" + lastId + "），已回到主界面");
            Navigate(lastPage ?? HomePage);
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

            // 主动记住最后显示的页面（落盘 %APPDATA%\HandlerGuide\ui.json）。
            // 放在这里而不是各处命令里：GoHomeCommand / ManualCommand / SecsGemCommand
            // 以及工具条每个按钮都走 Navigate，一处收口就不会漏。
            // Id 没变时 UiState 自己会跳过写盘。
            UiState.LastPageId = page.Id;
        }

        /// <summary>按 Id 找页面（开机恢复上次页面用）；找不到返回 null。</summary>
        private PageVm FindPageById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in AllPages)
                if (string.Equals(p.Id, id, StringComparison.Ordinal)) return p;
            return null;
        }

        // ---------------- 顶栏 / 工具条命令 ----------------

        public RelayCommand GoHomeCommand => new RelayCommand(() => Navigate(HomePage));

        public RelayCommand SaveCommand => new RelayCommand(() => Ops.SaveParams());

        public RelayCommand RecallCommand => new RelayCommand(() => Ops.LoadParams());

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
                    "设定模式 · 使能设定 · 温控设定 · 电机归零 · 信息记录 · 显示控制板\n\n" +
                    "本软件由 温启志 编写（联系方式：18719361399）\n" + WenQiZhiCopyright);
        });

        public RelayCommand ManualCommand => new RelayCommand(() => Navigate(ManualPage));

        /// <summary>命令中心工具条右侧「通讯」按钮：直达 SECS/GEM 通讯页。温启志 18719361399</summary>
        public RelayCommand SecsGemCommand => new RelayCommand(() => Navigate(SecsGemPage));

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

        // ---------------- 作者水印（温启志 18719361399） ----------------

        /// <summary>作者水印文字：温启志 18719361399（显示在版本条与底部状态条）。</summary>
        public string WenQiZhiMark => Owner.WenQiZhiMark;

        /// <summary>作者电话水印变量：18719361399。</summary>
        public string WenQiZhiPhone => Owner.WenQiZhiPhone;

        /// <summary>作者版权水印：Copyright (c) 温启志 18719361399。</summary>
        public string WenQiZhiCopyright => Owner.WenQiZhiCopyright;

        /// <summary>带随机空格的抗替换水印（温启志 18719361399）。</summary>
        public string WenQiZhiStamped => Owner.WenQiZhiStamped();

        /// <summary>窗口标题（含作者水印 温启志 18719361399）。</summary>
        public string WenQiZhiTitle => TitleText;

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

        // ---------------- 命令中心工具条（启动 / 暂停 / 停止 / 急停 / 归零） ----------------

        /// <summary>最近构造的实例。Ops 用它把状态变化即时推回工具栏（按钮可用性 / 状态灯）。</summary>
        public static MainViewModel Current { get; private set; }

        public RelayCommand StartCommand => new RelayCommand(() => Ops.StartProduction());
        public RelayCommand PauseCommand => new RelayCommand(() => Ops.PauseProduction());
        public RelayCommand ResumeCommand => new RelayCommand(() => Ops.ResumeProduction());
        public RelayCommand StopCommand => new RelayCommand(() => Ops.StopProduction());
        public RelayCommand SingleStepCommand => new RelayCommand(() => Ops.SingleStep());
        public RelayCommand EStopCommand => new RelayCommand(() => Ops.EmergencyStop());
        public RelayCommand ResetEStopCommand => new RelayCommand(() => Ops.ResetEmergency());
        public RelayCommand ClearAlarmCommand => new RelayCommand(() => Ops.ClearAlarm());
        public RelayCommand HomeAllCommand => new RelayCommand(() => Ops.HomeAll());
        public RelayCommand CancelHomeCommand => new RelayCommand(() => Ops.CancelHome());
        public RelayCommand StopAllCommand => new RelayCommand(() => Ops.StopAllAxes());
        public RelayCommand ServoOnCommand => new RelayCommand(() => Ops.ServoAll(true));
        public RelayCommand ServoOffCommand => new RelayCommand(() => Ops.ServoAll(false));
        public RelayCommand ResetCountersCommand => new RelayCommand(() => Ops.ResetCounters());
        public RelayCommand ExportLogCommand => new RelayCommand(() => Ops.ExportLog());

        // 温启志 18719361399：命令中心按钮已搬到主界面页内（HomePages.CommandCenter），
        // 可用性判断统一放在 Ops，这里保留同名属性只是为了兼容旧绑定，避免两份逻辑分叉。
        /// <summary>启动按钮可用：未运行、未急停、有卡、无报警。</summary>
        public bool CanStart => Ops.CanStartProduction;

        public bool CanPause => Ops.CanPauseProduction;
        public bool CanResume => Ops.CanResumeProduction;
        public bool CanStop => Ops.CanStopProduction;
        public bool CanHome => Ops.CanHomeAxes;
        public bool CanSingleStep => Ops.CanSingleStepProduction;

        public string StatusServo => "伺服：" + Ops.ServoOnCount + "/"
                                     + Motion.MotionManager.Instance.Axes.Count + " 使能";

        public string StatusHome => "归零：" + Ops.HomeProgressText;

        public string StatusTempActual => "实测：" + Ops.TempActual.ToString("0.0") + "°C";

        /// <summary>工具栏上的大状态字。</summary>
        public string RunStateText => M.RunState;

        /// <summary>状态变化后立即刷新工具栏（不等下一个 500ms tick）。</summary>
        public void RaiseStatusNow() => RaiseStatus();

        private void RaiseStatus()
        {
            Raise(nameof(StatusRun));
            Raise(nameof(StatusMode));
            Raise(nameof(StatusRecipe));
            Raise(nameof(StatusTemp));
            Raise(nameof(StatusPressure));
            Raise(nameof(StatusClock));
            Raise(nameof(StatusDot));
            Raise(nameof(StatusServo));
            Raise(nameof(StatusHome));
            Raise(nameof(StatusTempActual));
            Raise(nameof(RunStateText));
            Raise(nameof(CanStart));
            Raise(nameof(CanPause));
            Raise(nameof(CanResume));
            Raise(nameof(CanStop));
            Raise(nameof(CanHome));
            Raise(nameof(CanSingleStep));
        }

        public string VersionText => "Version: 2.0.23.709";

        /// <summary>由界面定时器驱动：推进仿真 + 刷新实时读数。</summary>
        public void Tick()
        {
            M.Tick();
            M.TickClock();
            // 批次运行 / 停机时间累计（Channel 500ms → 1/120 分钟）。温启志 18719361399
            LotOps.Tick(1.0 / 120.0);
            CheckClockFidelity();
            LiveRegistry.RefreshAll();
            RaiseStatus();
        }

        // 仿真时钟 vs 墙钟的健康检查：真机上机器时间必然等于墙钟时间，
        // 一旦偏差持续扩大，说明界面线程在拖住运控通道（节拍 / UPH 会失真）。
        private readonly System.Diagnostics.Stopwatch _wallClock = System.Diagnostics.Stopwatch.StartNew();
        private DateTime _clockWarnAt = DateTime.MinValue;

        /// <summary>仿真时钟是否跟得上墙钟（自检与现场排查用）。</summary>
        public double SimClockSeconds => Motion.MotionManager.Instance.SimClockSeconds;

        /// <summary>系统运行（墙钟）秒数。</summary>
        public double WallClockSeconds => _wallClock.Elapsed.TotalSeconds;

        private void CheckClockFidelity()
        {
            double wall = _wallClock.Elapsed.TotalSeconds;
            if (wall < 15) return;
            double sim = SimClockSeconds;
            // 允许 5s 或 5% 的漂移（启动阶段、窗口拖动都会短暂落后）
            if (Math.Abs(wall - sim) < Math.Max(5.0, wall * 0.05)) return;
            if ((DateTime.Now - _clockWarnAt).TotalSeconds < 60) return;
            _clockWarnAt = DateTime.Now;
            Ops.Warn("仿真时钟落后墙钟 " + (wall - sim).ToString("0.0") + "s（墙钟 "
                     + wall.ToString("0.0") + "s / 仿真 " + sim.ToString("0.0")
                     + "s）—— 界面卡顿已拖慢机器时间，此时的节拍与 UPH 不能当真实数据看");
        }

        /// <summary>
        /// 由主窗口 50ms 快速定时器驱动：只推进运动仿真与画布动画（约 20fps），
        /// 与 500ms 的读数刷新分开，保证轨迹动画流畅又不拖慢界面。
        /// </summary>
        public void FastTick(double dt)
        {
            Motion.MotionManager.Instance.Tick(dt);
            // 归零队列与温控闭环都按真实时间推进，和运控物理同一条通道
            Ops.TickHome(dt);
            Ops.TickTemp(dt);
            // 测试臂自动校准进度 / 手动测试脉冲宽度（温启志 18719361399）
            Ops.TickTestArmCalibration(dt);
            Ops.TickManualPulse(dt);
            Ops.TickTestIoPulse(dt);
            LiveRegistry.RefreshFast();
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
