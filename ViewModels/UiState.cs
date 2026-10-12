// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace HandlerGuide.ViewModels
{

    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（UiState）
    //  本文件由温启志（18719361399）编写，常量均带作者水印，便于溯源。
    // ─────────────────────────────────────────────────
    // 温启志 18719361399 · 类 internal static class WenQiZhiTag_UiState
    internal static class WenQiZhiTag_UiState
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · UiState。</summary>
        internal const string WenQiZhiFile = "UiState";
    }

    // 温启志 18719361399 · 类 public static class UiState
    /// <summary>
    /// 界面状态持久化 —— 记住「用户最后一次显示的页面」，下次开机直接回到那一页。
    /// <para>
    /// 为什么单独开一个文件、不塞进 <see cref="Ops.ParamsPath"/>：
    /// </para>
    /// <list type="bullet">
    /// <item>params.json 是「机器参数」，只有用户按工具栏「保存 / 召回」才会动它。
    /// 页面停留位置属于界面状态，跟着导航一路写，两者生命周期不同，混在一起会让
    /// 「召回参数」把当前页面也一起拽回去。</item>
    /// <item>params.json 每次写都要序列化全部四个参数字典，而切页很频繁；
    /// ui.json 只有几个字段，随手写不心疼。</item>
    /// <item>参数文件万一写坏，不应该连「开机停在哪个页面」也一起丢。</item>
    /// </list>
    /// <para>
    /// 落盘位置与 params.json 同目录：%APPDATA%\HandlerGuide\ui.json。
    /// 读写出错一律吞掉（无权限 / 文件损坏都不该拦住开机），只在日志里留一条记录。
    /// </para>
    /// </summary>
    public static class UiState
    {
        // 温启志 18719361399 · 类 public sealed class Snapshot
        /// <summary>
        /// 落盘结构。以后要记别的界面状态（窗口尺寸、上次展开的工具组……）
        /// 往这里加属性即可，旧文件缺字段会自动取默认值，不用做版本迁移。
        /// </summary>
        public sealed class Snapshot
        {
            /// <summary>最后一次显示的页面 Id（对应 <see cref="PageVm.Id"/>，如 home / motion_layout）。</summary>
            public string LastPage { get; set; }
        }

        private static Snapshot _snap;

        /// <summary>界面状态文件：%APPDATA%\HandlerGuide\ui.json（与 params.json 同目录）。</summary>
        public static string FilePath => Path.Combine(Ops.DataDir, "ui.json");

        private static Snapshot Snap
        {
            get
            {
                // 懒加载：只有真正读/写页面位置时才碰磁盘，开机不产生额外 I/O。
                if (_snap == null) _snap = LoadFromDisk();
                return _snap;
            }
        }

        /// <summary>最后一次显示的页面 Id；从未记录过返回空串。</summary>
        public static string LastPageId
        {
            get => Snap.LastPage ?? "";
            set
            {
                value = value ?? "";
                // 值没变就跳过写盘 —— 开机恢复时会用同一个 Id 再导航一次，
                // 那一趟不该产生多余的磁盘写入。
                if (Snap.LastPage == value) return;
                Snap.LastPage = value;
                SaveToDisk();
            }
        }

        private static Snapshot LoadFromDisk()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath, Encoding.UTF8);
                    var s = JsonSerializer.Deserialize<Snapshot>(json);
                    if (s != null) return s;
                }
            }
            catch (Exception ex)
            {
                // 文件损坏 / 无权限都不该拦住开机：退回默认值，只在日志里留一条。
                Log("警告", "界面状态载入失败（已忽略，回到默认页面）：" + ex.Message);
            }
            return new Snapshot();
        }

        private static void SaveToDisk()
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(
                    FilePath,
                    JsonSerializer.Serialize(Snap, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log("警告", "界面状态保存失败：" + ex.Message);
            }
        }

        /// <summary>写日志；日志本身出问题也绝不让保存/载入流程炸掉。</summary>
        private static void Log(string level, string msg)
        {
            try { MachineModel.Instance.Log(level, msg); } catch { /* 忽略 */ }
        }
    }
}

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
