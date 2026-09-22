// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Mvvm）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_Mvvm
    internal static class WenQiZhiTag_Mvvm
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Mvvm。</summary>
        internal const string WenQiZhiFile = "Mvvm";
    }

    // 温启志 18719361399 · 类 public abstract class ObservableObject : INotifyPropertyChanged
    /// <summary>MVVM 基类：属性变更通知。</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Raise([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>供外部（如容器视图模型）主动通知某个属性变化。</summary>
        public void Notify(string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Raise(name);
            return true;
        }
    }

    // 温启志 18719361399 · 类 public class RelayCommand : ICommand
    /// <summary>MVVM 命令。</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = _ => execute();
            _canExecute = canExecute == null ? null : new Func<object, bool>(_ => canExecute());
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // 温启志 18719361399 · 类 public static class Palette
    /// <summary>
    /// 复刻界面的调色板（沿用 macOS / 苹果风格色系，同时保留原软件的浅蓝/浅绿/米黄分区底色）。
    /// </summary>
    public static class Palette
    {
        public static Brush Primary => R("TextPrimaryBrush");
        public static Brush Secondary => R("TextSecondaryBrush");
        public static Brush Accent => R("AccentBrush");
        public static Brush Divider => R("DividerBrush");
        public static Brush CardBg => R("CardBgBrush");
        public static Brush WinBg => R("WinBgBrush");
        public static Brush Success => R("SuccessBrush");
        public static Brush Warning => R("WarningBrush");
        public static Brush Tip => R("TipBrush");
        public static Brush WarningBg => R("WarningBgBrush");
        public static Brush TipBg => R("TipBgBrush");

        public static readonly Brush BandBlue = new SolidColorBrush(Color.FromRgb(0xE8, 0xF1, 0xFF));
        public static readonly Brush BandGreen = new SolidColorBrush(Color.FromRgb(0xE6, 0xF7, 0xEC));
        public static readonly Brush BandAmber = new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xE0));
        public static readonly Brush BandGrey = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF3));
        public static readonly Brush BandRed = new SolidColorBrush(Color.FromRgb(0xFD, 0xEA, 0xE9));

        public static readonly Brush HeadBlue = new SolidColorBrush(Color.FromRgb(0xCF, 0xE3, 0xFB));
        public static readonly Brush HeadGreen = new SolidColorBrush(Color.FromRgb(0xCF, 0xEF, 0xD8));
        public static readonly Brush HeadAmber = new SolidColorBrush(Color.FromRgb(0xF5, 0xDC, 0xA8));
        public static readonly Brush HeadTeal = new SolidColorBrush(Color.FromRgb(0x9F, 0xC5, 0xC9));
        public static readonly Brush HeadGrey = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xDC));

        public static readonly Brush CellWhite = Brushes.White;
        public static readonly Brush CellDark = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x50));
        public static readonly Brush LineGrey = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xA0));
        public static readonly Brush OffDot = new SolidColorBrush(Color.FromRgb(0xC7, 0xC7, 0xCC));
        public static readonly Brush OnRed = new SolidColorBrush(Color.FromRgb(0xC0, 0x20, 0x2A));
        public static readonly Brush OnGreen = new SolidColorBrush(Color.FromRgb(0x1E, 0x8E, 0x3E));
        public static readonly Brush BeamBlue = new SolidColorBrush(Color.FromRgb(0x8F, 0xA8, 0xE8));
        public static readonly Brush ShaftBlue = new SolidColorBrush(Color.FromRgb(0xBD, 0xD4, 0xF0));
        public static readonly Brush TipBlue = new SolidColorBrush(Color.FromRgb(0x4A, 0x6A, 0xA8));
        public static readonly Brush FooterRed = new SolidColorBrush(Color.FromRgb(0x9B, 0x1B, 0x1B));
        public static readonly Brush Cyan = new SolidColorBrush(Color.FromRgb(0x5B, 0xC0, 0xE0));

        public static Brush R(string key) => System.Windows.Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    // 温启志 18719361399 · 类 public static class LiveRegistry
    /// <summary>
    /// 运行态只读读数的注册表：页面构建时登记，主视图模型每个 tick 统一刷新，
    /// 避免每个读数各自订阅模型事件造成泄漏。
    /// </summary>
    public static class LiveRegistry
    {
        private static readonly List<ILiveValue> Items = new List<ILiveValue>();

        // 温启志 18719361399 · 快速通道：仿真动画（画布逐帧重绘）用，
        // 由主窗口的 50ms 定时器驱动，与普通读数的 500ms 刷新互不干扰。
        private static readonly List<ILiveValue> FastItems = new List<ILiveValue>();

        public static void Register(ILiveValue item)
        {
            if (item != null && !Items.Contains(item)) Items.Add(item);
        }

        /// <summary>登记到快速刷新通道（约 20fps）。</summary>
        public static void RegisterFast(ILiveValue item)
        {
            if (item != null && !FastItems.Contains(item)) FastItems.Add(item);
        }

        public static void Clear()
        {
            Items.Clear();
            FastItems.Clear();
        }

        public static void RefreshAll()
        {
            for (int i = 0; i < Items.Count; i++) Items[i].Refresh();
        }

        /// <summary>只刷新快速通道（仿真动画）。</summary>
        public static void RefreshFast()
        {
            for (int i = 0; i < FastItems.Count; i++)
            {
                try { FastItems[i].Refresh(); }
                catch { /* 单个元素异常不影响其它元素 */ }
            }
        }
    }

    public interface ILiveValue
    {
        void Refresh();
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
