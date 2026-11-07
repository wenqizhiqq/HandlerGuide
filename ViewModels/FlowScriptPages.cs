// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  ViewModels/FlowScriptPages.cs —— 运动仿真页底部的「工艺流程脚本」面板
//
//  布局：左侧流程表（绑 Flow/LuaFlowHost.Rows()，列 = 名称 / 状态 / 循环数 /
//  当前步 / 持有令牌 / 错误），右侧当前选中流程的 Lua 编辑器，
//  面板底部 [装载] [全部启动] [全部停止] [启动此条] [停止此条] [保存]。
//
//  接线全部沿用本工程现成的路子，没有另起任何 MVVM 框架：
//    · 元素树全部用 E.* 工厂（E.Band / E.H / E.V / E.Group / E.Scroll / E.Live /
//      E.MiniBtn / E.Btn / E.ShowIf）；
//    · 表格每一格是一个 LiveElement，走 **LiveValues 具名信号 + LiveRegistry 的
//      500ms 读数通道**（MainWindow 的 500ms DispatcherTimer → MainViewModel.Tick
//      → LiveRegistry.RefreshAll）；**不挂 50ms 快速动画通道**（RegisterFast /
//      FastTick 是给画布逐帧重绘用的，流程状态 500ms 刷一次就够）；
//    · 行要不要显示用 E.ShowIf（VisibleElement），同样是 500ms 通道，
//      所以装载后流程条数变了也不用重建页面；
//    · Pages/motion_sim.xaml 只加一行 <vw:D Method="FlowScriptPanel" />，
//      由 Diagrams.InvokeByName 反射调到本类的同名方法（与 SimPage / CardPage 一致）。
//
//  行数据的取法：一屏要读 64 行 × 6 格 + 64 个显示条件 ≈ 450 次，而 Rows() 每次
//  都要在宿主锁上重建一份列表，所以加一层「按墙钟最多 400ms 回源一次」的快照缓存
//  （见 Snapshot）：一个 500ms 周期里所有格子看到的是同一次扫描的结果。
// ---------------------------------------------------------------------------
//  温启志 18719361399 · 类 public static class FlowScriptPages
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandlerGuide.Editing;
using HandlerGuide.Flow;

namespace HandlerGuide.ViewModels
{
    /// <summary>工艺流程脚本（Lua）面板的声明式构建。</summary>
    public static class FlowScriptPages
    {
        /// <summary>预留行数：Scripts 目录以后加流程不必改这里，多出来的行由 E.ShowIf 自动隐藏。</summary>
        const int MaxRows = 64;

        // 列宽：选中符 / 流程名 / 状态 / 循环数 / 当前步 / 持有令牌 / 错误
        const double WMark = 20, WName = 126, WState = 46, WLoops = 54, WStep = 86, WTok = 70, WErr = 168;

        /// <summary>当前选中的流程名（右侧编辑器显示的就是它）。</summary>
        public static string Selected { get; private set; } = "";

        // ==================================================================
        //  快照缓存
        // ==================================================================

        static List<string[]> _snap = new List<string[]>();
        static int _snapAt = -10000;

        /// <summary>流程表快照：按墙钟最多 400ms 回源一次 LuaFlowHost.Rows()。</summary>
        static List<string[]> Snapshot()
        {
            int now = Environment.TickCount;
            if (now - _snapAt >= 400)
            {
                _snapAt = now;
                try { _snap = LuaFlowHost.Rows() ?? new List<string[]>(); }
                catch { _snap = new List<string[]>(); }
            }
            return _snap;
        }

        /// <summary>第 i 行第 c 列的文本（越界返回空串）。列序：名称 状态 循环数 当前步 持有令牌 错误。</summary>
        public static string Cell(int i, int c)
        {
            var rows = Snapshot();
            if (i < 0 || i >= rows.Count) return "";
            var r = rows[i];
            if (r == null || c < 0 || c >= r.Length) return "";
            return r[c] ?? "";
        }

        /// <summary>第 i 行的流程名（右侧编辑器与各按钮都以它为准）。</summary>
        public static string NameOf(int i) => Cell(i, 0);

