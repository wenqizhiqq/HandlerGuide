// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Motion/VarBox.cs — 具名变量黑板（流程线程之间唯一的共享内存）
//  项目：HandlerGuide（三温测试分选机上位机复刻）
//  职责：
//    1. 照 32-handler 真机的 VarInteface：中文 key 的名字变量（D 值 = double）
//       与字符串变量（C 值 = 工作模式 / 当前配方 / LotID 这类）；
//    2. 照真机 CoreWork.cs 的那 6 个 test-and-set 互斥标志：Take / Give 令牌，
//       一次锁内比较并交换，owner != 0 时只允许本人重入；
//    3. 真机的坑：那 12 个互斥标志只置 1、源码里不清零（清零藏在流程图数据里），
//       线程异常退出就把令牌永久钉死。本类**必须显式 Give**，
//       且 FlowThread 的 finally 里一定会把本线程持有的令牌全部归还。
//  线程模型：所有读写都在同一把锁里（真机只用 lock 保护 test-and-set 那两行，
//    这里统一加锁，语义等价且不会读到半个值）。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace HandlerGuide.Motion
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（VarBox）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_VarBox
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = "温启志 18719361399";

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = "18719361399";

        /// <summary>本文件署名：温启志 18719361399 · VarBox。</summary>
        internal const string WenQiZhiFile = "VarBox";
    }

    /// <summary>
    /// 具名变量黑板：D 值（数字 / 标志 / 计数）、C 值（字符串）、令牌（互斥）。
    /// </summary>
    public static class VarBox
    {
        /// <summary>统一大锁：真机只锁 test-and-set 两行，这里把所有读写都锁住。</summary>
        private static readonly object _lk = new object();

        /// <summary>名字变量（中文 key，值 = double）。标志位约定 0 = 假、1 = 真。</summary>
        private static readonly Dictionary<string, double> _d = new Dictionary<string, double>();

        /// <summary>字符串变量（工作模式 / 当前配方 / LotID / 报警文本 …）。</summary>
        private static readonly Dictionary<string, object> _c = new Dictionary<string, object>();

        /// <summary>令牌表：key → 占有人编号（0 = 空闲）。与真机一样放在同一把锁里。</summary>
        private static readonly Dictionary<string, double> _tok = new Dictionary<string, double>();

        /// <summary>owner = 0 时占位用的匿名占有人编号（真机就是「谁都能置 1」的裸标志）。</summary>
        public const int AnonymousOwner = 1;

        // ================== D 值 ==================

        /// <summary>读名字变量（没有登记过返回 def）。</summary>
        public static double GetD(string key, double def = 0)
        {
            if (string.IsNullOrEmpty(key)) return def;
            lock (_lk) return _d.TryGetValue(key, out var v) ? v : def;
        }

        /// <summary>写名字变量。</summary>
        public static void SetD(string key, double value)
        {
            if (string.IsNullOrEmpty(key)) return;
            lock (_lk) _d[key] = value;
        }

        /// <summary>读布尔标志（0/1 约定）。</summary>
        public static bool GetFlag(string key) => GetD(key) != 0;

        /// <summary>写布尔标志（内部还是 0 / 1，与真机一致）。</summary>
        public static void SetFlag(string key, bool on) => SetD(key, on ? 1 : 0);

        /// <summary>
        /// 计数：锁内「读 + 加 + 写」，返回加完后的新值（Interlocked 语义）。
        /// </summary>
        public static double Inc(string key, double n = 1)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            lock (_lk)
            {
                double v = (_d.TryGetValue(key, out var cur) ? cur : 0) + n;
                _d[key] = v;
                return v;
            }
        }

        // ================== C 值 ==================

        /// <summary>读字符串变量（没有登记过返回 def）。</summary>
        public static string GetC(string key, string def = "")
        {
            if (string.IsNullOrEmpty(key)) return def;
            lock (_lk)
            {
                if (!_c.TryGetValue(key, out var v) || v == null) return def;
                return v.ToString();
            }
        }

        /// <summary>写字符串变量（值一律 ToString 存起来，读回是字符串）。</summary>
        public static void SetC(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            lock (_lk) _c[key] = value;
        }

        // ================== 令牌（互斥） ==================

        /// <summary>
        /// 取令牌：一次锁内 test-and-set。
        /// owner == 0 → 按匿名占位（和真机裸标志一样，只是置 1）；
        /// owner != 0 → 记录占有人，同一 owner 可以重入，别的 owner 一律取不到。
        /// 取到返回 true；已被别人占着返回 false（调用方必须带 Sleep 重试，不许忙等）。
        /// </summary>
        public static bool Take(string key, int owner)
        {
            if (string.IsNullOrEmpty(key)) return false;
            int me = owner == 0 ? AnonymousOwner : owner;
            lock (_lk)
            {
                double cur = _tok.TryGetValue(key, out var v) ? v : 0;
                if (cur == 0)
                {
                    _tok[key] = me;
                    return true;
                }
                return cur == me;         // 本人重入
            }
        }

        /// <summary>
        /// 归还令牌：真机没有这一步（标志只置 1 不清零），我们补上。
        /// owner == 0 → 强制清（停机复位用）；否则只允许本人归还，别人的令牌不动。
        /// 返回是否真的清掉了。
        /// </summary>
        public static bool Give(string key, int owner)
        {
            if (string.IsNullOrEmpty(key)) return false;
            lock (_lk)
            {
                if (!_tok.TryGetValue(key, out var v)) return false;
                if (v == 0) return true;                       // 本来就是空的
                if (owner != 0 && (int)v != owner) return false;  // 不是自己的，不动
                _tok[key] = 0;
                return true;
            }
        }

        /// <summary>令牌当前占有人（0 = 空闲）。</summary>
        public static int OwnerOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            lock (_lk) return _tok.TryGetValue(key, out var v) ? (int)v : 0;
        }

        /// <summary>令牌是否被占用。</summary>
        public static bool IsTaken(string key) => OwnerOf(key) != 0;

        /// <summary>归还某个 owner 持有的全部令牌（线程退出 / 异常时用，防死锁）。</summary>
        public static int GiveAllOf(int owner)
        {
            if (owner == 0) return 0;
            lock (_lk)
            {
                int n = 0;
                foreach (var k in _tok.Keys.ToList())
                {
                    if ((int)_tok[k] == owner) { _tok[k] = 0; n++; }
                }
                return n;
            }
        }

        /// <summary>登记一条令牌（只为 Snapshot 里好看，值本身还是 0 = 空闲）。</summary>
        public static void Declare(string tokenKey)
        {
            if (string.IsNullOrEmpty(tokenKey)) return;
            lock (_lk) if (!_tok.ContainsKey(tokenKey)) _tok[tokenKey] = 0;
        }

        /// <summary>已登记的令牌名清单（停机复位时逐个强制清零用）。</summary>
        public static List<string> TokenKeys()
        {
            lock (_lk) return _tok.Keys.ToList();
        }

        // ================== 运维辅助 ==================

        /// <summary>全部变量 / 令牌的排序快照（界面「变量黑板」与日志用）。</summary>
        public static List<string> Snapshot()
        {
            lock (_lk)
            {
                var list = new List<string>();
                foreach (var kv in _c.OrderBy(k => k.Key, StringComparer.Ordinal))
                    list.Add("C " + kv.Key + " = " + kv.Value);
                foreach (var kv in _d.OrderBy(k => k.Key, StringComparer.Ordinal))
                    list.Add("D " + kv.Key + " = " + kv.Value.ToString("0.###"));
                foreach (var kv in _tok.OrderBy(k => k.Key, StringComparer.Ordinal))
                    list.Add("锁 " + kv.Key + " = " + ((int)kv.Value == 0 ? "空闲" : "持有人 " + (int)kv.Value));
                return list;
            }
        }

        /// <summary>当前登记的条目总数（D + C + 令牌）。</summary>
        public static int Count
        {
            get { lock (_lk) return _d.Count + _c.Count + _tok.Count; }
        }

        /// <summary>清空黑板（停机 / 换批时用；令牌全部清零）。</summary>
        public static void Clear()
        {
            lock (_lk)
            {
                _d.Clear();
                _c.Clear();
                _tok.Clear();
            }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
