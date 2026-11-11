// ============================================================================
//  温启志 18719361399 · 本文件由 温启志 18719361399 编写与维护
//  版权所有所有权利保留；界面与源码中文命名风格亦归温启志 18719361399 所有。
// ============================================================================
//  Motion/HandoverTables.cs —— 交接资料实测表。
//  来源 docs/三温测试分选机-IO表 7.30(1).xlsx（轴控 / 输入输出&阀岛 / 阀岛 / 485通信 / 飞拍通道）
//  工站名取自 32-handler 源码里的 GetStationIDByName 调用。
//  本文件只是资料数据，不直接驱动运动；驱动仍走 MotionManager 的仿真/雷赛卡。
using System;
using System.Collections.Generic;

namespace HandlerGuide.Motion
{
    /// <summary>资料实测的一根轴。</summary>
    public sealed class HandoverAxis
    {
        public int Index;
        public string Name;
        public string Branch;
        public string Kind;
        public string Power;
        public string Motor;
        public string Driver;
        public string Station;
    }

    /// <summary>资料实测的一个数字 IO 点。</summary>
    public sealed class HandoverIo
    {
        public string Block;
        public int Bit;
        public string Name;
        public string Sensor;
        public bool IsOutput;
        public string Station;
    }

    /// <summary>附表（GL20 远端 IO 版本）里的一个点：块 + 表内点号 + 块内位号 + 型号 + 脚位。</summary>
    public sealed class HandoverGl20
    {
        public string Area;      // 上料 / 下料
        public bool IsOutput;
        public string Block;     // U0 / U7 / U23 ...
        public int Addr;         // 表格里的点号（输入216 / 输出200）
        public int Bit;          // 块内位号
        public string Name;
        public string Model;     // 物料型号
        public string Pin;       // 脚位
    }

    /// <summary>附表里的一个 IO 模块（块）：区域 + 块号 + 位数 + 模块型号 / 安装位置。</summary>
    public sealed class HandoverGl20Block
    {
        public string Area;      // 上料 / 下料
        public string Block;     // U0 / U7 ...
        public bool IsOutput;
        public int Slots;        // 位数（32 / 16 / 8 / 模拟 2）
        public string Title;     // 模块型号与安装位置
    }

    /// <summary>阀岛上的一个气缸位。</summary>
    public sealed class HandoverValve
    {
        public string Island;
        public int Pos;
        public string Name;
        public string Control;
        public string Area;
        public string Sensor;
    }

    /// <summary>485 上挂的一台设备。</summary>
    public sealed class HandoverSerial
    {
        public string Name;
        public string Where;
        public string Power;
        public string Station;
    }

    /// <summary>拼出资料里的 IO 地址串，如 U0_X2.0。</summary>
    public static class HandoverAddr
    {
        public static string Address(HandoverIo p)
        {
            return p.Block + (p.IsOutput ? "_Y" : "_X") + (p.Bit / 8) + "." + (p.Bit % 8);
        }
    }

    /// <summary>三温机交接资料的轴 / IO / 阀岛 / 通讯表。</summary>
    public static class HandoverTables
    {
        /// <summary>资料实测 45 根轴：伺服 19 + 步进 26。</summary>
        public const int AxisCount = 45;
        /// <summary>数字输入有名点位数（地址空间 11 块 x 32 = 352）。</summary>
        public const int InputNamed = 303;
        /// <summary>数字输出有名点位数（地址空间 5 块 x 32 = 160）。</summary>
        public const int OutputNamed = 123;
        /// <summary>阀岛位数（3 座）。</summary>
        public const int ValveCount = 44;

