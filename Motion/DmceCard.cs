// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/DmceCard.cs — 雷赛 DMC-E 总线卡 / 脉冲卡统一后端
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  职责：把 IMotionCard 的语义翻译成 DmceApi 的 dmc_* / nmc_* 调用。
//        位置在接口层是工程单位（mm / °）；总线卡直接用 _unit 函数，
//        脉冲卡则先换算成脉冲。ProbeEx 会自动判别当前是总线卡还是脉冲卡。
//        每次原生调用都做 try/catch：一旦驱动异常即标记为断开，
//        由 MotionManager 决定是否降级到仿真卡（保证界面永不崩）。
// ============================================================================
using System;
using System.Collections.Generic;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（DmceCard）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_DmceCard
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · DmceCard。</summary>
        internal const string WenQiZhiFile = "DmceCard";
    }

    /// <summary>雷赛 LTDMC.dll（DMC-E 总线卡 / DMC 脉冲卡）后端。</summary>
    public sealed class DmceCard : IMotionCard
    {
        // 轴数组按「轴号」寻址，而真机轴号不连续（分支4 缺 17、分支2 缺 38，最大 46），
        // 所以配置时按需要扩容，不能假设数组长度 == 轴数。
        private AxisConfig[] _cfg;
        private AxisState[] _st;
        private bool[] _outCache;
        private bool[] _inCache;

        /// <summary>本卡实际配置过的轴号（升序）：StopAll / Snapshot 只碰这些轴。</summary>
        private readonly List<int> _axisIdx = new List<int>();

        private bool _open;
        private bool _isBus;
        private ushort _cardId;
        private string _vendor = "";
        private string _lastError = "";

        /// <summary>
        /// 构造：按指定轴数 + IO 位数准备（脉冲卡上限 32，总线卡上限依伺服节点）。
        /// inputCount / outputCount 由 MotionManager 按交接资料地址空间给出
        /// （真表 352 入 / 160 出，再加 32 位兼容段）。
        /// </summary>
        public DmceCard(int axisCount = 32, int inputCount = 352, int outputCount = 160)
        {
            if (axisCount < 1) axisCount = 1;
            if (inputCount < 1) inputCount = 1;
            if (outputCount < 1) outputCount = 1;
            _cfg = new AxisConfig[axisCount];
            _st = new AxisState[axisCount];
            _inCache = new bool[inputCount];
            _outCache = new bool[outputCount];
            FillSlots(0, axisCount - 1);
        }

        /// <summary>把 [from, to] 区间的槽位补成非空对象。</summary>
        private void FillSlots(int from, int to)
        {
            for (int i = from; i <= to; i++)
            {
                if (_cfg[i] == null) _cfg[i] = new AxisConfig { Index = i, Name = "Axis" + i };
                if (_st[i] == null) _st[i] = new AxisState();
            }
        }

        /// <summary>按轴号扩容（真机轴号可以大于轴数）。</summary>
        private void EnsureAxis(int axis)
        {
            if (axis < 0 || axis < _cfg.Length) return;
            int old = _cfg.Length;
            int n = axis + 1;
            Array.Resize(ref _cfg, n);
            Array.Resize(ref _st, n);
            FillSlots(old, n - 1);
        }

        /// <inheritdoc />
        public string CardName => _isBus
            ? "DMC-E 总线卡（EtherCAT / nmc_*）"
            : "DMC 脉冲卡（dmc_*）";

        /// <inheritdoc />
        public bool IsReal => true;

        /// <inheritdoc />
        public bool IsConnected => _open;

        /// <inheritdoc />
        public int AxisCount => _cfg.Length;

        /// <inheritdoc />
        public int InputCount => _inCache.Length;

        /// <inheritdoc />
        public int OutputCount => _outCache.Length;

        /// <inheritdoc />
        public string Info => _open
            ? _vendor + (_lastError.Length > 0 ? "；最近异常：" + _lastError : "")
            : "未连接";

        /// <inheritdoc />
        public bool Open()
        {
            if (!DmceApi.ProbeEx(out _cardId, out _isBus, out string desc))
            {
                _lastError = desc;
                _open = false;
                return false;
            }
            _vendor = desc;
            _open = true;
            _lastError = "";
            return true;
        }

        /// <inheritdoc />
        public void Close()
        {
            try { DmceApi.dmc_board_close(); }
            catch (Exception ex) { _lastError = ex.Message; }
            _open = false;
        }

        /// <inheritdoc />
        public void Configure(AxisConfig cfg)
        {
            if (cfg == null || cfg.Index < 0) return;
            EnsureAxis(cfg.Index);
            _cfg[cfg.Index] = cfg;
            if (!_axisIdx.Contains(cfg.Index)) { _axisIdx.Add(cfg.Index); _axisIdx.Sort(); }
            Guard(() =>
            {
                if (_isBus)
                {
                    // 总线卡：直接用工程单位速度曲线。Nm 的 home 由 nmc_home_move 单独处理。
                    DmceApi.dmc_set_profile_unit(_cardId, (ushort)cfg.Index,
                        cfg.MaxVel * 0.05, cfg.MaxVel, cfg.MaxVel / Math.Max(1, cfg.Acc),
                        cfg.MaxVel / Math.Max(1, cfg.Dec), 0);
                    DmceApi.dmc_set_home_profile_unit(_cardId, (ushort)cfg.Index,
                        cfg.MaxVel * 0.1, cfg.MaxVel * 0.5,
                        cfg.MaxVel / Math.Max(1, cfg.Acc), cfg.MaxVel / Math.Max(1, cfg.Dec));
                }
                else
                {
                    // 脉冲卡：把工程单位 → 脉冲当量（每单位脉冲数）。并用工程单位速度曲线。
                    // dmc_set_profile_unit 内部用工程单位；脉冲当量配置由 _cfg.PulseEquivalent 在 MoveAbs 里再换算。
                    DmceApi.dmc_set_profile_unit(_cardId, (ushort)cfg.Index,
                        cfg.MaxVel * 0.05, cfg.MaxVel, cfg.MaxVel / Math.Max(1, cfg.Acc),
                        cfg.MaxVel / Math.Max(1, cfg.Dec), 0);
                }
            });
        }

        /// <inheritdoc />
        public void SetServo(int axis, bool on)
        {
            Guard(() =>
            {
                if (_isBus)
                {
                    if (on) DmceApi.nmc_set_axis_enable(_cardId, (ushort)axis);
                    else DmceApi.nmc_set_axis_disable(_cardId, (ushort)axis);
                }
                // 脉冲卡不需要单独下伺服使能（驱动器硬件接线已使能）。
            });
        }

        /// <inheritdoc />
        public void SetSoftLimit(int axis, double min, double max)
        {
            var c = Cfg(axis);
            c.MinMm = min;
            c.MaxMm = max;
            Guard(() =>
            {
                // 工程单位 → 脉冲（仅脉冲卡需要换算）
                double n = Pulse(c, min);
                double p = Pulse(c, max);
                DmceApi.dmc_set_home_soft_limit(_cardId, (ushort)axis, (int)n, (int)p);
            });
        }

        /// <inheritdoc />
        public void SetProfile(int axis, double maxVel, double acc, double dec)
        {
            var c = Cfg(axis);
            if (maxVel > 0) c.MaxVel = maxVel;
            if (acc > 0) c.Acc = acc;
            if (dec > 0) c.Dec = dec;
            Guard(() => DmceApi.dmc_set_profile_unit(_cardId, (ushort)axis,
                c.MaxVel * 0.05, c.MaxVel,
                c.MaxVel / Math.Max(1, c.Acc), c.MaxVel / Math.Max(1, c.Dec), 0));
        }

        /// <inheritdoc />
        public MotionResult MoveAbs(int axis, double mm)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            var c = Cfg(axis);
            // 真卡同样先做一次软限位预判，避免把越界指令发给驱动器
            double lo = Math.Min(c.MinMm, c.MaxMm);
            double hi = Math.Max(c.MinMm, c.MaxMm);
            double clamped = Math.Min(Math.Max(mm, lo), hi);
            bool ok = true;
            Guard(() =>
            {
                if (_isBus)
                {
                    // 总线卡直接传工程单位
                    ok = DmceApi.dmc_t_pmove_unit(_cardId, (ushort)axis, clamped, 1) == 0;
                }
                else
                {
                    // 脉冲卡：先换算到脉冲，再下发脉冲位置。
                    int pulses = (int)Math.Round(Pulse(c, clamped));
                    ok = DmceApi.dmc_t_pmove_unit(_cardId, (ushort)axis, pulses, 1) == 0;
                }
            });
            return ok ? MotionResult.Ok() : MotionResult.Reject("dmc_t_pmove_unit 失败：" + _lastError);
        }

        /// <inheritdoc />
        public MotionResult MoveRel(int axis, double mm)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            var c = Cfg(axis);
            bool ok = true;
            Guard(() =>
            {
                if (_isBus) ok = DmceApi.dmc_t_pmove_unit(_cardId, (ushort)axis, mm, 0) == 0;
                else ok = DmceApi.dmc_t_pmove_unit(_cardId, (ushort)axis, Pulse(c, mm), 0) == 0;
            });
            return ok ? MotionResult.Ok() : MotionResult.Reject("dmc_t_pmove_unit 失败：" + _lastError);
        }

        /// <inheritdoc />
        public void Stop(int axis, MotionStopMode mode)
            => Guard(() => DmceApi.dmc_stop(_cardId, (ushort)axis, (ushort)(mode == MotionStopMode.Emergency ? 1 : 0)));

        /// <inheritdoc />
        public void StopAll(MotionStopMode mode)
        {
            // 只对真表里配置过的轴下停止指令，避免向不存在的轴号（17 / 38）发命令
            foreach (var i in _axisIdx) Stop(i, mode);
        }

        /// <inheritdoc />
        public MotionResult Home(int axis)
        {
            if (!_open) return MotionResult.Reject("控制卡未连接");
            bool ok = true;
            Guard(() =>
            {
                if (_isBus) ok = DmceApi.nmc_home_move(_cardId, (ushort)axis) == 0;
                else ok = DmceApi.dmc_set_position_unit(_cardId, (ushort)axis, 0) == 0;
            });
            return ok ? MotionResult.Ok() : MotionResult.Reject("home 失败：" + _lastError);
        }

        /// <inheritdoc />
        public double GetPosition(int axis)
        {
            double mm = 0;
            Guard(() =>
            {
                if (_isBus) DmceApi.dmc_get_position_unit(_cardId, (ushort)axis, ref mm);
                else
                {
                    int pulses = DmceApi.dmc_get_position(_cardId, (ushort)axis);
                    var c = Cfg(axis);
                    mm = c.PulseEquivalent > 0 ? pulses / c.PulseEquivalent : pulses;
                }
            });
            return mm;
        }

        /// <inheritdoc />
        public void SetPosition(int axis, double mm)
            => Guard(() => DmceApi.dmc_set_position_unit(_cardId, (ushort)axis,
                Cfg(axis).PulseEquivalent > 0 && !_isBus ? Pulse(Cfg(axis), mm) : mm));

        /// <inheritdoc />
        public AxisState GetState(int axis)
        {
            var st = St(axis);
            st.PositionMm = GetPosition(axis);
            // 状态字在总线卡来自 nmc_get_axis_statusword；脉冲卡通用函数没有标准接口，
            // 这里仅读运动速度（dmc_read_current_speed_unit 也已存在于例程）。
            if (_isBus)
            {
                int sw = 0;
                Guard(() => { DmceApi.nmc_get_axis_statusword(_cardId, (ushort)axis, ref sw); });
                st.Alarm = (sw & 0x0008) != 0;            // CiA402 bit3: fault
                st.InPosition = (sw & 0x0400) != 0;        // bit10: target reached
                st.ServoOn = (sw & 0x0200) != 0;          // bit9: operation enabled
                st.Busy = !st.InPosition && st.ServoOn;
            }
            return st;
        }

        /// <inheritdoc />
        public AxisState[] Snapshot()
        {
            var arr = new AxisState[_axisIdx.Count];
            for (int k = 0; k < _axisIdx.Count; k++)
            {
                arr[k] = GetState(_axisIdx[k]);
            }
            return arr;
        }

        /// <inheritdoc />
        public void WriteOut(int bit, bool on)
        {
            if (bit < 0 || bit >= _outCache.Length) return;
            _outCache[bit] = on;
            Guard(() => DmceApi.dmc_write_outbit(_cardId, (ushort)bit, (ushort)(on ? 1 : 0)));
        }

        /// <inheritdoc />
        public bool ReadIn(int bit)
        {
            if (bit < 0 || bit >= _inCache.Length) return false;
            ushort v = 0;
            // dmc_read_inbit 返回 short（0/1），但缓存里存的是 bool。统一映射。
            short raw = 0;
            Guard(() => { raw = DmceApi.dmc_read_inbit(_cardId, (ushort)bit); });
            v = raw > 0 ? (ushort)1 : (ushort)0;
            _inCache[bit] = v != 0;
            return _inCache[bit];
        }

        /// <inheritdoc />
        public bool ReadOut(int bit)
        {
            if (bit < 0 || bit >= _outCache.Length) return false;
            if (_outCache[bit]) return true;
            short raw = 0;
            Guard(() => { raw = DmceApi.dmc_read_outbit(_cardId, (ushort)bit); });
            return raw > 0;
        }

        /// <inheritdoc />
        public void SetInput(int bit, bool on)
        {
            // 真卡的输入来自硬件，软件不可写；这里只更新缓存（仿真 / 自检用）。
            if (bit >= 0 && bit < _inCache.Length) _inCache[bit] = on;
        }

        /// <inheritdoc />
        public bool[] SnapshotInputs()
        {
            var arr = new bool[_inCache.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = _inCache[i];
            return arr;
        }

        /// <inheritdoc />
        public bool[] SnapshotOutputs()
        {
            var arr = new bool[_outCache.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = _outCache[i];
            return arr;
        }

        /// <inheritdoc />
        public void Step(double dt)
        {
            // 真卡由硬件负责运动，软件不需要积分。
        }

        /// <inheritdoc />
        public void ClearAlarm(int axis)
            => Guard(() =>
            {
                if (_isBus) DmceApi.nmc_set_alarm_clear(_cardId, 0, (ushort)axis);
                // 脉冲卡由示例提供 dmc_clear_alarm_fieldbus / dmc_reset_* 等函数，因型号差异较大，
                // 这里保守不调，由 MotionManager 的 ClearAlarm 走 simulator 同等路径。
            });

        /// <summary>工程单位 → 脉冲。</summary>
        private static double Pulse(AxisConfig c, double mm)
            => Math.Round(mm * (c?.PulseEquivalent ?? 1000));

        private AxisConfig Cfg(int axis)
        {
            if (axis < 0) return new AxisConfig { Index = axis, Name = "Axis" + axis };
            EnsureAxis(axis);
            return _cfg[axis];
        }

        private AxisState St(int axis)
        {
            if (axis < 0) return new AxisState();
            EnsureAxis(axis);
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