        /// <summary>当前流程条数。</summary>
        public static int RowCount => Snapshot().Count;

        /// <summary>第 i 行是不是当前选中行。</summary>
        public static bool IsSelected(int i) => !string.IsNullOrEmpty(Selected) && Cell(i, 0) == Selected;

        /// <summary>让下一次读数立刻回源（装载 / 启停之后调，免得拿到 400ms 内的旧快照）。</summary>
        static void Invalidate() => _snapAt = -10000;

        // ==================================================================
        //  具名实时信号注册（LiveValues → 500ms 通道）
        // ==================================================================

        static bool _liveRegistered;

        /// <summary>注册本面板用到的具名信号；重复调用无副作用。</summary>
        public static void EnsureRegistered()
        {
            if (_liveRegistered) return;
            _liveRegistered = true;

            for (int i = 0; i < MaxRows; i++)
            {
                int row = i;
                LiveValues.Register("fs_name_" + i, _ => Cell(row, 0));
                LiveValues.Register("fs_state_" + i, _ => Cell(row, 1));
                LiveValues.Register("fs_loops_" + i, _ => Cell(row, 2));
                LiveValues.Register("fs_step_" + i, _ => Cell(row, 3));
                LiveValues.Register("fs_tok_" + i, _ => Cell(row, 4));
                LiveValues.Register("fs_err_" + i, _ => Cell(row, 5));
                LiveValues.Register("fs_mark_" + i, _ => IsSelected(row) ? "▶" : "");
            }

            LiveValues.Register("fs_caption", _ => Caption());
            LiveValues.Register("fs_rows", _ => RowCount);
            LiveValues.Register("fs_editors", _ => FlowScriptSession.BuiltCount);
        }

        /// <summary>右侧编辑器顶部说明条：流程名 + 未保存标记 + 运行只读提示。</summary>
        public static string Caption()
        {
            if (string.IsNullOrEmpty(Selected)) return "还没选中流程：点左侧任意一行的〔编辑〕";
            string mark = FlowScriptSession.IsDirty(Selected) ? "● 未保存" : "已保存";
            string run = LuaFlowHost.IsRunning(Selected) ? " · 正在运行，编辑器只读" : "";
            return "正在编辑：" + Selected + ".lua　[" + mark + "]" + run;
        }

        // ==================================================================
        //  面板
        // ==================================================================

        /// <summary>工艺流程脚本面板（Pages/motion_sim.xaml 用 vw:D Method="FlowScriptPanel" 调）。</summary>
        public static Element FlowScriptPanel() => FlowScriptPanel(430);

