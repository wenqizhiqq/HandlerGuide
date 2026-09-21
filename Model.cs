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
    // 诊断用电机状态
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
    public class LogEntry
    {
        public DateTime Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    // 机器运行模型(单例) —— 既是软件全局参数仓库，也驱动实时仿真
    public class MachineModel : INotifyPropertyChanged
    {
        public static MachineModel Instance { get; } = new MachineModel();

        // ---- 通用参数仓库(按 key 存取，覆盖手册各模块参数) ----
        public Dictionary<string, double> Nums { get; } = new();
        public Dictionary<string, string> Texts { get; } = new();
        public Dictionary<string, bool> Toggles { get; } = new();
        public Dictionary<string, int> Choices { get; } = new();

        public double GetNum(string k, double d = 0) => Nums.TryGetValue(k, out var v) ? v : d;
        public void SetNum(string k, double v) => Nums[k] = v;
        public string GetText(string k, string d = "") => Texts.TryGetValue(k, out var v) ? v : d;
        public void SetText(string k, string v) => Texts[k] = v;
        public bool GetToggle(string k, bool d = false) => Toggles.TryGetValue(k, out var v) ? v : d;
        public void SetToggle(string k, bool v) => Toggles[k] = v;
        public int GetChoice(string k, int d = 0) => Choices.TryGetValue(k, out var v) ? v : d;
        public void SetChoice(string k, int v) => Choices[k] = v;

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

        // 主界面状态栏附加
        public string BatchNo { get; set; } = "";
        public string SubBatchNo { get; set; } = "";
        public string Customer { get; set; } = "";
        public string Operator { get; set; } = "OP-001";
        public bool BatchOpen { get; set; } = false;
        public string StartMode { get; set; } = "初始模式";

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

        // 实时仿真 tick
        public void Tick()
        {
            if (RunState != "运行")
            {
                foreach (var m in Motors)
                {
                    m.Busy = false;
                    m.Torque = Math.Round(m.Torque * 0.9, 1);
                }
                return;
            }

            int processed = 1 + _rnd.Next(0, 3);
            TotalCount += processed;
            Accumulated += processed;
            int bad = _rnd.Next(0, 2);
            BadCount += bad;
            GoodCount += (processed - bad);
            Yield = TotalCount == 0 ? 0 : Math.Round((double)GoodCount / TotalCount * 100, 2);
            UPH = 8000 + _rnd.Next(0, 4000);
            CycleT = Math.Round(0.4 + _rnd.NextDouble() * 0.4, 3);
            TestT = Math.Round(2 + _rnd.NextDouble() * 6, 2);
            IndexT = Math.Round(0.45 + _rnd.NextDouble() * 0.2, 3);

            foreach (var b in Bins)
            {
                if (!b.Unused && _rnd.Next(0, 40) == 0) b.Count += _rnd.Next(1, 5);
            }

            foreach (var m in Motors)
            {
                m.Busy = _rnd.Next(0, 3) == 0;
                m.Position = Math.Round(m.Position + (_rnd.NextDouble() - 0.5) * 2, 1);
                m.Torque = Math.Round(1 + _rnd.NextDouble() * 8, 1);
                m.ALM = _rnd.Next(0, 60) == 0;
            }
            Pressure = Math.Round(240 + (_rnd.NextDouble() - 0.5) * 4, 1);
            UPH48 = 7000 + _rnd.Next(0, 4000);
            Stage = _rnd.Next(0, 4).ToString();
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
