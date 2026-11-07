// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（PageCatalog）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_PageCatalog
    internal static class WenQiZhiTag_PageCatalog
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · PageCatalog。</summary>
        internal const string WenQiZhiFile = "PageCatalog";
    }

    // 温启志 18719361399 · 类 public static class PageCatalog
    /// <summary>
    /// 页面目录：命令中心工具条的模块顺序与手册一致（复刻 image38 / image39）。
    /// </summary>
    public static class PageCatalog
    {
        private static PageVm D(string id, string icon, string name, string desc, Func<Element> build,
            bool noScroll = false)
            => new PageVm { Id = id, Icon = icon, Name = name, Desc = desc, Build = build, NoScroll = noScroll };

        public static List<ToolGroupVm> Toolbar(MainViewModel main)
        {
            var list = new List<ToolGroupVm>();

            list.Add(new ToolGroupVm
            {
                Name = "品种设定",
                Icon = "🏷️",
                Desc = "定义产品及 Kit 的所有信息集合（测试座分布、料盘尺寸、测试臂位置、接口、分类、温度、视觉、清洁盘）。",
                Items =
                {
                    D("recipe_maint", "📋", "品种维护", "品种的新建 / 删除 / 复制 / 重命名 / 导入 / 导出。", null),
                    D("recipe_testmode", "🧩", "测试模式", "定义测试座分布、测试通道映射及浮动传感器映射。", null),
                    D("recipe_tray", "🗄️", "料盘", "定义料盘尺寸信息及各区域料盘种类与取放方式。", null),
                    D("recipe_arm", "🦾", "测试臂", "定义测试臂取放料 / 测试高度及对测试座施加的压力。", null),
                    D("recipe_interface", "🔌", "测试接口", "定义与测试系统的接口定义（TTL / GPIB / RS232 / TCP）。", null),
                    D("recipe_bin", "🎨", "Bin设定", "定义产品分类到下料 6 个物理料盘的映射及分类处理方法。", null),
                    D("recipe_temp", "🌡️", "温度", "定义产品加热时间、加热温度及温度补偿等信息。", null),
                    D("recipe_vision", "👁️", "视觉", "2D / PiggyBack / 底部视觉参数设定与监视。", null),
                    D("recipe_clean", "🧽", "清洁盘", "打开关闭清洁功能及设定清洁盘相关参数、偏移、触发条件。", null)
                }
            });

            list.Add(new ToolGroupVm
            {
                Name = "单元设定",
                Icon = "⚙️",
                Desc = "定义设备各个部分工作的速度、时间参数和位置偏移调整值。",
                Items =
                {
                    D("unit_speed", "🚀", "速度", "定义各个区域电机工作的速度 / 加速度比率。", null),
                    D("unit_arm", "🦾", "上下料臂", "料臂吸料重试、计时器、自动归零、二速缓降等参数。", null),
                    D("unit_cart", "🚃", "运料小车", "运料小车震动、浮动检查、Gate 检查等参数。", null),
                    D("unit_testarm", "🧲", "测试臂", "测试臂计时器、压力超限、重测次数、测试通道检查。", null),
                    D("unit_transfer", "📦", "料盘传送", "料盘搬运臂机构的确认与超时时间设定。", null),
                    D("unit_io", "🎞️", "进出盘", "上下料自动换盘机构的延时 / 超时等时间参数。", null),
                    D("unit_offset", "🎯", "偏移设定", "上下料取放臂在各个区域取放料的位置偏移参数。", null),
                    D("unit_apply", "📤", "应用参数至其他品种", "把当前品种的单元参数批量应用到其它品种。", null)
                }
            });

            list.Add(new ToolGroupVm
            {
                Name = "诊断",
                Icon = "🛠️",
                Desc = "监视设备各部分状态、提供分解动作操控，便于排错、调整与检验参数。",
                Items =
                {
                    D("diag_motor", "📊", "电机状态", "全部位置控制电机的状态、信号与点动控制。", null),
                    D("diag_loader", "🦾", "上料臂", "上料臂单步分解动作控制（吸头 / 区域 / X-Y / 真空）。", null),
                    D("diag_unloader", "🦿", "下料臂", "下料臂单步分解动作控制。", null),
                    D("diag_area", "🔬", "测试区域", "测试区域工作状态、小车与测试臂分解动作、真空 IO。", null),
                    D("diag_trayio", "🎞️", "料盘进出", "进出盘区域 IO 状态与料盘传送臂位置控制。", null),
                    D("diag_panelio", "🎛️", "面板IO", "操作按钮面板输入输出、门关闭检测、信号灯状态。", null),
                    D("diag_testio", "🔗", "测试系统IO", "测试系统连接与测试状态、TTL 接口 IO 状态。", null)
                }
            });

            list.Add(new ToolGroupVm
            {
                Name = "维护配置",
                Icon = "🔧",
                Desc = "对设备基础参数的维护与设定，该区域内的参数与产品独立。",
                Items =
                {
                    D("maint_base", "📍", "工作基点", "机器各部分工作基准位置的设定与校准。", null),
                    D("maint_safe", "🛡️", "安全设定", "通用 / 测试区域 / 上下料臂 / 进出盘的安全与保护设定。", null),
                    D("maint_toffset", "🌡️", "温度偏移", "温度偏移值校准参数（基点 #1~#4 与补偿值）。", null),
                    D("maint_force", "⚖️", "接触力校准", "校正测试臂下压反馈值（0~255 级输出力矩对应反馈）。", null),
                    D("maint_user", "👥", "用户管理", "用户与权限管理，内置 Administrator / Engineer 不可删除。", null),
                    D("maint_light", "🚦", "信号灯设定", "定义不同状态下信号灯与蜂鸣器的状态。", null)
                }
            });

            list.Add(new ToolGroupVm
            {
                Name = "运动仿真",
                Icon = "🎛️",
                Desc = "雷赛 DMC-E 总线卡 / DMC 脉冲卡（LTDMC.dll）的轴与 IO 仿真、互锁、整机模块平面动画；无卡时自动降级为内置仿真卡。",
                Items =
                {
                    D("motion_layout", "🏭", "模块平面仿真",
                      "三温标准机 11 个模组的整机平面图，模组内零件随运动控制卡实时动作（料盘升降 / 流水走料 / 移盘穿梭 / 压测下压 / 料枪伸缩 / 加热盘通断），并内置安全演示节拍。",
                      null, noScroll: true),
                    D("motion_sim", "🎛️", "运动仿真",
                      "轴点动 / 回零 / 停止、32 入 32 出 IO 监视与手动翻转、10 条安全互锁规则、共享走廊轨迹动画与安全演示节拍。",
                      null),
                    D("motion_card", "🔧", "控制卡设置",
                      "卡型探测与连接、每轴脉冲当量 / 软限位 / 速度曲线 / 回零位置下发，以及控制卡诊断计数。",
                      null)
                }
            });

            // 温启志 18719361399 · SECS/GEM 通讯（HSMS-SS）：置于「设定模式」左侧，点击直达通讯页
            list.Add(new ToolGroupVm
            {
                Name = "通讯",
                Icon = "🔗",
                Desc = "SECS/GEM（HSMS-SS）通讯：连接设定、连接状态、常用报文收发与实时报文日志。",
                Direct = main.SecsGemPage
            });

            list.Add(new ToolGroupVm { Name = "设定模式", Icon = "🗂️", Desc = "设定当前设备的工作模式（正常 / 练习 / 空跑 / 清除 / 维护）。", Direct = D("mode", "🗂️", "设定模式", "设定当前设备的工作模式（正常 / 练习 / 空跑 / 清除 / 维护）。", null) });
            list.Add(new ToolGroupVm { Name = "使能设定", Icon = "🎚️", Desc = "设定设备各部分（测试臂 Nest / 单元 / Site）的使能是否打开。", Direct = D("enable", "🎚️", "使能设定", "设定设备各部分（测试臂 Nest / 单元 / Site）的使能是否打开。", null) });
            list.Add(new ToolGroupVm { Name = "温控设定", Icon = "🌡️", Desc = "选择加热器的工作条件（室温 / 高温）。", Direct = D("tempctl", "🌡️", "温控设定", "选择加热器的工作条件（室温 / 高温）。", null) });
            list.Add(new ToolGroupVm { Name = "电机归零", Icon = "🏠", Desc = "选择需要进行归零的电机或按区域归零。", Direct = D("motorhome", "🏠", "电机归零", "选择需要进行归零的电机或按区域归零。", null) });
            list.Add(new ToolGroupVm { Name = "信息记录", Icon = "📋", Desc = "查询设备运行过程中的事件记录并可导出为文本文件。", Direct = D("inforec", "📋", "信息记录", "查询设备运行过程中的事件记录并可导出为文本文件。", null) });

            list.Add(new ToolGroupVm
            {
                Name = "显示控制板",
                Icon = "🖥️",
                Desc = "设备状态窗口的显示 / 隐藏控制，可控制各区域 IC 状态与统计窗口。",
                Items =
                {
                    D("board_testarea", "🔬", "料状态-测试区域", "实时显示 Load Arm / Shuttle / Test Hand / Offload Arm 中的 IC 分布。", null),
                    D("board_inarea", "📥", "料状态-入料区域", "显示入料盘和清洁盘上的 IC 状态。", null),
                    D("board_outarea", "📤", "料状态-出料区域", "显示 6 个出料盘（Auto1~3 / Fix1~3）的料信息。", null),
                    D("board_batch", "📈", "批次数据统计", "显示数据清除后设备所处理产品的数量，按各部分分别统计。", null),
                    D("board_site", "🅰️", "测试工位统计-Site", "按测试臂及测试位详细显示好品数 / 处理数或良品率。", null),
                    D("board_bin", "🅱️", "测试工位统计-Bin", "显示测试结果在测试位及测试臂各个具体位置的分布情况。", null),
                    D("board_history", "🕘", "结果历史", "显示测试座或测试臂各个位置最近 16 个测试结果的历史。", null),
                    D("board_temp", "🌡️", "温度监测", "显示温度控制器各通道状态（温度模式、加热时间、设定温度）。", null),
                    D("board_traycls", "🎨", "料盘分类", "显示当前的 BIN 分类设定。", null),
                    D("board_spc", "📉", "SPC", "显示 SPC 统计数据，以天为单位统计并自动生成 spcrp 文件。", null),
                    D("board_alarm", "🚨", "报警统计", "统计 Jam 报警发生次数，可按批次 / 累计 / 开机后统计。", null)
                }
            });

            // 绑定导航命令
            foreach (var g in list)
            {
                if (g.Direct != null) g.Direct.OpenCommand = new RelayCommand(() => main.Navigate(g.Direct));
                foreach (var item in g.Items)
                {
                    var p = item;
                    p.OpenCommand = new RelayCommand(() => main.Navigate(p));
                }
                var grp = g;
                // 有子项：点击只负责展开下拉（由 IsOpen 驱动 Popup）；无子项：直接打开页面
                grp.Command = new RelayCommand(() => { if (grp.Direct != null) main.Navigate(grp.Direct); });
            }
            return list;
        }

        public static PageVm Home(MainViewModel main)
        {
            var p = D("home", "🏠", "主界面",
                 "控制程序基本显示界面：命令中心工具条 + 主界面，显示当前控制程序的主要状态。",
                 null);
            p.Fill = true;
            return p;
        }

        public static PageVm Manual(MainViewModel main)
            => D("manual", "📖", "说明书",
                 "原厂《Pick and Place Handler 9000 用户手册》全文。",
                 null);

        /// <summary>SECS/GEM 通讯页（入口在命令中心工具条「通讯」组，位于设定模式左侧，温启志 18719361399）。</summary>
        public static PageVm SecsGem(MainViewModel main)
            => D("secs_gem", "🔗", "SECS/GEM 通讯",
                 "SECS-II / HSMS-SS / GEM 通讯：连接设定、连接状态、常用报文收发与实时报文日志。",
                 null);
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
