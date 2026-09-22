// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/LeadshineCard.cs — 雷赛 DMC 系列真卡后端
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：把 IMotionCard 的语义翻译成 LTDMC.dll 的 d1000_* 调用。
//        位置在接口层是工程单位（mm），在本类内部换算成脉冲（× 脉冲当量）。
//        每次原生调用都做 try/catch：一旦驱动异常即标记为断开，
//        由 MotionManager 决定是否降级到仿真卡（保证界面永不崩）。
// ============================================================================
using System;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LeadshineCard）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_LeadshineCard
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · LeadshineCard。</summary>
        internal const string WenQiZhiFile = "LeadshineCard";
    }

    /// <summary>雷赛 LTDMC.dll 后端。</summary>
    public sealed class LeadshineCard : IMotionCard
    {
        private const int IoBits = 64;

        private readonly AxisConfig[] _cfg;
        private readonly AxisState[] _st;
        private readonly bool[] _outCache = new bool[IoBits];
        private bool _open;
        private string _lastError = "";

        /// <summary>构造：默认按 32 轴（DMC5800 级别）准备。</summary>
        public LeadshineCard(int axisCount = 32)
        {
            _cfg = new AxisConfig[axisCount];
            _st = new AxisState[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                _cfg[i] = new AxisConfig { Index = i, Name = "Axis" + i };
                _st[i] = new AxisState();
            }
        }

        /// <inheritdoc />
        public string CardName => "Leadshine DMC 系列（LTDMC.dll）";

        /// <inheritdoc />
        public bool IsReal => true;

        /// <inheritdoc />
        public bool IsConnected => _open;

        /// <inheritdoc />
        public int AxisCount => _cfg.Length;

        /// <inheritdoc />
        public int InputCount => 32;

        /// <inheritdoc />
        public int OutputCount => 32;

        /// <inheritdoc />
        public string Info => _open
            ? "已连接雷赛控制卡（d1000_* API）" + (_lastError.Length > 0 ? "；最近异常：" + _lastError : "")
            : "未连接";

        /// <inheritdoc />
        public bool Open()
        {
            try
            {
                short cards = LeadshineApi.d1000_board_init();
                _open = cards > 0;
                if (!_open) _lastError = "d1000_board_init 返回 " + cards + "（未检测到卡）";
                return _open;
            }
            catch (Exception ex)
            {
                _lastError = ex.GetType().Name + ": " + ex.Message;
                _open = false;
                return false;
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            try { LeadshineApi.d1000_board_close(); }
            catch (Exception ex) { _lastError = ex.Message; }
            _open = false;
        }

        /// <inheritdoc />
        public void Configure(AxisConfig cfg)
        {
            if (cfg == null || cfg.Index < 0 || cfg.Index >= _cfg.Length) return;
            _cfg[cfg.Index] = cfg;
            Guard(() =>
            {
                // 脉冲当量（每单位脉冲数）
                LeadshineApi.d1000_set_equiv((ushort)cfg.Index, cfg.PulseEquivalent);
                // 软限位（脉冲）
                LeadshineApi.d1000_set_soft_limit((ushort)cfg.Index,
                    Pulse(cfg, cfg.MaxMm), Pulse(cfg, cfg.MinMm));
                // 速度曲线：用「加速时间」表示（t = vMax / a）
                double tAcc = cfg.Acc > 0 ? cfg.MaxVel / cfg.Acc : 0.1;
                double tDec = cfg.Dec > 0 ? cfg.MaxVel / cfg.Dec : 0.1;
                LeadshineApi.d1000_set_profile((ushort)cfg.Index, 10, cfg.MaxVel, tAcc, tDec);
                LeadshineApi.d1000_set_s_profile((ushort)cfg.Index, 0.05);
                // 报警：使能，常开有效，减速停止
                LeadshineApi.d1000_set_alm_mode((ushort)cfg.Index, 1, 0, 0);
                // 限位：使能，常开有效，立即停止
                LeadshineApi.d1000_set_el_mode((ushort)cfg.Index, 1, 0, 1);
            });
        }

        /// <inheritdoc />
        public void SetServo(int axis, bool on)
            => Guard(() => LeadshineApi.d1000_set_servo((ushort)axis, (ushort)(on ? 1 : 0)));

        /// <inheritdoc />
        public void SetSoftLimit(int axis, double min, double max)
        {
            var c = Cfg(axis);
            c.MinMm = min;
            c.MaxMm = max;
            Guard(() => LeadshineApi.d1000_set_soft_limit((ushort)axis, Pulse(c, max), Pulse(c, min)));
        }

        /// <inheritdoc />
        public void SetProfile(int axis, double maxVel, double acc, double dec)
        {
            var c = Cfg(axis);
            if (maxVel > 0) c.MaxVel = maxVel;
            if (acc > 0) c.Acc = acc;
            if (dec > 0) c.Dec = dec;
            double tAcc = c.Acc > 0 ? c.MaxVel / c.Acc : 0.1;
            double tDec = c.Dec > 0 ? c.MaxVel / c.Dec : 0.1;
            Guard(() => LeadshineApi.d1000_set_profile((ushort)axis, 10, c.MaxVel, tAcc, tDec));
        }

        /// <inheritdoc />
        public MotionResult MoveAbs(int axis, double mm)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            var c = Cfg(axis);
            // 真卡同样先做一次软限位预判，避免把越界指令发给驱动器
            double clamped = Math.Min(Math.Max(mm, Math.Min(c.MinMm, c.MaxMm)), Math.Max(c.MinMm, c.MaxMm));
            bool ok = true;
            Guard(() => { ok = LeadshineApi.d1000_t_pmove((ushort)axis, Pulse(c, clamped), 1) == 0; });
            return ok ? MotionResult.Ok() : MotionResult.Reject("d1000_t_pmove 返回失败：" + _lastError);
        }

        /// <inheritdoc />
        public MotionResult MoveRel(int axis, double mm)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            var c = Cfg(axis);
            bool ok = true;
            Guard(() => { ok = LeadshineApi.d1000_t_pmove((ushort)axis, Pulse(c, mm), 0) == 0; });
            return ok ? MotionResult.Ok() : MotionResult.Reject("d1000_t_pmove 返回失败：" + _lastError);
        }

        /// <inheritdoc />
        public void Stop(int axis, MotionStopMode mode)
            => Guard(() => LeadshineApi.d1000_stop((ushort)axis, (ushort)(mode == MotionStopMode.Emergency ? 1 : 0)));

        /// <inheritdoc />
        public void StopAll(MotionStopMode mode)
        {
            for (int i = 0; i < _cfg.Length; i++) Stop(i, mode);
        }

        /// <inheritdoc />
        public MotionResult Home(int axis)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            bool ok = true;
            // home_mode 0 = 负向回零；速度模式 0 = 低速
            Guard(() => { ok = LeadshineApi.d1000_home_move((ushort)axis, 0, 0) == 0; });
            return ok ? MotionResult.Ok() : MotionResult.Reject("d1000_home_move 返回失败：" + _lastError);
        }

        /// <inheritdoc />
        public double GetPosition(int axis)
        {
            int pulses = 0;
            Guard(() => { pulses = LeadshineApi.d1000_get_position((ushort)axis); });
            var c = Cfg(axis);
            return c.PulseEquivalent > 0 ? pulses / c.PulseEquivalent : pulses;
        }

        /// <inheritdoc />
        public void SetPosition(int axis, double mm)
        {
            var c = Cfg(axis);
            Guard(() => LeadshineApi.d1000_set_position((ushort)axis, Pulse(c, mm)));
        }

        /// <inheritdoc />
        public AxisState GetState(int axis)
        {
            var st = St(axis);
            ushort word = 0;
            Guard(() => { LeadshineApi.d1000_get_axis_status((ushort)axis, out word); });
            st.Alarm = (word & 0x0001) != 0;
            st.InPosition = (word & 0x0002) != 0;
            st.Busy = (word & 0x0200) != 0;
            st.Org = (word & 0x0004) != 0;
            st.PositiveLimit = (word & 0x0008) != 0;
            st.NegativeLimit = (word & 0x0010) != 0;
            st.PositionMm = GetPosition(axis);
            return st;
        }

        /// <inheritdoc />
        public AxisState[] Snapshot()
        {
            for (int i = 0; i < _cfg.Length; i++) GetState(i);
            return _st;
        }

        /// <inheritdoc />
        public void WriteOut(int bit, bool on)
        {
            if (bit < 0 || bit >= IoBits) return;
            _outCache[bit] = on;
            Guard(() => LeadshineApi.d1000_write_outbit(0, (ushort)bit, (ushort)(on ? 1 : 0)));
        }

        /// <inheritdoc />
        public bool ReadIn(int bit)
        {
            ushort v = 0;
            Guard(() => { v = LeadshineApi.d1000_read_inbit(0, (ushort)bit); });
            return v != 0;
        }

        /// <inheritdoc />
        public bool ReadOut(int bit)
        {
            if (bit >= 0 && bit < IoBits && _outCache[bit]) return true;
            ushort v = 0;
            Guard(() => { v = LeadshineApi.d1000_read_outbit(0, (ushort)bit); });
            return v != 0;
        }

        /// <inheritdoc />
        public void SetInput(int bit, bool on)
        {
            // 真卡的输入来自硬件，软件不可写；此处刻意留空。
        }

        /// <inheritdoc />
        public bool[] SnapshotInputs()
        {
            var arr = new bool[IoBits];
            for (int i = 0; i < 32; i++) arr[i] = ReadIn(i);
            return arr;
        }

        /// <inheritdoc />
        public bool[] SnapshotOutputs()
        {
            var arr = new bool[IoBits];
            for (int i = 0; i < 32; i++) arr[i] = ReadOut(i);
            return arr;
        }

        /// <inheritdoc />
        public void Step(double dt)
        {
            // 真卡由硬件负责运动，软件不需要积分。
        }

        /// <inheritdoc />
        public void ClearAlarm(int axis)
            => Guard(() => LeadshineApi.d1000_clear_axis_status((ushort)axis));

        /// <summary>工程单位 → 脉冲。</summary>
        private static int Pulse(AxisConfig c, double mm)
            => (int)Math.Round(mm * (c?.PulseEquivalent ?? 1000));

        private AxisConfig Cfg(int axis)
        {
            if (axis < 0 || axis >= _cfg.Length) return new AxisConfig { Index = axis, Name = "Axis" + axis };
            return _cfg[axis];
        }

        private AxisState St(int axis)
        {
            if (axis < 0 || axis >= _st.Length)
            {
                var s = new AxisState();
                return s;
            }
            return _st[axis];
        }

        /// <summary>原生调用保护：任何异常都记录并标记断开，绝不让异常冒泡到界面。</summary>
        private void Guard(Action act)
        {
            try { act(); }
            catch (Exception ex)
            {
                _lastError = ex.GetType().Name + ": " + ex.Message;
                _open = false;
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
