// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Editing/LuaEditorControl.cs —— 可复用的 Lua 脚本编辑器控件
//  AvalonEdit TextEditor 的包装：等宽字体 + 行号 + Lua.xshd 语法高亮 +
//  语义着色 + 自动缩进 + 智能提示（补全 / 成员 / 函数签名）+ 悬停看宿主说明 +
//  Ctrl+S 触发保存回调 + 只读态 + 「有未保存改动」标记。
//
//  装配逻辑取自 NoCodeMotion 的 Views/LuaEditorView.xaml.cs 里与编辑器本身相关的那
//  一部分（SetupEditor / 智能提示两个 region），剥掉了 MoonSharp 调试器相关的东西：
//  断点边栏、当前执行行高亮、每行耗时边栏、运行时变量取值弹框 —— 本工程运行时是
//  NLua，一条流程一条线程，没有 MoonSharp 的调试器协议可问，全部不做。
//
//  一个实例 = 一个独立 TextDocument：宿主（Views/FlowEditor.cs）给每条流程建一个
//  本控件，切走的实例留在字典里，未保存的改动与撤销历史都不丢。
// ---------------------------------------------------------------------------
//  温启志 18719361399 · 类 public sealed class LuaEditorControl
// ============================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;

namespace HandlerGuide.Editing
{
    /// <summary>一个 Lua 脚本的编辑控件（带高亮 / 补全 / 缩进 / Ctrl+S / 脏标记）。</summary>
    public sealed class LuaEditorControl : TextEditor
    {
        // ---------------- 与宿主回调 ----------------

        /// <summary>Ctrl+S（或调用 RequestSave）时触发：由宿主负责真正写盘，并在写完后调 MarkSaved()。</summary>
        public event Action SaveRequested;

        /// <summary>脏状态变化（true = 有未保存改动）。</summary>
        public event Action<bool> DirtyChanged;

        private string _savedText = "";
        private bool _loading;

        private CompletionWindow _completionWindow;
        private InsightWindow _insightWindow;
        private readonly Popup _hoverTip;

        /// <summary>本编辑器对应的流程名（脚本文件名，不带 .lua）。</summary>
        public string FlowName { get; private set; }

        /// <summary>是否有未保存改动。</summary>
        public bool IsDirty { get; private set; }

        /// <summary>给界面显示的脏标记文字（● 未保存 / 已保存）。</summary>
        public string DirtyMark => IsDirty ? "● 未保存" : "已保存";

        /// <summary>语法高亮是否已挂上（装载后给日志用）。</summary>
        public bool HighlightingLoaded { get; private set; }

        public LuaEditorControl(string flowName)
        {
            FlowName = flowName ?? "";

            // ---- 外观：等宽 + 行号 ----
            FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New");
            FontSize = 13;
            ShowLineNumbers = true;
            LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x93));
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Padding = new Thickness(4, 6, 0, 6);
            Background = Brushes.White;

            Options.ConvertTabsToSpaces = true;
            Options.IndentationSize = 4;
            Options.HighlightCurrentLine = true;
            Options.EnableRectangularSelection = true;
            Options.AllowScrollBelowDocument = true;

            // ---- 自动缩进：then / do / function 进一级，end / else / until 退一级 ----
            TextArea.IndentationStrategy = new LuaIndentationStrategy();

            // ---- 语义着色：在 xshd 语法高亮之上，给用户自定义变量 / 函数区别色 ----
            TextArea.TextView.LineTransformers.Add(new LuaSemanticColorizer());

            LoadHighlighting();

            // ---- 智能提示 ----
            TextArea.TextEntering += TextArea_TextEntering;
            TextArea.TextEntered += TextArea_TextEntered;

            // ---- 脏跟踪 ----
            TextChanged += (s, e) =>
            {
                if (_loading) return;
                bool d = Text != _savedText;
                if (d == IsDirty) return;
                IsDirty = d;
                DirtyChanged?.Invoke(IsDirty);
            };

            // ---- Ctrl+S：只发保存请求，写盘由宿主做 ----
            PreviewKeyDown += LuaEditorControl_PreviewKeyDown;

