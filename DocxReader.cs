// ============================================================================
//  DocxReader.cs — 原生 docx 解析（无第三方依赖）
//  项目：HandlerGuide（EXCEED9000 控制软件界面复刻）
//  职责：用 System.IO.Compression + System.Xml 解析手册 .docx，
//        提取段落（保留 run 格式）/ 表格 / 图片，供「说明书」页复用。
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace HandlerGuide
{
    // 文档块类型
    public class DocRun
    {
        public string Text = "";
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public double Sz = 21;          // 半磅
        public string Color;            // 十六进制，如 FF0000
    }

    public class DocParagraph
    {
        public List<DocRun> Runs = new();
        public string Align = "left";
        public bool Title;
        public string Text => string.Concat(Runs.Select(r => r.Text));
    }

    public class DocTable
    {
        public int Cols;
        public List<List<List<DocRun>>> Rows = new(); // [row][col] = runs
    }

    public class DocImage
    {
        public BitmapImage Bmp;
        public int OrigW;
        public int OrigH;
        public bool Placeholder;
        public string Name;
    }

    // 标记类，便于在 UI 层用 is 判断
    public class ParaBlock { public DocParagraph P; }
    public class TableBlock { public DocTable T; }
    public class ImageBlock { public DocImage I; }

    public static class DocxReader
    {
        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static List<object> Read(string docxPath)
        {
            var blocks = new List<object>();
            using var zip = ZipFile.OpenRead(docxPath);

            // 关系映射：rId -> media 目标
            var rels = new Dictionary<string, string>();
            var relsEntry = zip.GetEntry("word/_rels/document.xml.rels");
            if (relsEntry != null)
            {
                var relsDoc = XDocument.Load(relsEntry.Open());
                foreach (var rel in relsDoc.Descendants().Where(e => e.Name.LocalName == "Relationship"))
                {
                    var id = rel.Attribute("Id")?.Value;
                    var target = rel.Attribute("Target")?.Value;
                    if (id != null && target != null) rels[id] = target;
                }
            }

            // 预读图片字节
            var mediaBytes = new Dictionary<string, byte[]>();
            foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase)))
            {
                using var s = entry.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                mediaBytes[entry.FullName.ToLowerInvariant()] = ms.ToArray();
            }

            var docEntry = zip.GetEntry("word/document.xml");
            if (docEntry == null) return blocks;
            var doc = XDocument.Load(docEntry.Open());

            XElement body = null;
            foreach (var el in doc.Descendants())
                if (el.Name.LocalName == "body") { body = el; break; }
            if (body == null) return blocks;

            ProcessContainer(body, blocks, rels, mediaBytes);
            return blocks;
        }

        // 处理一个容器（body 或 sdtContent），按顺序把段落/表格/图片加入 blocks
        private static void ProcessContainer(XElement container, List<object> blocks,
            Dictionary<string, string> rels, Dictionary<string, byte[]> mediaBytes)
        {
            foreach (var child in container.Elements())
            {
                var local = child.Name.LocalName;
                if (local == "p")
                {
                    ProcessParagraph(child, blocks, rels, mediaBytes);
                }
                else if (local == "tbl")
                {
                    var t = ParseTable(child);
                    if (t != null) blocks.Add(new TableBlock { T = t });
                }
                else if (local == "sdt")
                {
                    // 结构化文档标签：递归处理其 sdtContent
                    var content = child.Elements().FirstOrDefault(e => e.Name.LocalName == "sdtContent");
                    if (content != null) ProcessContainer(content, blocks, rels, mediaBytes);
                }
                // 其它（sectPr 等）忽略
            }
        }

        private static void ProcessParagraph(XElement p, List<object> blocks,
            Dictionary<string, string> rels, Dictionary<string, byte[]> mediaBytes)
        {
            var runs = ExtractRuns(p);
            var text = string.Concat(runs.Select(r => r.Text));
            var align = "left";
            var jc = p.Elements(W + "pPr").Elements(W + "jc").FirstOrDefault()?.Attribute(W + "val")?.Value;
            if (jc == "center") align = "center";
            else if (jc == "right") align = "right";
            else if (jc == "both" || jc == "distribute") align = "justify";

            bool hasBold = runs.Any(r => r.Bold);
            bool isTitle = !string.IsNullOrWhiteSpace(text)
                           && text.Length <= 40
                           && hasBold
                           && !text.All(char.IsDigit);

            if (!string.IsNullOrWhiteSpace(text))
            {
                blocks.Add(new ParaBlock
                {
                    P = new DocParagraph
                    {
                        Runs = runs.Where(r => r.Text.Length > 0).ToList(),
                        Align = align,
                        Title = isTitle
                    }
                });
            }

            // 段落内的图片（inline drawing）
            foreach (var blip in p.Descendants().Where(e => e.Name.LocalName == "blip"))
            {
                var rId = blip.Attribute(R + "embed")?.Value ?? blip.Attribute(R + "link")?.Value;
                if (rId != null && rels.TryGetValue(rId, out var target))
                {
                    var mediaPath = ("word/" + target).Replace('\\', '/').ToLowerInvariant();
                    if (mediaBytes.TryGetValue(mediaPath, out var bytes))
                    {
                        var img = MakeImage(bytes, target);
                        if (img != null) blocks.Add(new ImageBlock { I = img });
                    }
                }
            }
        }

        private static DocTable ParseTable(XElement tbl)
        {
            var rows = new List<List<List<DocRun>>>();
            int maxCols = 0;
            foreach (var tr in tbl.Elements().Where(e => e.Name.LocalName == "tr"))
            {
                var cells = new List<List<DocRun>>();
                foreach (var tc in tr.Elements().Where(e => e.Name.LocalName == "tc"))
                {
                    var runs = ExtractRuns(tc);
                    cells.Add(runs.Where(r => r.Text.Length > 0).ToList());
                }
                maxCols = Math.Max(maxCols, cells.Count);
                rows.Add(cells);
                if (rows.Count > 200) break; // 保护
            }
            if (rows.Count == 0) return null;
            return new DocTable { Cols = maxCols, Rows = rows };
        }

        private static List<DocRun> ExtractRuns(XElement el)
        {
            var runs = new List<DocRun>();
            foreach (var r in el.Descendants(W + "r"))
            {
                var rpr = r.Element(W + "rPr");
                bool b = rpr?.Element(W + "b") != null;
                bool i = rpr?.Element(W + "i") != null;
                bool u = rpr?.Element(W + "u") != null;
                double sz = 21;
                var szEl = rpr?.Element(W + "sz");
                if (szEl != null && double.TryParse(szEl.Attribute(W + "val")?.Value, out var s)) sz = s;
                string color = rpr?.Element(W + "color")?.Attribute(W + "val")?.Value;

                string txt = "";
                foreach (var c in r.Elements())
                {
                    if (c.Name == W + "t") txt += c.Value;
                    else if (c.Name == W + "tab") txt += "    ";
                    else if (c.Name == W + "br" || c.Name == W + "cr") txt += "\n";
                }
                if (txt.Length > 0)
                    runs.Add(new DocRun { Text = txt, Bold = b, Italic = i, Underline = u, Sz = sz, Color = color });
            }
            return runs;
        }

        private static DocImage MakeImage(byte[] bytes, string target)
        {
            var lower = target.ToLowerInvariant();
            var name = System.IO.Path.GetFileName(target);
            if (lower.EndsWith(".emf"))
            {
                return new DocImage { Placeholder = true, Name = name };
            }
            try
            {
                var bmp = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return new DocImage
                {
                    Bmp = bmp,
                    OrigW = bmp.PixelWidth,
                    OrigH = bmp.PixelHeight,
                    Placeholder = false,
                    Name = name
                };
            }
            catch
            {
                return new DocImage { Placeholder = true, Name = name };
            }
        }

        // 把十六进制颜色转成画刷，失败返回 null
        public static Brush ColorToBrush(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            try
            {
                var s = hex.Trim();
                if (s.StartsWith("#")) s = s.Substring(1);
                if (s.Length == 6)
                    return new SolidColorBrush(Color.FromRgb(
                        Convert.ToByte(s.Substring(0, 2), 16),
                        Convert.ToByte(s.Substring(2, 2), 16),
                        Convert.ToByte(s.Substring(4, 2), 16)));
                if (s.Length == 8)
                    return new SolidColorBrush(Color.FromRgb(
                        Convert.ToByte(s.Substring(2, 2), 16),
                        Convert.ToByte(s.Substring(4, 2), 16),
                        Convert.ToByte(s.Substring(6, 2), 16)));
            }
            catch { }
            return null;
        }
    }
}