        /// <summary>同上；listHeight 供紧凑页面（如模块平面仿真右栏）压缩流程表高度。</summary>
        public static Element FlowScriptPanel(double listHeight)
        {
            EnsureRegistered();

            // 编辑器槽是单例：高度跟着流程表一起收（紧凑页面传更小的 listHeight）
            Slot().Height = listHeight;

            // 第一次进页面先把脚本目录扫一遍，否则 Rows() 是空的，左边一张白表
            if (RowCount == 0)
            {
                try { LuaFlowHost.Scan(); } catch { /* 目录读不到不拦页面，装载按钮还能再试 */ }
                Invalidate();
            }

            // 首次进面板就选中第一条：右侧编辑器不会是空白一块，列表也立刻有内容
            if (RowCount > 0 && string.IsNullOrEmpty(Selected)) Select(NameOf(0));

            // ---------------- 左侧流程表 ----------------
            var rows = E.V();
            for (int i = 0; i < MaxRows; i++)
            {
                int row = i;
                rows.Children.Add(E.ShowIf(_ => row < RowCount, FlowRow(row)));
            }

            var list = E.Group("流程列表（" + RowCount + " 条，绑 LuaFlowHost.Rows()，500ms 刷新）",
                E.Scroll(E.V(FlowHead(), rows), listHeight,
                         ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto));

            // ---------------- 右侧编辑器 ----------------
            var editorArea = E.V(
                E.H(Sig("fs_caption", 0, 11.5, E.P, E.BandBlue)),
                Slot());
            editorArea.HAlign = HorizontalAlignment.Stretch;

            var body = E.Grid().Cols("Auto", "*").Rows("Auto");
            body.Add(list, 0, 0);
            body.Add(editorArea, 0, 1);

            return E.Band("工艺流程脚本（Lua · NLua：一条流程 = 一条线程 = 一个独立 Lua 状态）", E.BandGreen,
                body,
                E.H(
                    E.Btn("装载", Load, "PrimaryButton", 12),
                    E.Btn("全部启动", StartAll, "PrimaryButton", 12),
                    E.Btn("全部停止", StopAll, "DangerButton", 12),
                    E.SpacerW(10),
                    E.Btn("启动此条", StartSelected, "PrimaryButton", 12),
                    E.Btn("停止此条", StopSelected, "DangerButton", 12),
                    E.Btn("保存", SaveSelected, "PrimaryButton", 12),
                    E.SpacerW(12),
                    E.T("流程数", 11.5, E.Sec).Wd(48), Sig("fs_rows", 44, 11.5, E.P),
                    E.T("编辑器实例", 11.5, E.Sec).Wd(62).Marg(new Thickness(8, 0, 0, 0)),
                    Sig("fs_editors", 40, 11.5, E.P),
                    E.T("　Ctrl+S = 保存当前脚本 · 打字弹补全 · 点 . 弹成员 · 点 ( 弹签名 · 鼠标停在函数上看宿主说明",
                        10.5, E.Sec)));
        }

        /// <summary>表头一行。</summary>
        static Element FlowHead()
        {
            return E.H(
                Col("", WMark), Col("流程名", WName), Col("状态", WState), Col("循环数", WLoops),
                Col("当前步", WStep), Col("持有令牌", WTok), Col("错误", WErr), Col("", 52))
                .Marg(new Thickness(0, 0, 0, 2));
        }

        static Element Col(string text, double w) => E.T(text, 11, E.Sec, true).Wd(w).Marg(new Thickness(0, 0, 4, 0));

        /// <summary>一行流程：6 个具名信号格 + 一个〔编辑〕按钮。</summary>
        static Element FlowRow(int i)
        {
            return E.H(
                Sig("fs_mark_" + i, WMark, 11, E.Acc),
                Sig("fs_name_" + i, WName, 11, E.P),
                Sig("fs_state_" + i, WState, 10.5, E.Sec),
                Sig("fs_loops_" + i, WLoops, 10.5, E.Sec),
                Sig("fs_step_" + i, WStep, 10.5, E.P),
                Sig("fs_tok_" + i, WTok, 10.5, E.Sec),
                Sig("fs_err_" + i, WErr, 10.5, E.OnRed),
                E.MiniBtn("编辑", () => Select(i), 46))
                .Marg(new Thickness(0, 0, 0, 2));
        }

        /// <summary>一格具名信号读数（LiveElement，注册进 LiveRegistry 的 500ms 通道）。</summary>
        static LiveElement Sig(string signal, double w, double fs, Brush fg, Brush bg = null)
        {
            var e = new LiveElement
            {
                Width = w > 0 ? w : double.NaN,
                Background = bg ?? E.BandGrey,
                Foreground = fg ?? E.P,
                FontSize = fs,
                Align = TextAlignment.Left,
                TextHAlign = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 4, 0)
            };
            e.Signal = signal;
            return e;
        }

        static LuaEditorSlotElement _slot;

        /// <summary>右侧编辑器槽（整个面板就一个槽；切流程只换槽里挂的编辑器实例）。</summary>
        static LuaEditorSlotElement Slot()
        {
            if (_slot != null) return _slot;
            _slot = new LuaEditorSlotElement
            {
                Height = 424,
                HAlign = HorizontalAlignment.Stretch,
                VAlign = VerticalAlignment.Stretch,
                Margin = new Thickness(6, 0, 0, 0)
            };
            return _slot;
        }

        // ==================================================================
        //  面板上的动作
        // ==================================================================

