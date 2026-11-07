// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Editing/LuaSemanticColorizer.cs —— 语法高亮之上的语义着色
//  从 NoCodeMotion 的 Editing/LuaSemanticColorizer.cs 移植。改动一处：
//  原来「xshd 已负责着色」的名单取 LuaApi.HardwareList（源项目的 AxisMove / ReadIO
//  那一套硬件函数），现在取本工程宿主注册的 LuaApi.HostList（running / moveabs …）；
//  标准库表名那一半（LuaApi.ModuleNames）不变。
// ---------------------------------------------------------------------------
//  温启志 18719361399 · 类 public sealed class LuaSemanticColorizer
// ============================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace HandlerGuide.Editing
{
    /// <summary>
    /// 语义着色：在语法高亮（关键字 / 字符串 / 数字 / 注释）之上，对「用户定义的变量 / 函数 / 表字段」
    /// 额外着以区别色，使变量在编辑器中一眼可辨。
    /// 仅在标识符不在注释 / 字符串内时才着色，避免误染。
    /// 宿主注册的全局函数与标准库表名交给 xshd 着色，这里不重复覆盖，避免同色冲突。
    /// </summary>
    public sealed class LuaSemanticColorizer : DocumentColorizingTransformer
    {
        // 用户自定义变量：蓝；用户自定义函数：棕（与 xshd 的 Function 色一致）
        // 苹果浅色系：变量深蓝、自定义函数青绿（关键字紫 #9B2393 / 宿主函数蓝 #0B6FA4 在 xshd 里）
        private static readonly Brush VarBrush = new SolidColorBrush(Color.FromRgb(0x0B, 0x5F, 0xA5));
        private static readonly Brush FuncBrush = new SolidColorBrush(Color.FromRgb(0x0B, 0x72, 0x85));

        private static readonly Regex Identifier = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly Regex CommentLine = new Regex(@"--[^\n]*", RegexOptions.Compiled);
        private static readonly Regex StringLiteral = new Regex("\"(\\\\.|[^\"\\\\])*\"|'(\\\\.|[^'\\\\])*'", RegexOptions.Compiled);

        /// <summary>
        /// 已由 xshd 语法高亮负责的名字（宿主注册的全局函数 + 标准库表名），语义着色器不再覆盖，
        /// 避免出现「同色冲突 / 颜色被刷掉」：宿主函数保持棕色、标准库表保持青绿、关键字保持紫色。
        /// </summary>
        private static readonly HashSet<string> XshdCovered = BuildXshdCovered();

        private static HashSet<string> BuildXshdCovered()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (LuaSymbol s in LuaApi.HostList) set.Add(s.Name);
            foreach (string m in LuaApi.ModuleNames) set.Add(m);
            return set;
        }

        // 按文档内容缓存符号集合，避免每行重复分析
        private string _cacheText;
        private HashSet<string> _funcSet;
        private HashSet<string> _varSet;

        protected override void ColorizeLine(DocumentLine line)
        {
            string full = CurrentContext.Document.Text;
            if (full != _cacheText)
            {
                RebuildCache(full);
                _cacheText = full;
            }

            string text = CurrentContext.Document.GetText(line);
            string masked = CommentLine.Replace(text, m => Blank(m.Value));
            masked = StringLiteral.Replace(masked, m => Blank(m.Value));

            foreach (Match m in Identifier.Matches(masked))
            {
                string id = m.Value;
                if (LuaApi.Keywords.Contains(id)) continue;   // 关键字交给 xshd 着色
                if (XshdCovered.Contains(id)) continue;       // 宿主函数 / 标准库表交给 xshd 着色

                Brush brush = null;
                if (_funcSet.Contains(id)) brush = FuncBrush;
                else if (_varSet.Contains(id)) brush = VarBrush;

                if (brush != null)
                    ChangeLinePart(line.Offset + m.Index, line.Offset + m.Index + m.Length,
                        v => v.TextRunProperties.SetForegroundBrush(brush));
            }
        }

        private void RebuildCache(string full)
        {
            var locals = LuaDocumentAnalyzer.Analyze(full);
            _funcSet = new HashSet<string>(StringComparer.Ordinal);
            _varSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (LuaSymbol s in locals)
            {
                if (s.Kind == SymbolKind.Function) _funcSet.Add(s.Name);
                else if (s.Kind != SymbolKind.Keyword) _varSet.Add(s.Name);
            }
        }

        private static string Blank(string original) =>
            new string(original.Select(c => c == '\n' ? '\n' : ' ').ToArray());
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
