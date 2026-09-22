// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/MotionTypes.cs — 运动控制的数据契约
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：定义「轴配置 / 轴运行态 / IO 点 / 运动判定结果」等纯数据结构，
//        供雷赛真卡后端（LTDMC.dll）与内置仿真卡共同使用。
// ============================================================================
using System;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（MotionTypes）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_MotionTypes
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · MotionTypes。</summary>
        internal const string WenQiZhiFile = "MotionTypes";
    }

    /// <summary>
    /// 轴状态位（与雷赛 d1000_get_axis_status 的状态字按位对应，便于真卡/仿真共用）。
    /// </summary>
    [Flags]
    public enum AxisStatusBits
    {
        /// <summary>无状态。</summary>
        None = 0,
        /// <summary>伺服报警 ALM。</summary>
        Alarm = 1 << 0,
        /// <summary>到位信号 INP。</summary>
        InPosition = 1 << 1,
        /// <summary>正在运动。</summary>
        Busy = 1 << 2,
        /// <summary>已回零（仿真卡内部维护）。</summary>
        Homed = 1 << 3,
        /// <summary>原点信号 ORG。</summary>
        Org = 1 << 4,
        /// <summary>正向硬限位 +EL。</summary>
        PositiveLimit = 1 << 5,
        /// <summary>负向硬限位 -EL。</summary>
        NegativeLimit = 1 << 6,
        /// <summary>伺服使能。</summary>
        ServoOn = 1 << 7,
        /// <summary>超出软限位（仿真卡置位）。</summary>
        SoftLimit = 1 << 8,
    }

    /// <summary>停止方式（对应 d1000_stop 的 stop_mode）。</summary>
    public enum MotionStopMode
    {
        /// <summary>减速停止。</summary>
        Deceleration = 0,
        /// <summary>紧急停止（立即断使能）。</summary>
        Emergency = 1,
    }

    /// <summary>回零方式（对应 d1000_home_move 的 home_mode 常用取值）。</summary>
    public enum HomeMode
    {
        /// <summary>负向回零，原点在负方向。</summary>
        Negative = 0,
        /// <summary>正向回零，原点在正方向。</summary>
        Positive = 1,
        /// <summary>回到预先设定的回零位置（仿真卡默认）。</summary>
        PresetPosition = 2,
    }

    /// <summary>
    /// 一根轴的静态配置：软限位、速度、脉冲当量、区域归属、防撞参数。
    /// </summary>
    public sealed class AxisConfig
    {
        /// <summary>控制卡轴号（0 起）。</summary>
        public int Index { get; set; }

        /// <summary>逻辑轴名（与手册《诊断 / 电机状态》表一致）。</summary>
        public string Name { get; set; } = "";

        /// <summary>所属区域（测试区域 / 上料臂 / 下料臂 / 小车 / 传送臂）。</summary>
        public string Group { get; set; } = "";

        /// <summary>工程单位（mm / deg）。</summary>
        public string Unit { get; set; } = "mm";

        /// <summary>脉冲当量：1 个工程单位对应多少脉冲。</summary>
        public double PulseEquivalent { get; set; } = 1000;

        /// <summary>负向软限位。</summary>
        public double MinMm { get; set; }

        /// <summary>正向软限位。</summary>
        public double MaxMm { get; set; } = 100;

        /// <summary>回零位置。</summary>
        public double HomeMm { get; set; }

        /// <summary>最大速度（单位/秒）。</summary>
        public double MaxVel { get; set; } = 200;

        /// <summary>加速度（单位/秒²）。</summary>
        public double Acc { get; set; } = 2000;

        /// <summary>减速度（单位/秒²）。</summary>
        public double Dec { get; set; } = 2000;

        /// <summary>是否垂直轴（Z 轴）——防撞互锁的关键判据。</summary>
        public bool IsVertical { get; set; }

        /// <summary>垂直轴的「抬起」阈值：位置 ≤ 该值视为已抬起。</summary>
        public double ZUpMm { get; set; }

        /// <summary>共享导轨标识；同一导轨上的轴互为运动互斥。</summary>
        public string RailId { get; set; } = "";

        /// <summary>安全间隙（共享导轨上两根轴之间的最小允许间距）。</summary>
        public double ClearanceMm { get; set; } = 40;

        /// <summary>是否参与自动生产（仿真节拍用）。</summary>
        public bool AutoCycle { get; set; } = true;
    }

    /// <summary>轴的运行期状态（每 tick 更新）。</summary>
    public sealed class AxisState
    {
        /// <summary>当前工程位置。</summary>
        public double PositionMm;

        /// <summary>目标工程位置。</summary>
        public double TargetMm;

        /// <summary>当前速度（单位/秒）。</summary>
        public double Velocity;

        /// <summary>伺服使能。</summary>
        public bool ServoOn;

        /// <summary>已回零。</summary>
        public bool Homed;

        /// <summary>报警。</summary>
        public bool Alarm;

        /// <summary>运动中。</summary>
        public bool Busy;

        /// <summary>到位。</summary>
        public bool InPosition = true;

        /// <summary>原点信号。</summary>
        public bool Org;

        /// <summary>正向硬限位。</summary>
        public bool PositiveLimit;

        /// <summary>负向硬限位。</summary>
        public bool NegativeLimit;

        /// <summary>超出软限位。</summary>
        public bool SoftLimit;

        /// <summary>输出力矩（%）。</summary>
        public double Torque;

        /// <summary>跟随误差（指令位置 − 实际位置）。</summary>
        public double FollowingError;

        /// <summary>最近一次被安全策略拒绝的原因（空 = 未发生）。</summary>
        public string LastReject = "";

        /// <summary>组装为状态位。</summary>
        public AxisStatusBits Bits()
        {
            var b = AxisStatusBits.None;
            if (Alarm) b |= AxisStatusBits.Alarm;
            if (InPosition) b |= AxisStatusBits.InPosition;
            if (Busy) b |= AxisStatusBits.Busy;
            if (Homed) b |= AxisStatusBits.Homed;
            if (Org) b |= AxisStatusBits.Org;
            if (PositiveLimit) b |= AxisStatusBits.PositiveLimit;
            if (NegativeLimit) b |= AxisStatusBits.NegativeLimit;
            if (ServoOn) b |= AxisStatusBits.ServoOn;
            if (SoftLimit) b |= AxisStatusBits.SoftLimit;
            return b;
        }
    }

    /// <summary>一个具名 IO 点。</summary>
    public sealed class IoPoint
    {
        /// <summary>位号（0 起）。</summary>
        public int Bit { get; set; }

        /// <summary>点名（与手册一致）。</summary>
        public string Name { get; set; } = "";

        /// <summary>所属区域。</summary>
        public string Group { get; set; } = "";

        /// <summary>true = 输出，false = 输入。</summary>
        public bool IsOutput { get; set; }

        /// <summary>当前值。</summary>
        public bool Value { get; set; }

        /// <summary>是否可手动操作（输出点允许在仿真页点击翻转）。</summary>
        public bool Manual { get; set; } = true;

        /// <summary>输出点联动的输入位（仿真：输出 ON 后反馈输入随之 ON）。</summary>
        public int FeedbackInputBit { get; set; } = -1;

        /// <summary>输出点联动输入的显示名（用于说明）。</summary>
        public string FeedbackInputName { get; set; } = "";

        /// <summary>低电平有效（如急停 / 报警输入常闭）。</summary>
        public bool ActiveLow { get; set; }
    }

    /// <summary>一次运动请求的判定结果（含被拒绝的原因）。</summary>
    public sealed class MotionResult
    {
        /// <summary>是否被接受。</summary>
        public bool Accepted { get; set; }

        /// <summary>拒绝原因（Accepted=true 时为空）。</summary>
        public string Reason { get; set; } = "";

        /// <summary>构造「接受」。</summary>
        public static MotionResult Ok() => new MotionResult { Accepted = true };

        /// <summary>构造「拒绝」。</summary>
        public static MotionResult Reject(string why)
            => new MotionResult { Accepted = false, Reason = why ?? "被安全策略拒绝" };
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