        /// <summary>选中第 i 行（按行号现取流程名，装载过也不点错）。</summary>
        public static void Select(int index)
        {
            Invalidate();
            string name = NameOf(index);
            if (string.IsNullOrEmpty(name))
            {
                E.M.Log("警告", "第 " + (index + 1) + " 行没有流程，先点〔装载〕");
                return;
            }
            Select(name);
        }

        /// <summary>把右侧编辑器换到指定流程（该流程的实例没建过就建一个）。</summary>
        public static void Select(string flowName)
        {
            if (string.IsNullOrEmpty(flowName)) return;
            Selected = flowName;
            Slot().Show(flowName);
            Invalidate();
        }

        /// <summary>装载：重扫脚本目录 + 未改动的编辑器换成盘上内容 + 补一行日志。</summary>
        public static void Load()
        {
            int n;
            try { n = LuaFlowHost.Scan(); }
            catch (Exception ex) { E.M.Log("警告", "脚本目录扫描失败：" + ex.Message); return; }

            var names = new List<string>();
            foreach (var r in LuaFlowHost.Rows())
                if (r != null && r.Length > 0) names.Add(r[0]);

            FlowScriptSession.Prune(names);
            FlowScriptSession.ReloadAll();
            Invalidate();

            E.M.Log("信息", "已装载脚本目录：" + LuaFlowHost.ScriptDir + "，共 " + n + " 条流程");

            if (!string.IsNullOrEmpty(Selected) && !names.Contains(Selected)) Selected = "";
            if (string.IsNullOrEmpty(Selected) && names.Count > 0) Select(names[0]);
        }

        /// <summary>全部启动：一条流程一条线程，各自一个独立 Lua 状态。</summary>
        public static void StartAll()
        {
            SaveIfDirtyAll();
            int n = LuaFlowHost.StartAll();
            FlowScriptSession.SetReadOnlyAll(true);
            E.M.Log("信息", "已启动全部流程：" + n + " 条");
        }

        /// <summary>全部停止。</summary>
        public static void StopAll()
        {
            LuaFlowHost.StopAll();
            FlowScriptSession.SetReadOnlyAll(false);
            E.M.Log("信息", "已请求停止全部流程");
        }

        /// <summary>启动选中的这一条。</summary>
        public static void StartSelected()
        {
            if (EnsureSelected()) return;
            if (FlowScriptSession.IsDirty(Selected)) FlowScriptSession.Save();
            if (LuaFlowHost.StartFlow(Selected))
            {
                var ed = FlowScriptSession.Current;
                if (ed != null) ed.EditorReadOnly = true;
                E.M.Log("信息", "已启动流程：" + Selected);
            }
            else E.M.Log("警告", "启动失败（这条流程不在本次扫描结果里）：" + Selected);
        }

        /// <summary>停止选中的这一条。</summary>
        public static void StopSelected()
        {
            if (EnsureSelected()) return;
            if (LuaFlowHost.StopFlow(Selected))
            {
                var ed = FlowScriptSession.Current;
                if (ed != null) ed.EditorReadOnly = false;
                E.M.Log("信息", "已请求停止流程：" + Selected);
            }
            else E.M.Log("警告", "停止失败（这条流程不在本次扫描结果里）：" + Selected);
        }

        /// <summary>保存当前编辑器内容到盘上（与 Ctrl+S 走同一条路）。</summary>
        public static void SaveSelected()
        {
            if (EnsureSelected()) return;
            FlowScriptSession.Save();
        }

        /// <summary>没选中任何流程就提示一句并返回真。</summary>
        static bool EnsureSelected()
        {
            if (!string.IsNullOrEmpty(Selected)) return false;
            E.M.Log("警告", "先点左侧流程列表里任意一行的〔编辑〕");
            return true;
        }

        /// <summary>全部启动前先把当前带改动的脚本落盘，免得跑的还是旧脚本。</summary>
        static void SaveIfDirtyAll()
        {
            if (!FlowScriptSession.AnyDirty) return;
            FlowScriptSession.Save(Selected);
            E.M.Log("信息", "启动前已把带改动的脚本落盘");
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
