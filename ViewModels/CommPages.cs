// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System.Windows;
using System.Windows.Controls;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（CommPages）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_CommPages
    internal static class WenQiZhiTag_CommPages
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · CommPages。</summary>
        internal const string WenQiZhiFile = "CommPages";
    }

    // ==================================================================
    //  “通讯”工具条：SECS/GEM（HSMS-SS）通讯设定 + 实时报文监视。
    //  收发与连接由 ViewModels/SecsGemService.cs（免费开源库 Secs4Net）实现。
    // ==================================================================
    // 温启志 18719361399 · 类 public static class CommPages
    public static class CommPages
    {
        /// <summary>
        /// SECS/GEM 通讯页（供 Pages/secs_gem.xaml 经 vw:D Method="SecsGemPage" 调用）。
        /// 左列：连接设定 / 连接状态 / 发送报文；右列：实时报文日志。
        /// </summary>
        public static Element SecsGemPage()
        {
            var m = E.M;
            var form = Ops.PageForm("SECS/GEM 通讯", "sg_");

            // ---------------- 左：连接设定 ----------------
            var conn = E.Group("连接设定",
                E.Row("启用通讯", E.Chk("sg_enable", "开机自动连接", false), 100),
                E.Row("连接模式", E.Cmb("sg_mode", new[] { "被动 Passive", "主动 Active" }, 170), 100),
                E.Row("IP 地址", E.Text("sg_ip", "127.0.0.1", 170), 100),
                E.Row("端口", E.Num("sg_port", 5000, 120, "0", 1, 65535), 100),
                E.Row("Device ID", E.Num("sg_devid", 0, 120, "0", 0, 65535), 100),
                E.Row("T3 超时", E.Num("sg_t3", 45000, 120, "0", 1000, 120000), 100, "ms"),
                E.Chk("sg_autoreply", "自动应答（S1F1 / S1F13 / S1F3 / S2F41 / S6F11 …）", true));

            // ---------------- 左：连接状态 ----------------
            var status = E.Group("连接状态",
                E.H(
                    E.LiveLamp(_ => SecsGemService.IsConnected, false, true, 14),
                    E.SpacerW(6),
                    E.Live(_ => SecsGemService.StateText, null, 200, E.BandGrey, E.P, 12.5),
                    E.SpacerW(10),
                    E.Live(_ => SecsGemService.CountText, null, 190, E.BandGrey, E.P, 12)),
                E.Spacer(6),
                E.H(
                    E.Btn("连接", SecsGemService.Connect, "PrimaryButton", 12),
                    E.Btn("断开", SecsGemService.Disconnect, "PrimaryButton", 12),
                    E.MiniBtn("链路测试 S1F1", SecsGemService.LinkTest)));

            // ---------------- 左：发送报文 ----------------
            var send = E.Group("发送报文",
                E.Row("报文", E.Cmb("sg_msg", SecsGemService.MessageNames, 300), 56),
                E.Row("参数", E.Text("sg_arg", "", 300), 56),
                E.H(
                    E.Btn("发送", SecsGemService.SendSelected, "PrimaryButton", 12),
                    E.MiniBtn("S1F1", () => SecsGemService.SendQuick(0)),
                    E.MiniBtn("S1F13", () => SecsGemService.SendQuick(1)),
                    E.MiniBtn("S1F3", () => SecsGemService.SendQuick(2))));

            var left = E.V(
                conn, E.Spacer(8),
                status, E.Spacer(8),
                send, E.Spacer(10),
                E.H(E.MiniBtn("恢复", form.Restore), E.Btn("应用", form.Apply)));

            // ---------------- 右：实时报文日志 ----------------
            var logEl = E.Live(_ => SecsGemService.LogText, null, 860, E.CardBg, E.P, 11.5);
            logEl.Align = TextAlignment.Left;
            logEl.TextHAlign = HorizontalAlignment.Left;
            logEl.Wrap = TextWrapping.Wrap;

            var logBox = E.Group("报文日志（实时）",
                E.Scroll(logEl, 430, ScrollBarVisibility.Auto, ScrollBarVisibility.Auto),
                E.H(E.MiniBtn("清空日志", SecsGemService.ClearLog), E.MiniBtn("导出日志", SecsGemService.ExportLog)));

            return E.V(
                E.Grid().Cols("520", "*").Add(left, 0, 0).Add(logBox, 0, 1),
                E.Group("说明",
                    E.T("SECS/GEM：半导体设备与上位机 / EAP 之间的标准通讯协议（SEMI E4 SECS-I、E37 HSMS-SS、E5 SECS-II、E30 GEM）。" +
                        "本页使用免费开源库 Secs4Net（MIT 许可）实现：被动模式下本机作为 TCP 服务端等待上位机连接，" +
                        "主动模式下由本机连接上位机；连接进入「已选中（通讯就绪）」后即可收发报文。" +
                        "勾选自动应答时，收到 S1F1 / S1F13 / S1F3 / S1F11 / S1F15 / S1F17 / S2F33 / S2F35 / S2F37 / S2F41 / S6F11 会自动回标准应答。",
                        11.5, E.Sec, false, TextWrapping.Wrap)));
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
