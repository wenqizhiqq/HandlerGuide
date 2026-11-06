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
