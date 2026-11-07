// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Editing/FlowScriptSession.cs —— 「一条流程一个编辑器实例」的登记表
//
//  职责只有一件：把 流程名 → LuaEditorControl 实例 一一对应地存住，并且只存住。
//    · 每条流程第一次被选中时才 new 一个 LuaEditorControl（27 条全建太浪费）；
//    · 实例建好就一直活在本字典里，切走不销毁 —— 未保存的文本、撤销栈、光标位置、
//      滚动位置全在实例自己的 TextDocument / TextArea 上，所以都不会丢；
//    · 因此「不共享文档」是结构上保证的：TextEditor 基类在构造时就各自 new 了一个
//      TextDocument，本类从不把某个实例的 Document 赋给另一个实例。
//      （Debug 校验：GetEditor(a).Document != GetEditor(b).Document，a≠b 恒成立。）
//
//  读盘 / 写盘一律走 Flow/LuaFlowHost.cs 的 ReadScript / WriteScript，本类不碰路径。
// ---------------------------------------------------------------------------
//  温启志 18719361399 · 类 public static class FlowScriptSession
// ============================================================================
#nullable disable
using System;
using System.Collections.Generic;
using HandlerGuide.Flow;

namespace HandlerGuide.Editing
{
    /// <summary>Lua 流程脚本的编辑器登记表（一条流程一个实例）。</summary>
    public static class FlowScriptSession
    {
        static readonly Dictionary<string, LuaEditorControl> Editors =
            new Dictionary<string, LuaEditorControl>(StringComparer.Ordinal);

        static string _selected = "";

        /// <summary>当前选中（正在编辑）的流程名；没选过时为空串。</summary>
        public static string Selected
        {
            get => _selected;
            private set { _selected = value ?? ""; }
        }

        /// <summary>当前选中的编辑器实例；未选中任何流程时返回 null。</summary>
        public static LuaEditorControl Current =>
            string.IsNullOrEmpty(Selected) ? null : GetEditor(Selected);

        /// <summary>取（或首次建）某条流程的编辑器实例。</summary>
        public static LuaEditorControl GetEditor(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (Editors.TryGetValue(name, out var ed)) return ed;

            ed = new LuaEditorControl(name);
            ed.LoadText(SafeRead(name));
            ed.SaveRequested += () => Save(name);
            Editors[name] = ed;
            return ed;
        }

        /// <summary>选中某条流程，返回它的编辑器实例（页面右侧显示这个）。</summary>
        public static LuaEditorControl Select(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Selected = name;
            return GetEditor(name);
        }

        /// <summary>某条流程是否有未保存改动。</summary>
        public static bool IsDirty(string name) =>
            !string.IsNullOrEmpty(name) && Editors.TryGetValue(name, out var ed) && ed.IsDirty;

        /// <summary>任意一条流程有未保存改动时为真。</summary>
        public static bool AnyDirty
        {
            get
            {
                foreach (var kv in Editors)
                    if (kv.Value.IsDirty) return true;
                return false;
            }
        }

        /// <summary>脏标记文字（给界面显示：● 未保存 / 已保存）。</summary>
        public static string DirtyMark
        {
            get
            {
                var ed = Current;
                return ed == null ? "" : ed.DirtyMark;
            }
        }

        /// <summary>把当前选中流程的编辑器内容写盘（LuaFlowHost.WriteScript）。</summary>
        public static bool Save() => Save(Selected);

        /// <summary>把某条流程的编辑器内容写盘；成功返回 true。</summary>
        public static bool Save(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!Editors.TryGetValue(name, out var ed)) return false;
            try
            {
                LuaFlowHost.WriteScript(name, ed.Text);
                ed.MarkSaved();
                MachineModel.Instance.Log("信息", "脚本已保存：" + name + ".lua");
                return true;
            }
            catch (Exception ex)
            {
                MachineModel.Instance.Log("警告", "脚本保存失败：" + name + " · " + ex.Message);
                return false;
            }
        }

        /// <summary>重新扫描后调用：丢掉已经不存在的热编辑器，留下还在的（未保存内容不冲掉）。</summary>
        public static void Prune(ICollection<string> liveNames)
        {
            if (liveNames == null) return;
            var dead = new List<string>();
            foreach (var kv in Editors)
                if (!liveNames.Contains(kv.Key) && !kv.Value.IsDirty) dead.Add(kv.Key);
            foreach (var k in dead) Editors.Remove(k);
            if (!string.IsNullOrEmpty(Selected) && !liveNames.Contains(Selected)) Selected = "";
        }

        /// <summary>把所有编辑器换成磁盘上的最新内容（运行中不叫用，避免冲掉未保存改动）。</summary>
        public static void ReloadAll()
        {
            foreach (var kv in Editors)
                if (!kv.Value.IsDirty) kv.Value.LoadText(SafeRead(kv.Key));
        }

        /// <summary>只读态：流程在跑的时候编辑器冻住（改脚本要么先停，要么改了不会被踩到）。</summary>
        public static void SetReadOnlyAll(bool readOnly)
        {
            foreach (var kv in Editors) kv.Value.EditorReadOnly = readOnly;
        }

        /// <summary>已建出的编辑器条数（自检 / 日志用）。</summary>
        public static int BuiltCount => Editors.Count;

        /// <summary>校验：已建的编辑器之间文档互不相同（两条流程绝不共享同一份文本）。</summary>
        public static bool DocumentsAreDistinct()
        {
            var seen = new HashSet<ICSharpCode.AvalonEdit.Document.TextDocument>(
                ReferenceComparer<ICSharpCode.AvalonEdit.Document.TextDocument>.Instance);
            foreach (var kv in Editors)
                if (!seen.Add(kv.Value.Document)) return false;
            return true;
        }

        static string SafeRead(string name)
        {
            try { return LuaFlowHost.ReadScript(name) ?? ""; }
            catch { return ""; }
        }
    }

    /// <summary>引用相等比较器（只为 DocumentsAreDistinct 的自检用）。</summary>
    sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
        public bool Equals(T a, T b) => ReferenceEquals(a, b);
        public int GetHashCode(T v) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(v);
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
