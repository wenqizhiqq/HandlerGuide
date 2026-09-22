// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/LeadshineApi.cs — 雷赛（Leadshine）运动控制卡原生接口
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//  说明：对应 DMC 系列（DMC2410 / DMC5480 / DMC5800 等）随卡发布的 LTDMC.dll，
//        函数族为经典的 d1000_*。
//        本机若未安装 LTDMC.dll / 未插卡，Probe() 返回 false，
//        MotionManager 会自动降级到内置仿真卡，保证界面与逻辑仍可完整演示。
// ============================================================================
using System;
using System.Runtime.InteropServices;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LeadshineApi）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_LeadshineApi
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · LeadshineApi。</summary>
        internal const string WenQiZhiFile = "LeadshineApi";
    }

    /// <summary>
    /// LTDMC.dll（雷赛 DMC 系列）P/Invoke 封装。
    /// 所有调用都可能在无卡环境下抛 <see cref="DllNotFoundException"/>，
    /// 因此调用方必须用 try/catch 包裹（见 <see cref="Probe"/> 与 LeadshineCard）。
    /// </summary>
    internal static class LeadshineApi
    {
        /// <summary>动态库名（随雷赛驱动安装到 System32）。</summary>
        internal const string Dll = "LTDMC.dll";

        // ---------------- 板卡 ----------------

        /// <summary>初始化板卡，返回检测到的卡数量（0 = 未检测到）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_board_init();

        /// <summary>关闭板卡。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern void d1000_board_close();

        /// <summary>读取轴状态字。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_get_axis_status(ushort axis, out ushort status);

        /// <summary>清除轴报警状态。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_clear_axis_status(ushort axis);

        // ---------------- 位置 / 单位 ----------------

        /// <summary>读取指令位置（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern int d1000_get_position(ushort axis);

        /// <summary>设置指令位置（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_position(ushort axis, int pos);

        /// <summary>读取编码器反馈位置（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern int d1000_get_encoder(ushort axis);

        /// <summary>设置脉冲当量（脉冲/单位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_equiv(ushort axis, double equiv);

        // ---------------- 限位 / 保护 ----------------

        /// <summary>设置软限位（脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_soft_limit(ushort axis, int positive, int negative);

        /// <summary>报警输入模式（enable / 逻辑 / 动作）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_alm_mode(ushort axis, ushort enable, ushort logic, ushort action);

        /// <summary>限位输入模式（enable / 逻辑 / 停止方式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_el_mode(ushort axis, ushort enable, ushort logic, ushort mode);

        // ---------------- 速度曲线 / 运动 ----------------

        /// <summary>设置速度曲线（最小速度 / 最大速度 / 加速时间 / 减速时间）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_profile(ushort axis, double minVel, double maxVel, double tAcc, double tDec);

        /// <summary>S 曲线平滑时间。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_s_profile(ushort axis, double sPara);

        /// <summary>梯形速度曲线定长运动；posiMode：0 = 相对，1 = 绝对。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_t_pmove(ushort axis, int dist, ushort posiMode);

        /// <summary>连续运动（Jog）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_vmove(ushort axis, ushort dir);

        /// <summary>停止；stopMode：0 = 减速停，1 = 急停。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_stop(ushort axis, ushort stopMode);

        /// <summary>回零运动。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_home_move(ushort axis, ushort homeMode, ushort velMode);

        /// <summary>伺服使能 / 断使能。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_set_servo(ushort axis, ushort onOff);

        // ---------------- IO ----------------

        /// <summary>写输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_write_outbit(ushort node, ushort bit, ushort onOff);

        /// <summary>读输入位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern ushort d1000_read_inbit(ushort node, ushort bit);

        /// <summary>读输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern ushort d1000_read_outbit(ushort node, ushort bit);

        /// <summary>读 16 位输入端口。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern ushort d1000_read_inport(ushort node, ushort port);

        /// <summary>写 16 位输出端口。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        internal static extern short d1000_write_outport(ushort node, ushort port, ushort value);

        // ---------------- 探测 ----------------

        /// <summary>
        /// 探测 LTDMC.dll 是否可用并且板卡初始化成功。
        /// 任何原生异常（缺 DLL / 缺符号 / 位数不匹配）都视为不可用。
        /// </summary>
        internal static bool Probe()
        {
            try
            {
                return d1000_board_init() > 0;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
            catch (BadImageFormatException) { return false; }
            catch (Exception) { return false; }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
