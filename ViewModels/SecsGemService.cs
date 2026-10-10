// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Secs4Net;
using static Secs4Net.Item;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（SecsGemService）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_SecsGemService
    internal static class WenQiZhiTag_SecsGemService
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · SecsGemService。</summary>
        internal const string WenQiZhiFile = "SecsGemService";
    }

    // 温启志 18719361399 · 类 public static class SecsGemService
    /// <summary>
    /// SECS/GEM（HSMS-SS）通讯服务（温启志 18719361399）。
    ///
    /// 采用免费开源库 <b>Secs4Net</b>（MIT 许可，https://github.com/mkjeff/secs4net）实现
    /// SEMI E37（HSMS-SS）/ E5（SECS-II）/ E30（GEM）报文收发：
    ///   · 主动（Active）或被动（Passive）建立 HSMS-SS TCP 连接；
    ///   · 后台线程接收 Primary 报文并按需自动应答（S1F1/S1F13/S1F3/S2F41/S6F11 …）；
    ///   · 收发报文与链路状态写入环形日志，界面通过 LiveRegistry 每 500ms 轮询刷新。
    /// 连接参数全部来自 <see cref="MachineModel"/>（前缀 sg_），随参数文件自动保存/载入。
    /// </summary>
    public static class SecsGemService
    {
        private const int MaxLog = 200;        // 日志环形缓冲行数
        private const int MaxLineChars = 220;  // 单行截断长度（避免超长报文拖慢界面）

        private static readonly object _lock = new object();
        private static readonly List<string> _log = new List<string>();
        private static int _logVersion;
        private static string _logCache = "（尚未通讯）";
        private static int _logCacheVersion = -1;

        private static int _tx;
        private static int _rx;
        private static string _stateText = "未连接";
        private static bool _autoReply = true;

        private static HsmsConnection _hsms;
        private static SecsGem _gem;
        private static CancellationTokenSource _cts;
        private static GemLogger _logger;

        // ---------------- 只读状态（供界面轮询） ----------------

        /// <summary>HSMS 是否已进入 Selected 状态（此时才可收发数据报文）。</summary>
        public static bool IsConnected
        {
            get { var h = _hsms; return h != null && h.State == Secs4Net.ConnectionState.Selected; }
        }

        /// <summary>连接状态文字。</summary>
        public static string StateText { get { lock (_lock) { return _stateText; } } }

        /// <summary>已发送报文数。</summary>
        public static int TxCount { get { lock (_lock) { return _tx; } } }

        /// <summary>已接收报文数。</summary>
        public static int RxCount { get { lock (_lock) { return _rx; } } }

        /// <summary>收发计数文字（界面显示）。</summary>
        public static string CountText => "发送 " + TxCount + " / 接收 " + RxCount;

        /// <summary>实时报文日志全文（多行）。内容未变时返回同一引用，避免界面重复重排。</summary>
        public static string LogText
        {
            get
            {
                lock (_lock)
                {
                    if (_logCacheVersion != _logVersion)
                    {
                        _logCache = _log.Count == 0 ? "（尚未通讯）" : string.Join("\n", _log);
                        _logCacheVersion = _logVersion;
                    }
                    return _logCache;
                }
            }
        }

        /// <summary>可发送的常用报文清单（下拉框选项）。</summary>
        public static readonly string[] MessageNames =
        {
            "S1F1   Are You There",
            "S1F13  Establish Communications",
            "S1F3   Selected Equipment Status",
            "S1F11  Status Variable Namelist",
            "S1F17  Request ON-LINE",
            "S1F15  Request OFF-LINE",
            "S2F41  Host Command",
            "S2F33  Define Report",
            "S2F35  Link Event Report",
            "S2F37  Enable/Disable Event Report",
            "S6F11  Event Report Send",
            "S10F3  Terminal Display",
        };

        // ---------------- 连接 / 断开 ----------------

        /// <summary>按当前界面参数建立 HSMS-SS 连接（会先断开已有连接）。</summary>
        public static void Connect()
        {
            Disconnect(silent: true);

            var m = MachineModel.Instance;
            int mode = m.GetChoice("sg_mode", 0);               // 0=被动(Passive) 1=主动(Active)
            string ip = (m.GetText("sg_ip", "127.0.0.1") ?? "").Trim();
            int port = (int)m.GetNum("sg_port", 5000);
            int devid = (int)m.GetNum("sg_devid", 0);
            int t3 = (int)m.GetNum("sg_t3", 45000);
            _autoReply = m.GetToggle("sg_autoreply", true);

            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out _))
            {
                AddLog("警告", "IP 地址无效（" + ip + "），已回退 127.0.0.1");
                ip = "127.0.0.1";
            }
            if (port < 1 || port > 65535) { AddLog("警告", "端口越界（" + port + "），已回退 5000"); port = 5000; }

            try
            {
                var options = new SecsGemOptions
                {
                    DeviceId = (ushort)Math.Max(0, Math.Min(65535, devid)),
                    IsActive = mode == 1,
                    IpAddress = ip,
                    Port = port,
                    T3 = Math.Max(1000, t3),
                    T5 = 10000,
                    T6 = 5000,
                    T7 = 10000,
                    T8 = 5000,
                    LinkTestInterval = 60000,
                };
                var iopts = Options.Create(options);

                _logger = new GemLogger();
                _hsms = new HsmsConnection(iopts, _logger);
                _hsms.ConnectionChanged += OnConnectionChanged;
                _gem = new SecsGem(iopts, _hsms, _logger);

                _cts = new CancellationTokenSource();
                _hsms.Start(_cts.Token);
                var token = _cts.Token;
                _ = Task.Run(() => ReceiveLoopAsync(token), token);

                AddLog("系统", "启动 HSMS-SS（" + (mode == 1 ? "主动 Active" : "被动 Passive") + "） "
                             + ip + ":" + port + "  DeviceID=" + devid + "  T3=" + t3 + "ms");
                Ops.Info("SECS/GEM 已启动：" + (mode == 1 ? "主动" : "被动") + " " + ip + ":" + port);
            }
            catch (Exception ex)
            {
                SetStateText("启动失败");
                AddLog("错误", "启动失败：" + ex.Message);
                Ops.Warn("SECS/GEM 启动失败：" + ex.Message);
                SafeDispose();
            }
        }

        /// <summary>断开并释放 HSMS 连接。</summary>
        public static void Disconnect() => Disconnect(silent: false);

        private static void Disconnect(bool silent)
        {
            bool was = _hsms != null;
            try
            {
                _cts?.Cancel();
            }
            catch { /* 忽略 */ }

            var gem = _gem; _gem = null;
            if (gem != null) { try { gem.Dispose(); } catch { /* 忽略 */ } }

            var hsms = _hsms; _hsms = null;
            if (hsms != null)
            {
                // DisposeAsync 会关闭 socket / 停止解码管道；放后台线程避免阻塞界面。
                _ = Task.Run(async () => { try { await hsms.DisposeAsync().ConfigureAwait(false); } catch { /* 忽略 */ } });
            }

            try { _cts?.Dispose(); } catch { /* 忽略 */ } finally { _cts = null; }
            _logger = null;

            if (was && !silent)
            {
                SetStateText("未连接");
                AddLog("系统", "已断开 SECS/GEM 通讯");
                Ops.Info("SECS/GEM 已断开");
            }
        }

        private static void SafeDispose()
        {
            var gem = _gem; _gem = null;
            if (gem != null) { try { gem.Dispose(); } catch { /* 忽略 */ } }
            var hsms = _hsms; _hsms = null;
            if (hsms != null)
            {
                _ = Task.Run(async () => { try { await hsms.DisposeAsync().ConfigureAwait(false); } catch { /* 忽略 */ } });
            }
            try { _cts?.Dispose(); } catch { /* 忽略 */ } finally { _cts = null; }
        }

        /// <summary>开机自动连接（仅当界面「启用通讯」勾选时）。</summary>
        public static void TryAutoStart()
        {
            try
            {
                if (MachineModel.Instance.GetToggle("sg_enable", false)) Connect();
            }
            catch (Exception ex) { AddLog("错误", "自动连接失败：" + ex.Message); }
        }

        // ---------------- 发送 ----------------

        /// <summary>发送下拉框当前选中的报文。</summary>
        public static void SendSelected()
        {
            var m = MachineModel.Instance;
            Send(Build(m.GetChoice("sg_msg", 0), m.GetText("sg_arg", "")));
        }

        /// <summary>按序号直接发送一条常用报文（快捷按钮用）。</summary>
        public static void SendQuick(int idx) => Send(Build(idx, MachineModel.Instance.GetText("sg_arg", "")));

        /// <summary>链路测试：发送 S1F1（Are You There）。</summary>
        public static void LinkTest() => Send(Build(0, ""));

        private static void Send(SecsMessage msg)
        {
            if (msg == null) return;
            var gem = _gem;
            if (gem == null || !IsConnected)
            {
                Ops.Warn("SECS/GEM 未连接，无法发送报文");
                msg.Dispose();
                return;
            }
            Task.Run(async () =>
            {
                try
                {
                    var reply = await gem.SendAsync(msg).ConfigureAwait(false);
                    reply?.Dispose();
                }
                catch (Exception ex)
                {
                    AddLog("错误", "发送失败：" + ex.Message);
                }
                finally
                {
                    msg.Dispose();
                }
            });
        }

        // ---------------- 接收 / 自动应答 ----------------

        private static async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var e in _gem.GetPrimaryMessageAsync(ct).WithCancellation(ct).ConfigureAwait(false))
                {
                    try
                    {
                        var primary = e.PrimaryMessage;
                        if (!primary.ReplyExpected) continue;

                        SecsMessage reply = _autoReply ? AutoReply(primary) : null;
                        await e.TryReplyAsync(reply, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!ct.IsCancellationRequested) AddLog("错误", "应答失败：" + ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) { /* 正常断开 */ }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested) AddLog("错误", "接收循环结束：" + ex.Message);
            }
        }

        /// <summary>对收到的 Primary 报文生成默认 Secondary 应答（GEM 最小实现）。</summary>
        private static SecsMessage AutoReply(SecsMessage p)
        {
            switch (p.S)
            {
                case 1:
                    switch (p.F)
                    {
                        case 1:  return new SecsMessage(1, 2, false)  { Name = "Are You There Ack",           SecsItem = L(A("EXCEED9000"), A("2.0.23.709")) };
                        case 13: return new SecsMessage(1, 14, false) { Name = "EstablishComm Ack",          SecsItem = L(B(0), L(A("EXCEED9000"), A("2.0.23.709"))) };
                        case 3:  return new SecsMessage(1, 4, false)  { Name = "Selected Equipment Status",  SecsItem = L(A("SV1"), A("0")) };
                        case 11: return new SecsMessage(1, 12, false) { Name = "Status Variable Namelist",   SecsItem = L(L(U4(1), A("SV1"))) };
                        case 15: return new SecsMessage(1, 16, false) { Name = "Request OFF-LINE Ack",      SecsItem = L(B(0), L()) };
                        case 17: return new SecsMessage(1, 18, false) { Name = "Request ON-LINE Ack",       SecsItem = L(B(0), L()) };
                    }
                    break;
                case 2:
                    switch (p.F)
                    {
                        case 33: return new SecsMessage(2, 34, false) { Name = "Define Report Ack",         SecsItem = L(B(0), L()) };
                        case 35: return new SecsMessage(2, 36, false) { Name = "Link Event Report Ack",     SecsItem = L(B(0), L()) };
                        case 37: return new SecsMessage(2, 38, false) { Name = "Enable/Disable Event Ack",  SecsItem = L(B(0), L()) };
                        case 41: return new SecsMessage(2, 42, false) { Name = "Host Command Ack",          SecsItem = L(B(0), L(L(U1(0), L()))) };
                    }
                    break;
                case 6:
                    if (p.F == 11 || p.F == 12 || p.F == 15)
                        return new SecsMessage(6, 12, false) { Name = "Event Report Ack", SecsItem = L(B(0), L()) };
                    break;
            }
            return null; // 未知报文 → 由库自动回 S9F7
        }

        // ---------------- 报文构造 ----------------

        private static SecsMessage Build(int idx, string arg)
        {
            string a = string.IsNullOrEmpty(arg) ? "" : arg.Trim();
            switch (idx)
            {
                case 0:  return new SecsMessage(1, 1)  { Name = "Are You There" };
                case 1:  return new SecsMessage(1, 13) { Name = "Establish Communications", SecsItem = L(A("EXCEED9000"), A("2.0.23.709")) };
                case 2:  return new SecsMessage(1, 3)  { Name = "Selected Equipment Status", SecsItem = L(U4(1u), U4(2u), U4(3u), U4(4u), U4(5u)) };
                case 3:  return new SecsMessage(1, 11) { Name = "Status Variable Namelist", SecsItem = L(U4(1u), U4(2u), U4(3u), U4(4u), U4(5u)) };
                case 4:  return new SecsMessage(1, 17) { Name = "Request ON-LINE" };
                case 5:  return new SecsMessage(1, 15) { Name = "Request OFF-LINE" };
                case 6:  return new SecsMessage(2, 41) { Name = "Host Command", SecsItem = L(A(a.Length == 0 ? "START" : a), L(L(A(""), L()))) };
                case 7:  return new SecsMessage(2, 33) { Name = "Define Report", SecsItem = L(L(U4(1u), L(A("CEID1")))) };
                case 8:  return new SecsMessage(2, 35) { Name = "Link Event Report", SecsItem = L(U4(1u), L(L(U4(1u), L(U4(1u))))) };
                case 9:  return new SecsMessage(2, 37) { Name = "Enable/Disable Event Report", SecsItem = L(Boolean(true), L(U4(1u))) };
                case 10: return new SecsMessage(6, 11) { Name = "Event Report Send", SecsItem = L(U4(1u), U4(1u), L(L(U4(1u), L(U4(0u))))) };
                case 11: return new SecsMessage(10, 3) { Name = "Terminal Display", SecsItem = L(A(a.Length == 0 ? "EXCEED9000 Handler Ready" : a)) };
                default: return new SecsMessage(1, 1)  { Name = "Are You There" };
            }
        }

        // ---------------- 日志 ----------------

        /// <summary>清空报文日志（保留计数）。</summary>
        public static void ClearLog()
        {
            lock (_lock) { _log.Clear(); _logVersion++; }
            AddLog("系统", "报文日志已清空");
        }

        /// <summary>导出报文日志到文本文件（通过视图注入的保存对话框服务）。</summary>
        public static void ExportLog()
        {
            try
            {
                string def = "secsgem_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                string path = MainViewModel.Current?.PickSaveFile?.Invoke("导出 SECS/GEM 报文日志", "文本文件 (*.txt)|*.txt", def);
                if (string.IsNullOrEmpty(path)) return;
                System.IO.File.WriteAllText(path, LogText, Encoding.UTF8);
                Ops.Info("已导出 SECS/GEM 报文日志：" + path);
            }
            catch (Exception ex)
            {
                Ops.Warn("导出 SECS/GEM 报文日志失败：" + ex.Message);
            }
        }

        private static void OnConnectionChanged(object sender, Secs4Net.ConnectionState state) => SetState(state);

        private static void SetState(Secs4Net.ConnectionState state)
        {
            string t;
            switch (state)
            {
                case Secs4Net.ConnectionState.Connecting: t = "连接中…"; break;
                case Secs4Net.ConnectionState.Connected:  t = "已连接（等待选中）"; break;
                case Secs4Net.ConnectionState.Selected:   t = "已选中（通讯就绪）"; break;
                case Secs4Net.ConnectionState.Retry:      t = "重连中…"; break;
                default: t = state.ToString(); break;
            }
            SetStateText(t);
            AddLog("状态", t);
        }

        private static void SetStateText(string t) { lock (_lock) { _stateText = t; } }

        private static void AddLog(string level, string text)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + text;
            Push(line);
        }

        private static void Push(string line)
        {
            if (line != null && line.Length > MaxLineChars) line = line.Substring(0, MaxLineChars) + " …";
            lock (_lock)
            {
                _log.Add(line ?? "");
                if (_log.Count > MaxLog) _log.RemoveRange(0, _log.Count - MaxLog);
                _logVersion++;
            }
        }

        /// <summary>由 <see cref="GemLogger"/> 回调：记录一条收/发报文。</summary>
        internal static void LogMessage(string dir, SecsMessage msg)
        {
            if (msg == null) return;
            lock (_lock) { if (dir == "→") _tx++; else _rx++; }
            string body = msg.SecsItem != null ? "  " + DumpItem(msg.SecsItem) : "";
            Push(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + dir + "  " + msg + body);
        }

        /// <summary>把 Item 递归转成 SML 风格字符串（供日志显示）。</summary>
        private static string DumpItem(Item it)
        {
            if (it == null) return "";
            if (it.Format == SecsFormat.List)
            {
                var parts = new List<string>();
                var children = it.Items;
                if (children != null)
                    foreach (var c in children) parts.Add(DumpItem(c));
                return "L[" + it.Count + "]{ " + string.Join(", ", parts) + " }";
            }
            if (it.Format == SecsFormat.ASCII || it.Format == SecsFormat.JIS8)
            {
                string s;
                try { s = it.GetString(); } catch { s = ""; }
                return "A \"" + s + "\"";
            }
            return it.Format + "[" + it.Count + "]";
        }

        // ---------------- 库日志适配 ----------------

        // 温启志 18719361399 · 类 private sealed class GemLogger : ISecsGemLogger
        /// <summary>把 Secs4Net 的收发报文与链路日志接到本服务的环形日志。</summary>
        private sealed class GemLogger : ISecsGemLogger
        {
            public void MessageIn(SecsMessage msg, int id) => LogMessage("←", msg);
            public void MessageOut(SecsMessage msg, int id) => LogMessage("→", msg);
            public void Debug(string msg) { /* 忽略调试噪音 */ }
            public void Info(string msg) => AddLog("信息", msg);
            public void Warning(string msg) => AddLog("警告", msg);
            public void Error(string msg) => AddLog("错误", msg);
            public void Error(string msg, Exception ex) => AddLog("错误", msg + "：" + (ex?.Message ?? ""));
            public void Error(string msg, SecsMessage message, Exception ex) => AddLog("错误", msg);
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
