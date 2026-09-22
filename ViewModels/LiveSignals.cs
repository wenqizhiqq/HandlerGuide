// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LiveSignals）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_LiveSignals
    internal static class WenQiZhiTag_LiveSignals
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · LiveSignals。</summary>
        internal const string WenQiZhiFile = "LiveSignals";
    }

    // 温启志 18719361399 · 类 public static class LiveSignals
    /// <summary>
    /// 命名实时信号注册表：诊断页的状态灯通过字符串信号名从模型读取布尔状态。
    /// 仅作为“数据→布尔”的映射，不含任何界面构造逻辑（满足 ViewModels 不动态添加控件）。
    /// </summary>
    public static class LiveSignals
    {
        private static readonly Dictionary<string, Func<MachineModel, bool>> _named =
            new Dictionary<string, Func<MachineModel, bool>>();

        /// <summary>注册一个具名信号（供 XAML 的 LiveLampElement.Signal 引用）。</summary>
        public static void Register(string name, Func<MachineModel, bool> predicate)
        {
            if (!string.IsNullOrEmpty(name)) _named[name] = predicate;
        }

        public static bool TryGet(string name, out Func<MachineModel, bool> predicate)
        {
            predicate = null;
            if (string.IsNullOrEmpty(name)) return false;

            // 电机状态：motor_<prop>_<idx> -> MachineModel.Motors[idx].<prop>
            if (name.StartsWith("motor_", StringComparison.Ordinal))
            {
                var rest = name.Substring(6);
                int us = rest.LastIndexOf('_');
                if (us > 0 && int.TryParse(rest.Substring(us + 1), out int idx))
                {
                    string prop = rest.Substring(0, us);
                    predicate = mm => ReadMotor(mm, idx, prop);
                    return true;
                }
            }

            return _named.TryGetValue(name, out predicate);
        }

        private static bool ReadMotor(MachineModel mm, int idx, string prop)
        {
            if (mm?.Motors == null || idx < 0 || idx >= mm.Motors.Count) return false;
            var m = mm.Motors[idx];
            switch (prop)
            {
                case "homed": return m.Homed;
                case "org": return m.ORG;
                case "alm": return m.ALM;
                case "busy": return m.Busy;
                case "inp": return m.INP;
                case "on": return m.ORG;
                default: return false;
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
