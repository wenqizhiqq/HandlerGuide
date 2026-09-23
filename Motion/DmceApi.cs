// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/DmceApi.cs — 雷赛 DMC-E 总线控制卡原生接口
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  说明：对应 DMC-E（DMC3000/5000 系列）随卡发布的 LTDMC.dll。
//        函数族为 dmc_*（脉冲与通用）+ nmc_*（EtherCAT 总线专用）。
//        本机若未安装 LTDMC.dll / 未插卡，ProbeEx() 返回 false，
//        MotionManager 会自动降级到内置仿真卡，保证界面与逻辑仍可完整演示。
// ============================================================================
using System;
using System.Runtime.InteropServices;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（DmceApi）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_DmceApi
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · DmceApi。</summary>
        internal const string WenQiZhiFile = "DmceApi";
    }

    /// <summary>
    /// LTDMC.dll（雷赛 DMC 系列）P/Invoke 封装，签名完全照搬
    /// 例程「例18_总线操作/总线操作/LTDMC.cs」的官方声明。
    /// 所有调用都可能在无卡环境下抛 <see cref="DllNotFoundException"/>，
    /// 因此调用方必须用 try/catch 包裹（见 <see cref="ProbeEx"/> 与 DmceCard）。
    /// </summary>
    internal static class DmceApi
    {
        /// <summary>动态库名（随雷赛驱动安装到 System32）。</summary>
        internal const string Dll = "LTDMC.dll";

        // ---------------- 板卡初始化 ----------------

        /// <summary>初始化板卡；返回检测到的卡数量（0 = 未检测到）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_board_init();

        /// <summary>关闭板卡。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_board_close();

        /// <summary>查询卡列表（型号常量 + CardId）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_CardInfList(ref ushort CardNum, uint[] CardTypeList, ushort[] CardIdList);

        /// <summary>读取固件版本。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_card_soft_version(ushort CardNo, ref uint FirmID, ref uint SubFirmID);

        // ---------------- 单轴 IO ----------------

        /// <summary>读输入位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_read_inbit(ushort CardNo, ushort bitno);

        /// <summary>写输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_write_outbit(ushort CardNo, ushort bitno, ushort on_off);

        /// <summary>读输出位（回读）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_read_outbit(ushort CardNo, ushort bitno);

        /// <summary>读 32 位输入端口（0..7 共 256 点）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern UInt32 dmc_read_inport(ushort CardNo, ushort portno);

        /// <summary>写 32 位输出端口。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_write_outport(ushort CardNo, ushort portno, UInt32 outport_val);

        // ---------------- 单轴：脉冲单位 / 梯形曲线 ----------------

        /// <summary>设置单轴速度曲线（工程单位 MinVel/MaxVel/Tacc/Tdec/StopVel，单位：mm/s 与 ms）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_set_profile_unit(ushort CardNo, ushort Axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);

        /// <summary>梯形曲线定长运动（工程单位）；posi_mode：0 = 相对，1 = 绝对。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_t_pmove_unit(ushort CardNo, ushort axis, double Dist, ushort posi_mode);

        /// <summary>对称 T 型定长（同 dmc_t_pmove_unit 一致）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_pmove_unit(ushort CardNo, ushort axis, double Dist, ushort posi_mode);

        /// <summary>连续运动（Jog）；dir：0 = 负向，1 = 正向。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_vmove(ushort CardNo, ushort axis, ushort dir);

        /// <summary>停止单轴；stop_mode：0 = 减速停，1 = 急停。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_stop(ushort CardNo, ushort axis, ushort stop_mode);

        // ---------------- 单轴：位置（工程单位） ----------------

        /// <summary>读取当前指令位置（mm / °）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_position_unit(ushort CardNo, ushort axis, ref double pos);

        /// <summary>设置当前位置为指定工程单位值（不影响编码器）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_set_position_unit(ushort CardNo, ushort axis, double pos);

        /// <summary>读取编码器反馈位置（工程单位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_encoder_unit(ushort CardNo, ushort axis, ref double pos);

        /// <summary>读取指令位置（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern Int32 dmc_get_position(ushort CardNo, ushort axis);

        /// <summary>读取当前目标位置（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern Int32 dmc_get_target_position(ushort CardNo, ushort axis);

        // ---------------- 单轴：限位 / 报警 / 回零 ----------------

        /// <summary>设置回零软限位（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_set_home_soft_limit(ushort CardNo, ushort Axis, Int32 N_limit, Int32 P_limit);

        /// <summary>读取回零软限位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_home_soft_limit(ushort CardNo, ushort Axis, ref Int32 N_limit, ref Int32 P_limit);

        /// <summary>设置回零参数（工程单位速度 + 加减速时间）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_set_home_profile_unit(ushort CardNo, ushort axis, double Low_Vel, double High_Vel, double Tacc, double Tdec);

        /// <summary>读取回零参数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_home_profile_unit(ushort CardNo, ushort axis, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec);

        /// <summary>设置回零位置补偿（工程单位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_set_home_position_unit(ushort CardNo, ushort axis, ushort enable, double position);

        // ---------------- 状态读取（脉冲卡通用） ----------------

        /// <summary>读取单轴速度曲线参数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short dmc_get_profile(ushort CardNo, ushort Axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel);

        // ---------------- 总线卡（nmc_*）专用 ----------------

        /// <summary>读取总线卡轴总数（含未使能）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_total_axes(ushort CardNo, ref uint TotalAxis);

        /// <summary>读取总线卡的输入/输出点位总数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_total_ionum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);

        /// <summary>总线卡轴 402 状态字。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_axis_statusword(ushort CardNo, ushort axis, ref int status);

        /// <summary>总线卡轴状态机（0..7）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_axis_state_machine(ushort CardNo, ushort axis, ref ushort state);

        /// <summary>总线卡轴使能（402 控制字 0x06）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_set_axis_enable(ushort CardNo, ushort axis);

        /// <summary>总线卡轴失能（402 控制字 0x07）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_set_axis_disable(ushort CardNo, ushort axis);

        /// <summary>总线卡轴回零（2 参数）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_home_move(ushort CardNo, ushort axis);

        /// <summary>总线卡轴写输出位（PDO 位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_write_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ushort IoValue);

        /// <summary>总线卡轴读输入位（PDO 位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_read_inbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);

        /// <summary>总线卡轴读输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_read_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);

        /// <summary>总线卡写 32 位输出端口。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_write_outport(ushort CardNo, ushort NodeID, ushort PortNo, UInt32 IoValue);

        /// <summary>总线卡读 32 位输入端口。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_read_inport(ushort CardNo, ushort NodeID, ushort PortNo, ref UInt32 IoValue);

        /// <summary>总线卡清除节点报警。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_set_alarm_clear(ushort CardNo, ushort PortNum, ushort nodenum);

        /// <summary>查询节点心跳丢失。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_LostHeartbeat_Nodes(ushort CardNo, ushort PortNum, ushort[] NodeID, ref ushort NodeNum);

        /// <summary>查询节点紧急消息。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_get_EmergeneyMessege_Nodes(ushort CardNo, ushort PortNum, UInt32[] NodeMsg, ref ushort MsgNum);

        /// <summary>EtherCAT 扫描启动。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_start_scan_ethercat(ushort CardNo, ushort AddressID);

        /// <summary>EtherCAT 扫描停止。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short nmc_stop_scan_ethercat(ushort CardNo, ushort AddressID);

        // =================================================
        //  探测与高层判断
        // =================================================

        /// <summary>
        /// 探测 LTDMC.dll + 板卡（含 DMC-E 总线卡）。
        /// 返回：true = 真有板；并回填 CardId / IsBus / Vendor / Description。
        /// 型号常量参考（不完全列表）：
        ///   0x1021 / 0x1024 = 脉冲卡（DMC2410 / DMC5480 等）
        ///   0x1034 / 0x1035 = 网口脉冲卡（DMC5830 / DMC5800 等）
        ///   0xF0xx / 0xF1xx = 总线卡（DMC-E 系列 EtherCAT）
        /// </summary>
        internal static bool ProbeEx(out ushort cardId, out bool isBus, out string description)
        {
            cardId = 0; isBus = false; description = "";
            try
            {
                short cards = dmc_board_init();
                if (cards <= 0) { description = "dmc_board_init 返回 " + cards + "（未检测到板卡）"; return false; }
                ushort num = 0;
                var types = new uint[8];
                var ids = new ushort[8];
                short r = dmc_get_CardInfList(ref num, types, ids);
                if (r != 0 || num == 0) { description = "dmc_get_CardInfList 返回失败（" + r + "）"; return false; }
                cardId = ids[0];
                uint t = types[0];
                isBus = (t & 0xF000) == 0xF000;
                description = isBus
                    ? "DMC-E 总线卡 0x" + t.ToString("X4") + "（CardId=" + cardId + "，型号取自例程）"
                    : "DMC 脉冲卡 0x" + t.ToString("X4") + "（CardId=" + cardId + "，型号取自例程）";
                return true;
            }
            catch (DllNotFoundException) { description = "LTDMC.dll 缺失（未安装雷赛驱动）"; return false; }
            catch (EntryPointNotFoundException) { description = "LTDMC.dll 缺少入口符号"; return false; }
            catch (BadImageFormatException) { description = "LTDMC.dll 位数不匹配（需要 x86/x64 对应）"; return false; }
            catch (Exception ex) { description = ex.GetType().Name + ": " + ex.Message; return false; }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------