            // ---- 悬停看宿主 API 说明 ----
            _hoverTip = new Popup
            {
                AllowsTransparency = true,
                StaysOpen = false,
                Placement = PlacementMode.Relative,
                PlacementTarget = this
            };
            TextArea.TextView.MouseHover += TextView_MouseHover;
            TextArea.TextView.MouseHoverStopped += (s, e) => { _hoverTip.IsOpen = false; };
        }

        // ==================================================================
        //  语法高亮（Assets/Lua.xshd，编译进程序集）
        // ==================================================================

        /// <summary>
        /// 从嵌入资源里取 Lua.xshd。本工程源文件没有磁盘加密包装，
        /// 所以嵌入资源这一条路就够（源项目还要留一条内联 XML 的兜底，这里不留）。
        /// </summary>
        void LoadHighlighting()
        {
            try
            {
                Assembly asm = typeof(LuaEditorControl).Assembly;
                string found = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("Lua.xshd", StringComparison.OrdinalIgnoreCase));
                if (found == null) return;

                using (Stream stream = asm.GetManifestResourceStream(found))
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingLoaded = true;
                }
            }
            catch
            {
                // 高亮加载失败不拦编辑：只是退回无颜色，脚本照样能写能存
                HighlightingLoaded = false;
            }
        }

        // ==================================================================
        //  内容装载 / 保存
        // ==================================================================

        /// <summary>装入脚本文本并清脏标记（程序装载用，不算用户改动）。</summary>
        public void LoadText(string text)
        {
            _loading = true;
            Document.Text = text ?? "";
            _savedText = Document.Text;
            _loading = false;
            if (IsDirty) { IsDirty = false; DirtyChanged?.Invoke(false); }
            TextArea.Caret.Offset = 0;
            ScrollToLine(1);
        }

        /// <summary>宿主写盘成功后调用：当前文本变成新的基准，脏标记清掉。</summary>
        public void MarkSaved()
        {
            _savedText = Text;
            if (IsDirty) { IsDirty = false; DirtyChanged?.Invoke(false); }
        }

        /// <summary>主动请求保存（界面按钮与 Ctrl+S 共用一条路）。</summary>
        public void RequestSave() => SaveRequested?.Invoke();

        /// <summary>只读态：运行中改脚本容易自欺欺人，宿主用它把编辑器冻住。</summary>
        public bool EditorReadOnly
        {
            get => IsReadOnly;
            set { IsReadOnly = value; }
        }

        void LuaEditorControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (!IsReadOnly) RequestSave();
                e.Handled = true;
            }
            else if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                ShowWordCompletion(true);
                e.Handled = true;
            }
        }

        // ==================================================================
        //  智能提示
        // ==================================================================

        void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (_completionWindow == null || e.Text.Length == 0) return;

            char c = e.Text[0];
            if (!char.IsLetterOrDigit(c) && c != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (IsReadOnly || e.Text.Length == 0) return;
            char c = e.Text[0];

            if (c == '.' || c == ':')
                ShowMemberCompletion();
            else if (char.IsLetter(c) || c == '_')
            {
                if (_completionWindow == null) ShowWordCompletion(false);
            }
            else if (c == '(')
                ShowSignatureInsight();
        }

        /// <summary>标识符 / 关键字 / 代码片段补全。</summary>
        void ShowWordCompletion(bool force)
        {
            int caret = CaretOffset;
            int start = FindWordStart(caret);
            string prefix = Document.GetText(start, caret - start);

            if (!force && prefix.Length == 0) return;

            var items = new List<LuaCompletionData>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            void Add(LuaSymbol symbol, double priority)
            {
                if (symbol == null || !used.Add(symbol.Name)) return;
                items.Add(new LuaCompletionData(symbol, priority));
            }

            // 1. 宿主注册的全局函数（最有用，排最前）：写错就是运行时 nil
            foreach (LuaSymbol s in LuaApi.HostList) Add(s, 100);

            // 2. 当前脚本里自己定义的函数 / 变量
            foreach (LuaSymbol s in LuaDocumentAnalyzer.Analyze(Text)) Add(s, 50);

            // 3. Lua 标准库全局
            foreach (LuaSymbol s in LuaApi.Globals) Add(s, 20);

            // 4. 关键字与代码片段
            foreach (string k in LuaApi.Keywords)
                Add(new LuaSymbol(k, SymbolKind.Keyword, k, "Lua 关键字"), 10);
            foreach (LuaSymbol s in LuaApi.Snippets) Add(s, 5);

            ShowCompletionWindow(start, items);
        }

        /// <summary>"." / ":" 之后的成员补全。</summary>
        void ShowMemberCompletion()
        {
            int caret = CaretOffset;
            if (caret < 2) return;

            string root = GetIdentifierBefore(caret - 1);
            if (string.IsNullOrEmpty(root)) return;

            var items = new List<LuaCompletionData>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            void Add(LuaSymbol symbol, double priority)
            {
                if (symbol == null || !used.Add(symbol.Name)) return;
                items.Add(new LuaCompletionData(symbol, priority));
            }

            // 标准库成员（string.format 这一类）
            if (LuaApi.TryGetMembers(root, out List<LuaSymbol> members))
                foreach (LuaSymbol s in members) Add(s, 60);

            // 当前脚本里出现过的字段（点位表 / 自建表）
            foreach (LuaSymbol s in LuaDocumentAnalyzer.GetTableFields(Text, root)) Add(s, 30);

            if (items.Count == 0) return;
            ShowCompletionWindow(caret, items);
        }

        void ShowCompletionWindow(int startOffset, List<LuaCompletionData> items)
        {
            if (items.Count == 0) return;

            _completionWindow = new CompletionWindow(TextArea)
            {
                Width = 360,
                MaxHeight = 320,
                CloseAutomatically = true,
                StartOffset = startOffset,
                EndOffset = CaretOffset
            };

            foreach (LuaCompletionData item in items.OrderByDescending(i => i.Priority))
                _completionWindow.CompletionList.CompletionData.Add(item);

            _completionWindow.CompletionList.SelectItem(
                Document.GetText(startOffset, CaretOffset - startOffset));

            _completionWindow.Closed += (s, e) => _completionWindow = null;
            _completionWindow.Show();
        }

        /// <summary>输入 "(" 时的函数签名提示。</summary>
        void ShowSignatureInsight()
        {
            int caret = CaretOffset;
            string name = GetIdentifierBefore(caret - 1);
            if (string.IsNullOrEmpty(name)) return;

            string module = null;
            int idStart = caret - 1 - name.Length;
            if (idStart > 0 && (Document.GetCharAt(idStart - 1) == '.' || Document.GetCharAt(idStart - 1) == ':'))
                module = GetIdentifierBefore(idStart - 1);

            LuaSymbol symbol = module != null
                ? LuaApi.FindMember(module, name) ?? LuaApi.Find(name)
                : LuaApi.Find(name) ?? LuaDocumentAnalyzer.Analyze(Text).FirstOrDefault(s => s.Name == name);

            if (symbol == null || symbol.Kind == SymbolKind.Keyword) return;

            _insightWindow?.Close();
            _insightWindow = new InsightWindow(TextArea) { Content = BuildDocPanel(symbol, 460) };
            _insightWindow.Closed += (s, e) => _insightWindow = null;
            _insightWindow.Show();
        }

        // ==================================================================
        //  悬停：鼠标停在宿主函数上看签名与说明
        // ==================================================================

        void TextView_MouseHover(object sender, MouseEventArgs e)
        {
            var view = TextArea.TextView;
            Point mousePos = e.GetPosition(view);
            TextViewPosition? pos = view.GetPosition(mousePos + view.ScrollOffset);
            if (pos == null) return;

            int offset = Document.GetOffset(pos.Value.Line, pos.Value.Column);
            string word = WordAt(offset);
            LuaSymbol api = LuaApi.FindHost(word) ?? LuaApi.Find(word);
            if (api == null) { _hoverTip.IsOpen = false; return; }

            _hoverTip.Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xCC)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 7, 10, 7),
                Child = BuildDocPanel(api, 420)
            };
            _hoverTip.HorizontalOffset = mousePos.X + 12;
            _hoverTip.VerticalOffset = mousePos.Y + 16;
            _hoverTip.IsOpen = true;
        }

        static StackPanel BuildDocPanel(LuaSymbol symbol, double maxWidth)
        {
            var panel = new StackPanel { MaxWidth = maxWidth };
            panel.Children.Add(new TextBlock
            {
                Text = symbol.Signature,
                FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New"),
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(symbol.Description))
                panel.Children.Add(new TextBlock
                {
                    Text = symbol.Description,
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
                });
            return panel;
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>从 offset 往回找当前标识符的起点。</summary>
        int FindWordStart(int offset)
        {
            int start = offset;
            while (start > 0)
            {
                char c = Document.GetCharAt(start - 1);
                if (char.IsLetterOrDigit(c) || c == '_') start--;
                else break;
            }
            return start;
        }

        /// <summary>取 offset 之前紧挨着的那个标识符。</summary>
        string GetIdentifierBefore(int offset)
        {
            int end = offset;
            while (end > 0)
            {
                char c = Document.GetCharAt(end - 1);
                if (char.IsLetterOrDigit(c) || c == '_') end--;
                else break;
            }
            int start = end;
            while (start < Document.TextLength)
            {
                char c = Document.GetCharAt(start);
                if (char.IsLetterOrDigit(c) || c == '_') start++;
                else break;
            }
            if (start <= end) return "";
            return Document.GetText(end, start - end);
        }

        /// <summary>取光标所在处的整个标识符（悬停用）。</summary>
        string WordAt(int offset)
        {
            if (offset <= 0 || offset > Document.TextLength) return "";
            char at = offset < Document.TextLength ? Document.GetCharAt(offset) : '\0';
            if (!char.IsLetterOrDigit(at) && at != '_')
            {
                if (offset == 0) return "";
                offset--;
                at = Document.GetCharAt(offset);
                if (!char.IsLetterOrDigit(at) && at != '_') return "";
            }
            int s = offset, t = offset;
            while (s > 0 && (char.IsLetterOrDigit(Document.GetCharAt(s - 1)) || Document.GetCharAt(s - 1) == '_')) s--;
            while (t < Document.TextLength && (char.IsLetterOrDigit(Document.GetCharAt(t)) || Document.GetCharAt(t) == '_')) t++;
            return Document.GetText(s, t - s);
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