        public static readonly HandoverAxis[] Axes =
        {
            new HandoverAxis { Index = 0, Name = "前压头Z轴", Branch = "分支4", Kind = "伺服", Power = "2000W", Motor = "汇川 MS1H2-20C30CB-A334R（2000W 带刹车）", Driver = "汇川 SV635NS012I (1300W/2000W EtherCAT总线型)", Station = "压头测试工位" },
            new HandoverAxis { Index = 1, Name = "后压头Z轴", Branch = "分支4", Kind = "伺服", Power = "2000W", Motor = "汇川 MS1H2-20C30CB-A334R（2000W 带刹车）", Driver = "汇川 SV635NS012I (1300W/2000W EtherCAT总线型)", Station = "压头测试工位" },
            new HandoverAxis { Index = 2, Name = "前压头Y轴", Branch = "分支4", Kind = "伺服", Power = "750W", Motor = "汇川 MS1H4-75B30CB-A331R(750W不带刹车)", Driver = "汇川 SV660NDS5R5I", Station = "压头测试工位" },
            new HandoverAxis { Index = 3, Name = "后压头Y轴", Branch = "分支4", Kind = "伺服", Power = "750W", Motor = "汇川 MS1H4-75B30CB-A331R(750W不带刹车)", Driver = "", Station = "压头测试工位" },
            new HandoverAxis { Index = 4, Name = "前梭车X轴", Branch = "分支4", Kind = "伺服", Power = "400W", Motor = "汇川 MS1H4-40B30CB-A331R-S(400W不带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "梭车工位" },
            new HandoverAxis { Index = 5, Name = "后梭车X轴", Branch = "分支4", Kind = "伺服", Power = "400W", Motor = "汇川 MS1H4-40B30CB-A331R-S(400W不带刹车)", Driver = "", Station = "梭车工位" },
            new HandoverAxis { Index = 6, Name = "Auto1Z轴", Branch = "分支4", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 7, Name = "Auto2Z轴", Branch = "分支4", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 8, Name = "Auto3Z轴", Branch = "分支4", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 9, Name = "下料旋转U轴", Branch = "分支4", Kind = "伺服", Power = "100W", Motor = "", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 10, Name = "下料X轴", Branch = "分支4", Kind = "伺服", Power = "400W", Motor = "汇川 MS1H4-40B30CB-A331R-S(400W不带刹车)", Driver = "汇川 SV635NS2R8I(400W EtherCAT总线型)", Station = "下料臂" },
            new HandoverAxis { Index = 11, Name = "下料Y轴", Branch = "分支4", Kind = "伺服", Power = "1000W", Motor = "汇川 MS1H4-10C30CB-A331R(1000W不带刹车)", Driver = "汇川 SV635NS7R6I-FS (1000W EtherCAT总线型)", Station = "下料臂" },
            new HandoverAxis { Index = 12, Name = "下料变距X轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 42CME08(42闭环)", Driver = "雷赛 2CL3-EC403T", Station = "下料臂" },
            new HandoverAxis { Index = 13, Name = "下料变距Y轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 42CME08(42闭环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 14, Name = "Auto1Y轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "雷赛 4DM3C-EC556", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 15, Name = "Auto2Y轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 16, Name = "Auto3Y轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "", Station = "Auto1/2/3 收料仓" },
            new HandoverAxis { Index = 18, Name = "下料吸嘴1Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "雷赛 4DM3C-EC432", Station = "下料臂" },
            new HandoverAxis { Index = 19, Name = "下料吸嘴2Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 20, Name = "下料吸嘴3Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 21, Name = "下料吸嘴4Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 22, Name = "下料吸嘴5Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "雷赛 4DM3C-EC432", Station = "下料臂" },
            new HandoverAxis { Index = 23, Name = "下料吸嘴6Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 24, Name = "下料吸嘴7Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 25, Name = "下料吸嘴8Z轴", Branch = "分支4", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "下料臂" },
            new HandoverAxis { Index = 26, Name = "上料X轴", Branch = "分支2", Kind = "伺服", Power = "400W", Motor = "汇川 MS1H4-40B30CB-A331R-S(400W不带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "上料臂" },
            new HandoverAxis { Index = 27, Name = "搬运X轴", Branch = "分支2", Kind = "伺服", Power = "400W", Motor = "汇川 MS1H4-40B30CB-A331R-S(400W不带刹车)", Driver = "", Station = "料盘搬运臂" },
            new HandoverAxis { Index = 28, Name = "进料仓Z轴", Branch = "分支2", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "进料仓" },
            new HandoverAxis { Index = 29, Name = "出盘仓Z轴", Branch = "分支2", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "", Station = "出盘仓" },
            new HandoverAxis { Index = 30, Name = "空盘仓Z轴", Branch = "分支2", Kind = "伺服", Power = "100W", Motor = "汇川 MS1H4-10B30CB-A332R(100W带刹车)", Driver = "汇川 SV660NDS2R8I", Station = "空盘仓" },
            new HandoverAxis { Index = 31, Name = "上料旋转U轴", Branch = "分支2", Kind = "伺服", Power = "100W", Motor = "", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 32, Name = "上料Y轴", Branch = "分支2", Kind = "伺服", Power = "1000W", Motor = "汇川 MS1H4-10C30CB-A331R(1000W不带刹车)", Driver = "汇川 SV635NS7R6I-FS (1000W EtherCAT总线型)", Station = "上料臂" },
            new HandoverAxis { Index = 33, Name = "上料变距X", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 42CME08(42闭环)", Driver = "雷赛 2CL3-EC403T", Station = "上料臂" },
            new HandoverAxis { Index = 34, Name = "上料变距Y", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 42CME08(42闭环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 35, Name = "进料仓Y轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "雷赛 4DM3C-EC556", Station = "进料仓" },
            new HandoverAxis { Index = 36, Name = "出盘仓Y轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "", Station = "出盘仓" },
            new HandoverAxis { Index = 37, Name = "空盘仓Y轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 57CM23-K(开环)", Driver = "", Station = "空盘仓" },
            new HandoverAxis { Index = 39, Name = "上料吸嘴1Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "雷赛 4DM3C-EC432", Station = "上料臂" },
            new HandoverAxis { Index = 40, Name = "上料吸嘴2Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 41, Name = "上料吸嘴3Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 42, Name = "上料吸嘴4Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 43, Name = "上料吸嘴5Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "雷赛 4DM3C-EC432", Station = "上料臂" },
            new HandoverAxis { Index = 44, Name = "上料吸嘴6Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 45, Name = "上料吸嘴7Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
            new HandoverAxis { Index = 46, Name = "上料吸嘴8Z轴", Branch = "分支2", Kind = "步进", Power = "-", Motor = "雷赛 28CM006(开环)", Driver = "", Station = "上料臂" },
        };

        public static readonly HandoverIo[] Inputs =
        {
            new HandoverIo { Block = "U0", Bit = 0, Name = "启动按钮（绿）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 1, Name = "复位按钮（黄）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 2, Name = "暂停按钮（红，自锁）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 3, Name = "使能关闭按钮（蓝，自锁）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 4, Name = "单步按钮（白）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 5, Name = "蜂鸣屏蔽按钮（白）", Sensor = "", IsOutput = false, Station = "操作面板" },
            new HandoverIo { Block = "U0", Bit = 6, Name = "门禁按钮（黄）", Sensor = "", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 8, Name = "急停按钮", Sensor = "", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 9, Name = "治具锁定按钮（金属）", Sensor = "", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U0", Bit = 16, Name = "左门禁1", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 17, Name = "左门禁2", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 18, Name = "右门禁1", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 19, Name = "右门禁2", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 20, Name = "后左门禁1", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 21, Name = "后左门禁2", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 22, Name = "后右门禁1", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 23, Name = "后右门禁2", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U0", Bit = 24, Name = "压头门禁1", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U0", Bit = 25, Name = "压头门禁2", Sensor = "接近开关 LE Raun TQF18-05NO", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U0", Bit = 26, Name = "左前中转敲击气缸+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "气缸与阀岛" },
            new HandoverIo { Block = "U0", Bit = 27, Name = "左后中转敲击气缸+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "气缸与阀岛" },
            new HandoverIo { Block = "U0", Bit = 28, Name = "右前中转敲击气缸+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "气缸与阀岛" },
            new HandoverIo { Block = "U0", Bit = 29, Name = "右后中转敲击气缸+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "气缸与阀岛" },
            new HandoverIo { Block = "U1", Bit = 0, Name = "离子风1维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 1, Name = "离子风1异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 2, Name = "离子风2维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 3, Name = "离子风2异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 4, Name = "离子风3维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 5, Name = "离子风3异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 6, Name = "离子风4维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 7, Name = "离子风4异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 8, Name = "离子风5维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 9, Name = "离子风5异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 10, Name = "离子风6维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 11, Name = "离子风6异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 12, Name = "离子风7维护信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 13, Name = "离子风7异常信号", Sensor = "SMC IZF31-B", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 14, Name = "离子棒维护信号", Sensor = "IZT41-28D14HL-NNF-X10", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 15, Name = "离子棒异常信号", Sensor = "IZT41-28D14HL-NNF-X10", IsOutput = false, Station = "静电与露点" },
            new HandoverIo { Block = "U1", Bit = 16, Name = "料盘搬运上下+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U1", Bit = 17, Name = "料盘搬运上下-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U1", Bit = 18, Name = "料盘搬运夹爪+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U1", Bit = 19, Name = "料盘搬运夹爪-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U1", Bit = 20, Name = "料盘搬运夹爪有无感应", Sensor = "OMRON E3Z-D61", IsOutput = false, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U1", Bit = 21, Name = "缓存台1有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 22, Name = "缓存台2有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 23, Name = "缓存台3有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 24, Name = "前左压力表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 25, Name = "前右压力表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 26, Name = "后左压力表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 27, Name = "后右压力表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 28, Name = "进气正压表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U1", Bit = 29, Name = "进气负压表", Sensor = "SMC ZSE20-N-M5-L", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U2", Bit = 0, Name = "治具锁定气缸1+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 1, Name = "治具锁定气缸1-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 2, Name = "治具锁定气缸2+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 3, Name = "治具锁定气缸2-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 4, Name = "治具锁定气缸3+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 5, Name = "治具锁定气缸3-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 6, Name = "治具锁定气缸4+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 7, Name = "治具锁定气缸4-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 8, Name = "治具锁定气缸5+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 9, Name = "治具锁定气缸5-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 10, Name = "治具锁定气缸6+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 11, Name = "治具锁定气缸6-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U2", Bit = 12, Name = "门禁气缸1+", Sensor = "SMC D-M9NL", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U2", Bit = 13, Name = "门禁气缸1-", Sensor = "SMC D-M9NL", IsOutput = false, Station = "机架与安全" },
            new HandoverIo { Block = "U2", Bit = 14, Name = "飞拍相机气缸+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "视觉与飞拍" },
            new HandoverIo { Block = "U2", Bit = 15, Name = "飞拍相机气缸-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "视觉与飞拍" },
            new HandoverIo { Block = "U2", Bit = 16, Name = "进料仓气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 17, Name = "进料仓气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 18, Name = "进料仓气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 19, Name = "进料仓气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 20, Name = "进料仓气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 21, Name = "进料仓气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 22, Name = "进料仓气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 23, Name = "进料仓气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U2", Bit = 24, Name = "出空盘仓气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 25, Name = "出空盘仓气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 26, Name = "出空盘仓气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 27, Name = "出空盘仓气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 28, Name = "出空盘仓气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 29, Name = "出空盘仓气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 30, Name = "出空盘仓气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U2", Bit = 31, Name = "出空盘仓气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U3", Bit = 0, Name = "进空盘仓气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 1, Name = "进空盘仓气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 2, Name = "进空盘仓气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 3, Name = "进空盘仓气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 4, Name = "进空盘仓气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 5, Name = "进空盘仓气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 6, Name = "进空盘仓气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 7, Name = "进空盘仓气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U3", Bit = 8, Name = "OK料仓气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 9, Name = "OK料仓气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 10, Name = "OK料仓气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 11, Name = "OK料仓气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 12, Name = "OK料仓气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 13, Name = "OK料仓气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 14, Name = "OK料仓气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 15, Name = "OK料仓气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 16, Name = "NG料仓1气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 17, Name = "NG料仓1气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 18, Name = "NG料仓1气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 19, Name = "NG料仓1气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 20, Name = "NG料仓1气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 21, Name = "NG料仓1气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 22, Name = "NG料仓1气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 23, Name = "NG料仓1气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 24, Name = "NG料仓2气缸1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 25, Name = "NG料仓2气缸1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 26, Name = "NG料仓2气缸2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 27, Name = "NG料仓2气缸2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 28, Name = "NG料仓2气缸3+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 29, Name = "NG料仓2气缸3-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 30, Name = "NG料仓2气缸4+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U3", Bit = 31, Name = "NG料仓2气缸4-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 0, Name = "进料仓满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 1, Name = "进料仓料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 2, Name = "进料仓料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 3, Name = "进料仓料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 4, Name = "进料仓料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 5, Name = "进料仓料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 6, Name = "进料仓料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 7, Name = "进料仓料盘有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U4", Bit = 8, Name = "出空盘仓满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 9, Name = "出空盘仓料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 10, Name = "出空盘仓料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 11, Name = "出空盘仓料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 12, Name = "出空盘仓料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 13, Name = "出空盘仓料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 14, Name = "出空盘仓料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 15, Name = "出空盘仓料盘有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U4", Bit = 16, Name = "进空盘仓满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 17, Name = "进空盘仓料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 18, Name = "进空盘仓料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 19, Name = "进空盘仓料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 20, Name = "进空盘仓料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 21, Name = "进空盘仓料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 22, Name = "进空盘仓料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 23, Name = "进空盘仓料盘有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U4", Bit = 24, Name = "OK料仓满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 25, Name = "OK料仓料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 26, Name = "OK料仓料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 27, Name = "OK料仓料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 28, Name = "OK料仓料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 29, Name = "OK料仓料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 30, Name = "OK料仓料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U4", Bit = 31, Name = "OK料仓有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 0, Name = "NG料仓1满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 1, Name = "NG料仓1料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 2, Name = "NG料仓1料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 3, Name = "NG料仓1料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 4, Name = "NG料仓1料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 5, Name = "NG料仓1料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 6, Name = "NG料仓1料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 7, Name = "NG料仓1有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 8, Name = "NG料仓2满料感应", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 9, Name = "NG料仓2料盘检测1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 10, Name = "NG料仓2料盘检测2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 11, Name = "NG料仓2料盘感应1", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 12, Name = "NG料仓2料盘感应2", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 13, Name = "NG料仓2料盘感应3", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 14, Name = "NG料仓2料盘感应4", Sensor = "松下 EX-13EB", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 15, Name = "NG料仓2有无感应", Sensor = "松下 EX-14A", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U5", Bit = 16, Name = "进料仓侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 17, Name = "进料仓侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 18, Name = "进料仓侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 19, Name = "进料仓侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 20, Name = "进料仓竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 21, Name = "进料仓竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "进料仓" },
            new HandoverIo { Block = "U5", Bit = 24, Name = "出空盘仓侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U5", Bit = 25, Name = "出空盘仓侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U5", Bit = 26, Name = "出空盘仓侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U5", Bit = 27, Name = "出空盘仓侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U5", Bit = 28, Name = "出空盘仓竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U5", Bit = 29, Name = "出空盘仓竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "出空盘仓" },
            new HandoverIo { Block = "U6", Bit = 0, Name = "进空盘仓侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 1, Name = "进空盘仓侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 2, Name = "进空盘仓侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 3, Name = "进空盘仓侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 4, Name = "进空盘仓竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 5, Name = "进空盘仓竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "进空盘仓" },
            new HandoverIo { Block = "U6", Bit = 8, Name = "OK料仓侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 9, Name = "OK料仓侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 10, Name = "OK料仓侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 11, Name = "OK料仓侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 12, Name = "OK料仓竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 13, Name = "OK料仓竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 16, Name = "NG料仓1侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 17, Name = "NG料仓1侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 18, Name = "NG料仓1侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 19, Name = "NG料仓1侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 20, Name = "NG料仓1竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 21, Name = "NG料仓1竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 24, Name = "NG料仓2侧推1+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 25, Name = "NG料仓2侧推1-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 26, Name = "NG料仓2侧推2+", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 27, Name = "NG料仓2侧推2-", Sensor = "SMC D-F8NL", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 28, Name = "NG料仓2竖推+", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U6", Bit = 29, Name = "NG料仓2竖推-", Sensor = "AirTAC DMSG-N020", IsOutput = false, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U7", Bit = 0, Name = "左前中转光纤1", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 1, Name = "左前中转光纤2", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 2, Name = "左前中转光纤3", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 3, Name = "左前中转光纤4", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 4, Name = "左前中转光纤5", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 5, Name = "左前中转光纤6", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 6, Name = "左前中转光纤7", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 7, Name = "左前中转光纤8", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 8, Name = "左后中转光纤1", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 9, Name = "左后中转光纤2", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 10, Name = "左后中转光纤3", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 11, Name = "左后中转光纤4", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 12, Name = "左后中转光纤5", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 13, Name = "左后中转光纤6", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 14, Name = "左后中转光纤7", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 15, Name = "左后中转光纤8", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U7", Bit = 16, Name = "左上料真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 17, Name = "左上料真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 18, Name = "左上料真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 19, Name = "左上料真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 20, Name = "左上料真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 21, Name = "左上料真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 22, Name = "左上料真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 23, Name = "左上料真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U7", Bit = 24, Name = "右下料真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 25, Name = "右下料真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 26, Name = "右下料真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 27, Name = "右下料真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 28, Name = "右下料真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 29, Name = "右下料真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 30, Name = "右下料真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U7", Bit = 31, Name = "右下料真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U8", Bit = 0, Name = "前压头真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 1, Name = "前压头真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 2, Name = "前压头真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 3, Name = "前压头真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 4, Name = "前压头真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 5, Name = "前压头真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 6, Name = "前压头真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 7, Name = "前压头真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 8, Name = "前压头真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 9, Name = "前压头真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 10, Name = "前压头真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 11, Name = "前压头真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 12, Name = "前压头真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 13, Name = "前压头真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 14, Name = "前压头真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 15, Name = "前压头真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 16, Name = "后压头真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 17, Name = "后压头真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 18, Name = "后压头真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 19, Name = "后压头真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 20, Name = "后压头真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 21, Name = "后压头真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 22, Name = "后压头真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 23, Name = "后压头真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 24, Name = "后压头真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 25, Name = "后压头真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 26, Name = "后压头真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 27, Name = "后压头真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 28, Name = "后压头真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 29, Name = "后压头真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 30, Name = "后压头真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U8", Bit = 31, Name = "后压头真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = false, Station = "压头测试工位" },
            new HandoverIo { Block = "U9", Bit = 0, Name = "右前中转光纤1", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U9", Bit = 1, Name = "右前中转光纤2", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U9", Bit = 2, Name = "右前中转光纤3", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U9", Bit = 3, Name = "右后中转光纤1", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U9", Bit = 4, Name = "右后中转光纤2", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U9", Bit = 5, Name = "右后中转光纤3", Sensor = "松下 FX-551-C2", IsOutput = false, Station = "其他" },
            new HandoverIo { Block = "U10", Bit = 0, Name = "左上料气缸1+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 1, Name = "左上料气缸1-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 2, Name = "左上料气缸2+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 3, Name = "左上料气缸2-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 4, Name = "左上料气缸3+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 5, Name = "左上料气缸3-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 6, Name = "左上料气缸4+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 7, Name = "左上料气缸4-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 8, Name = "左上料气缸5+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 9, Name = "左上料气缸5-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 10, Name = "左上料气缸6+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 11, Name = "左上料气缸6-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 12, Name = "左上料气缸7+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 13, Name = "左上料气缸7-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 14, Name = "左上料气缸8+", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 15, Name = "左上料气缸8-", Sensor = "预留IO", IsOutput = false, Station = "上料臂" },
            new HandoverIo { Block = "U10", Bit = 16, Name = "右下料气缸1+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 17, Name = "右下料气缸1-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 18, Name = "右下料气缸2+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 19, Name = "右下料气缸2-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 20, Name = "右下料气缸3+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 21, Name = "右下料气缸3-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 22, Name = "右下料气缸4+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 23, Name = "右下料气缸4-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 24, Name = "右下料气缸5+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 25, Name = "右下料气缸5-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 26, Name = "右下料气缸6+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 27, Name = "右下料气缸6-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 28, Name = "右下料气缸7+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 29, Name = "右下料气缸7-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 30, Name = "右下料气缸8+", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
            new HandoverIo { Block = "U10", Bit = 31, Name = "右下料气缸8-", Sensor = "预留IO", IsOutput = false, Station = "下料臂" },
        };

        public static readonly HandoverIo[] Outputs =
        {
            new HandoverIo { Block = "U20", Bit = 0, Name = "红灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 1, Name = "黄灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 2, Name = "绿灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 3, Name = "蜂鸣器", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 4, Name = "电箱照明", Sensor = "", IsOutput = true, Station = "静电与露点" },
            new HandoverIo { Block = "U20", Bit = 5, Name = "启动按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 6, Name = "复位按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 7, Name = "暂停按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 8, Name = "使能关闭按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 9, Name = "单步按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 10, Name = "蜂鸣屏蔽按钮灯", Sensor = "", IsOutput = true, Station = "操作面板" },
            new HandoverIo { Block = "U20", Bit = 11, Name = "门禁按钮灯", Sensor = "", IsOutput = true, Station = "机架与安全" },
            new HandoverIo { Block = "U20", Bit = 13, Name = "治具锁定按钮灯", Sensor = "", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U20", Bit = 14, Name = "离子棒1", Sensor = "IZT41-28D14HL-NNF-X10", IsOutput = true, Station = "静电与露点" },
            new HandoverIo { Block = "U20", Bit = 15, Name = "离子棒2", Sensor = "IZT41-28D14HL-NNF-X10", IsOutput = true, Station = "静电与露点" },
            new HandoverIo { Block = "U20", Bit = 16, Name = "进料仓Z刹车", Sensor = "", IsOutput = true, Station = "进料仓" },
            new HandoverIo { Block = "U20", Bit = 17, Name = "出空盘仓Z刹车", Sensor = "", IsOutput = true, Station = "出空盘仓" },
            new HandoverIo { Block = "U20", Bit = 18, Name = "进空盘仓Z刹车", Sensor = "", IsOutput = true, Station = "进空盘仓" },
            new HandoverIo { Block = "U20", Bit = 19, Name = "OK料仓Z刹车", Sensor = "", IsOutput = true, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U20", Bit = 20, Name = "NG料仓Z1刹车", Sensor = "", IsOutput = true, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U20", Bit = 21, Name = "NG料仓Z2刹车", Sensor = "", IsOutput = true, Station = "OK/NG 料仓" },
            new HandoverIo { Block = "U20", Bit = 22, Name = "前压头Z刹车", Sensor = "", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U20", Bit = 23, Name = "后压头Z刹车", Sensor = "", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U21", Bit = 0, Name = "料盘搬运夹爪松", Sensor = "SMC SY3320-5LZD-M5", IsOutput = true, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U21", Bit = 1, Name = "料盘搬运夹爪夹", Sensor = "SMC SY3320-5LZD-M5", IsOutput = true, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U21", Bit = 2, Name = "料盘搬运上下上", Sensor = "SMC SY3320-5LZD-M5", IsOutput = true, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U21", Bit = 3, Name = "料盘搬运上下下", Sensor = "SMC SY3320-5LZD-M5", IsOutput = true, Station = "料盘搬运臂" },
            new HandoverIo { Block = "U21", Bit = 16, Name = "左上料吸真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 17, Name = "左上料破真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 18, Name = "左上料吸真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 19, Name = "左上料破真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 20, Name = "左上料吸真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 21, Name = "左上料破真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 22, Name = "左上料吸真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 23, Name = "左上料破真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 24, Name = "左上料吸真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 25, Name = "左上料破真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 26, Name = "左上料吸真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 27, Name = "左上料破真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 28, Name = "左上料吸真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 29, Name = "左上料破真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 30, Name = "左上料吸真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U21", Bit = 31, Name = "左上料破真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "上料臂" },
            new HandoverIo { Block = "U22", Bit = 0, Name = "右下料吸真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 1, Name = "右下料破真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 2, Name = "右下料吸真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 3, Name = "右下料破真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 4, Name = "右下料吸真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 5, Name = "右下料破真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 6, Name = "右下料吸真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 7, Name = "右下料破真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 8, Name = "右下料吸真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 9, Name = "右下料破真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 10, Name = "右下料吸真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 11, Name = "右下料破真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 12, Name = "右下料吸真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 13, Name = "右下料破真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 14, Name = "右下料吸真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 15, Name = "右下料破真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "下料臂" },
            new HandoverIo { Block = "U22", Bit = 16, Name = "前压头吸真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 17, Name = "前压头吸真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 18, Name = "前压头吸真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 19, Name = "前压头吸真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 20, Name = "前压头吸真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 21, Name = "前压头吸真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 22, Name = "前压头吸真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 23, Name = "前压头吸真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 24, Name = "前压头吸真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 25, Name = "前压头吸真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 26, Name = "前压头吸真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 27, Name = "前压头吸真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 28, Name = "前压头吸真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 29, Name = "前压头吸真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 30, Name = "前压头吸真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U22", Bit = 31, Name = "前压头吸真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 0, Name = "前压头破真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 1, Name = "前压头破真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 2, Name = "前压头破真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 3, Name = "前压头破真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 4, Name = "前压头破真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 5, Name = "前压头破真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 6, Name = "前压头破真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 7, Name = "前压头破真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 8, Name = "前压头破真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 9, Name = "前压头破真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 10, Name = "前压头破真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 11, Name = "前压头破真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 12, Name = "前压头破真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 13, Name = "前压头破真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 14, Name = "前压头破真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 15, Name = "前压头破真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 16, Name = "后压头吸真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 17, Name = "后压头吸真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 18, Name = "后压头吸真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 19, Name = "后压头吸真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 20, Name = "后压头吸真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 21, Name = "后压头吸真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 22, Name = "后压头吸真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 23, Name = "后压头吸真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 24, Name = "后压头吸真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 25, Name = "后压头吸真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 26, Name = "后压头吸真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 27, Name = "后压头吸真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 28, Name = "后压头吸真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 29, Name = "后压头吸真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 30, Name = "后压头吸真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U23", Bit = 31, Name = "后压头吸真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 0, Name = "后压头破真空1", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 1, Name = "后压头破真空2", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 2, Name = "后压头破真空3", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 3, Name = "后压头破真空4", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 4, Name = "后压头破真空5", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 5, Name = "后压头破真空6", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 6, Name = "后压头破真空7", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 7, Name = "后压头破真空8", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 8, Name = "后压头破真空9", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 9, Name = "后压头破真空10", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 10, Name = "后压头破真空11", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 11, Name = "后压头破真空12", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 12, Name = "后压头破真空13", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 13, Name = "后压头破真空14", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 14, Name = "后压头破真空15", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
            new HandoverIo { Block = "U24", Bit = 15, Name = "后压头破真空16", Sensor = "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4-4", IsOutput = true, Station = "压头测试工位" },
        };


        /// <summary>
        /// 附表里的 26 个 IO 模块（块）清单：按表里出现的先后排列，Title 记录模块型号与
        /// 安装位置（GL20-3200END = 32 位输入模块，GL20-0032ETN = 32 位输出模块，
        /// 「远端IO-…」= 远端扩展板，U10/U11 = 前 / 后压头比例阀模拟量）。
        /// </summary>
        public static readonly HandoverGl20Block[] Gl20Blocks =
        {
            new HandoverGl20Block { Area = "上料", Block = "U0", IsOutput = false, Slots = 32, Title = "GL20-3200END（操作面板 / 门禁 / 前压头真空）" },
            new HandoverGl20Block { Area = "上料", Block = "U1", IsOutput = false, Slots = 32, Title = "GL20-3200END（离子棒 / 料盘搬运 / 治具锁定）" },
            new HandoverGl20Block { Area = "上料", Block = "U23", IsOutput = false, Slots = 32, Title = "GL20-3200END（表面电位 / 门板 / 料盘检测）" },
            new HandoverGl20Block { Area = "上料", Block = "U5", IsOutput = false, Slots = 32, Title = "远端IO-上料侧内部大板" },
            new HandoverGl20Block { Area = "上料", Block = "U2", IsOutput = false, Slots = 16, Title = "远端IO-进料仓外部" },
            new HandoverGl20Block { Area = "上料", Block = "U3", IsOutput = false, Slots = 16, Title = "远端IO-出空盘仓外部" },
            new HandoverGl20Block { Area = "上料", Block = "U4", IsOutput = false, Slots = 16, Title = "远端IO-进空盘仓外部" },
            new HandoverGl20Block { Area = "上料", Block = "U6", IsOutput = false, Slots = 8, Title = "远端IO-上料Y轴处（左上料真空）" },
            new HandoverGl20Block { Area = "上料", Block = "U7", IsOutput = true, Slots = 32, Title = "GL20-0032ETN（面板灯 / 料盘搬运 / 真空泵）" },
            new HandoverGl20Block { Area = "上料", Block = "U8", IsOutput = true, Slots = 32, Title = "GL20-0032ETN（前压头吸真空 / 破真空）" },
            new HandoverGl20Block { Area = "上料", Block = "U9", IsOutput = true, Slots = 16, Title = "上料Y轴处（左上料吸 / 破真空）" },
            new HandoverGl20Block { Area = "上料", Block = "U10", IsOutput = false, Slots = 2, Title = "模拟输入（前 / 后压头比例阀）" },
            new HandoverGl20Block { Area = "上料", Block = "U11", IsOutput = true, Slots = 2, Title = "模拟输出（前 / 后压头比例阀）" },
            new HandoverGl20Block { Area = "下料", Block = "U12", IsOutput = false, Slots = 32, Title = "GL20-3200END（面板 / 缓存台 / 停止按钮）" },
            new HandoverGl20Block { Area = "下料", Block = "U13", IsOutput = false, Slots = 32, Title = "GL20-3200END（离子棒 / 门板 / 后压头真空）" },
            new HandoverGl20Block { Area = "下料", Block = "U24", IsOutput = false, Slots = 32, Title = "GL20-3200END（表格未填点名，32 位空槽）" },
            new HandoverGl20Block { Area = "下料", Block = "U18", IsOutput = false, Slots = 32, Title = "远端IO-下料侧内部大板缓存区" },
            new HandoverGl20Block { Area = "下料", Block = "U17", IsOutput = false, Slots = 32, Title = "远端IO-下料侧内部大板料仓区" },
            new HandoverGl20Block { Area = "下料", Block = "U14", IsOutput = false, Slots = 16, Title = "远端IO-Auto1仓外部" },
            new HandoverGl20Block { Area = "下料", Block = "U15", IsOutput = false, Slots = 16, Title = "远端IO-Auto2仓外部" },
            new HandoverGl20Block { Area = "下料", Block = "U16", IsOutput = false, Slots = 16, Title = "远端IO-Auto3仓外部" },
            new HandoverGl20Block { Area = "下料", Block = "U19", IsOutput = false, Slots = 8, Title = "远端IO-上料Y轴处（右下料真空）" },
            new HandoverGl20Block { Area = "下料", Block = "U20", IsOutput = true, Slots = 32, Title = "GL20-0032ETN（面板灯 / 敲击 / 料仓Z刹车）" },
            new HandoverGl20Block { Area = "下料", Block = "U21", IsOutput = true, Slots = 32, Title = "GL20-0032ETN（后压头吸真空 / 破真空 / 加热指示灯）" },
            new HandoverGl20Block { Area = "下料", Block = "U25", IsOutput = true, Slots = 32, Title = "GL20-0032ETN（表格未填点名，32 位空槽）" },
            new HandoverGl20Block { Area = "下料", Block = "U22", IsOutput = true, Slots = 16, Title = "上料Y轴处（右下料吸 / 破真空）" },
        };

        /// <summary>
        /// 附表「上料-输入输出 / 下料-输入输出」（GL20 远端 IO 版本）里的一个点。
        /// 这两张表与主表（轴控 / 输入输出&amp;阀岛）不是同一套编号：主表是 U0_X2.0 这种
        /// 「块_字节.位」，附表是「输入216 / 输出200」这种连续点号，而且块号重名
        /// （主表 U0 有 23 个输入，附表的 U0 是另外 32 个）——所以这里单独成表、按表原样
        /// 记录 点号 + 块 + 块内位号 + 型号 + 脚位，不与主表混编。
        /// </summary>
        public static readonly HandoverGl20[] Gl20Inputs =
        {

            // ---- U0 · 32 位输入 · GL20-3200END（操作面板 / 门禁 / 压头真空） ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 216, Bit = 0, Name = "POWEROFF", Model = "ONPOWLAS1-AF-11R24VP（S1）", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 217, Bit = 1, Name = "POWERON", Model = "ONPOWLAS1-AF-11W24VP（S2）", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 218, Bit = 2, Name = "ENABLE", Model = "ONPOWLAS1-AF-11G24VP（S3）", Pin = "A3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 219, Bit = 3, Name = "RESET", Model = "ONPOWLAS1-AF-11R24VP（S4）", Pin = "A4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 220, Bit = 4, Name = "PUASUE", Model = "ONPOWLAS1-AF-11ZY24VP（S5）", Pin = "A5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 221, Bit = 5, Name = "HOME", Model = "ONPOWLAS1-AF-11G24VP（S6）", Pin = "A6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 222, Bit = 6, Name = "START", Model = "ONPOWLAS1-AF-11G24VP（S7）", Pin = "A7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 223, Bit = 7, Name = "ONECYCLE", Model = "ONPOWLAS1-AF-11W24VP（S8）", Pin = "A8" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 224, Bit = 8, Name = "RETRY", Model = "ONPOWLAS1-AF-11W24VP（S9）", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 225, Bit = 9, Name = "SKIP", Model = "ONPOWLAS1-AF-11W24VP（S10）", Pin = "B2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 226, Bit = 10, Name = "CLEANOUT", Model = "ONPOWLAS1-AF-11W24VP（S11）", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 227, Bit = 11, Name = "TRAYFEED", Model = "ONPOWLAS1-AF-11W24VP（S12）", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 228, Bit = 12, Name = "TRAYEND", Model = "ONPOWLAS1-AF-11W24VP（S13）", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 229, Bit = 13, Name = "ALARMRESET", Model = "ONPOWLAS1-AF-11W24VP（S14）", Pin = "B6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 230, Bit = 14, Name = "前压头报警清除按钮", Model = "ONPOWLAS1-AF-11W24VP（S15）", Pin = "B7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 231, Bit = 15, Name = "后压头报警清除按钮", Model = "ONPOWLAS1-AF-11W24VP（S16）", Pin = "B8" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 232, Bit = 16, Name = "左前门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 233, Bit = 17, Name = "左后门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 234, Bit = 18, Name = "后左门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 235, Bit = 19, Name = "压头门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 236, Bit = 20, Name = "后右门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 237, Bit = 21, Name = "右后门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 238, Bit = 22, Name = "右前门禁", Model = "OMRONTL-Q5MC1-Z", Pin = "C7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 239, Bit = 23, Name = "LeftLoadTray", Model = "", Pin = "C8" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 240, Bit = 24, Name = "前压头真空Aa", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "D1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 241, Bit = 25, Name = "前压头真空Ab", Model = "", Pin = "D2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 242, Bit = 26, Name = "前压头真空Ac", Model = "", Pin = "D3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 243, Bit = 27, Name = "前压头真空Ad", Model = "", Pin = "D4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 244, Bit = 28, Name = "前压头真空Ba", Model = "", Pin = "D5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 245, Bit = 29, Name = "前压头真空Bb", Model = "", Pin = "D6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 246, Bit = 30, Name = "前压头真空Bc", Model = "", Pin = "D7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U0", Addr = 247, Bit = 31, Name = "前压头真空Bd", Model = "", Pin = "D8" },
            // ---- U1 · 32 位输入 · GL20-3200END（离子棒 / 料盘搬运 / 治具锁定） ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 248, Bit = 0, Name = "离子棒1维护信号", Model = "SMCIZTB42-46D26H-F", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 249, Bit = 1, Name = "离子棒1异常信号", Model = "SMCIZTB42-46D27H-F", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 258, Bit = 10, Name = "料盘搬运上下+", Model = "AirTACDMSG-N020", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 259, Bit = 11, Name = "料盘搬运上下-", Model = "AirTACDMSG-N020", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 260, Bit = 12, Name = "料盘搬运夹爪+", Model = "SMCD-M9NL", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 261, Bit = 13, Name = "料盘搬运夹爪-", Model = "SMCD-M9NL", Pin = "B6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 262, Bit = 14, Name = "料盘搬运夹爪有无感应", Model = "OMRONE3Z-D61", Pin = "B7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 264, Bit = 16, Name = "治具锁定气缸1+", Model = "SMCD-F8NL", Pin = "C1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 265, Bit = 17, Name = "治具锁定气缸1-", Model = "SMCD-F8NL", Pin = "C2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 266, Bit = 18, Name = "治具锁定气缸2+", Model = "SMCD-F8NL", Pin = "C3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 267, Bit = 19, Name = "治具锁定气缸2-", Model = "SMCD-F8NL", Pin = "C4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 268, Bit = 20, Name = "治具锁定气缸3+", Model = "SMCD-F8NL", Pin = "C5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 269, Bit = 21, Name = "治具锁定气缸3-", Model = "SMCD-F8NL", Pin = "C6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 270, Bit = 22, Name = "治具锁定气缸4+", Model = "SMCD-F8NL", Pin = "C7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 271, Bit = 23, Name = "治具锁定气缸4-", Model = "SMCD-F8NL", Pin = "C8" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 272, Bit = 24, Name = "治具锁定气缸5+", Model = "SMCD-F8NL", Pin = "D1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 273, Bit = 25, Name = "治具锁定气缸5-", Model = "SMCD-F8NL", Pin = "D2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 274, Bit = 26, Name = "治具锁定气缸6+", Model = "SMCD-F8NL", Pin = "D3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 275, Bit = 27, Name = "治具锁定气缸6-", Model = "SMCD-F8NL", Pin = "D4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 276, Bit = 28, Name = "左前急停信号", Model = "", Pin = "D5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 277, Bit = 29, Name = "左后急停信号", Model = "", Pin = "D6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U1", Addr = 278, Bit = 30, Name = "治具锁定按钮", Model = "", Pin = "D7" },
            // ---- U23 · 32 位输入 · GL20-3200END（表面电位 / 门板 / 料盘检测） ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 280, Bit = 0, Name = "表面电位检测1", Model = "SMCIZD10-110", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 281, Bit = 1, Name = "表面电位检测2", Model = "SMCIZD10-110", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 282, Bit = 2, Name = "表面电位检测3", Model = "SMCIZD10-110", Pin = "A3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 283, Bit = 3, Name = "表面电位检测4", Model = "SMCIZD10-110", Pin = "A4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 284, Bit = 4, Name = "表面电位检测5", Model = "SMCIZD10-110", Pin = "A5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 285, Bit = 5, Name = "表面电位检测6", Model = "SMCIZD10-110", Pin = "A6" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 286, Bit = 6, Name = "表面电位检测7", Model = "SMCIZD10-110", Pin = "A7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 287, Bit = 7, Name = "表面电位检测8", Model = "SMCIZD10-110", Pin = "A8" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 288, Bit = 8, Name = "表面电位检测9", Model = "SMCIZD10-110", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 289, Bit = 9, Name = "门板1+", Model = "接近开关", Pin = "B2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 290, Bit = 10, Name = "门板2+", Model = "接近开关", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 291, Bit = 11, Name = "中继小门1", Model = "", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 292, Bit = 12, Name = "中继小门2", Model = "", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 296, Bit = 16, Name = "进料仓料盘检测3", Model = "", Pin = "C1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 297, Bit = 17, Name = "进料仓料盘检测4", Model = "", Pin = "C2" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 298, Bit = 18, Name = "进料仓料盘检测5", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 299, Bit = 19, Name = "出空盘仓料盘检测3", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 300, Bit = 20, Name = "出空盘仓料盘检测4", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 302, Bit = 22, Name = "进空盘仓料盘检测3", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U23", Addr = 303, Bit = 23, Name = "进空盘仓料盘检测4", Model = "", Pin = "C8" },
            // ---- U5 · 32 位输入 · 远端IO-上料侧内部大板 ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 312, Bit = 0, Name = "左前中转感应Ba", Model = "松下EX-13EB", Pin = "00" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 313, Bit = 1, Name = "左前中转感应Bb", Model = "松下EX-13EB", Pin = "01" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 314, Bit = 2, Name = "左前中转感应Bc", Model = "松下EX-13EB", Pin = "02" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 315, Bit = 3, Name = "左前中转感应Bd", Model = "松下EX-13EB", Pin = "03" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 317, Bit = 5, Name = "左后中转感应Aa", Model = "松下EX-13EB", Pin = "05" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 318, Bit = 6, Name = "左后中转感应Ab", Model = "松下EX-13EB", Pin = "06" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 319, Bit = 7, Name = "左后中转感应Ac", Model = "松下EX-13EB", Pin = "07" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 320, Bit = 8, Name = "左后中转感应Ad", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 322, Bit = 10, Name = "门板气缸1+", Model = "SMCD-M9NL", Pin = "10" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 323, Bit = 11, Name = "门板气缸1-", Model = "SMCD-M9NL", Pin = "11" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 324, Bit = 12, Name = "门板气缸2+", Model = "SMCD-M9NL", Pin = "12" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 325, Bit = 13, Name = "门板气缸2-", Model = "SMCD-M9NL", Pin = "13" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 328, Bit = 16, Name = "上料芯片旋转感应Aa", Model = "松下EX-13EB", Pin = "16" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 329, Bit = 17, Name = "上料芯片旋转感应Ab", Model = "松下EX-13EB", Pin = "17" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 330, Bit = 18, Name = "上料芯片旋转感应Ac", Model = "松下EX-13EB", Pin = "18" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 331, Bit = 19, Name = "上料芯片旋转感应Ad", Model = "松下EX-13EB", Pin = "19" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 332, Bit = 20, Name = "上料芯片旋转感应Ba", Model = "松下EX-13EB", Pin = "20" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 333, Bit = 21, Name = "上料芯片旋转感应Bb", Model = "松下EX-13EB", Pin = "21" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 334, Bit = 22, Name = "上料芯片旋转感应Bc", Model = "松下EX-13EB", Pin = "22" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 335, Bit = 23, Name = "上料芯片旋转感应Bd", Model = "松下EX-13EB", Pin = "23" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 336, Bit = 24, Name = "进料仓侧推1+", Model = "SMCD-F8NL", Pin = "24" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 337, Bit = 25, Name = "进料仓侧推1-", Model = "SMCD-F8NL", Pin = "25" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 338, Bit = 26, Name = "进料仓侧推2+", Model = "SMCD-F8NL", Pin = "26" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 339, Bit = 27, Name = "进料仓侧推2-", Model = "SMCD-F8NL", Pin = "27" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 340, Bit = 28, Name = "进料仓竖向定位+", Model = "SMCD-M9NL", Pin = "28" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 341, Bit = 29, Name = "进料仓竖向定位-", Model = "SMCD-M9NL", Pin = "29" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 342, Bit = 30, Name = "风阀气缸+", Model = "", Pin = "30" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U5", Addr = 343, Bit = 31, Name = "风阀气缸-", Model = "", Pin = "31" },
            // ---- U2 · 16 位输入 · 远端IO-进料仓外部 ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 344, Bit = 0, Name = "进料仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 345, Bit = 1, Name = "进料仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 346, Bit = 2, Name = "进料仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 347, Bit = 3, Name = "进料仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 348, Bit = 4, Name = "进料仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 349, Bit = 5, Name = "进料仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 350, Bit = 6, Name = "进料仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 351, Bit = 7, Name = "进料仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 352, Bit = 8, Name = "进料仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 353, Bit = 9, Name = "进料仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 354, Bit = 10, Name = "进料仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 355, Bit = 11, Name = "进料仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 356, Bit = 12, Name = "进料仓料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 357, Bit = 13, Name = "进料仓料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 358, Bit = 14, Name = "进料仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U2", Addr = 359, Bit = 15, Name = "进料仓开关门-", Model = "", Pin = "0F" },
            // ---- U3 · 16 位输入 · 远端IO-出空盘仓外部 ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 360, Bit = 0, Name = "出空盘仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 361, Bit = 1, Name = "出空盘仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 362, Bit = 2, Name = "出空盘仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 363, Bit = 3, Name = "出空盘仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 364, Bit = 4, Name = "出空盘仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 365, Bit = 5, Name = "出空盘仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 366, Bit = 6, Name = "出空盘仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 367, Bit = 7, Name = "出空盘仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 368, Bit = 8, Name = "出空盘仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 369, Bit = 9, Name = "出空盘仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 370, Bit = 10, Name = "出空盘仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 371, Bit = 11, Name = "出空盘仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 372, Bit = 12, Name = "出空盘料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 373, Bit = 13, Name = "出空盘料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 374, Bit = 14, Name = "出空盘仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U3", Addr = 375, Bit = 15, Name = "出空盘仓开关门-", Model = "", Pin = "0F" },
            // ---- U4 · 16 位输入 · 远端IO-进空盘仓外部 ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 376, Bit = 0, Name = "进空盘仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 377, Bit = 1, Name = "进空盘仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 378, Bit = 2, Name = "进空盘仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 379, Bit = 3, Name = "进空盘仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 380, Bit = 4, Name = "进空盘仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 381, Bit = 5, Name = "进空盘仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 382, Bit = 6, Name = "进空盘仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 383, Bit = 7, Name = "进空盘仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 384, Bit = 8, Name = "进空盘仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 385, Bit = 9, Name = "进空盘仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 386, Bit = 10, Name = "进空盘仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 387, Bit = 11, Name = "进空盘仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 388, Bit = 12, Name = "进空盘仓料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 389, Bit = 13, Name = "进空盘仓料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 390, Bit = 14, Name = "进空盘仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U4", Addr = 391, Bit = 15, Name = "进空盘仓开关门-", Model = "", Pin = "0F" },
            // ---- U6 · 8 位输入 · 远端IO-上料Y轴处（左上料真空） ----
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 400, Bit = 0, Name = "左上料真空Aa", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "00" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 401, Bit = 1, Name = "左上料真空Ab", Model = "", Pin = "01" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 402, Bit = 2, Name = "左上料真空Ac", Model = "", Pin = "02" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 403, Bit = 3, Name = "左上料真空Ad", Model = "", Pin = "03" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 404, Bit = 4, Name = "左上料真空Ba", Model = "", Pin = "04" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 405, Bit = 5, Name = "左上料真空Bb", Model = "", Pin = "05" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 406, Bit = 6, Name = "左上料真空Bc", Model = "", Pin = "06" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U6", Addr = 407, Bit = 7, Name = "左上料真空Bd", Model = "", Pin = "07" },
            // ---- U12 · 32 位输入 · GL20-3200END（面板 / 缓存台 / 停止按钮） ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 8, Bit = 0, Name = "POWEROFF", Model = "ONPOWLAS1-AF-11R24VP（S17）", Pin = "A1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 9, Bit = 1, Name = "POWERON", Model = "ONPOWLAS1-AF-11W24VP（S18）", Pin = "A2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 10, Bit = 2, Name = "ENABLE", Model = "ONPOWLAS1-AF-11G24VP（S19）", Pin = "A3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 11, Bit = 3, Name = "RESET", Model = "ONPOWLAS1-AF-11R24VP（S20）", Pin = "A4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 12, Bit = 4, Name = "PUASUE", Model = "ONPOWLAS1-AF-11ZY24VP（S21）", Pin = "A5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 13, Bit = 5, Name = "HOME", Model = "ONPOWLAS1-AF-11G24VP（S22）", Pin = "A6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 14, Bit = 6, Name = "START", Model = "ONPOWLAS1-AF-11G24VP（S23）", Pin = "A7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 15, Bit = 7, Name = "ONECYCLE", Model = "ONPOWLAS1-AF-11W24VP（S24）", Pin = "A8" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 16, Bit = 8, Name = "RETRY", Model = "ONPOWLAS1-AF-11W24VP（S25）", Pin = "B1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 17, Bit = 9, Name = "SKIP", Model = "ONPOWLAS1-AF-11W24VP（S26）", Pin = "B2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 18, Bit = 10, Name = "CLEANOUT", Model = "ONPOWLAS1-AF-11W24VP（S27）", Pin = "B3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 19, Bit = 11, Name = "TRAYFEED", Model = "ONPOWLAS1-AF-11W24VP（S28）", Pin = "B4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 20, Bit = 12, Name = "TRAYEND", Model = "ONPOWLAS1-AF-11W24VP（S29）", Pin = "B5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 21, Bit = 13, Name = "ALARMRESET", Model = "ONPOWLAS1-AF-11W24VP（S30）", Pin = "B6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 22, Bit = 14, Name = "SAFELOCK", Model = "ONPOWLAS1-AF-11W24VP（S31）", Pin = "B7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 23, Bit = 15, Name = "STEP", Model = "ONPOWLAS1-AF-11W24VP（S32）", Pin = "B8" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 24, Bit = 16, Name = "T.START", Model = "ONPOWLAS1-AF-11W24VP（S33）", Pin = "C1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 25, Bit = 17, Name = "缓存台1有无感应", Model = "松下EX-14A", Pin = "C2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 26, Bit = 18, Name = "缓存台2有无感应", Model = "松下EX-14A", Pin = "C3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 27, Bit = 19, Name = "缓存台3有无感应", Model = "松下EX-14A", Pin = "C4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 28, Bit = 20, Name = "上料区停止按钮", Model = "ONPOWLAS1-AF-11W24VP（S33）", Pin = "C5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 29, Bit = 21, Name = "中继区停止按钮", Model = "ONPOWLAS1-AF-11W24VP（S34）", Pin = "C6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 30, Bit = 22, Name = "下料区停止按钮", Model = "ONPOWLAS1-AF-11W24VP（S35）", Pin = "C7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 31, Bit = 23, Name = "真空切换开关", Model = "施耐德XB2BD25C（S36）", Pin = "C8" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 32, Bit = 24, Name = "进气负压表", Model = "SMCZSE20-N-M5-L", Pin = "D1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 33, Bit = 25, Name = "缓存横移+", Model = "SMCD-M9NL", Pin = "D2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 34, Bit = 26, Name = "缓存横移-", Model = "SMCD-M9NL", Pin = "D3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 35, Bit = 27, Name = "前后面板选择开关", Model = "ONPOWLAS1-AJ-11X21NN24V((1组常开1组常闭两位置无灯)(S37)", Pin = "D4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 36, Bit = 28, Name = "右前急停信号", Model = "", Pin = "D5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 37, Bit = 29, Name = "右后急停信号", Model = "", Pin = "D6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 38, Bit = 30, Name = "RightLoadTray", Model = "", Pin = "D7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U12", Addr = 39, Bit = 31, Name = "面板日光灯按钮", Model = "", Pin = "D8" },
            // ---- U13 · 32 位输入 · GL20-3200END（离子棒 / 门板 / 后压头真空） ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 40, Bit = 0, Name = "离子棒2维护信号", Model = "", Pin = "A1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 41, Bit = 1, Name = "离子棒2异常信号", Model = "", Pin = "A2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 42, Bit = 2, Name = "离子棒3维护信号", Model = "", Pin = "A3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 43, Bit = 3, Name = "离子棒3异常信号", Model = "", Pin = "A4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 49, Bit = 9, Name = "门板3+", Model = "接近开关", Pin = "B2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 50, Bit = 10, Name = "门板4+", Model = "接近开关", Pin = "B3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 51, Bit = 11, Name = "中继小门3", Model = "", Pin = "B4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 52, Bit = 12, Name = "中继小门4", Model = "", Pin = "B5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 56, Bit = 16, Name = "后压头真空Ca", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "C1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 57, Bit = 17, Name = "后压头真空Cb", Model = "", Pin = "C2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 58, Bit = 18, Name = "后压头真空Cc", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 59, Bit = 19, Name = "后压头真空Cd", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 60, Bit = 20, Name = "后压头真空Da", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 61, Bit = 21, Name = "后压头真空Db", Model = "", Pin = "C6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 62, Bit = 22, Name = "后压头真空Dc", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U13", Addr = 63, Bit = 23, Name = "后压头真空Dd", Model = "", Pin = "C8" },
            // ---- U18 · 32 位输入 · 远端IO-下料侧内部大板缓存区 ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 112, Bit = 8, Name = "下料芯片旋转感应Ae", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 113, Bit = 9, Name = "下料芯片旋转感应Af", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 114, Bit = 10, Name = "下料芯片旋转感应Ag", Model = "松下EX-13EB", Pin = "10" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 115, Bit = 11, Name = "下料芯片旋转感应Ah", Model = "松下EX-13EB", Pin = "11" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 116, Bit = 12, Name = "下料芯片旋转感应Be", Model = "松下EX-13EB", Pin = "12" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 117, Bit = 13, Name = "下料芯片旋转感应Bf", Model = "松下EX-13EB", Pin = "13" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 118, Bit = 14, Name = "下料芯片旋转感应Bg", Model = "松下EX-13EB", Pin = "14" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U18", Addr = 119, Bit = 15, Name = "下料芯片旋转感应Bh", Model = "松下EX-13EB", Pin = "15" },
            // ---- U17 · 32 位输入 · 远端IO-下料侧内部大板料仓区 ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 136, Bit = 0, Name = "Auto1仓侧推1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 137, Bit = 1, Name = "Auto1仓侧推1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 138, Bit = 2, Name = "Auto1仓侧推2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 139, Bit = 3, Name = "Auto1仓侧推2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 140, Bit = 4, Name = "Auto1仓竖向定位+", Model = "SMCD-M9NL", Pin = "04" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 141, Bit = 5, Name = "Auto1仓竖向定位-", Model = "SMCD-M9NL", Pin = "05" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 142, Bit = 6, Name = "Auto2仓侧推1+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 143, Bit = 7, Name = "Auto2仓侧推1-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 144, Bit = 8, Name = "Auto2仓侧推2+", Model = "SMCD-F8NL", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 145, Bit = 9, Name = "Auto2仓侧推2-", Model = "SMCD-F8NL", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 146, Bit = 10, Name = "Auto2仓竖向定位+", Model = "SMCD-M9NL", Pin = "10" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 147, Bit = 11, Name = "Auto2仓竖向定位-", Model = "SMCD-M9NL", Pin = "11" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 148, Bit = 12, Name = "Auto3仓侧推1+", Model = "SMCD-F8NL", Pin = "12" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 149, Bit = 13, Name = "Auto3仓侧推1-", Model = "SMCD-F8NL", Pin = "13" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 150, Bit = 14, Name = "Auto3仓侧推2+", Model = "SMCD-F8NL", Pin = "14" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 151, Bit = 15, Name = "Auto3仓侧推2-", Model = "SMCD-F8NL", Pin = "15" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 152, Bit = 16, Name = "Auto3仓竖向定位+", Model = "SMCD-M9NL", Pin = "16" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 153, Bit = 17, Name = "Auto3仓竖向定位-", Model = "SMCD-M9NL", Pin = "17" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 154, Bit = 18, Name = "门板气缸3+", Model = "SMCD-M9NL", Pin = "18" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 155, Bit = 19, Name = "门板气缸3-", Model = "SMCD-M9NL", Pin = "19" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 156, Bit = 20, Name = "门板气缸4+", Model = "SMCD-M9NL", Pin = "20" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 157, Bit = 21, Name = "门板气缸4-", Model = "SMCD-M9NL", Pin = "21" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 158, Bit = 22, Name = "Auto1仓料盘检测3", Model = "", Pin = "22" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 159, Bit = 23, Name = "Auto1仓料盘检测4", Model = "", Pin = "23" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 160, Bit = 24, Name = "Auto1仓料盘检测5", Model = "", Pin = "24" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 161, Bit = 25, Name = "Auto2仓料盘检测3", Model = "", Pin = "25" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 162, Bit = 26, Name = "Auto2仓料盘检测4", Model = "", Pin = "26" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 163, Bit = 27, Name = "Auto2仓料盘检测5", Model = "", Pin = "27" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 164, Bit = 28, Name = "Auto3仓料盘检测3", Model = "", Pin = "28" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 165, Bit = 29, Name = "Auto3仓料盘检测4", Model = "", Pin = "29" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U17", Addr = 166, Bit = 30, Name = "Auto3仓料盘检测5", Model = "", Pin = "30" },
            // ---- U14 · 16 位输入 · 远端IO-Auto1仓外部 ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 168, Bit = 0, Name = "Auto1仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 169, Bit = 1, Name = "Auto1仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 170, Bit = 2, Name = "Auto1仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 171, Bit = 3, Name = "Auto1仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 172, Bit = 4, Name = "Auto1仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 173, Bit = 5, Name = "Auto1仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 174, Bit = 6, Name = "Auto1仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 175, Bit = 7, Name = "Auto1仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 176, Bit = 8, Name = "Auto1仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 177, Bit = 9, Name = "Auto1仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 178, Bit = 10, Name = "Auto1仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 179, Bit = 11, Name = "Auto1仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 180, Bit = 12, Name = "Auto1仓料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 181, Bit = 13, Name = "Auto1仓料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 182, Bit = 14, Name = "Auto1仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U14", Addr = 183, Bit = 15, Name = "Auto1仓开关门-", Model = "", Pin = "0F" },
            // ---- U15 · 16 位输入 · 远端IO-Auto2仓外部 ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 184, Bit = 0, Name = "Auto2仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 185, Bit = 1, Name = "Auto2仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 186, Bit = 2, Name = "Auto2仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 187, Bit = 3, Name = "Auto2仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 188, Bit = 4, Name = "Auto2仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 189, Bit = 5, Name = "Auto2仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 190, Bit = 6, Name = "Auto2仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 191, Bit = 7, Name = "Auto2仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 192, Bit = 8, Name = "Auto2仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 193, Bit = 9, Name = "Auto2仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 194, Bit = 10, Name = "Auto2仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 195, Bit = 11, Name = "Auto2仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 196, Bit = 12, Name = "Auto2仓料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 197, Bit = 13, Name = "Auto2仓料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 198, Bit = 14, Name = "Auto2仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U15", Addr = 199, Bit = 15, Name = "Auto2仓开关门-", Model = "", Pin = "0F" },
            // ---- U16 · 16 位输入 · 远端IO-Auto3仓外部 ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 200, Bit = 0, Name = "Auto3仓气缸1+", Model = "SMCD-F8NL", Pin = "00" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 201, Bit = 1, Name = "Auto3仓气缸1-", Model = "SMCD-F8NL", Pin = "01" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 202, Bit = 2, Name = "Auto3仓气缸2+", Model = "SMCD-F8NL", Pin = "02" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 203, Bit = 3, Name = "Auto3仓气缸2-", Model = "SMCD-F8NL", Pin = "03" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 204, Bit = 4, Name = "Auto3仓气缸3+", Model = "SMCD-F8NL", Pin = "04" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 205, Bit = 5, Name = "Auto3仓气缸3-", Model = "SMCD-F8NL", Pin = "05" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 206, Bit = 6, Name = "Auto3仓气缸4+", Model = "SMCD-F8NL", Pin = "06" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 207, Bit = 7, Name = "Auto3仓气缸4-", Model = "SMCD-F8NL", Pin = "07" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 208, Bit = 8, Name = "Auto3仓料盘有无感应", Model = "松下EX-13EB", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 209, Bit = 9, Name = "Auto3仓料盘检测1", Model = "松下EX-13EB", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 210, Bit = 10, Name = "Auto3仓料盘检测2", Model = "松下EX-13EB", Pin = "0A" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 211, Bit = 11, Name = "Auto3仓满料感应", Model = "松下EX-13EB", Pin = "0B" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 212, Bit = 12, Name = "Auto3仓料盘感应1", Model = "", Pin = "0C" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 213, Bit = 13, Name = "Auto3仓料盘感应2", Model = "", Pin = "0D" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 214, Bit = 14, Name = "Auto3仓开关门+", Model = "", Pin = "0E" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U16", Addr = 215, Bit = 15, Name = "Auto3仓开关门-", Model = "", Pin = "0F" },
            // ---- U19 · 8 位输入 · 远端IO-上料Y轴处（右下料真空） ----
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 392, Bit = 0, Name = "右下料真空Ae", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "C1" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 393, Bit = 1, Name = "右下料真空Af", Model = "", Pin = "C2" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 394, Bit = 2, Name = "右下料真空Ag", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 395, Bit = 3, Name = "右下料真空Ah", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 396, Bit = 4, Name = "右下料真空Be", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 397, Bit = 5, Name = "右下料真空Bf", Model = "", Pin = "C6" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 398, Bit = 6, Name = "右下料真空Bg", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "下料", IsOutput = false, Block = "U19", Addr = 399, Bit = 7, Name = "右下料真空Bh", Model = "", Pin = "C8" },
        };

        /// <summary>附表里的数字输出（上料 69 + 下料 76 = 145 点）。</summary>
        public static readonly HandoverGl20[] Gl20Outputs =
        {

            // ---- U7 · 32 位输出 · GL20-0032ETN（面板灯 / 料盘搬运 / 真空） ----
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 200, Bit = 0, Name = "POWEROFF", Model = "ONPOWLAS1-AF-11R24VP（S1）", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 201, Bit = 1, Name = "POWERON", Model = "ONPOWLAS1-AF-11W24VP（S2）", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 202, Bit = 2, Name = "ENABLE", Model = "ONPOWLAS1-AF-11G24VP（S3）", Pin = "A3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 203, Bit = 3, Name = "RESET", Model = "ONPOWLAS1-AF-11R24VP（S4）", Pin = "A4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 204, Bit = 4, Name = "PUASUE", Model = "ONPOWLAS1-AF-11ZY24VP（S5）", Pin = "A5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 205, Bit = 5, Name = "HOME", Model = "ONPOWLAS1-AF-11G24VP（S6）", Pin = "A6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 206, Bit = 6, Name = "START", Model = "ONPOWLAS1-AF-11G24VP（S7）", Pin = "A7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 207, Bit = 7, Name = "ONECYCLE", Model = "ONPOWLAS1-AF-11W24VP（S8）", Pin = "A8" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 208, Bit = 8, Name = "RETRY", Model = "ONPOWLAS1-AF-11W24VP（S9）", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 209, Bit = 9, Name = "SKIP", Model = "ONPOWLAS1-AF-11W24VP（S10）", Pin = "B2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 210, Bit = 10, Name = "CLEANOUT", Model = "ONPOWLAS1-AF-11W24VP（S11）", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 211, Bit = 11, Name = "TRAYFEED", Model = "ONPOWLAS1-AF-11W24VP（S12）", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 212, Bit = 12, Name = "TRAYEND", Model = "ONPOWLAS1-AF-11W24VP（S13）", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 213, Bit = 13, Name = "ALARMRESET", Model = "ONPOWLAS1-AF-11W24VP（S14）", Pin = "B6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 217, Bit = 17, Name = "REDLED", Model = "欧恩ONN-M4-3-A-B-F-T-RYG-50可折式", Pin = "C2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 218, Bit = 18, Name = "YELLOWLED", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 219, Bit = 19, Name = "GREENLED", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 220, Bit = 20, Name = "Buzzer", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 221, Bit = 21, Name = "料盘搬运夹爪松", Model = "SMCSY3320-5LZD-M5(三位五通中封)", Pin = "C6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 222, Bit = 22, Name = "料盘搬运夹爪夹", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 223, Bit = 23, Name = "料盘搬运上下上", Model = "SMCSY3320-5LZD-M5(三位五通中封)", Pin = "C8" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 224, Bit = 24, Name = "料盘搬运上下下", Model = "", Pin = "D1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 225, Bit = 25, Name = "进料仓敲击", Model = "SMCSY3120-5LZD-M5(二位五通单电控)", Pin = "D2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 226, Bit = 26, Name = "真空内外切换阀", Model = "SMCVT325", Pin = "D3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 227, Bit = 27, Name = "真空泵供电", Model = "OMRONMY2N-GS24VDC", Pin = "D4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 228, Bit = 28, Name = "进料仓Z刹车", Model = "多贺MRB208", Pin = "D5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 229, Bit = 29, Name = "出空盘仓Z刹车", Model = "", Pin = "D6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 230, Bit = 30, Name = "进空盘仓Z刹车", Model = "", Pin = "D7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U7", Addr = 231, Bit = 31, Name = "照明灯", Model = "", Pin = "D8" },
            // ---- U8 · 32 位输出 · GL20-0032ETN（压头吸真空 / 破真空） ----
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 232, Bit = 0, Name = "前压头吸真空Aa", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 233, Bit = 1, Name = "前压头吸真空Ab", Model = "", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 234, Bit = 2, Name = "前压头吸真空Ac", Model = "", Pin = "A3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 235, Bit = 3, Name = "前压头吸真空Ad", Model = "", Pin = "A4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 236, Bit = 4, Name = "前压头吸真空Ba", Model = "", Pin = "A5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 237, Bit = 5, Name = "前压头吸真空Bb", Model = "", Pin = "A6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 238, Bit = 6, Name = "前压头吸真空Bc", Model = "", Pin = "A7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 239, Bit = 7, Name = "前压头吸真空Bd", Model = "", Pin = "A8" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 240, Bit = 8, Name = "前压头破真空Aa", Model = "", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 241, Bit = 9, Name = "前压头破真空Ab", Model = "", Pin = "B2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 242, Bit = 10, Name = "前压头破真空Ac", Model = "", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 243, Bit = 11, Name = "前压头破真空Ad", Model = "", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 244, Bit = 12, Name = "前压头破真空Ba", Model = "", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 245, Bit = 13, Name = "前压头破真空Bb", Model = "", Pin = "B6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 246, Bit = 14, Name = "前压头破真空Bc", Model = "", Pin = "B7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 247, Bit = 15, Name = "前压头破真空Bd", Model = "", Pin = "B8" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 248, Bit = 16, Name = "加热控制1", Model = "", Pin = "C1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 249, Bit = 17, Name = "加热控制2", Model = "", Pin = "C2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 250, Bit = 18, Name = "加热控制3", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 251, Bit = 19, Name = "加热控制4", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 252, Bit = 20, Name = "加热控制5", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 253, Bit = 21, Name = "加热控制6", Model = "", Pin = "C6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 254, Bit = 22, Name = "加热控制7", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U8", Addr = 256, Bit = 24, Name = "左离子棒", Model = "", Pin = "D1" },
            // ---- U9 · 16 位输出 · 上料Y轴处（左上料吸/破真空） ----
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 280, Bit = 0, Name = "左上料吸真空Aa", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 281, Bit = 1, Name = "左上料破真空Aa", Model = "", Pin = "A2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 282, Bit = 2, Name = "左上料吸真空Ab", Model = "", Pin = "A3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 283, Bit = 3, Name = "左上料破真空Ab", Model = "", Pin = "A4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 284, Bit = 4, Name = "左上料吸真空Ac", Model = "", Pin = "A5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 285, Bit = 5, Name = "左上料破真空Ac", Model = "", Pin = "A6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 286, Bit = 6, Name = "左上料吸真空Ad", Model = "", Pin = "A7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 287, Bit = 7, Name = "左上料破真空Ad", Model = "", Pin = "A8" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 288, Bit = 8, Name = "左上料吸真空Ba", Model = "", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 289, Bit = 9, Name = "左上料破真空Ba", Model = "", Pin = "B2" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 290, Bit = 10, Name = "左上料吸真空Bb", Model = "", Pin = "B3" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 291, Bit = 11, Name = "左上料破真空Bb", Model = "", Pin = "B4" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 292, Bit = 12, Name = "左上料吸真空Bc", Model = "", Pin = "B5" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 293, Bit = 13, Name = "左上料破真空Bc", Model = "", Pin = "B6" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 294, Bit = 14, Name = "左上料吸真空Bd", Model = "", Pin = "B7" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U9", Addr = 295, Bit = 15, Name = "左上料破真空Bd", Model = "", Pin = "B8" },
            // ---- U20 · 32 位输出（面板灯 / 敲击 / 料仓Z刹车） ----
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 104, Bit = 0, Name = "POWEROFF", Model = "ONPOWLAS1-AF-11R24VP（S17）", Pin = "A1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 105, Bit = 1, Name = "POWERON", Model = "ONPOWLAS1-AF-11W24VP（S18）", Pin = "A2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 106, Bit = 2, Name = "ENABLE", Model = "ONPOWLAS1-AF-11G24VP（S19）", Pin = "A3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 107, Bit = 3, Name = "RESET", Model = "ONPOWLAS1-AF-11R24VP（S20）", Pin = "A4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 108, Bit = 4, Name = "PUASUE", Model = "ONPOWLAS1-AF-11ZY24VP（S21）", Pin = "A5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 109, Bit = 5, Name = "HOME", Model = "ONPOWLAS1-AF-11G24VP（S22）", Pin = "A6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 110, Bit = 6, Name = "START", Model = "ONPOWLAS1-AF-11G24VP（S23）", Pin = "A7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 111, Bit = 7, Name = "ONECYCLE", Model = "ONPOWLAS1-AF-11W24VP（S24）", Pin = "A8" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 112, Bit = 8, Name = "RETRY", Model = "ONPOWLAS1-AF-11W24VP（S25）", Pin = "B1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 113, Bit = 9, Name = "SKIP", Model = "ONPOWLAS1-AF-11W24VP（S26）", Pin = "B2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 114, Bit = 10, Name = "CLEANOUT", Model = "ONPOWLAS1-AF-11W24VP（S27）", Pin = "B3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 115, Bit = 11, Name = "TRAYFEED", Model = "ONPOWLAS1-AF-11W24VP（S28）", Pin = "B4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 116, Bit = 12, Name = "TRAYEND", Model = "ONPOWLAS1-AF-11W24VP（S29）", Pin = "B5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 117, Bit = 13, Name = "ALARMRESET", Model = "ONPOWLAS1-AF-11W24VP（S30）", Pin = "B6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 118, Bit = 14, Name = "SAFELOCK", Model = "ONPOWLAS1-AF-11W24VP（S31）", Pin = "B7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 119, Bit = 15, Name = "STEP", Model = "ONPOWLAS1-AF-11W24VP（S32）", Pin = "B8" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 120, Bit = 16, Name = "T.START", Model = "ONPOWLAS1-AF-11W24VP（S33）", Pin = "C1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 121, Bit = 17, Name = "缓存敲击", Model = "SMCSY3120-5LZD-M5(二位五通单电控)", Pin = "C2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 122, Bit = 18, Name = "Auto1料仓敲击", Model = "SMCSY3120-5LZD-M5(二位五通单电控)", Pin = "C3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 123, Bit = 19, Name = "Auto2料仓敲击", Model = "SMCSY3120-5LZD-M5(二位五通单电控)", Pin = "C4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 124, Bit = 20, Name = "Auto3料仓敲击", Model = "SMCSY3120-5LZD-M5(二位五通单电控)", Pin = "C5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 126, Bit = 22, Name = "右离子棒", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 128, Bit = 24, Name = "Auto1料仓Z轴刹车", Model = "", Pin = "D1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 129, Bit = 25, Name = "Auto2料仓Z轴刹车", Model = "", Pin = "D2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 130, Bit = 26, Name = "Auto3料仓Z轴刹车", Model = "", Pin = "D3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 131, Bit = 27, Name = "前压头Z刹车", Model = "", Pin = "D4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 132, Bit = 28, Name = "后压头Z刹车", Model = "", Pin = "D5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U20", Addr = 133, Bit = 29, Name = "照明灯", Model = "", Pin = "D6" },
            // ---- U21 · 32 位输出（后压头吸真空 / 破真空 / 加热指示灯） ----
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 136, Bit = 0, Name = "后压头吸真空Ca", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "A1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 137, Bit = 1, Name = "后压头吸真空Cb", Model = "", Pin = "A2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 138, Bit = 2, Name = "后压头吸真空Cc", Model = "", Pin = "A3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 139, Bit = 3, Name = "后压头吸真空Cd", Model = "", Pin = "A4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 140, Bit = 4, Name = "后压头吸真空Da", Model = "", Pin = "A5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 141, Bit = 5, Name = "后压头吸真空Db", Model = "", Pin = "A6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 142, Bit = 6, Name = "后压头吸真空Dc", Model = "", Pin = "A7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 143, Bit = 7, Name = "后压头吸真空Dd", Model = "", Pin = "A8" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 144, Bit = 8, Name = "后压头破真空Ca", Model = "", Pin = "B1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 145, Bit = 9, Name = "后压头破真空Cb", Model = "", Pin = "B2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 146, Bit = 10, Name = "后压头破真空Cc", Model = "", Pin = "B3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 147, Bit = 11, Name = "后压头破真空Cd", Model = "", Pin = "B4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 148, Bit = 12, Name = "后压头破真空Da", Model = "", Pin = "B5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 149, Bit = 13, Name = "后压头破真空Db", Model = "", Pin = "B6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 150, Bit = 14, Name = "后压头破真空Dc", Model = "", Pin = "B7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 151, Bit = 15, Name = "后压头破真空Dd", Model = "", Pin = "B8" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 152, Bit = 16, Name = "压头吸嘴加热指示灯Aa", Model = "暂未选型", Pin = "C1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 153, Bit = 17, Name = "压头吸嘴加热指示灯Ab", Model = "", Pin = "C2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 154, Bit = 18, Name = "压头吸嘴加热指示灯Ac", Model = "", Pin = "C3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 155, Bit = 19, Name = "压头吸嘴加热指示灯Ad", Model = "", Pin = "C4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 156, Bit = 20, Name = "压头吸嘴加热指示灯Ba", Model = "", Pin = "C5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 157, Bit = 21, Name = "压头吸嘴加热指示灯Bb", Model = "", Pin = "C6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 158, Bit = 22, Name = "压头吸嘴加热指示灯Bc", Model = "", Pin = "C7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 159, Bit = 23, Name = "压头吸嘴加热指示灯Bd", Model = "", Pin = "C8" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 160, Bit = 24, Name = "压头吸嘴加热指示灯Ca", Model = "", Pin = "D1" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 161, Bit = 25, Name = "压头吸嘴加热指示灯Cb", Model = "", Pin = "D2" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 162, Bit = 26, Name = "压头吸嘴加热指示灯Cc", Model = "", Pin = "D3" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 163, Bit = 27, Name = "压头吸嘴加热指示灯Cd", Model = "", Pin = "D4" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 164, Bit = 28, Name = "压头吸嘴加热指示灯Da", Model = "", Pin = "D5" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 165, Bit = 29, Name = "压头吸嘴加热指示灯Db", Model = "", Pin = "D6" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 166, Bit = 30, Name = "压头吸嘴加热指示灯Dc", Model = "", Pin = "D7" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U21", Addr = 167, Bit = 31, Name = "压头吸嘴加热指示灯Dd", Model = "", Pin = "D8" },
            // ---- U22 · 16 位输出 · 上料Y轴处（右下料吸/破真空） ----
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 264, Bit = 0, Name = "右下料吸真空Ae", Model = "SMCZZB08-S01M5_ZB0030-K15L-FAG-C4", Pin = "00" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 265, Bit = 1, Name = "右下料破真空Ae", Model = "", Pin = "01" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 266, Bit = 2, Name = "右下料吸真空Af", Model = "", Pin = "02" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 267, Bit = 3, Name = "右下料破真空Af", Model = "", Pin = "03" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 268, Bit = 4, Name = "右下料吸真空Ag", Model = "", Pin = "04" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 269, Bit = 5, Name = "右下料破真空Ag", Model = "", Pin = "05" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 270, Bit = 6, Name = "右下料吸真空Ah", Model = "", Pin = "06" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 271, Bit = 7, Name = "右下料破真空Ah", Model = "", Pin = "07" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 272, Bit = 8, Name = "右下料吸真空Be", Model = "", Pin = "08" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 273, Bit = 9, Name = "右下料破真空Be", Model = "", Pin = "09" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 274, Bit = 10, Name = "右下料吸真空Bf", Model = "", Pin = "10" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 275, Bit = 11, Name = "右下料破真空Bf", Model = "", Pin = "11" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 276, Bit = 12, Name = "右下料吸真空Bg", Model = "", Pin = "12" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 277, Bit = 13, Name = "右下料破真空Bg", Model = "", Pin = "13" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 278, Bit = 14, Name = "右下料吸真空Bh", Model = "", Pin = "14" },
            new HandoverGl20 { Area = "下料", IsOutput = true, Block = "U22", Addr = 279, Bit = 15, Name = "右下料破真空Bh", Model = "", Pin = "15" },
        };


        /// <summary>附表里的模拟量点：U10 模拟输入 2 路 / U11 模拟输出 2 路（前 / 后压头比例阀）。</summary>
        public static readonly HandoverGl20[] Gl20Analog =
        {
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U10", Addr = 0, Bit = 0, Name = "前压头比例阀输出", Model = "模拟输入 VI0+", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = false, Block = "U10", Addr = 1, Bit = 1, Name = "后压头比例阀输出", Model = "模拟输入 VI1+", Pin = "B1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U11", Addr = 0, Bit = 0, Name = "前压头比例阀输入", Model = "模拟输出 VI0+", Pin = "A1" },
            new HandoverGl20 { Area = "上料", IsOutput = true, Block = "U11", Addr = 1, Bit = 1, Name = "后压头比例阀输入", Model = "模拟输出 VI1+", Pin = "B1" },
        };

        /// <summary>附表「U30 版卡-高速输入」IN01~IN08（中转感应5 / 料梭产品检测）。</summary>
        public static readonly string[][] Gl20FastIn =
        {
            new[] { "左前中转感应5", "IN01" },
            new[] { "左后中转感应5", "IN02" },
            new[] { "右前中转感应5", "IN03" },
            new[] { "右后中转感应5", "IN04" },
            new[] { "前料梭产品检测1", "IN05" },
            new[] { "前料梭产品检测2", "IN06" },
            new[] { "后料梭产品检测1", "IN07" },
            new[] { "后料梭产品检测2", "IN08" }
        };

        /// <summary>附表「轴感应器」列出的 20 根带原点感应的轴（上料 10 + 下料 10）。</summary>
        public static readonly string[] Gl20OriginAxes =
        {
            "上料变距X", "上料变距Y", "上料吸嘴1Z轴", "上料吸嘴2Z轴", "上料吸嘴3Z轴", "上料吸嘴4Z轴", "上料吸嘴5Z轴", "上料吸嘴6Z轴", "上料吸嘴7Z轴", "上料吸嘴8Z轴", "下料变距X", "下料变距Y", "下料吸嘴1Z轴", "下料吸嘴2Z轴", "下料吸嘴3Z轴", "下料吸嘴4Z轴", "下料吸嘴5Z轴", "下料吸嘴6Z轴", "下料吸嘴7Z轴", "下料吸嘴8Z轴"
        };

        /// <summary>工作簿 Sheet2：EtherCAT 五个分支（X2~X6）挂什么。</summary>
        public static readonly string[][] BusBranches =
        {
            new[] { "X2", "阀岛总线" },
            new[] { "X3", "下料侧轴" },
            new[] { "X4", "上料侧 IO 模块" },
            new[] { "X5", "下料侧 IO 模块" },
            new[] { "X6", "上料侧轴" }
        };

        /// <summary>工作簿 Sheet1：传感器 / 阀岛 / 真空件的料号与型号清单（原表 22 行，按料号去重后 14 种）。</summary>
        public static readonly string[][] SpareParts =
        {
            new[] { "020701-1012844", "接近开关", "OMRON TL-Q5MC1-Z" },
            new[] { "020902-1018789", "离子风棒", "SMC IZTB42-46D26H-F" },
            new[] { "020701-1017853", "露点传感器", "VAISALA DMT-143（线长 3 米）" },
            new[] { "010308-0008494", "真空表", "SMC ZSE20-N-M5-L" },
            new[] { "020706-1013650", "手持扫码器", "基恩士 HR-X100" },
            new[] { "010302-0071619", "总线阀岛", "EMC ES1V-EC32-6D3C11S-8C4C611C4E4-UN1" },
            new[] { "020799-1018738", "静电传感器", "SMC IZD10-110" },
            new[] { "010306-0008274", "比例阀", "SMC ITV1030-311L" },
            new[] { "020701-1018813", "光电传感器支架", "KEYENCE OP-88542" },
            new[] { "020701-1005919", "磁性开关", "SMC D-M9NL" },
            new[] { "020701-1006276", "感应器", "松下 EX-13EB" },
            new[] { "020701-1006277", "传感器", "松下 EX-14A" },
            new[] { "020701-1005912", "磁性开关", "SMC D-F8NL" },
            new[] { "010308-0056875", "集装式真空发生器", "SMC ZZB08-S01M5_ZB0030-K15L-FAG-C4" }
        };

        public static readonly HandoverValve[] Valves =
        {
            new HandoverValve { Island = "阀岛2(20位)", Pos = 8, Name = "Auto1料仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 10, Name = "Auto2料仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 12, Name = "Auto3料仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 14, Name = "下飞拍相机吹气", Control = "双控", Area = "index 中继模块（压头模组）", Sensor = "" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 16, Name = "定拍相机相机", Control = "双控", Area = "index 中继模块（压头模组）", Sensor = "" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 18, Name = "横移气缸", Control = "双控", Area = "固定盘模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 20, Name = "门板气缸3", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 22, Name = "门板气缸4", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 24, Name = "后气浮头气缸", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 26, Name = "Auto1侧推气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 27, Name = "Auto1竖向定位气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 28, Name = "Auto1压爪气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 29, Name = "Auto2侧推气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 30, Name = "Auto2竖向定位气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 31, Name = "Auto2压爪气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 32, Name = "Auto3侧推气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 33, Name = "Auto3竖向定位气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛2(20位)", Pos = 34, Name = "Auto3压爪气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 40, Name = "进料仓门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 42, Name = "出空盘仓门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 44, Name = "进空盘仓门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 46, Name = "Auto1门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 48, Name = "Auto2门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 50, Name = "Auto3门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛3(7位)", Pos = 52, Name = "风阀门", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 72, Name = "进料仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 74, Name = "出空盘仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 76, Name = "进空盘仓分盘气缸", Control = "双控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 78, Name = "AB门气缸1", Control = "双控", Area = "index 中继模块（压头模组）", Sensor = "" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 80, Name = "AB门气缸2", Control = "双控", Area = "index 中继模块（压头模组）", Sensor = "" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 82, Name = "治具夹紧气缸", Control = "双控", Area = "治具夹紧模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 84, Name = "门板气缸1", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 86, Name = "门板气缸2", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 88, Name = "前气浮头气缸", Control = "双控(中封)", Area = "index 中继模块（压头模组）", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 90, Name = "进料仓侧推气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 91, Name = "进料仓竖向定位气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 92, Name = "进料仓压爪气缸", Control = "单控", Area = "料仓模组", Sensor = "有" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 93, Name = "左侧前门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 94, Name = "左侧后门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 95, Name = "左后侧门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 96, Name = "中后侧门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 97, Name = "右后侧门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 98, Name = "右侧后门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
            new HandoverValve { Island = "阀岛1(20位)", Pos = 99, Name = "右侧前门锁气缸", Control = "单控", Area = "机架", Sensor = "无" },
        };

        public static readonly HandoverSerial[] TempStations =
        {
            new HandoverSerial { Name = "温控器", Where = "前预温盘", Power = "430", Station = "1" },
            new HandoverSerial { Name = "温控器", Where = "后预温盘", Power = "430", Station = "2" },
            new HandoverSerial { Name = "温控器", Where = "前梭车", Power = "", Station = "3" },
            new HandoverSerial { Name = "温控器", Where = "后梭车", Power = "", Station = "4" },
            new HandoverSerial { Name = "温控器", Where = "中继", Power = "1030", Station = "5" },
            new HandoverSerial { Name = "温控器", Where = "Shuttle加热", Power = "400", Station = "6" },
            new HandoverSerial { Name = "温控器", Where = "Shuttle底部加热", Power = "630", Station = "7" },
            new HandoverSerial { Name = "流量传感器", Where = "Chiller", Power = "-", Station = "8" },
            new HandoverSerial { Name = "语音提示器", Where = "上机架下料显示屏旁边", Power = "", Station = "9" },
            new HandoverSerial { Name = "露点传感器1", Where = "", Power = "", Station = "10" },
            new HandoverSerial { Name = "露点传感器2", Where = "", Power = "", Station = "11" },
            new HandoverSerial { Name = "露点传感器3", Where = "", Power = "", Station = "12" },
        };

        public static readonly HandoverSerial[] PanelStations =
        {
            new HandoverSerial { Name = "显示面板-进料仓", Where = "进料仓", Power = "", Station = "1" },
            new HandoverSerial { Name = "显示面板-出空盘仓", Where = "出空盘仓", Power = "", Station = "2" },
            new HandoverSerial { Name = "显示面板-进空盘仓", Where = "进空盘仓", Power = "", Station = "3" },
            new HandoverSerial { Name = "显示面板-Auto1仓", Where = "Auto1仓", Power = "", Station = "4" },
            new HandoverSerial { Name = "显示面板-Auto2仓", Where = "Auto2仓", Power = "", Station = "5" },
            new HandoverSerial { Name = "显示面板-Auto3仓", Where = "Auto3仓", Power = "", Station = "6" },
            new HandoverSerial { Name = "显示面板-固定仓1", Where = "固定仓1", Power = "", Station = "7" },
            new HandoverSerial { Name = "显示面板-固定仓2", Where = "固定仓2", Power = "", Station = "8" },
            new HandoverSerial { Name = "显示面板-固定仓3", Where = "固定仓3", Power = "", Station = "9" },
        };

        /// <summary>32-handler 源码里的工站名。</summary>
        public static readonly string[] Stations = { "上料XY工站", "上料变距XY工站", "上料前排吸嘴工站", "上料后排吸嘴工站", "前梭车工站", "后梭车工站", "下料XY工站", "下料变距XY工站", "下料前排吸嘴工站", "下料后排吸嘴工站", "搬运料盘X", "前梭车X", "后梭车X", "Auto1-料盘仓", "Auto2-料盘仓", "Auto3-料盘仓", "进料盘仓", "进空盘仓", "出空盘仓" };
        /// <summary>料仓虚轴流水线名。</summary>
        public static readonly string[] Lines = { "进料仓流水线", "进空盘仓流水线", "出空盘仓流水线", "Auto1仓流水线", "Auto2仓流水线", "Auto3仓流水线" };
        /// <summary>飞拍通道：绑定的轴 / 相机 / 拍照方式 / 光源通道。</summary>
        public static readonly string[][] FlyShot =
        {
            new string[] { "上料X轴", "下相机", "飞拍", "LC1 CH1" },
            new string[] { "前梭车", "前梭车相机1 / 前梭车相机2", "飞拍", "LC1 CH2" },
            new string[] { "后梭车", "后梭车相机1 / 后梭车相机2", "飞拍", "LC1 CH3" },
            new string[] { "压头左", "压头左相机", "定拍", "LC2 CH1" },
            new string[] { "压头右", "压头右相机", "定拍", "LC2 CH2" },
        };

        static readonly Dictionary<string, HandoverAxis> _ax = MapAx();
        static readonly Dictionary<string, HandoverIo> _in = MapIo(Inputs);
        static readonly Dictionary<string, HandoverIo> _out = MapIo(Outputs);

        static Dictionary<string, HandoverAxis> MapAx()
        {
            var d = new Dictionary<string, HandoverAxis>();
            foreach (var a in Axes) d[a.Name] = a;
            return d;
        }

        static Dictionary<string, HandoverIo> MapIo(HandoverIo[] src)
        {
            var d = new Dictionary<string, HandoverIo>();
            foreach (var p in src) d[p.Name] = p;
            return d;
        }

        /// <summary>按名取资料轴，找不到返回 null。</summary>
        public static HandoverAxis Axis(string name)
        {
            HandoverAxis v; return _ax.TryGetValue(name, out v) ? v : null;
        }

        /// <summary>按名取资料输入点。</summary>
        public static HandoverIo Input(string name)
        {
            HandoverIo v; return _in.TryGetValue(name, out v) ? v : null;
        }

        /// <summary>按名取资料输出点。</summary>
        public static HandoverIo Output(string name)
        {
            HandoverIo v; return _out.TryGetValue(name, out v) ? v : null;
        }

        /// <summary>某工位下的资料轴。</summary>
        public static List<HandoverAxis> AxesOf(string station)
        {
            var r = new List<HandoverAxis>();
            foreach (var x in Axes) if (x.Station == station) r.Add(x);
            return r;
        }

        /// <summary>某工位下的资料输入点。</summary>
        public static List<HandoverIo> InputsOf(string station)
        {
            var r = new List<HandoverIo>();
            foreach (var x in Inputs) if (x.Station == station) r.Add(x);
            return r;
        }

        /// <summary>某工位下的资料输出点。</summary>
        public static List<HandoverIo> OutputsOf(string station)
        {
            var r = new List<HandoverIo>();
            foreach (var x in Outputs) if (x.Station == station) r.Add(x);
            return r;
        }

        /// <summary>资料划分出的全部工位名，按轴数从多到少。</summary>
        public static List<string> StationNames()
        {
            var c = new Dictionary<string, int>();
            foreach (var a in Axes) c[a.Station] = c.ContainsKey(a.Station) ? c[a.Station] + 1 : 1;
            var ks = new List<string>(c.Keys);
            ks.Sort((x, y) => c[y].CompareTo(c[x]));
            return ks;
        }
    }
}
