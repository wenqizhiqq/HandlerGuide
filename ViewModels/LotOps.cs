// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  LotOps.cs — 批次(Lot)管理 + 运行统计 + 批次记录检索
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件界面复刻）
//
//  业务依据（不是凭空想象，全部来自设备资料）：
//   · 《ASTI Hexa Maxx System Manual》Chapter 3 - Machine Operation
//      3.1.7 Lot Status（Lot Number / Lot Size / Operator ID / Shift / Package / Start Date&Time）
//      3.1.8 Run Time Data（Gross PPH / Nett PPH / MTBA / Lot Time / Run Time / Jam Time / Stop Time）
//      3.2.3 View Lot Info（按日期 / 批号 / 操作员 / 班别 / 封装检索批次记录）
//   · 《ASTI Hexa Maxx System Manual》Chapter 4 - Operator's Menu
//      4.1 Open Lot；4.2 Operator Access（Abort Lot / Purge Tray(s) / Change Carrier /
//      Rescan Mode / Print Lot Summary / KGD Tray Sort / OTF Homing）
//      4.3 Extra Control（View Event Log / View Error Log / Auto Printing）
//   · 天津金海通 EXCEED-9000 系列：最多 32 工位同测、6 自动盘 + 3 手动盘输出、
//     控温 25℃±2℃、接触力 240kgf、Tester 接口 TTL / GPIB / RS232 / Network。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LotOps）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_LotOps
    internal static class WenQiZhiTag_LotOps
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · LotOps。</summary>
        internal const string WenQiZhiFile = "LotOps";
    }

    /// <summary>一条批次记录（结批时落盘，供《查看批次记录》检索）。</summary>
    // 温启志 18719361399 · 类 public class LotRecord
    public class LotRecord
    {
        public string LotNo { get; set; } = "";
        public string SubLotNo { get; set; } = "";
        public string Customer { get; set; } = "";
        public string OperatorId { get; set; } = "";
        public string Shift { get; set; } = "";
        public string Package { get; set; } = "";
        public string OutputType { get; set; } = "Tray";
        public int LotSize { get; set; }
        public int Total { get; set; }
        public int Good { get; set; }
        public int Bad { get; set; }
        public double Yield { get; set; }
        public double GrossPPH { get; set; }
        public double NettPPH { get; set; }
        public double MtbaMin { get; set; }
        public double LotTimeMin { get; set; }
        public double RunTimeMin { get; set; }
        public double JamTimeMin { get; set; }
        public double StopTimeMin { get; set; }
        public int JamCount { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        /// <summary>转成一行文本（写入 .crd 汇总文件用）。</summary>
        public string ToCsv()
        {
            return string.Join(",", new[]
            {
                Csv(LotNo), Csv(SubLotNo), Csv(Customer), Csv(OperatorId), Csv(Shift), Csv(Package),
                Csv(OutputType), LotSize.ToString(CultureInfo.InvariantCulture),
                Total.ToString(CultureInfo.InvariantCulture),
                Good.ToString(CultureInfo.InvariantCulture),
                Bad.ToString(CultureInfo.InvariantCulture),
                Yield.ToString("0.00", CultureInfo.InvariantCulture),
                GrossPPH.ToString("0", CultureInfo.InvariantCulture),
                NettPPH.ToString("0", CultureInfo.InvariantCulture),
                MtbaMin.ToString("0.0", CultureInfo.InvariantCulture),
                LotTimeMin.ToString("0.0", CultureInfo.InvariantCulture),
                RunTimeMin.ToString("0.0", CultureInfo.InvariantCulture),
                JamTimeMin.ToString("0.0", CultureInfo.InvariantCulture),
                StopTimeMin.ToString("0.0", CultureInfo.InvariantCulture),
                JamCount.ToString(CultureInfo.InvariantCulture),
                Start.ToString("yyyy-MM-dd HH:mm:ss"),
                End.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>从一行文本解析回来（检索 .crd 文件用）。</summary>
        public static LotRecord FromCsv(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var f = line.Split(',');
            if (f.Length < 22) return null;
            var r = new LotRecord();
            try
            {
                r.LotNo = UnCsv(f[0]); r.SubLotNo = UnCsv(f[1]); r.Customer = UnCsv(f[2]);
                r.OperatorId = UnCsv(f[3]); r.Shift = UnCsv(f[4]); r.Package = UnCsv(f[5]);
                r.OutputType = UnCsv(f[6]);
                r.LotSize = int.Parse(f[7], CultureInfo.InvariantCulture);
                r.Total = int.Parse(f[8], CultureInfo.InvariantCulture);
                r.Good = int.Parse(f[9], CultureInfo.InvariantCulture);
                r.Bad = int.Parse(f[10], CultureInfo.InvariantCulture);
                r.Yield = double.Parse(f[11], CultureInfo.InvariantCulture);
                r.GrossPPH = double.Parse(f[12], CultureInfo.InvariantCulture);
                r.NettPPH = double.Parse(f[13], CultureInfo.InvariantCulture);
                r.MtbaMin = double.Parse(f[14], CultureInfo.InvariantCulture);
                r.LotTimeMin = double.Parse(f[15], CultureInfo.InvariantCulture);
                r.RunTimeMin = double.Parse(f[16], CultureInfo.InvariantCulture);
                r.JamTimeMin = double.Parse(f[17], CultureInfo.InvariantCulture);
                r.StopTimeMin = double.Parse(f[18], CultureInfo.InvariantCulture);
                r.JamCount = int.Parse(f[19], CultureInfo.InvariantCulture);
                r.Start = DateTime.ParseExact(f[20], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                r.End = DateTime.ParseExact(f[21], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch { return null; }
            return r;
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            return s.IndexOf(',') >= 0 ? "\"" + s + "\"" : s;
        }

        private static string UnCsv(string s)
        {
            s = s ?? "";
            return s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"' ? s.Substring(1, s.Length - 2) : s;
        }
    }

    // 批次管理 / 运行统计 / 批次记录
    // 温启志 18719361399 · 类 public static class LotOps
    public static class LotOps
    {
        private static MachineModel M => MachineModel.Instance;
        private static MotionManager Mm => MotionManager.Instance;

        /// <summary>批次记录目录（%APPDATA%\HandlerGuide\Lot）。</summary>
        public static string LotDir
        {
            get
            {
                var d = Path.Combine(Ops.DataDir, "Lot");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        // ==================================================================
        //  运行统计（Chapter 3.1.8 Run Time Data）
        // ==================================================================

        /// <summary>总产出速率（Gross PPH）= 总处理数 / 总运行时间（小时）。</summary>
        public static double GrossPPH
        {
            get
            {
                double h = M.RunTimeMin / 60.0;
                if (h <= 0) h = TotalRunHours();
                return h <= 0 ? 0 : Math.Round(M.TotalCount / h, 0);
            }
        }

        /// <summary>净产出速率（Nett PPH）= 总处理数 / 批次总时间（小时，含停机）。</summary>
        public static double NettPPH
        {
            get
            {
                double h = LotTimeMin / 60.0;
                if (h <= 0) h = TotalRunHours();
                return h <= 0 ? 0 : Math.Round(M.TotalCount / h, 0);
            }
        }

        /// <summary>MTBA（平均无辅助时间，分钟）= 批次时间 / 卡料次数。</summary>
        public static double MtbaMin
        {
            get
            {
                if (M.JamCount <= 0) return 0;
                return Math.Round(LotTimeMin / M.JamCount, 1);
            }
        }

        /// <summary>批次时间（分钟）= 运行时间 + 卡料时间 + 停机时间。</summary>
        public static double LotTimeMin => Math.Round(M.RunTimeMin + M.JamTimeMin + M.StopTimeMin, 1);

        /// <summary>进度百分比（按批量 LotSize 计算；批量为 0 时返回 0）。</summary>
        public static double ProgressPercent
        {
            get
            {
                if (M.LotSize <= 0) return 0;
                return Math.Round(Math.Min(100.0, M.TotalCount * 100.0 / M.LotSize), 1);
            }
        }

        /// <summary>剩余数量（批量 - 已处理）。</summary>
        public static int Remaining => Math.Max(0, M.LotSize - M.TotalCount);

        private static double TotalRunHours()
        {
            // 没有批次时间累计时，用实时运行时间兜底
            var span = DateTime.Now - (M.BatchStart ?? DateTime.Now);
            return Math.Max(0.0, span.TotalHours);
        }

        /// <summary>
        /// 由 500ms 刷新通道驱动：累计「运行 / 停机 / 卡料」三种时间。
        /// 温启志 18719361399
        /// </summary>
        public static void Tick(double dtMinutes)
        {
            if (!M.BatchOpen) return;

            if (M.Purging)
            {
                // 清料过程单独归到停机里，不算有效产出时间
                M.StopTimeMin += dtMinutes;
                return;
            }

            if (Ops.IsRunning) M.RunTimeMin += dtMinutes;
            else M.StopTimeMin += dtMinutes;
        }

        /// <summary>发生一次卡料（Jam）：计数 +1，并把当前停机归到卡料时间。</summary>
        public static void JamOccurred(string reason)
        {
            M.JamCount++;
            M.JamTimeMin += 0.5;                 // 一次卡料至少占用 0.5 分钟
            M.StopTimeMin = Math.Max(0, M.StopTimeMin - 0.5);
            Ops.Warn("卡料 #" + M.JamCount + "：" + (string.IsNullOrEmpty(reason) ? "原因未知" : reason)
                     + "（MTBA " + MtbaMin.ToString("0.0") + " min）");
        }

        // ==================================================================
        //  开批 / 结批（Chapter 4.1 Open Lot）
        // ==================================================================

        /// <summary>
        /// 把界面上的批次字段（f_LotSize / f_Shift / f_output_type）同步进模型。
        /// 温启志 18719361399：必须先同步再校验，否则校验会用上一批次的旧值。
        /// </summary>
        public static void SyncLotFieldsFromUi()
        {
            M.LotSize = (int)Math.Round(M.GetNum("f_LotSize", 0));
            M.Shift = M.GetText("f_Shift", "A");
            M.OutputType = M.GetChoice("f_output_type", 0) == 1 ? "Tape" : "Tray";
        }

        /// <summary>开批前置校验；返回 null 表示可以开批，否则返回不可开批的原因。</summary>
        public static string CheckCanOpenLot()
        {
            SyncLotFieldsFromUi();
            if (M.BatchOpen) return "已有打开的批次，请先结批";
            if (Ops.IsRunning) return "运行中不允许开新批次";
            if (Mm.EStop) return "急停未复位，不允许开批";
            if (!Mm.Connected) return "控制卡未连接，不允许开批";
            if (Mm.AnyAlarm()) return "存在伺服报警，请先报警解除";
            if (string.IsNullOrWhiteSpace(M.GetText("f_BatchNo"))) return "批号(MO)不能为空";
            if (M.LotSize <= 0) return "批量(Lot Size)必须大于 0";
            if (string.IsNullOrWhiteSpace(M.GetText("f_Operator"))) return "操作员不能为空";
            if (!Mm.Axes.All(a => Mm.State(a.Index).Homed)) return "有轴未归零，请先全部归零";
            return null;
        }

        /// <summary>
        /// 开批：校验 → 记开始时间 → 清零统计 → 落盘一条「开批」事件 → 可选自动打印。
        /// 温启志 18719361399
        /// </summary>
        public static bool OpenLot()
        {
            string why = CheckCanOpenLot();
            if (why != null) { Ops.Warn("开批失败：" + why); return false; }

            M.BatchNo = M.GetText("f_BatchNo");
            M.SubBatchNo = M.GetText("f_SubBatchNo");
            M.Customer = M.GetText("f_Customer");
            M.Operator = M.GetText("f_Operator");
            // 批量 / 班别 / 输出介质已在 SyncLotFieldsFromUi 里同步（键 f_LotSize / f_Shift / f_output_type）
            M.BatchOpen = true;
            M.BatchStart = DateTime.Now;
            M.BatchEnd = null;
            M.Purging = false;

            Ops.ResetCounters();
            M.RunTimeMin = 0; M.StopTimeMin = 0; M.JamTimeMin = 0; M.JamCount = 0;
            M.Carrier1Count = 0; M.Carrier2Count = 0;

            Ops.Info(string.Format(
                "已开批：{0}  批量 {1} 件  班别 {2}  操作员 {3}  输出 {4}  封装 {5}",
                M.BatchNo, M.LotSize, M.Shift, M.Operator, M.OutputType, M.Recipe));

            if (M.AutoPrinting) ExportLotSummaryDialog("开批");
            Ops.RaiseStatus();
            return true;
        }

        /// <summary>结批前置校验。</summary>
        public static string CheckCanCloseLot()
        {
            if (!M.BatchOpen) return "当前没有打开的批次";
            if (Ops.IsRunning) return "运行中不允许结批，请先停止";
            return null;
        }

        /// <summary>
        /// 结批：写批次记录（.lot）+ 当日汇总（.crd）→ 可选自动打印 → 关闭批次。
        /// 温启志 18719361399
        /// </summary>
        public static bool CloseLot()
        {
            string why = CheckCanCloseLot();
            if (why != null) { Ops.Warn("结批失败：" + why); return false; }

            M.BatchEnd = DateTime.Now;
            var rec = BuildCurrentRecord();

            try
            {
                // 单批文件：E:\Machine Data\XXXXXX.lot 的对应实现
                string lotPath = Path.Combine(LotDir, Safe(rec.LotNo) + ".lot");
                File.WriteAllText(lotPath, LotSummaryText(rec), Encoding.UTF8);

                // 当日汇总：DailyYYYYMMDD.crd（追加）
                string crd = Path.Combine(LotDir, "Daily" + DateTime.Now.ToString("yyyyMMdd") + ".crd");
                File.AppendAllText(crd, rec.ToCsv() + Environment.NewLine, Encoding.UTF8);

                Ops.Info("批次记录已写入：" + Path.GetFileName(lotPath));
            }
            catch (Exception ex)
            {
                Ops.Warn("写批次记录失败：" + ex.Message);
            }

            M.BatchOpen = false;
            M.Purging = false;

            Ops.Info(string.Format(
                "已结批：{0}  处理 {1} 件（好品 {2} / 次品 {3}）  良率 {4:0.00}%  UPH {5:0}  用时 {6:0.0} min",
                rec.LotNo, rec.Total, rec.Good, rec.Bad, rec.Yield, rec.NettPPH, rec.LotTimeMin));

            if (M.AutoPrinting) ExportLotSummaryDialog("结批");
            Ops.RaiseStatus();
            return true;
        }

        private static string Safe(string s)
        {
            var bad = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in (s ?? "")) sb.Append(bad.Contains(c) ? '_' : c);
            var r = sb.ToString().Trim();
            return string.IsNullOrEmpty(r) ? "NONAME" : r;
        }

        /// <summary>把当前批次状态打包成一条 LotRecord（不落盘）。</summary>
        public static LotRecord BuildCurrentRecord()
        {
            return new LotRecord
            {
                LotNo = M.BatchNo,
                SubLotNo = M.SubBatchNo,
                Customer = M.Customer,
                OperatorId = M.Operator,
                Shift = M.Shift,
                Package = M.Recipe,
                OutputType = M.OutputType,
                LotSize = M.LotSize,
                Total = M.TotalCount,
                Good = M.GoodCount,
                Bad = M.BadCount,
                Yield = M.Yield,
                GrossPPH = GrossPPH,
                NettPPH = NettPPH,
                MtbaMin = MtbaMin,
                LotTimeMin = LotTimeMin,
                RunTimeMin = Math.Round(M.RunTimeMin, 1),
                JamTimeMin = Math.Round(M.JamTimeMin, 1),
                StopTimeMin = Math.Round(M.StopTimeMin, 1),
                JamCount = M.JamCount,
                Start = M.BatchStart ?? DateTime.Now,
                End = M.BatchEnd ?? DateTime.Now
            };
        }

        // ==================================================================
        //  Operator Access（Chapter 4.2）
        // ==================================================================

        /// <summary>
        /// Abort Lot：机台内器件按默认流程走完（检测 / 剔除管理）后收尾；
        /// Tray 输出时进 Output Tray，Tape 输出时编带并打 Trailer。
        /// </summary>
        public static bool AbortLot()
        {
            if (!M.BatchOpen) { Ops.Warn("没有打开的批次，无法中止"); return false; }

            M.Purging = true;
            Ops.StopProduction();
            Ops.Info("中止批次(Abort Lot)：机台内器件按默认流程清完，"
                     + (M.OutputType == "Tape" ? "编带后打 Trailer 收尾" : "流入输出料盘"));

            // 清料需要时间，用归零通道之外的独立标记由 Tick 累计到停机时间里
            return true;
        }

        /// <summary>
        /// Purge Tray(s)：机台内器件不再检测 / 不判 Bin，直接送输出料盘。
        /// </summary>
        public static bool PurgeTrays()
        {
            if (!M.BatchOpen) { Ops.Warn("没有打开的批次，无法清盘"); return false; }

            M.Purging = true;
            Ops.StopProduction();
            Ops.Info("清盘(Purge Tray)：机台内器件跳过检测与剔除管理，直接送输出料盘");
            return true;
        }

        /// <summary>清料完成（界面/流程收尾时调用）。</summary>
        public static void FinishPurge()
        {
            if (!M.Purging) return;
            M.Purging = false;
            Ops.Info("清料完成，机台内已无器件");
            Ops.RaiseStatus();
        }

        /// <summary>
        /// Change Carrier：换上新载带后把对应载带计数器清零。
        /// idx = 1 或 2，对应 Carrier #1 / #2。
        /// </summary>
        public static bool ChangeCarrier(int idx)
        {
            if (idx != 1 && idx != 2) { Ops.Warn("载带编号只能是 1 或 2"); return false; }
            if (M.OutputType != "Tape") { Ops.Warn("当前输出介质是 Tray，无需更换载带"); return false; }

            if (idx == 1) { M.Carrier1Count = 0; Ops.Info("载带 #1 已更换，计数器清零"); }
            else { M.Carrier2Count = 0; Ops.Info("载带 #2 已更换，计数器清零"); }
            return true;
        }

        /// <summary>切换重扫模式（Rescan Mode）。</summary>
        public static bool SetRescanMode(bool on)
        {
            if (Ops.IsRunning) { Ops.Warn("运行中不允许切换重扫模式"); return false; }
            M.RescanMode = on;
            Ops.Info(on ? "已切换到重扫模式(Rescan Mode)" : "已退出重扫模式");
            return true;
        }

        /// <summary>KGD 补盘排序：把料盘空穴位用 KGD 器件按行/列填满。</summary>
        public static bool SetKgdTraySort(bool on)
        {
            M.KgdTraySort = on;
            Ops.Info(on ? "KGD 补盘排序已开启：空穴位将被 KGD 器件填满" : "KGD 补盘排序已关闭");
            return true;
        }

        /// <summary>OTF Homing：不按急停也让各机械臂回到原点。</summary>
        public static bool OtfHoming()
        {
            if (Ops.IsRunning) { Ops.Warn("运行中不允许执行 OTF Homing"); return false; }

            // 先把所有 Z 抬起，再让水平轴回零，避免撞机（与《电机归零》同策略）
            foreach (var a in Mm.Axes.Where(x => x.IsVertical)) Mm.MoveAbs(a.Index, a.HomeMm);
            bool ok = Ops.HomeAll();
            Ops.Info(ok ? "OTF Homing 已启动：各机械臂回原点（未触发急停）"
                        : "OTF Homing 未能启动，请检查伺服/急停状态");
            return ok;
        }

        // ==================================================================
        //  Extra Control（Chapter 4.3）
        // ==================================================================

        /// <summary>自动打印开关（开批 / 结批时自动输出汇总）。</summary>
        public static bool SetAutoPrinting(bool on)
        {
            M.AutoPrinting = on;
            Ops.Info(on ? "自动打印已开启（开批/结批自动输出批次汇总）" : "自动打印已关闭");
            return true;
        }

        /// <summary>事件日志（Event Log）：登录 / 开批 / 停机 / 改参数等。</summary>
        public static List<LogEntry> EventLog()
        {
            string[] evt = { "信息" };
            return M.Logs.Where(l => evt.Contains(l.Level)).ToList();
        }

        /// <summary>错误日志（Error Log）：报警 / 警告类。</summary>
        public static List<LogEntry> ErrorLog()
        {
            string[] err = { "警告", "报警", "错误" };
            return M.Logs.Where(l => err.Contains(l.Level)).ToList();
        }

        /// <summary>导出事件日志文本。</summary>
        public static string ExportEventLog() => LogText(EventLog(), "事件日志");

        /// <summary>导出错误日志文本。</summary>
        public static string ExportErrorLog() => LogText(ErrorLog(), "错误日志");

        private static string LogText(List<LogEntry> list, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== HandlerGuide " + title + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
            sb.AppendLine("共 " + list.Count + " 条");
            sb.AppendLine();
            foreach (var l in list)
                sb.AppendLine(l.Time.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  [" + l.Level + "]  " + l.Message);
            return sb.ToString();
        }

        /// <summary>批次汇总文本（开批 / 结批 / 打印批次汇总共用）。</summary>
        public static string LotSummaryText(LotRecord r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==============================================");
            sb.AppendLine("  HandlerGuide 批次汇总 / Lot Summary");
            sb.AppendLine("  " + (r.End.Year > 1 ? r.End.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine("==============================================");
            sb.AppendLine("批号 Lot No.      : " + r.LotNo + (string.IsNullOrEmpty(r.SubLotNo) ? "" : " / " + r.SubLotNo));
            sb.AppendLine("客户 Customer     : " + r.Customer);
            sb.AppendLine("封装 Package      : " + r.Package);
            sb.AppendLine("操作员 Operator   : " + r.OperatorId + "   班别 Shift: " + r.Shift);
            sb.AppendLine("输出介质 Output   : " + r.OutputType);
            sb.AppendLine("批量 Lot Size     : " + r.LotSize);
            sb.AppendLine("开始 Start        : " + r.Start.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("结束 End          : " + r.End.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("----------------------------------------------");
            sb.AppendLine("处理数 Total      : " + r.Total);
            sb.AppendLine("好品 Good         : " + r.Good);
            sb.AppendLine("次品 Bad          : " + r.Bad);
            sb.AppendLine("良率 Yield        : " + r.Yield.ToString("0.00") + " %");
            sb.AppendLine("----------------------------------------------");
            sb.AppendLine("Gross PPH         : " + r.GrossPPH.ToString("0"));
            sb.AppendLine("Nett PPH          : " + r.NettPPH.ToString("0"));
            sb.AppendLine("MTBA (min)        : " + r.MtbaMin.ToString("0.0"));
            sb.AppendLine("批次时间 (min)    : " + r.LotTimeMin.ToString("0.0"));
            sb.AppendLine("  运行 Run        : " + r.RunTimeMin.ToString("0.0"));
            sb.AppendLine("  卡料 Jam        : " + r.JamTimeMin.ToString("0.0") + "  (" + r.JamCount + " 次)");
            sb.AppendLine("  停机 Stop       : " + r.StopTimeMin.ToString("0.0"));
            sb.AppendLine("==============================================");
            return sb.ToString();
        }

        /// <summary>打印/导出当前批次汇总（Chapter 4.2 Print Lot Summary）。</summary>
        public static string PrintLotSummary() => LotSummaryText(BuildCurrentRecord());

        /// <summary>把批次汇总写到文件；返回路径（文件对话框被抑制时写到批次目录）。</summary>
        public static string ExportLotSummaryDialog(string tag)
        {
            try
            {
                Directory.CreateDirectory(LotDir);
                string name = "summary_" + (string.IsNullOrEmpty(tag) ? "" : tag + "_")
                              + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                string p = Path.Combine(LotDir, name);
                File.WriteAllText(p, PrintLotSummary(), Encoding.UTF8);
                Ops.Info("批次汇总已导出：" + name);
                return p;
            }
            catch (Exception ex)
            {
                Ops.Warn("导出批次汇总失败：" + ex.Message);
                return null;
            }
        }

        // ==================================================================
        //  View Lot Info（Chapter 3.2.3）：按条件检索批次记录
        // ==================================================================

        /// <summary>读取全部批次记录（来自各 Daily*.crd 汇总文件）。</summary>
        public static List<LotRecord> AllLotRecords()
        {
            var list = new List<LotRecord>();
            try
            {
                foreach (var f in Directory.GetFiles(LotDir, "Daily*.crd").OrderBy(x => x))
                {
                    foreach (var line in File.ReadAllLines(f))
                    {
                        var r = LotRecord.FromCsv(line);
                        if (r != null) list.Add(r);
                    }
                }
            }
            catch { /* 目录还没建过就当没有记录 */ }
            return list;
        }

        /// <summary>
        /// 检索批次记录：日期区间 + 批号 / 操作员 / 班别 / 封装 的模糊匹配。
        /// 任一条件为空表示不过滤该项。
        /// </summary>
        public static List<LotRecord> SearchLotRecords(DateTime? from, DateTime? to,
            string lotNo = null, string operatorId = null, string shift = null, string package = null)
        {
            var q = AllLotRecords().AsEnumerable();

            if (from.HasValue) q = q.Where(r => r.Start.Date >= from.Value.Date);
            if (to.HasValue) q = q.Where(r => r.Start.Date <= to.Value.Date);
            if (!string.IsNullOrWhiteSpace(lotNo))
                q = q.Where(r => (r.LotNo ?? "").IndexOf(lotNo, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(operatorId))
                q = q.Where(r => (r.OperatorId ?? "").IndexOf(operatorId, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(shift))
                q = q.Where(r => string.Equals(r.Shift, shift, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(package))
                q = q.Where(r => (r.Package ?? "").IndexOf(package, StringComparison.OrdinalIgnoreCase) >= 0);

            return q.OrderByDescending(r => r.Start).ToList();
        }

        /// <summary>批次记录总数（界面显示 / 自检用）。</summary>
        public static int LotRecordCount => AllLotRecords().Count;

        /// <summary>批次状态文本（主界面显示）。</summary>
        public static string LotStateText
        {
            get
            {
                if (!M.BatchOpen) return "未开批";
                if (M.Purging) return "清料中";
                if (Ops.IsRunning) return "运行中";
                if (Ops.IsPaused) return "暂停";
                return "已开批";
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
