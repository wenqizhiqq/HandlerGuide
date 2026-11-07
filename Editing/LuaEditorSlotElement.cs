// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Editing/LuaEditorSlotElement.cs —— 编辑器槽（把编辑器接进现有 Element 树）
//
//  HandlerGuide 的页面是「Element 视图模型树 + Views/Elements.xaml 的 DataTemplate」，
//  树里全是视图模型、没有控件。脚本编辑器天生是个真控件（AvalonEdit TextEditor），
//  所以给它开一个槽：槽本身是一个 Element（视图模型），槽里挂一条流程专属的
//  LuaEditorControl 实例。切换流程 = 换槽里的实例，不是换文档，因此
//  「每条流程一个编辑器实例、切走时各自的未保存状态不丢」是结构性成立的。
//
//  为什么放在 Editing 命名空间而不是 ViewModels：这样 ViewModels 一侧
//  不需要出现任何 WPF 控件类型，控件的创建与持有都收在编辑模块里。
// ---------------------------------------------------------------------------
//  温启志 18719361399 · 类 public class LuaEditorSlotElement
// ============================================================================
#nullable disable
using System;
using System.Windows;
using HandlerGuide.ViewModels;

namespace HandlerGuide.Editing
{
    /// <summary>脚本编辑器槽位：把某条流程专属的编辑器实例显示在 Element 树里。</summary>
    public class LuaEditorSlotElement : Element
    {
        UIElement _child;
        string _caption = "";

        /// <summary>槽里当前挂的编辑器实例（由 <see cref="FlowScriptSession"/> 按流程名给出）。</summary>
        public UIElement Child
        {
            get => _child;
            set
            {
                if (Set(ref _child, value)) { Raise(nameof(HasChild)); Raise(nameof(ChildVis)); }
            }
        }

        /// <summary>槽顶部的说明条文字（当前流程名 + 未保存标记 + 只读提示）。</summary>
        public string Caption
        {
            get => _caption;
            set { Set(ref _caption, value); }
        }

        /// <summary>视图层用它决定占位提示是否显示。</summary>
        public bool HasChild => _child != null;

        /// <summary>视图层绑定：Child 为空时显示「先点左侧流程行的〔编辑〕」占位。</summary>
        public Visibility ChildVis => _child == null ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>换挂某条流程的编辑器实例（实例由会话登记，一条流程一个，绝不共享文档）。</summary>
        public void Show(string flowName)
        {
            var ed = FlowScriptSession.Select(flowName);
            Child = ed;
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
