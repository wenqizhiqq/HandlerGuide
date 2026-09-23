// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/SimMotionCard.cs — 内置运动控制卡仿真器
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：在没有雷赛硬件（LTDMC.dll / DMC 卡）的机器上，完整模拟
//        「31 轴梯形速度曲线运动 + 软限位钳制 + 伺服/回零/报警 + 64 位 IO」。
//        物理积分每 tick 推进一次，位置用工程单位（mm）表示。
//  安全：本类只负责「单轴物理 + 软限位」，多轴互锁/防撞由 MotionManager 统一裁决，
//        避免把拓扑知识散落到驱动层。
// ============================================================================
using System;
using System.Collections.Generic;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（SimMotionCard）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_SimMotionCard
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · SimMotionCard。</summary>
        internal const string WenQiZhiFile = "SimMotionCard";
    }

    /// <summary>内置仿真运动卡：无硬件依赖，行为与雷赛 DMC 卡保持一致的外部语义。</summary>
    public sealed class SimMotionCard : IMotionCard
    {
        /// <summary>IO 点数（与 DMC2410 的 24 入 / 16 出取大者对齐为 64）。</summary>
        private const int IoBits = 64;

        /// <summary>到位判定阈值（工程单位）。</summary>
        private const double InPosTol = 0.002;

        private readonly Dictionary<int, AxisConfig> _cfg = new Dictionary<int, AxisConfig>();
        private readonly Dictionary<int, AxisState> _st = new Dictionary<int, AxisState>();
        private readonly bool[] _in = new bool[IoBits];
        private readonly bool[] _out = new bool[IoBits];
        private readonly Random _rnd = new Random(20240921);

        private int _axisCount;
        private bool _open;

        /// <summary>构造。</summary>
        public SimMotionCard(int axisCount = 32)
        {
            _axisCount = axisCount;
            for (int i = 0; i < _axisCount; i++)
            {
                _cfg[i] = new AxisConfig { Index = i, Name = "Axis" + i };
                _st[i] = new AxisState();
            }
            // 上电默认：急停未按下（输入 0 常闭 → 仿真里用 false 表示「未触发」）
            _in[0] = false;
            _in[1] = true;   // 安全门关闭
            _in[2] = true;   // 气压正常
            _in[3] = true;   // 主电源正常
        }

        /// <inheritdoc />
        public string CardName => "内置仿真卡（雷赛 DMC-E 兼容）";

        /// <inheritdoc />
        public bool IsReal => false;

        /// <inheritdoc />
        public bool IsConnected => _open;

        /// <inheritdoc />
        public int AxisCount => _axisCount;

        /// <inheritdoc />
        public int InputCount => 32;

        /// <inheritdoc />
        public int OutputCount => 32;

        /// <inheritdoc />
        public string Info => "仿真模式：DMC-E 语义 · 梯形速度曲线 + 软限位钳制 + IO 反馈联动";

        /// <inheritdoc />
        public bool Open()
        {
            _open = true;
            return true;
        }

        /// <inheritdoc />
        public void Close()
        {
            StopAll(MotionStopMode.Deceleration);
            _open = false;
        }

        /// <inheritdoc />
        public void Configure(AxisConfig cfg)
        {
            if (cfg == null) return;
            _cfg[cfg.Index] = cfg;
            if (!_st.TryGetValue(cfg.Index, out var st))
            {
                st = new AxisState();
                _st[cfg.Index] = st;
            }
            st.PositionMm = Clamp(cfg, st.PositionMm);
            st.TargetMm = st.PositionMm;
            st.ServoOn = true;
            st.InPosition = true;
            st.Homed = true;
        }

        /// <inheritdoc />
        public void SetServo(int axis, bool on)
        {
            var st = St(axis);
            st.ServoOn = on;
            if (!on) { st.Busy = false; st.Velocity = 0; }
        }

        /// <inheritdoc />
        public void SetSoftLimit(int axis, double min, double max)
        {
            var c = Cfg(axis);
            c.MinMm = min;
            c.MaxMm = max;
            var st = St(axis);
            st.PositionMm = Clamp(c, st.PositionMm);
            st.TargetMm = Clamp(c, st.TargetMm);
        }

        /// <inheritdoc />
        public void SetProfile(int axis, double maxVel, double acc, double dec)
        {
            var c = Cfg(axis);
            if (maxVel > 0) c.MaxVel = maxVel;
            if (acc > 0) c.Acc = acc;
            if (dec > 0) c.Dec = dec;
        }

        /// <inheritdoc />
        public MotionResult MoveAbs(int axis, double mm)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            var c = Cfg(axis);
            var st = St(axis);
            if (st.Alarm) return MotionResult.Reject(c.Name + " 处于报警状态，请先清除报警");
            if (!st.ServoOn) return MotionResult.Reject(c.Name + " 伺服未使能");

            // 软限位钳制：把目标夹进允许区间，并记录被钳制的事实（绝不越界 → 不撞机）
            double clamped = Clamp(c, mm);
            if (Math.Abs(clamped - mm) > 1e-9)
            {
                st.SoftLimit = true;
                st.LastReject = "目标 " + mm.ToString("0.###") + " 超出软限位 ["
                                + c.MinMm.ToString("0.###") + ", " + c.MaxMm.ToString("0.###") + "]，已钳制到 "
                                + clamped.ToString("0.###");
            }
            st.TargetMm = clamped;
            st.Busy = Math.Abs(st.TargetMm - st.PositionMm) > InPosTol;
            st.InPosition = !st.Busy;
            return MotionResult.Ok();
        }

        /// <inheritdoc />
        public MotionResult MoveRel(int axis, double mm)
            => MoveAbs(axis, GetPosition(axis) + mm);

        /// <inheritdoc />
        public void Stop(int axis, MotionStopMode mode)
        {
            var st = St(axis);
            st.TargetMm = st.PositionMm;
            st.Busy = false;
            st.InPosition = true;
            st.Velocity = 0;
            if (mode == MotionStopMode.Emergency) st.ServoOn = false;
        }

        /// <inheritdoc />
        public void StopAll(MotionStopMode mode)
        {
            for (int i = 0; i < _axisCount; i++) Stop(i, mode);
        }

        /// <inheritdoc />
        public MotionResult Home(int axis)
        {
            var c = Cfg(axis);
            var st = St(axis);
            if (st.Alarm) return MotionResult.Reject(c.Name + " 处于报警状态，请先清除报警");
            st.ServoOn = true;
            st.Homed = false;
            st.TargetMm = Clamp(c, c.HomeMm);
            st.Busy = Math.Abs(st.TargetMm - st.PositionMm) > InPosTol;
            st.InPosition = !st.Busy;
            if (!st.Busy)
            {
                st.Homed = true;
                st.Org = true;
            }
            return MotionResult.Ok();
        }

        /// <inheritdoc />
        public double GetPosition(int axis) => St(axis).PositionMm;

        /// <inheritdoc />
        public void SetPosition(int axis, double mm)
        {
            var st = St(axis);
            st.PositionMm = Clamp(Cfg(axis), mm);
            st.TargetMm = st.PositionMm;
            st.InPosition = true;
            st.Busy = false;
        }

        /// <inheritdoc />
        public AxisState GetState(int axis) => St(axis);

        /// <inheritdoc />
        public AxisState[] Snapshot()
        {
            var arr = new AxisState[_axisCount];
            for (int i = 0; i < _axisCount; i++) arr[i] = _st[i];
            return arr;
        }

        /// <inheritdoc />
        public void WriteOut(int bit, bool on)
        {
            if (bit < 0 || bit >= IoBits) return;
            _out[bit] = on;
        }

        /// <inheritdoc />
        public bool ReadIn(int bit) => bit >= 0 && bit < IoBits && _in[bit];

        /// <inheritdoc />
        public bool ReadOut(int bit) => bit >= 0 && bit < IoBits && _out[bit];

        /// <inheritdoc />
        public void SetInput(int bit, bool on)
        {
            if (bit >= 0 && bit < IoBits) _in[bit] = on;
        }

        /// <inheritdoc />
        public bool[] SnapshotInputs() => (bool[])_in.Clone();

        /// <inheritdoc />
        public bool[] SnapshotOutputs() => (bool[])_out.Clone();

        /// <inheritdoc />
        public void ClearAlarm(int axis)
        {
            var st = St(axis);
            st.Alarm = false;
            st.PositiveLimit = false;
            st.NegativeLimit = false;
            st.SoftLimit = false;
            st.LastReject = "";
        }

        /// <summary>
        /// 推进一个仿真时间步：按梯形速度曲线积分各轴位置。
        /// 关键防撞手段：每一步都对位置做软限位硬钳制，任何情况下都不会越界。
        /// </summary>
        public void Step(double dt)
        {
            if (!_open || dt <= 0) return;
            if (dt > 0.2) dt = 0.2; // 防止界面卡顿后一次跳太远

            for (int i = 0; i < _axisCount; i++)
            {
                var c = _cfg[i];
                var st = _st[i];

                // 报警轴 / 未使能轴：只做力矩衰减
                if (st.Alarm || !st.ServoOn)
                {
                    st.Busy = false;
                    st.Velocity = 0;
                    st.Torque = Math.Max(0, st.Torque - 40 * dt);
                    continue;
                }

                double err = st.TargetMm - st.PositionMm;
                if (Math.Abs(err) <= InPosTol && Math.Abs(st.Velocity) < 0.5)
                {
                    st.PositionMm = st.TargetMm;
                    st.Velocity = 0;
                    st.Busy = false;
                    st.InPosition = true;
                    st.FollowingError = 0;
                    st.Torque = Math.Max(0, st.Torque - 60 * dt);
                    continue;
                }

                double dir = Math.Sign(err);
                double remain = Math.Abs(err);

                // 本步允许的速度上限，三重约束取最小：
                //   ① 轴的最大速度
                //   ② 梯形曲线的减速段（还能刹得住）
                //   ③ 一步之内不越过目标（离散积分必须显式防越界，
                //      否则 sqrt 减速曲线在小剩余量时会来回振荡，永远到不了位）
                double vBrake = Math.Sqrt(2 * Math.Max(1, c.Dec) * remain);
                double vStep = remain / dt;
                double vDesired = Math.Min(c.MaxVel, Math.Min(vBrake, vStep)) * dir;

                // 朝目标速度加速/减速
                double dv = vDesired - st.Velocity;
                double rate = (Math.Abs(vDesired) > Math.Abs(st.Velocity)) ? c.Acc : c.Dec;
                double maxDv = Math.Max(1, rate) * dt;
                if (Math.Abs(dv) > maxDv) dv = Math.Sign(dv) * maxDv;
                st.Velocity += dv;

                // 本步位移；若会越过目标则直接吸附到目标（模拟伺服到位）
                double step = st.Velocity * dt;
                if (Math.Sign(step) == dir && Math.Abs(step) >= remain)
                {
                    st.PositionMm = st.TargetMm;
                    st.Velocity = 0;
                    st.Busy = false;
                    st.InPosition = true;
                    st.FollowingError = 0;
                    st.Org = Math.Abs(st.PositionMm - c.HomeMm) <= 0.5;
                    if (st.Org) st.Homed = true;
                    st.Torque = Math.Max(0, st.Torque - 60 * dt);
                    continue;
                }
                st.PositionMm += step;

                // 位置噪声：让读数看起来像真实编码器反馈（极小，不影响判定）。
                // 必须放在软限位钳制之前，否则噪声可能把位置顶到限位之外而误报警。
                st.PositionMm += (_rnd.NextDouble() - 0.5) * 0.0008;

                // ---- 软限位硬钳制（防撞的最后一道闸）----
                double clamped = Clamp(c, st.PositionMm);
                if (Math.Abs(clamped - st.PositionMm) > 1e-9)
                {
                    st.PositionMm = clamped;
                    st.TargetMm = clamped;
                    st.Velocity = 0;
                    st.Busy = false;
                    st.InPosition = true;
                    st.SoftLimit = true;
                    // 撞到软限位即产生报警，需人工清除 —— 提醒操作员不要继续顶
                    st.Alarm = true;
                    st.LastReject = c.Name + " 触发软限位（" + clamped.ToString("0.###") + "），已停机报警";
                    continue;
                }

                // 状态刷新
                st.Busy = Math.Abs(st.TargetMm - st.PositionMm) > InPosTol || Math.Abs(st.Velocity) > 0.5;
                st.InPosition = !st.Busy;
                st.FollowingError = st.TargetMm - st.PositionMm;

                // 原点信号：位置落在回零位置 ±0.5 内认为压到原点
                st.Org = Math.Abs(st.PositionMm - c.HomeMm) <= 0.5;
                if (st.Org && !st.Homed && !st.Busy) st.Homed = true;

                // 硬限位（软限位之外还有一层，正常不会触发）
                st.PositiveLimit = st.PositionMm >= c.MaxMm - 1e-6;
                st.NegativeLimit = st.PositionMm <= c.MinMm + 1e-6;

                // 力矩：加速段与负载相关，静止时归零
                double accNow = dt > 0 ? Math.Abs(dv) / dt : 0;
                double load = 8 + 4 * Math.Sin(i * 1.7 + st.PositionMm * 0.05);
                st.Torque = Math.Min(95, Math.Max(0, load + accNow / 120.0));
                if (st.Busy == false) st.Torque = Math.Max(0, st.Torque - 60 * dt);
            }
        }

        /// <summary>软限位钳制。</summary>
        private static double Clamp(AxisConfig c, double v)
        {
            if (c == null) return v;
            double lo = Math.Min(c.MinMm, c.MaxMm);
            double hi = Math.Max(c.MinMm, c.MaxMm);
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        /// <summary>取轴配置（不存在则建一个默认的，避免抛异常）。</summary>
        private AxisConfig Cfg(int axis)
        {
            if (!_cfg.TryGetValue(axis, out var c))
            {
                c = new AxisConfig { Index = axis, Name = "Axis" + axis };
                _cfg[axis] = c;
            }
            return c;
        }

        /// <summary>取轴状态（不存在则建一个）。</summary>
        private AxisState St(int axis)
        {
            if (!_st.TryGetValue(axis, out var s))
            {
                s = new AxisState();
                _st[axis] = s;
            }
            return s;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
