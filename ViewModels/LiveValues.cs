using System;
using System.Collections.Generic;
using System.Reflection;

namespace HandlerGuide.ViewModels
{
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
