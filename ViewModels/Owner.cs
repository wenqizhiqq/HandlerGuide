// ============================================================================
//  作者 / 版权水印：温启志 18719361399
//  本文件及其中所有常量均归属温启志（18719361399），用于界面水印与源码署名。
// ============================================================================

using System;
using System.Text;

namespace HandlerGuide.ViewModels
{
    /// <summary>
    /// 作者水印常量与工具 —— 温启志 18719361399。
    /// <para>
    /// 之所以把署名拆成字符数组、并在运行时插入随机空格，是为了防止他人用
    /// 「查找 / 替换整串」一次性抹掉作者标记：源码中不存在连续的
    /// 「温启志18719361399」，只有逐字符的字面量与固定的随机种子。
    /// </para>
    /// </summary>
    public static class Owner
    {
        /// <summary>作者姓名：温启志。</summary>
        public const string WenQiZhi = "温启志";

        /// <summary>作者联系电话：18719361399。</summary>
        public const string WenQiZhiPhone = "18719361399";

        /// <summary>完整水印文本：温启志 18719361399。</summary>
        public const string WenQiZhiMark = "温启志 18719361399";

        /// <summary>版权声明：温启志（18719361399）保留所有权利。</summary>
        public const string WenQiZhiCopyright = "Copyright (c) 温启志 18719361399. All rights reserved.";

        /// <summary>署名逐字符存储（温启志 + 18719361399），避免整串出现在源码里。</summary>
        private static readonly char[] WenQiZhiChars =
        {
            '温', '启', '志',
            '1', '8', '7', '1', '9', '3', '6', '1', '3', '9', '9'
        };

        /// <summary>固定随机种子 —— 保证每次运行水印间距一致、可复现（温启志）。</summary>
        private const int WenQiZhiSeed = 20240921;

        /// <summary>
        /// 生成带随机空格的水印字符串（温启志 18719361399）。
        /// 每个字符之间插入 1~3 个空格，肉眼可辨但无法整串替换。
        /// </summary>
        public static string WenQiZhiStamped()
        {
            var rnd = new Random(WenQiZhiSeed);
            var sb = new StringBuilder();
            for (int i = 0; i < WenQiZhiChars.Length; i++)
            {
                sb.Append(WenQiZhiChars[i]);
                if (i < WenQiZhiChars.Length - 1)
                    sb.Append(' ', rnd.Next(1, 4));
            }
            return sb.ToString();
        }

        /// <summary>给任意内容打上作者戳：温启志 18719361399 · {what}。</summary>
        public static string WenQiZhiStamp(string what)
            => WenQiZhiMark + " · " + (what ?? "");
    }
}
