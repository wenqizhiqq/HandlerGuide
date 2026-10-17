// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
// ============================================================================
//  Model.cs — 机器运行时模型（单例 MachineModel）
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件界面复刻）
//  职责：参数仓库（数值/文本/开关/下拉，按 key 存取）+ 运行状态（INPC）
//        + 电机/Bin/日志集合 + 定时器实时仿真（良品/次品/UPH/周期、电机抖动）。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace HandlerGuide
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Model）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Model
    internal static class WenQiZhiTag_Model
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = ViewModels.Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = ViewModels.Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Model。</summary>
        internal const string WenQiZhiFile = "Model";
    }

    // 诊断用电机状态
    // 温启志 18719361399 · 类 public class MotorInfo : INotifyPropertyChanged
    public class MotorInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private bool _org, _alm, _homed, _busy, _inp;
        private double _pos, _torque;
        private string _code = "";

        public bool ORG { get => _org; set { _org = value; OnChanged(); } }
        public bool ALM { get => _alm; set { _alm = value; OnChanged(); } }
        public bool Homed { get => _homed; set { _homed = value; OnChanged(); } }
        public bool Busy { get => _busy; set { _busy = value; OnChanged(); } }
        public bool INP { get => _inp; set { _inp = value; OnChanged(); } }
        public double Position { get => _pos; set { _pos = value; OnChanged(); } }
        public double Torque { get => _torque; set { _torque = value; OnChanged(); } }
        public string Code { get => _code; set { _code = value; OnChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }

    // 分类(Bin)信息
    // 温启志 18719361399 · 类 public class BinInfo : INotifyPropertyChanged
    public class BinInfo : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string ColorText { get; set; } = "#34C759";
        public int Count { get; set; }
        public bool IsBad { get; set; }
        public bool ContinuousBad { get; set; }
        public bool Retest { get; set; }
        public bool YieldAlarm { get; set; }
        public bool Unused { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }

    // 信息记录(报警/事件)日志
    // 温启志 18719361399 · 类 public class LogEntry
    public class LogEntry
    {
        public DateTime Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    // 机器运行模型(单例) —— 既是软件全局参数仓库，也驱动实时仿真
    // 温启志 18719361399 · 类 public class MachineModel : INotifyPropertyChanged
    public class MachineModel : INotifyPropertyChanged
    {
        public static MachineModel Instance { get; } = new MachineModel();

        // ---- 通用参数仓库(按 key 存取，覆盖手册各模块参数) ----
        public Dictionary<string, double> Nums { get; } = new();
        public Dictionary<string, string> Texts { get; } = new();
        public Dictionary<string, bool> Toggles { get; } = new();
        public Dictionary<string, int> Choices { get; } = new();

        public double GetNum(string k, double d = 0) => Nums.TryGetValue(k, out var v) ? v : d;
        public void SetNum(string k, double v) { Nums[k] = v; MarkDirty(); }
        public string GetText(string k, string d = "") => Texts.TryGetValue(k, out var v) ? v : d;
        public void SetText(string k, string v) { Texts[k] = v; MarkDirty(); }
        public bool GetToggle(string k, bool d = false) => Toggles.TryGetValue(k, out var v) ? v : d;
        public void SetToggle(string k, bool v) { Toggles[k] = v; MarkDirty(); }
        public int GetChoice(string k, int d = 0) => Choices.TryGetValue(k, out var v) ? v : d;
        public void SetChoice(string k, int v) { Choices[k] = v; MarkDirty(); }

        // ===== 参数自动保存 / 载入（配方参数 + 系统参数 一并持久化） =====
        // 温启志 18719361399 · 参数持久化（AppData/Local/HandlerGuide/params.json）
        private static readonly string _paramFile = System.IO.Path.Combine(
            System.AppContext.BaseDirectory, "params.json");

        private System.Timers.Timer _saveTimer;
        private bool _dirty;
        private bool _loaded;

        private void MarkDirty()
        {
            _dirty = true;
            if (_saveTimer == null)
            {
                _saveTimer = new System.Timers.Timer(800) { AutoReset = false };
                _saveTimer.Elapsed += (s, e) => { _saveTimer.Stop(); FlushSave(); };
            }
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void FlushSave()
        {
            try { Save(); _dirty = false; }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("参数保存失败: " + ex.Message); }
        }

        /// <summary>把四个参数字典 + 关键配置属性序列化到 JSON（配方参数与系统参数一并保存）。</summary>
        public void Save()
        {
            if (!_dirty) return;
            var cfg = new System.Collections.Generic.Dictionary<string, object>
            {
                ["Mode"] = Mode, ["Recipe"] = Recipe, ["UserLevel"] = UserLevel,
                ["TempMode"] = TempMode, ["Operator"] = Operator, ["Customer"] = Customer,
                ["BatchNo"] = BatchNo, ["SubBatchNo"] = SubBatchNo, ["Shift"] = Shift,
                ["OutputType"] = OutputType, ["StartMode"] = StartMode, ["Chinese"] = Chinese,
                ["KeyboardLocked"] = KeyboardLocked, ["RescanMode"] = RescanMode,
                ["LotSize"] = LotSize, ["Pressure"] = Pressure
            };
            var bag = new System.Collections.Generic.Dictionary<string, object>
            {
                ["Nums"] = Nums, ["Texts"] = Texts, ["Toggles"] = Toggles, ["Choices"] = Choices,
                ["Config"] = cfg
            };
            var json = System.Text.Json.JsonSerializer.Serialize(bag, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var dir = System.IO.Path.GetDirectoryName(_paramFile);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(_paramFile, json);
        }

        /// <summary>从 JSON 载入并合并到现有字典/配置（缺失项保留代码默认值）。</summary>
        public void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!System.IO.File.Exists(_paramFile)) return;
                var json = System.IO.File.ReadAllText(_paramFile);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("Nums", out var nums)) foreach (var p in nums.EnumerateObject()) Nums[p.Name] = p.Value.GetDouble();
                if (root.TryGetProperty("Texts", out var texts)) foreach (var p in texts.EnumerateObject()) Texts[p.Name] = p.Value.GetString() ?? "";
                if (root.TryGetProperty("Toggles", out var tog)) foreach (var p in tog.EnumerateObject()) Toggles[p.Name] = p.Value.GetBoolean();
                if (root.TryGetProperty("Choices", out var ch)) foreach (var p in ch.EnumerateObject()) Choices[p.Name] = p.Value.GetInt32();
                if (root.TryGetProperty("Config", out var cfg))
                {
                    if (cfg.TryGetProperty("Mode", out var v)) Mode = v.GetString();
                    if (cfg.TryGetProperty("Recipe", out var v2)) Recipe = v2.GetString();
                    if (cfg.TryGetProperty("UserLevel", out var v3)) UserLevel = v3.GetString();
                    if (cfg.TryGetProperty("TempMode", out var v4)) TempMode = v4.GetString();
                    if (cfg.TryGetProperty("Operator", out var v5)) Operator = v5.GetString();
                    if (cfg.TryGetProperty("Customer", out var v6)) Customer = v6.GetString();
                    if (cfg.TryGetProperty("BatchNo", out var v7)) BatchNo = v7.GetString();
                    if (cfg.TryGetProperty("SubBatchNo", out var v8)) SubBatchNo = v8.GetString();
                    if (cfg.TryGetProperty("Shift", out var v9)) Shift = v9.GetString();
                    if (cfg.TryGetProperty("OutputType", out var v10)) OutputType = v10.GetString();
                    if (cfg.TryGetProperty("StartMode", out var v11)) StartMode = v11.GetString();
                    if (cfg.TryGetProperty("Chinese", out var v12)) Chinese = v12.GetBoolean();
                    if (cfg.TryGetProperty("KeyboardLocked", out var v13)) KeyboardLocked = v13.GetBoolean();
                    if (cfg.TryGetProperty("RescanMode", out var v14)) RescanMode = v14.GetBoolean();
                    if (cfg.TryGetProperty("LotSize", out var v15)) LotSize = v15.GetInt32();
                    if (cfg.TryGetProperty("Pressure", out var v16)) Pressure = v16.GetDouble();
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("参数载入失败: " + ex.Message); }
        }


        // ---- 运行时状态(带通知，驱动主界面/诊断实时刷新) ----
        private string _runState = "停止";
        private string _mode = "连续模式";
        private string _recipe = "Demo_QFN_7x7";
        private string _userLevel = "工程师";
        private string _tempMode = "常温模式";
        private double _pressure = 240;
        private int _good, _bad, _total;
        private double _uph, _yield, _cycleT, _testT, _indexT;
        private int _accumulated = 0;
        private bool _chinese = true;
        private bool _keyboardLocked = false;

        public string RunState { get => _runState; set { _runState = value; OnChanged(); } }
        public string Mode { get => _mode; set { _mode = value; OnChanged(); } }
        public string Recipe { get => _recipe; set { _recipe = value; OnChanged(); } }
        public string UserLevel { get => _userLevel; set { _userLevel = value; OnChanged(); } }
        public string TempMode { get => _tempMode; set { _tempMode = value; OnChanged(); } }
        public double Pressure { get => _pressure; set { _pressure = value; OnChanged(); } }
        public int GoodCount { get => _good; set { _good = value; OnChanged(); } }
        public int BadCount { get => _bad; set { _bad = value; OnChanged(); } }
        public int TotalCount { get => _total; set { _total = value; OnChanged(); } }
        public double UPH { get => _uph; set { _uph = value; OnChanged(); } }
        public double Yield { get => _yield; set { _yield = value; OnChanged(); } }
        public double CycleT { get => _cycleT; set { _cycleT = value; OnChanged(); } }
        public double TestT { get => _testT; set { _testT = value; OnChanged(); } }
        public double IndexT { get => _indexT; set { _indexT = value; OnChanged(); } }
        public int Accumulated { get => _accumulated; set { _accumulated = value; OnChanged(); } }
        public bool Chinese { get => _chinese; set { _chinese = value; OnChanged(); } }
        public bool KeyboardLocked { get => _keyboardLocked; set { _keyboardLocked = value; OnChanged(); } }

        // ---- 上排料盘堆叠层数（温启志 18719361399）------------------------------
        // 「整机俯视图」（motion_layout 页）里上排 6 个深盘原先画的是「格子 +
        // 物料点」，现按用户要求改成「叠层」表示料盘层数。层数需要一个数据源，
        // 本工程是界面原型 Demo（不含底层业务），因此先给一组**模拟值**：
        //   · 初值：1..MaxTrayLayers 随机（固定种子，保证每次启动一致、可复现）；
        //   · 演示节拍里：从满盘被逐层取走（见 LayoutPages 的逐帧动画）。
        // 接真机时只要把 TopTrayLayers 换成料盘堆叠传感器的真实读数即可，
        // 视图层一行都不用改。
        /// <summary>单个料盘最多可堆叠的层数。</summary>
        public const int MaxTrayLayers = 20;

        /// <summary>层数低于此值 → 黄色提示「需增加料盘」。</summary>
        public const int MinTrayAlarmLayers = 3;

        /// <summary>上排 6 个深盘的堆叠层数（1..<see cref="MaxTrayLayers"/>）。</summary>
        public int[] TopTrayLayers { get; } = NewTopTrayLayers();

        private static int[] NewTopTrayLayers()
        {
            var rnd = new Random(20261012);            // 固定种子 → 每次启动的初值一致
            var a = new int[6];
            bool hasAlarm = false;
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = rnd.Next(1, MaxTrayLayers + 1);
                if (a[i] < MinTrayAlarmLayers) hasAlarm = true;
            }
            // 演示用：保证静态首屏至少有一处黄色报警（否则要等演示节拍跑到底才出现）。
            // 需求 45 起报警位钉在自动盘塔 a[0]（原先钉 a[5] 进料盘）：俯视图演示一启动
            // 就能看到「判断到自动盘空了 → 搬运臂从进空盘补盘」这一幕。
            if (!hasAlarm) a[0] = MinTrayAlarmLayers - 1;
            return a;
        }

        // 主界面状态栏附加
        public string BatchNo { get; set; } = "";
        public string SubBatchNo { get; set; } = "";
        public string Customer { get; set; } = "";
        public string Operator { get; set; } = "OP-001";
        public bool BatchOpen { get; set; } = false;
        public string StartMode { get; set; } = "初始模式";

        /// <summary>批次开始时间（开批次时写入，界面显示批次时长用）。</summary>
        public DateTime? BatchStart { get; set; }

        /// <summary>批次结束时间（结批次时写入）。</summary>
        public DateTime? BatchEnd { get; set; }

        // ------------------------------------------------------------------
        //  批次(Lot)管理与运行统计（温启志 18719361399）
        //  参考 ASTI Hexa Maxx《Chapter 4 Operator's Menu》与《Chapter 3 Run Time Data》：
        //  Lot Number / Lot Size / Operator ID / Shift / Package / Start Date & Time /
        //  Gross PPH / Nett PPH / MTBA / Lot Time / Run Time / Jam Time / Stop Time。
        // ------------------------------------------------------------------

        /// <summary>批量（该批次计划投产的器件数）。</summary>
        public int LotSize { get; set; }

        /// <summary>班别（如 A / B / C 或 1 / 2）。</summary>
        public string Shift { get; set; } = "A";

        /// <summary>输出介质：Tray（料盘）或 Tape（载带）。</summary>
        public string OutputType { get; set; } = "Tray";

        /// <summary>重扫模式（Rescan Mode）：对已有结果重新扫描分类。</summary>
        public bool RescanMode { get; set; }

        /// <summary>卡料次数（用于计算 MTBA）。</summary>
        public int JamCount { get; set; }

        /// <summary>卡料累计时间（分钟）。</summary>
        public double JamTimeMin { get; set; }

        /// <summary>停机累计时间（分钟，含人为停止与等待）。</summary>
        public double StopTimeMin { get; set; }

        /// <summary>纯运行累计时间（分钟，不含任何停机）。</summary>
        public double RunTimeMin { get; set; }

        /// <summary>载带 #1 已编带数量（Tape 输出时有效）。</summary>
        public int Carrier1Count { get; set; }

        /// <summary>载带 #2 已编带数量（Tape 输出时有效）。</summary>
        public int Carrier2Count { get; set; }

        /// <summary>自动打印：开批 / 结批时自动输出批次汇总。</summary>
        public bool AutoPrinting { get; set; }

        /// <summary>批次是否处于「清料(Purge/Abort)」过程中。</summary>
        public bool Purging { get; set; }

        /// <summary>KGD 补盘排序（把空穴位用 KGD 器件填满）。</summary>
        public bool KgdTraySort { get; set; }

        /// <summary>批次记录存放目录。</summary>
        public string LotDirName => "Lot";

        // ---- 主界面（复刻 image37）所需的实时读数 ----
        private string _clock = "00:00:00";
        private string _runTime = "0:00:00";
        private string _stage = "0";
        private double _uph48, _tempSet = 25;
        private DateTime _zeroTime = DateTime.Now;

        public string Clock { get => _clock; set { _clock = value; OnChanged(); } }
        public string RunTime { get => _runTime; set { _runTime = value; OnChanged(); } }
        public string Stage { get => _stage; set { _stage = value; OnChanged(); } }
        public double UPH48 { get => _uph48; set { _uph48 = value; OnChanged(); } }
        public double TempSet { get => _tempSet; set { _tempSet = value; OnChanged(); } }
        public string TempTag => ":T";
        public string OnlineText => "Online";
        public string SiteLayoutName { get; set; } = "ThirtyTwo-Sites_R";

        public string[] Recipes { get; set; } =
        {
            "SGS_QFP32_7X7_32_1", "SGS_QFN48_7X7_32_1", "SGS_QFN28_4X4_32_1",
            "SGS_QFP64_10X10_24_1", "SGS_QFN32_5X5_32_1", "InitDevice"
        };

        // 设备规格(来自手册)
        public string ModelName { get; } = "EXCEED9000 高速分选机";
        public string SpecUPH { get; } = "Max. 15000";
        public int SiteCount { get; set; } = 16;

        // 电机状态表(诊断)
        public ObservableCollection<MotorInfo> Motors { get; } = new();
        // 分类表(Bin设定)
        public ObservableCollection<BinInfo> Bins { get; } = new();
        // 信息记录
        public ObservableCollection<LogEntry> Logs { get; } = new();

        private readonly Random _rnd = new();

        private MachineModel()
        {
            InitMotors();
            InitBins();
            Load();
            Logs.Add(new LogEntry { Time = DateTime.Now, Level = "信息", Message = "控制程序启动完成" });
        }

        private void InitMotors()
        {
            // 轴名称与手册《诊断 / 电机状态》表一致（共 31 轴）
            string[] names =
            {
                "测试位移动Y1", "测试位移动Y2", "测试位下压Z1", "测试位下压Z2",
                "上料取放臂X轴", "上料取放臂Y轴", "上料取放臂PX轴", "上料取放臂PY轴",
                "上料取放臂Z1轴", "上料取放臂Z2轴", "上料取放臂Z3轴", "上料取放臂Z4轴",
                "上料取放臂Z5轴", "上料取放臂Z6轴", "上料取放臂Z7轴", "上料取放臂Z8轴",
                "下料取放臂X轴", "下料取放臂Y轴", "下料取放臂PX轴", "下料取放臂PY轴",
                "下料取放臂Z1轴", "下料取放臂Z2轴", "下料取放臂Z3轴", "下料取放臂Z4轴",
                "下料取放臂Z5轴", "下料取放臂Z6轴", "下料取放臂Z7轴", "下料取放臂Z8轴",
                "测试运料小车A", "测试运料小车B", "料盘传送臂"
            };
            foreach (var n in names)
            {
                var m = new MotorInfo
                {
                    Name = n,
                    ORG = true,
                    Homed = true,
                    INP = true,
                    Position = Math.Round(_rnd.NextDouble() * 100, 1),
                    Torque = Math.Round(_rnd.NextDouble() * 5, 1)
                };
                Motors.Add(m);
            }
        }

        private void InitBins()
        {
            string[] labels = { "Bin1", "Bin2", "Bin3", "Bin4", "Bin5", "Bin6", "Bin7", "Bin8",
                                "Bin9", "Bin10", "Bin11", "Bin12", "Bin13", "Bin14", "Bin15", "Bin16" };
            for (int i = 0; i < labels.Length; i++)
            {
                Bins.Add(new BinInfo
                {
                    Index = i + 1,
                    Name = labels[i],
                    ColorText = i == 0 ? "#34C759" : (i < 4 ? "#30B0C7" : "#FF9F0A"),
                    IsBad = i >= 12,
                    Count = 0
                });
            }
            // 特殊 Bin
            Bins.Add(new BinInfo { Index = 0, Name = "Bin NT(未测试)", ColorText = "#8E8E93" });
            Bins.Add(new BinInfo { Index = 99, Name = "Bin NG(视觉Fail)", ColorText = "#FF3B30" });
            Bins.Add(new BinInfo { Index = 98, Name = "Bin ERR(无效)", ColorText = "#FF3B30" });
        }

        // 实时仿真 tick（500ms 通道）
        //
        // 重要：产量统计**不在这里**做了。以前这段会自己随机累加 Total/Good/Bad，
        // 结果是「暂停了数字还在涨」，而且和运控真实节拍完全脱节。
        // 现在产量由 Ops.OnCycleCompleted() 在真实取放料循环结束时统计。
        // 这里只保留「环境量」的轻微波动，以及未接卡时的电机阻尼。
        public void Tick()
        {
            var mm = Motion.MotionManager.Instance;
            bool cardLive = mm != null && mm.Connected;

            if (RunState != "运行")
            {
                if (!cardLive)
                    foreach (var m in Motors)
                    {
                        m.Busy = false;
                        m.Torque = Math.Round(m.Torque * 0.9, 1);
                    }
                return;
            }

            long ms = Environment.TickCount64;

            // 气压：围绕 240Kgf 轻微摆动，看起来像真的
            Pressure = Math.Round(240 + Math.Sin(ms / 2600.0) * 2.0, 1);

            // 48 小时趋势：以当前 UPH 为基准小幅波动
            double baseUph = UPH > 0 ? UPH : 0;
            UPH48 = Math.Round(baseUph * (0.94 + 0.05 * Math.Sin(ms / 9000.0)), 0);

            Stage = ((ms / 4000) % 4).ToString();

            // 电机状态：接了卡就完全以运控层回写的真实值为准，绝不在这里随机覆盖，
            // 否则《诊断 / 电机状态》页会跟着乱跳，看不出真实运动。
            if (!cardLive)
                foreach (var m in Motors)
                {
                    m.Busy = false;
                    m.Torque = Math.Round(m.Torque * 0.9, 1);
                }
        }

        /// <summary>由界面定时器驱动，刷新时间类读数。</summary>
        public void TickClock()
        {
            Clock = DateTime.Now.ToString("HH:mm:ss");
            var span = DateTime.Now - _zeroTime;
            RunTime = ((int)span.TotalHours).ToString() + span.ToString(@"\:mm\:ss");
        }

        public void ResetZeroTime() => _zeroTime = DateTime.Now;

        public void Log(string level, string msg)
        {
            Logs.Insert(0, new LogEntry { Time = DateTime.Now, Level = level, Message = msg });
            if (Logs.Count > 500) Logs.RemoveAt(Logs.Count - 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
