// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（LiveValues）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_LiveValues
    internal static class WenQiZhiTag_LiveValues
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · LiveValues。</summary>
        internal const string WenQiZhiFile = "LiveValues";
    }

    // 温启志 18719361399 · 类 public static class LiveValues
    /// <summary>
    /// 命名实时数值源注册表：Live 显示框通过字符串信号名从模型读取数值。
    /// 仅做“数据→对象”的映射，不含任何界面构造逻辑。
    /// </summary>
    public static class LiveValues
    {
        private static readonly Dictionary<string, Func<MachineModel, object>> _named =
            new Dictionary<string, Func<MachineModel, object>>();

        public static void Register(string name, Func<MachineModel, object> getter)
        {
            if (!string.IsNullOrEmpty(name)) _named[name] = getter;
        }

        public static bool TryGet(string name, out Func<MachineModel, object> getter)
        {
            getter = null;
            if (string.IsNullOrEmpty(name)) return false;
            if (_named.TryGetValue(name, out getter)) return true;

            // 反射回退：name 直接对应 MachineModel 的属性（如 "RunState"、"TempSet"）
            var prop = typeof(MachineModel).GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                getter = mm => prop.GetValue(mm);
                return true;
            }
            return false;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
