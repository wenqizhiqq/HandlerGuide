// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  测试机通讯（“测试接口”页）：TTL/GPIO、GPIB、RS232、TCP 四种通道的**真实**收发。
//    · RS232 → System.IO.Ports.SerialPort（真实串口）
//    · TCP   → TcpClient / TcpListener（真实 Socket，默认本机为客户端主动连测试机）
//    · TTL   → 运控卡 DIO 位（MotionManager；插真卡时直通硬件，无卡时为内置仿真卡）
//    · GPIB  → VISA（visa32.dll）优先，缺失时退回 NI-488.2（gpib-32.dll）
//  连接参数取自“测试接口”页的 ti_* 模型键，随 params.json 持久化。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（TestInterfaceService）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_TestInterface
    internal static class WenQiZhiTag_TestInterface
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · TestInterfaceService。</summary>
        internal const string WenQiZhiFile = "TestInterfaceService";
    }

    // ==================================================================
    //  测试机通讯服务：真实连接 / 收发 / 日志。
    //  由“测试接口”页（品种工具条）与“测试状态反馈”诊断页共同使用。
    // ==================================================================
    // 温启志 18719361399 · 类 public static class TestInterfaceService
    public static class TestInterfaceService
    {
        // ---------------- 接口方式 ----------------
        public const int WayTtl = 0;
        public const int WayGpib = 1;
        public const int WayRs232 = 2;
        public const int WayTcp = 3;

        static readonly string[] WayNames = { "TTL", "GPIB", "RS232", "TCP" };
        static readonly int[] BaudRates = { 9600, 19200, 38400, 57600, 115200 };

        const int MaxLog = 400;
        const int MaxLine = 220;

        // ---------------- 运行状态 ----------------
        static readonly object _lock = new object();
        static ITransport _tp;
        static string _state = "未连接";
        static int _tx, _rx;
        static string _lastRx = "";
        static readonly List<string> _log = new List<string>();
        static int _logVersion;
        static string _logCache;
        static int _logCacheVersion = -1;

        static MachineModel M => MachineModel.Instance;

        /// <summary>当前接口方式（0=TTL 1=GPIB 2=RS232 3=TCP）。</summary>
        public static int Way
        {
            get
            {
                int v = M.GetChoice("ti_type", 0);
                return v < 0 ? 0 : (v > 3 ? 3 : v);
            }
        }

        /// <summary>当前接口方式名称。</summary>
        public static string WayName => WayNames[Way];

        /// <summary>是否已建立真实连接。</summary>
        public static bool IsConnected { get { lock (_lock) return _tp != null; } }

        /// <summary>连接状态文本（供实时徽标显示）。</summary>
        public static string StateText { get { lock (_lock) return _state; } }

        /// <summary>收发计数文本。</summary>
        public static string CountText { get { lock (_lock) return "发送 " + _tx + " 帧 / 接收 " + _rx + " 帧"; } }

        /// <summary>最后一帧接收内容（原始文本）。</summary>
        public static string LastRx { get { lock (_lock) return _lastRx; } }

        /// <summary>
        /// 实时收发日志（多行）。内容未变化时返回同一字符串引用，
        /// 避免 LiveElement 每 500ms 轮询都触发一次重排。
        /// </summary>
        public static string LogText
        {
            get
            {
                lock (_lock)
                {
                    if (_logCache != null && _logCacheVersion == _logVersion) return _logCache;
                    _logCache = _log.Count == 0 ? "（尚无报文）" : string.Join("\n", _log);
                    _logCacheVersion = _logVersion;
                    return _logCache;
                }
            }
        }

        /// <summary>当前接口参数摘要（用于状态栏 / 说明）。</summary>
        public static string Describe()
        {
            switch (Way)
            {
                case WayTcp:
                    return "TCP " + (M.GetChoice("ti_role", 0) == 0 ? "客户端" : "服务端")
                         + " " + M.GetText("ti_ip", "127.0.0.1") + ":" + M.GetNum("ti_port", 5001).ToString("0");
                case WayRs232:
                    return "RS232 " + SerialPortName()
                         + " " + BaudRates[Clamp(M.GetChoice("ti_baud", 0), BaudRates.Length - 1)]
                         + "," + (M.GetChoice("ti_data", 0) == 0 ? 7 : 8)
                         + "," + (M.GetChoice("ti_stop", 0) == 0 ? 1 : 2)
                         + "," + new[] { "None", "Odd", "Even" }[Clamp(M.GetChoice("ti_parity", 0), 2)];
                case WayGpib:
                    return "GPIB " + VisaResource();
                default:
                    return "TTL/DIO " + MotionManager.Instance.CardName
                         + " SOT=OUT" + M.GetNum("ti_sotbit", 0).ToString("0")
                         + " EOT=IN" + M.GetNum("ti_eotbit", 1).ToString("0")
                         + " DUT=IN" + M.GetNum("ti_dutbit", 2).ToString("0");
            }
        }

        /// <summary>本机可用串口列表（找不到时补 COM1~COM8，保证下拉框永远可用）。</summary>
        public static string[] AvailablePorts()
        {
            var list = new List<string>();
            try { list.AddRange(SerialPort.GetPortNames()); } catch { /* 无串口子系统 */ }
            for (int i = 1; i <= 8; i++)
            {
                string n = "COM" + i;
                if (!list.Contains(n)) list.Add(n);
            }
            return list.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        // ================== 连接 / 断开 ==================

        /// <summary>按当前接口方式建立真实连接（后台线程执行，避免阻塞界面）。</summary>
        public static void Connect()
        {
            if (IsConnected) { Ops.Warn("测试机通讯已连接（" + WayName + "），无需重复连接"); return; }

            int way = Way;
            SetState("正在连接 " + WayNames[way] + " …");
            AddLog("开始连接：" + Describe());

            var th = new Thread(() =>
            {
                ITransport tp = null;
                try
                {
                    tp = Build(way);
                    tp.Open(OnRx, OnClosed);
                    lock (_lock) { _tp = tp; }
                    SetState("已连接：" + tp.Info);
                    AddLog("连接成功：" + tp.Info);
                    UiInfo("测试机通讯已连接（" + tp.Info + "）");
                }
                catch (Exception ex)
                {
                    try { tp?.Close(); } catch { /* 忽略 */ }
                    lock (_lock) { _tp = null; }
                    SetState("连接失败：" + ex.Message);
                    AddLog("连接失败：" + ex.Message);
                    UiWarn("测试机通讯连接失败（" + WayNames[way] + "）：" + ex.Message);
                }
            })
            { IsBackground = true, Name = "TestIo-Connect" };
            th.Start();
        }

        /// <summary>断开当前连接。</summary>
        public static void Disconnect()
        {
            ITransport tp;
            lock (_lock) { tp = _tp; _tp = null; }
            if (tp == null) { Ops.Warn("测试机通讯未连接"); return; }

            string info = tp.Info;
            try { tp.Close(); } catch { /* 忽略 */ }
            SetState("未连接");
            AddLog("已断开：" + info);
            Ops.Info("测试机通讯已断开（" + info + "）");
        }

        static ITransport Build(int way)
        {
            int timeout = (int)Math.Max(200, M.GetNum("ti_timeout", 5000));
            switch (way)
            {
                case WayTcp:
                    return new TcpTransport(
                        M.GetText("ti_ip", "127.0.0.1"),
                        (int)Math.Max(1, M.GetNum("ti_port", 5001)),
                        M.GetChoice("ti_role", 0) == 1,
                        timeout);

                case WayRs232:
                    return new SerialTransport(
                        SerialPortName(),
                        BaudRates[Clamp(M.GetChoice("ti_baud", 0), BaudRates.Length - 1)],
                        M.GetChoice("ti_data", 0) == 0 ? 7 : 8,
                        M.GetChoice("ti_stop", 0) == 0 ? StopBits.One : StopBits.Two,
                        new[] { Parity.None, Parity.Odd, Parity.Even }[Clamp(M.GetChoice("ti_parity", 0), 2)],
                        timeout);

                case WayGpib:
                    return new GpibTransport(
                        (int)Math.Max(0, M.GetNum("ti_gpib", 11)),
                        timeout);

                default:
                    return new TtlTransport(
                        (int)M.GetNum("ti_sotbit", 0),
                        (int)M.GetNum("ti_eotbit", 1),
                        (int)M.GetNum("ti_dutbit", 2));
            }
        }

        static string SerialPortName()
        {
            var ports = AvailablePorts();
            int idx = Clamp(M.GetChoice("ti_com", 0), ports.Length - 1);
            return ports.Length == 0 ? "COM1" : ports[idx];
        }

        /// <summary>GPIB 资源串（VISA 命名规则：GPIB0::地址::INSTR）。</summary>
        static string VisaResource()
        {
            return "GPIB0::" + M.GetNum("ti_gpib", 11).ToString("0") + "::INSTR";
        }

        static int Clamp(int v, int max) { return v < 0 ? 0 : (v > max ? max : v); }

        // ================== 发送 ==================

        /// <summary>按当前接口方式把文本发出去（TCP / RS232 / GPIB 走报文，TTL 走 SOT 脉冲）。</summary>
        public static void Send(string msg)
        {
            if (string.IsNullOrEmpty(msg)) { Ops.Warn("发送内容为空"); return; }

            ITransport tp;
            lock (_lock) { tp = _tp; }
            if (tp == null)
            {
                AddLog("发送被拒（未连接）：" + msg);
                Ops.Warn("测试机通讯未连接，请先在「测试接口」页点「连接」");
                return;
            }

            try
            {
                tp.Send(Frame(msg));
                LogFrame("→", Frame(msg));
                lock (_lock) { _tx++; }
                UiStatus();
            }
            catch (Exception ex)
            {
                AddLog("发送失败：" + ex.Message);
                Ops.Warn("测试机通讯发送失败：" + ex.Message);
            }
        }

        /// <summary>[SOT] 开始测试信号：TTL 打 SOT 位脉冲，其余通道发 SOT 报文。</summary>
        public static void SendSot()
        {
            if (Way == WayTtl)
            {
                // TTL 通道由运控卡 SOT 位承担；仍走既有 40ms 脉冲链路，保证与手册一致
                Ops.TestIo_SotPulse();
                return;
            }
            Send("SOT");
        }

        /// <summary>[TestInfo] 读取测试系统信息：GPIB/TCP/RS232 发查询并记录应答。</summary>
        public static void RequestTestInfo()
        {
            string info = "测试系统信息（" + WayName + "）\n"
                        + "  接口方式 : " + WayName + "\n"
                        + "  通道参数 : " + Describe() + "\n"
                        + "  超时设定 : " + M.GetNum("ti_timeout", 5000).ToString("0") + " ms\n"
                        + "  开测延时 : " + M.GetNum("ti_sotdelay", 10).ToString("0") + " ms\n"
                        + "  连接状态 : " + (IsConnected ? "已连接" : "未连接");
            AddLog(info);

            if (!IsConnected) { Ops.Warn("测试机通讯未连接，仅显示本地接口信息"); return; }

            // GPIB / 串口惯用 *IDN? 查询；TCP 用测试机约定的 TESTINFO
            Send(Way == WayGpib || Way == WayRs232 ? "*IDN?" : "TESTINFO");
        }

        // ================== 日志 ==================

        /// <summary>清空收发日志。</summary>
        public static void ClearLog()
        {
            lock (_lock) { _log.Clear(); _logVersion++; }
            Ops.Info("测试接口日志已清空");
        }

        /// <summary>导出收发日志到文本文件。</summary>
        public static void ExportLog()
        {
            string text = LogText;
            string def = "testio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            string path = MainViewModel.Current?.PickSaveFile?.Invoke("导出测试接口日志", "文本文件 (*.txt)|*.txt", def);
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                File.WriteAllText(path, text, new UTF8Encoding(true));
                Ops.Info("已导出测试接口日志：" + path);
            }
            catch (Exception ex)
            {
                Ops.Warn("导出测试接口日志失败：" + ex.Message);
            }
        }

        /// <summary>报文结束符（0=CR+LF 1=CR 2=LF 3=无）。</summary>
        static byte[] Frame(string msg)
        {
            string tail;
            switch (M.GetChoice("ti_delim", 0))
            {
                case 1: tail = "\r"; break;
                case 2: tail = "\n"; break;
                case 3: tail = ""; break;
                default: tail = "\r\n"; break;
            }
            return Encoding.ASCII.GetBytes(msg + tail);
        }

        /// <summary>记录一帧收发（带方向、接口名、可读文本与十六进制）。</summary>
        static void LogFrame(string dir, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            string line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + dir + " " + WayName + " : \""
                        + Dump(data) + "\"  [" + Hex(data) + "]";
            if (line.Length > MaxLine) line = line.Substring(0, MaxLine) + " …";
            AddLog(line);
        }

        internal static void AddLog(string line)
        {
            lock (_lock)
            {
                _log.Insert(0, line);
                while (_log.Count > MaxLog) _log.RemoveAt(_log.Count - 1);
                _logVersion++;
            }
        }

        /// <summary>可读文本（不可打印字符转义）。</summary>
        static string Dump(byte[] d)
        {
            var sb = new StringBuilder();
            foreach (var b in d)
            {
                if (b == 13) sb.Append("\\r");
                else if (b == 10) sb.Append("\\n");
                else if (b == 9) sb.Append("\\t");
                else if (b >= 32 && b < 127) sb.Append((char)b);
                else sb.Append("\\x").Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>十六进制（最多显示 24 字节）。</summary>
        static string Hex(byte[] d)
        {
            int n = Math.Min(d.Length, 24);
            string s = string.Join(" ", d.Take(n).Select(b => b.ToString("X2")));
            return d.Length > n ? s + " …" : s;
        }

        // ================== 传输层回调 ==================

        static void OnRx(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            lock (_lock)
            {
                _rx++;
                _lastRx = Dump(data);
            }
            LogFrame("←", data);
            UiStatus();
        }

        static void OnClosed(ITransport tp)
        {
            bool mine;
            lock (_lock)
            {
                mine = ReferenceEquals(_tp, tp);
                if (mine) _tp = null;
            }
            if (!mine) return;
            SetState("链路已断开");
            AddLog("链路已断开：" + tp.Info);
            UiWarn("测试机通讯链路已断开：" + tp.Info);
        }

        static void SetState(string s) { lock (_lock) { _state = s; } }

        // ================== 线程安全地回调界面 ==================

        static void Post(Action a)
        {
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d == null) { try { a(); } catch { /* 忽略 */ } return; }
            if (d.CheckAccess()) { try { a(); } catch { /* 忽略 */ } }
            else d.BeginInvoke(a);
        }

        static void UiInfo(string msg) { Post(() => Ops.Info(msg)); }
        static void UiWarn(string msg) { Post(() => Ops.Warn(msg)); }
        static void UiStatus() { Post(() => Ops.RaiseStatus()); }

        // ==================================================================
        //  传输层
        // ==================================================================

        // 温启志 18719361399 · 接口 private interface ITransport
        interface ITransport
        {
            /// <summary>通道描述（显示在状态栏）。</summary>
            string Info { get; }

            /// <summary>打开通道并启动接收线程。</summary>
            void Open(Action<byte[]> onRx, Action<ITransport> onClosed);

            /// <summary>发送一帧原始字节。</summary>
            void Send(byte[] data);

            /// <summary>关闭通道。</summary>
            void Close();
        }

        // ---------------- TCP（客户端 / 服务端） ----------------
        sealed class TcpTransport : ITransport
        {
            readonly string _ip;
            readonly int _port;
            readonly bool _server;
            readonly int _timeout;
            TcpClient _cli;
            TcpListener _lsn;
            NetworkStream _ns;
            Thread _rx;
            volatile bool _run;

            public TcpTransport(string ip, int port, bool server, int timeout)
            {
                _ip = ip; _port = port; _server = server; _timeout = timeout;
            }

            public string Info => (_server ? "TCP 服务端 0.0.0.0:" : "TCP 客户端 ")
                                + _ip + ":" + _port;

            public void Open(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                if (_server)
                {
                    _lsn = new TcpListener(IPAddress.Any, _port);
                    _lsn.Start();
                    var accept = _lsn.AcceptTcpClientAsync();
                    if (!accept.Wait(_timeout))
                    {
                        try { _lsn.Stop(); } catch { /* 忽略 */ }
                        throw new TimeoutException("等待测试机连入 " + _port + " 端口超时（" + _timeout + " ms）");
                    }
                    _cli = accept.Result;
                }
                else
                {
                    _cli = new TcpClient();
                    var conn = _cli.ConnectAsync(_ip, _port);
                    if (!conn.Wait(_timeout))
                    {
                        try { _cli.Close(); } catch { /* 忽略 */ }
                        throw new TimeoutException("连接 " + _ip + ":" + _port + " 超时（" + _timeout + " ms）");
                    }
                    if (conn.IsFaulted)
                        throw new IOException("连接 " + _ip + ":" + _port + " 失败：" + (conn.Exception?.InnerException?.Message ?? "未知错误"));
                }

                _cli.NoDelay = true;
                _ns = _cli.GetStream();
                _run = true;
                _rx = new Thread(() => RxLoop(onRx, onClosed)) { IsBackground = true, Name = "TestIo-TcpRx" };
                _rx.Start();
            }

            void RxLoop(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                var buf = new byte[4096];
                try
                {
                    while (_run && _ns != null)
                    {
                        int n = _ns.Read(buf, 0, buf.Length);
                        if (n <= 0) break;
                        onRx(buf.Take(n).ToArray());
                    }
                }
                catch { /* 链路异常按断开处理 */ }
                finally { if (_run) onClosed(this); }
            }

            public void Send(byte[] d)
            {
                if (_ns == null) throw new IOException("TCP 通道未打开");
                _ns.Write(d, 0, d.Length);
                _ns.Flush();
            }

            public void Close()
            {
                _run = false;
                try { _ns?.Close(); } catch { /* 忽略 */ }
                try { _cli?.Close(); } catch { /* 忽略 */ }
                try { _lsn?.Stop(); } catch { /* 忽略 */ }
                _ns = null;
            }
        }

        // ---------------- RS232（真实串口） ----------------
        sealed class SerialTransport : ITransport
        {
            readonly SerialPort _sp;
            Thread _rx;
            volatile bool _run;

            public SerialTransport(string port, int baud, int dataBits, StopBits stop, Parity parity, int timeout)
            {
                _sp = new SerialPort(port, baud, parity, dataBits, stop)
                {
                    ReadTimeout = Math.Max(200, timeout),
                    WriteTimeout = Math.Max(200, timeout)
                };
            }

            public string Info => "RS232 " + _sp.PortName + " " + _sp.BaudRate + ","
                                + _sp.DataBits + "," + _sp.StopBits + "," + _sp.Parity;

            public void Open(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                _sp.Open();
                _sp.DiscardInBuffer();
                _run = true;
                _rx = new Thread(() => RxLoop(onRx, onClosed)) { IsBackground = true, Name = "TestIo-SerialRx" };
                _rx.Start();
            }

            void RxLoop(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                var buf = new byte[4096];
                try
                {
                    while (_run)
                    {
                        try
                        {
                            int n = _sp.Read(buf, 0, buf.Length);
                            if (n > 0) onRx(buf.Take(n).ToArray());
                        }
                        catch (TimeoutException) { /* 正常：等待期间无数据 */ }
                    }
                }
                catch { /* 串口被拔掉等按断开处理 */ }
                finally { if (_run) onClosed(this); }
            }

            public void Send(byte[] d)
            {
                if (!_sp.IsOpen) throw new IOException("串口 " + _sp.PortName + " 未打开");
                _sp.Write(d, 0, d.Length);
            }

            public void Close()
            {
                _run = false;
                try { if (_sp.IsOpen) _sp.Close(); } catch { /* 忽略 */ }
            }
        }

        // ---------------- TTL / DIO（运控卡 IO 位） ----------------
        sealed class TtlTransport : ITransport
        {
            readonly int _sotBit, _eotBit, _dutBit;
            Thread _poll;
            volatile bool _run;
            bool _eotLast, _dutLast;

            public TtlTransport(int sotBit, int eotBit, int dutBit)
            {
                _sotBit = sotBit; _eotBit = eotBit; _dutBit = dutBit;
            }

            public string Info => "TTL/DIO " + MotionManager.Instance.CardName
                                + " · SOT=OUT" + _sotBit + " EOT=IN" + _eotBit + " DUT=IN" + _dutBit;

            public void Open(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                var mm = MotionManager.Instance;
                _eotLast = mm.ReadIn(_eotBit);
                _dutLast = mm.ReadIn(_dutBit);
                _run = true;
                _poll = new Thread(() => Poll(onRx, onClosed)) { IsBackground = true, Name = "TestIo-TtlPoll" };
                _poll.Start();
            }

            void Poll(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                var mm = MotionManager.Instance;
                try
                {
                    while (_run)
                    {
                        bool eot = mm.ReadIn(_eotBit);
                        if (eot != _eotLast)
                        {
                            _eotLast = eot;
                            onRx(Encoding.ASCII.GetBytes("EOT=" + (eot ? "1" : "0")));
                        }
                        bool dut = mm.ReadIn(_dutBit);
                        if (dut != _dutLast)
                        {
                            _dutLast = dut;
                            onRx(Encoding.ASCII.GetBytes("DUT=" + (dut ? "1" : "0")));
                        }
                        Thread.Sleep(20);
                    }
                }
                catch { /* 运控卡异常按断开处理 */ }
                finally { if (_run) onClosed(this); }
            }

            public void Send(byte[] d)
            {
                // TTL 侧没有“报文”概念：把 SOT 位打一个 40ms 脉冲（与手册一致）
                var mm = MotionManager.Instance;
                mm.WriteOut(_sotBit, true);
                Thread.Sleep(40);
                mm.WriteOut(_sotBit, false);
            }

            public void Close()
            {
                _run = false;
                try { MotionManager.Instance.WriteOut(_sotBit, false); } catch { /* 忽略 */ }
            }
        }

        // ---------------- GPIB（VISA 优先，NI-488.2 兜底） ----------------
        sealed class GpibTransport : ITransport
        {
            readonly int _addr;
            readonly int _timeout;
            Thread _rx;
            volatile bool _run;

            int _rm;            // VISA 资源管理器
            int _vi;            // VISA 会话
            bool _visa;         // 是否走 VISA
            int _ud;            // NI-488.2 设备描述符

            public GpibTransport(int addr, int timeout) { _addr = addr; _timeout = timeout; }

            public string Info => "GPIB " + VisaResource();

            public void Open(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                string res = VisaResource();
                Exception visaErr = null;

                // ① VISA（NI-VISA / Keysight IO Libraries 都提供 visa32.dll）
                try
                {
                    int st = Visa.viOpenDefaultRM(out _rm);
                    if (st < 0) throw new IOException("viOpenDefaultRM 失败（0x" + st.ToString("X8") + "）");
                    st = Visa.viOpen(_rm, res, 0, _timeout, out _vi);
                    if (st < 0) throw new IOException("viOpen(" + res + ") 失败（0x" + st.ToString("X8") + "）");
                    Visa.viSetAttribute(_vi, Visa.ViAttrTmoValue, _timeout);
                    _visa = true;
                }
                catch (Exception ex)
                {
                    visaErr = ex;
                    _visa = false;
                }

                // ② 退回 NI-488.2（gpib-32.dll）
                if (!_visa)
                {
                    try
                    {
                        _ud = Ni488.ibdev(0, _addr, 0, 11 /*T10s*/, 1 /*EOT*/, 0 /*EOS*/);
                        if (_ud < 0) throw new IOException("ibdev 失败（GPIB 地址 " + _addr + "）");
                    }
                    catch (Exception ex)
                    {
                        throw new IOException(
                            "GPIB 不可用：VISA 失败（" + (visaErr?.Message ?? "visa32.dll 缺失") + "），"
                            + "NI-488.2 失败（" + ex.Message + "）。请安装 NI-VISA / Keysight IO Libraries 或 NI-488.2。");
                    }
                }

                _run = true;
                _rx = new Thread(() => RxLoop(onRx, onClosed)) { IsBackground = true, Name = "TestIo-GpibRx" };
                _rx.Start();
            }

            void RxLoop(Action<byte[]> onRx, Action<ITransport> onClosed)
            {
                var buf = new byte[4096];
                try
                {
                    while (_run)
                    {
                        if (_visa)
                        {
                            int st = Visa.viRead(_vi, buf, buf.Length, out int got);
                            if (got > 0) onRx(buf.Take(got).ToArray());
                            else if (st < 0 && st != Visa.ViErrorTmo) break;   // 超时属正常空读
                        }
                        else
                        {
                            int st = Ni488.ibrd(_ud, buf, buf.Length);
                            int got = Ni488.ThreadIbcntl();
                            if (got > 0) onRx(buf.Take(Math.Min(got, buf.Length)).ToArray());
                            else if ((st & 0x4000) != 0) break;                 // ERR 位置位
                        }
                    }
                }
                catch { /* GPIB 异常按断开处理 */ }
                finally { if (_run) onClosed(this); }
            }

            public void Send(byte[] d)
            {
                if (_visa)
                {
                    int st = Visa.viWrite(_vi, d, d.Length, out int wrote);
                    if (st < 0) throw new IOException("viWrite 失败（0x" + st.ToString("X8") + "）");
                }
                else
                {
                    int st = Ni488.ibwrt(_ud, d, d.Length);
                    if ((st & 0x8000) != 0) throw new IOException("ibwrt 失败（status 0x" + st.ToString("X4") + "）");
                }
            }

            public void Close()
            {
                _run = false;
                try { if (_visa && _vi != 0) Visa.viClose(_vi); } catch { /* 忽略 */ }
                try { if (_visa && _rm != 0) Visa.viClose(_rm); } catch { /* 忽略 */ }
                _vi = 0; _rm = 0;
                try { if (!_visa && _ud >= 0) Ni488.ibonl(_ud, 0); } catch { /* 忽略 */ }
            }
        }

        // ---------------- VISA（visa32.dll）互操作 ----------------
        static class Visa
        {
            const string Dll = "visa32.dll";

            public const int ViAttrTmoValue = 0x3FFF001A;
            public const int ViErrorTmo = unchecked((int)0xBFFF0015);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int viOpenDefaultRM(out int sesn);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
            public static extern int viOpen(int sesn, string name, int accessMode, int timeout, out int vi);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int viClose(int vi);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int viSetAttribute(int vi, int attr, int value);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int viWrite(int vi, byte[] buf, int count, out int retCount);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int viRead(int vi, byte[] buf, int count, out int retCount);
        }

        // ---------------- NI-488.2（gpib-32.dll）互操作 ----------------
        static class Ni488
        {
            const string Dll = "gpib-32.dll";

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int ibdev(int boardID, int pad, int sad, int tmo, int eot, int eos);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int ibwrt(int ud, byte[] buf, int cnt);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int ibrd(int ud, byte[] buf, int cnt);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
            public static extern int ibonl(int ud, int online);

            [DllImport(Dll, CallingConvention = CallingConvention.StdCall, EntryPoint = "ThreadIbcntl")]
            public static extern int ThreadIbcntl();
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
