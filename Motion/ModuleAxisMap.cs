// ============================================================================
//  温启志 18719361399 · 本文件由 温启志 18719361399 编写与维护
//  版权所有所有权利保留；界面与源码中文命名风格亦归温启志 18719361399 所有。
// ============================================================================
//  Motion/ModuleAxisMap.cs —— 「模块平面仿真」模组与真表轴/IO 的对照层。
//  换真表后，平面仿真页文案里写死的旧轴号（轴 30 / 轴 28 / 轴 2 / OUT15）会静默
//  指向别的轴，甚至指向根本不存在的点。这里统一按真名解析，轴号与 IO 地址只从
//  HandoverTables 现取，界面拿到的文案永远是当前真表里的号。
//  接线：LayoutPages 的模组表读 Rows()；App.xaml.cs 自检调 Verify() 与
//  UnreferencedAxes()，防止真表加了轴而图示永远是空的。

using System;
using System.Collections.Generic;

namespace HandlerGuide.Motion
{
    /// <summary>一个仿真模组依赖的真实轴 / IO 点 / 外部总线设备。</summary>
    public sealed class ModuleRef
    {
        public string Module;
        public string[] AxisNames;
        public string[] IoNames;
        public string External;
        public string Note;
    }

    /// <summary>模组 → 真表引用对照。</summary>
    public static class ModuleAxisMap
    {
        static readonly string[] LoadNozzles = {
            "上料吸嘴1Z轴", "上料吸嘴2Z轴", "上料吸嘴3Z轴", "上料吸嘴4Z轴",
            "上料吸嘴5Z轴", "上料吸嘴6Z轴", "上料吸嘴7Z轴", "上料吸嘴8Z轴" };

        static readonly string[] UnloadNozzles = {
            "下料吸嘴1Z轴", "下料吸嘴2Z轴", "下料吸嘴3Z轴", "下料吸嘴4Z轴",
            "下料吸嘴5Z轴", "下料吸嘴6Z轴", "下料吸嘴7Z轴", "下料吸嘴8Z轴" };

        public static readonly ModuleRef[] Items =
        {
            new ModuleRef { Module = "上料模组",
                AxisNames = new[] { "上料X轴", "上料Y轴", "上料变距X", "上料变距Y" },
                IoNames = new string[0],
                External = null,
                Note = "上料取放臂在 ARM_X 导轨上走位，变距 XY 调吸距" },

            new ModuleRef { Module = "下料模组",
                AxisNames = new[] { "下料X轴", "下料Y轴", "下料变距X轴", "下料变距Y轴" },
                IoNames = new string[0],
                External = null,
                Note = "下料取放臂反向走位，按 Bin 把已测料分进 Auto1/2/3 仓" },

            new ModuleRef { Module = "移盘模组",
                AxisNames = new[] { "前梭车X轴", "后梭车X轴" },
                IoNames = new string[0],
                External = null,
                Note = "两台梭车在 SHUTTLE 导轨上保持最小间距，上料位↔测试位↔下料位往返" },

            new ModuleRef { Module = "压测模组",
                AxisNames = new[] { "前压头Z轴", "前压头Y轴", "后压头Z轴", "后压头Y轴" },
                IoNames = new[] { "治具锁定气缸1+", "治具锁定气缸1-" },
                External = null,
                Note = "压头按真实 Z 位置下压 / 抬起，接触力由比例阀闭环" },

            new ModuleRef { Module = "料枪模组",
                AxisNames = AllNozzles(),
                IoNames = new[] { "左上料吸真空1", "左上料破真空1" },
                External = null,
                Note = "上下料各 8 根吸嘴独立 Z，滑座与枪管随轴位置伸缩" },

            new ModuleRef { Module = "加热盘模组",
                AxisNames = new string[0],
                IoNames = new string[0],
                External = "485 温控器站6 Shuttle加热 / 站7 Shuttle底部加热",
                Note = "加热由 RS485 温控器驱动；真表数字 IO 里没有加热使能点，旧界面的 OUT15 系臆造" },

            new ModuleRef { Module = "料盘搬运模组",
                AxisNames = new[] { "搬运X轴" },
                IoNames = new[] { "料盘搬运夹爪+" },
                External = null,
                Note = "在进空盘仓↔出空盘仓↔Auto 仓之间搬盘，进区前必须先取令牌" },

            new ModuleRef { Module = "进料仓模组",
                AxisNames = new[] { "进料仓Z轴", "进料仓Y轴" },
                IoNames = new string[0],
                External = null,
                Note = "叠层供料：仓 Z 顶升待测盘，仓 Y 走位对准吸嘴阵列" },

            new ModuleRef { Module = "空盘仓模组",
                AxisNames = new[] { "空盘仓Z轴", "空盘仓Y轴" },
                IoNames = new string[0],
                External = null,
                Note = "空盘源，供搬运臂取盘补位" },

            new ModuleRef { Module = "出盘仓模组",
                AxisNames = new[] { "出盘仓Z轴", "出盘仓Y轴" },
                IoNames = new string[0],
                External = null,
                Note = "满盘整盘升出后进仓塔" },

            new ModuleRef { Module = "Auto1/2/3 收料仓模组",
                AxisNames = new[] { "Auto1Z轴", "Auto1Y轴", "Auto2Z轴", "Auto2Y轴", "Auto3Z轴", "Auto3Y轴" },
                IoNames = new string[0],
                External = null,
                Note = "三仓分时收料，换盘由 isAskForNewTray 握手" },

            new ModuleRef { Module = "旋转工位模组",
                AxisNames = new[] { "上料旋转U轴", "下料旋转U轴" },
                IoNames = new string[0],
                External = null,
                Note = "吸嘴阵列换向，适应不同盘位间距" },

            new ModuleRef { Module = "流水线类模组",
                AxisNames = new string[0],
                IoNames = new string[0],
                External = null,
                Note = "上料/黑盘/彩盘/下料流水线与料盘缓存、手工盘均为时间驱动，不绑真实轴" },
        };

