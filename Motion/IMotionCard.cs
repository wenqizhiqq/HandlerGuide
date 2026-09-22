// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/IMotionCard.cs — 运动控制卡统一接口
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：屏蔽「雷赛真卡（LTDMC.dll）」与「内置仿真卡」的差异，
//        上层（MotionManager / 界面）只面向本接口编程。
//        位置一律使用工程单位（mm / deg），脉冲换算在各自后端内部完成。
// ============================================================================
namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（IMotionCard）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_IMotionCard
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · IMotionCard。</summary>
        internal const string WenQiZhiFile = "IMotionCard";
    }

    /// <summary>运动控制卡（雷赛真卡或内置仿真卡）。</summary>
    public interface IMotionCard
    {
        /// <summary>卡型名称，如 "DMC2410" / "内置仿真卡"。</summary>
        string CardName { get; }

        /// <summary>true = 真实硬件；false = 仿真。</summary>
        bool IsReal { get; }

        /// <summary>是否已连接。</summary>
        bool IsConnected { get; }

        /// <summary>轴数。</summary>
        int AxisCount { get; }

        /// <summary>输入点数。</summary>
        int InputCount { get; }

        /// <summary>输出点数。</summary>
        int OutputCount { get; }

        /// <summary>一句话状态描述（显示在仿真页头部）。</summary>
        string Info { get; }

        /// <summary>打开板卡。</summary>
        bool Open();

        /// <summary>关闭板卡。</summary>
        void Close();

        /// <summary>下发一根轴的静态配置（软限位 / 速度曲线 / 脉冲当量）。</summary>
        void Configure(AxisConfig cfg);

        /// <summary>伺服使能 / 断使能。</summary>
        void SetServo(int axis, bool on);

        /// <summary>设置软限位（工程单位）。</summary>
        void SetSoftLimit(int axis, double min, double max);

        /// <summary>设置速度曲线（工程单位）。</summary>
        void SetProfile(int axis, double maxVel, double acc, double dec);

        /// <summary>绝对定位。</summary>
        MotionResult MoveAbs(int axis, double mm);

        /// <summary>相对定位。</summary>
        MotionResult MoveRel(int axis, double mm);

        /// <summary>停止单轴。</summary>
        void Stop(int axis, MotionStopMode mode);

        /// <summary>停止全部轴。</summary>
        void StopAll(MotionStopMode mode);

        /// <summary>回零。</summary>
        MotionResult Home(int axis);

        /// <summary>读取当前位置（工程单位）。</summary>
        double GetPosition(int axis);

        /// <summary>直接设定当前位置（工程单位，不产生运动）。</summary>
        void SetPosition(int axis, double mm);

        /// <summary>读取单轴状态。</summary>
        AxisState GetState(int axis);

        /// <summary>读取全部轴状态快照。</summary>
        AxisState[] Snapshot();

        /// <summary>写输出位。</summary>
        void WriteOut(int bit, bool on);

        /// <summary>读输入位。</summary>
        bool ReadIn(int bit);

        /// <summary>读输出位。</summary>
        bool ReadOut(int bit);

        /// <summary>
        /// 仿真用：直接置位某个输入点（真卡为空实现，输入来自硬件）。
        /// </summary>
        void SetInput(int bit, bool on);

        /// <summary>读全部输入快照。</summary>
        bool[] SnapshotInputs();

        /// <summary>读全部输出快照。</summary>
        bool[] SnapshotOutputs();

        /// <summary>推进仿真一个时间步（真卡为空实现）。</summary>
        void Step(double dt);

        /// <summary>清除报警。</summary>
        void ClearAlarm(int axis);
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