        static string[] AllNozzles()
        {
            var r = new string[LoadNozzles.Length + UnloadNozzles.Length];
            Array.Copy(LoadNozzles, 0, r, 0, LoadNozzles.Length);
            Array.Copy(UnloadNozzles, 0, r, LoadNozzles.Length, UnloadNozzles.Length);
            return r;
        }

        /// <summary>把模组解析成「真名（轴 N）/ 真名（IO 地址）」文案。</summary>
        public static string DriverText(ModuleRef r)
        {
            var parts = new List<string>();
            foreach (var n in r.AxisNames)
            {
                var a = HandoverTables.Axis(n);
                parts.Add(a == null ? n + "（真表缺此轴）" : n + "（轴 " + a.Index + "）");
            }
            foreach (var n in r.IoNames)
            {
                var p = HandoverTables.Output(n) ?? HandoverTables.Input(n);
                parts.Add(p == null ? n + "（真表缺此点）" : n + "（IO " + HandoverAddr.Address(p) + "）");
            }
            string txt = parts.Count == 0 ? "时间驱动（扫描节拍）" : string.Join("、", parts.ToArray());
            if (string.IsNullOrEmpty(r.External)) return txt;
            return parts.Count == 0 ? r.External : txt + "，另由 " + r.External + " 驱动";
        }

        /// <summary>按真名取轴号，解析不到返回 -1。</summary>
        public static int AxisIndexOf(string name)
        {
            var a = HandoverTables.Axis(name);
            return a == null ? -1 : a.Index;
        }

        /// <summary>自检用：所有解析不到真名的引用。空列表 = 平面仿真引用全部命中真表。</summary>
        public static List<string> Verify()
        {
            var bad = new List<string>();
            foreach (var r in Items)
            {
                foreach (var n in r.AxisNames)
                    if (HandoverTables.Axis(n) == null) bad.Add(r.Module + " → 轴「" + n + "」不在真表");
                foreach (var n in r.IoNames)
                    if ((HandoverTables.Output(n) ?? HandoverTables.Input(n)) == null)
                        bad.Add(r.Module + " → IO「" + n + "」不在真表");
            }
            if (HandoverTables.TempStations.Length < 8)
                bad.Add("加热盘模组依赖 485 温控器站 6/7，但真表温控站不足 8 个");
            return bad;
        }

        /// <summary>自检用：真表里有、但没有任何模组引用到的轴（防止新轴沦为死数据）。</summary>
        public static List<string> UnreferencedAxes()
        {
            var used = new HashSet<string>();
            foreach (var r in Items)
                foreach (var n in r.AxisNames) used.Add(n);
            var rest = new List<string>();
            foreach (var a in HandoverTables.Axes)
                if (!used.Contains(a.Name)) rest.Add(a.Index + " " + a.Name);
            return rest;
        }

        /// <summary>
        /// 画布小字：每个模组要写在图上的轴名短标签。
        /// 去掉"轴/取放臂"等冗词、吸嘴按 1~8 合并，保证一行放得下。
        /// 画布端只认这个出口，字号与颜色由 LayoutPages 统一给。
        /// </summary>
        public static List<string> SmallLabels(ModuleRef r)
        {
            var list = new List<string>();
            foreach (var n in r.AxisNames) list.Add(Short(n));
            foreach (var n in r.IoNames) list.Add(Short(n));
            if (!string.IsNullOrEmpty(r.External)) list.Add(r.External);
            var uniq = new List<string>();
            foreach (var x in list) if (!uniq.Contains(x)) uniq.Add(x);
            return uniq;
        }

        /// <summary>按模组名取小字标签，模组名对不上返回空列表。</summary>
        public static List<string> SmallLabels(string moduleName)
        {
            foreach (var r in Items) if (r.Module == moduleName) return SmallLabels(r);
            return new List<string>();
        }

        static string Short(string n)
        {
            int k = n.IndexOf("吸嘴");
            if (k >= 0) return n.Substring(0, k + 3) + "1~8Z";
            return n.Replace("取放臂", "").Replace("轴", "");
        }

        /// <summary>给平面仿真页的三列表：模组 / 驱动（现取真号）/ 说明。</summary>
        public static List<string[]> Rows()
        {
            var list = new List<string[]>();
            foreach (var r in Items) list.Add(new[] { r.Module, DriverText(r), r.Note });
            return list;
        }
    }
}
