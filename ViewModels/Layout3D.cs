// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  ViewModels/Layout3D.cs — 「3D整机图」真·三维仿真（温启志 18719361399）
//  项目：HandlerGuide（EXCEED9000 高速分选机控制软件复刻）
//
//  【为什么单独写一个文件】
//    原来的「3D整机图」只是把 image/handler_3d.jpg 这张**斜二测渲染图**当底图，
//    再在上面盖 14 个半透明平行四边形 + 一个蓝框 —— 那不是三维仿真，是「照片贴纸」：
//    换不了视角、没有遮挡关系、没有光照、料盘和机台之间没有立体关系。
//    本文件改成**自己算三维**，不依赖任何三维库、也不需要底图：
//
//        世界坐标 → 相机坐标 → 透视投影 → 画家算法排序 → 逐面朗伯着色
//        → 用 PolyItem（<Polygon>）画到同一个 CanvasElement 上
//
//    机台底座、台面、14 个料盘、2 块加热盘、龙门四立柱、前后导轨、
//    横梁 / 滑座 / 吸嘴，全部是现算出来的实体，可以自由换视角。
//
//  【坐标系】右手系，Z 朝上，单位≈0.1mm，机台 1000×800
//      X  0 → 1000   左 → 右（**画面的右**，与俯视图一致）
//      Y  0 →  800   **近 → 远**（Y 越大离相机越远）
//      Z  0 →  440   地面 → 龙门顶
//
//    ★ Y 的取向是这里最容易搞错的地方，说明一下：
//      俯视图照片里「上排 6 深盘」在图片上方、「左下 3 绿盘」在图片下方。
//      相机放在 -Y 侧朝 +Y 看时，要让俯视图的「上方」落在远处，就必须
//      Y = (1 - v) · MY（v 是俯视图归一化纵坐标，向下增大）。
//      如果写成 Y = v · MY，整台机器会**前后颠倒**；
//      如果把相机放到 +Y 侧，画面又会**左右镜像**（加热盘跑到左边去）。
//      所以这里的取值是：相机在 -Y（方位角 ≈146°）+ Y = (1-v)·MY。
//
//    机台平面布局复用 LayoutPages.TrayPlan（真机俯视图逐像素量测、归一化 0..1），
//    所以「整机俯视图」和「3D整机图」上的料盘位置来自同一份数据，永远对得上。
//
//  【分层 —— 画家算法的正确性靠分层保证，不靠全局排序】
//      A1 层  地面网格                     永远最先画（网格线都在 z = -1 的平面上）
//      A2 层  机台底座 + 台面              压在网格之上
//      B  层  料盘 / 加热盘 / 立柱 / 导轨 / 芯片   按相机空间深度排序后画
//      C  层  横梁 / 滑座 / 吸嘴            永远最后画（Z 恒在 B 层之上）
//    为什么要分层 —— 这里有两个「质心深度骗人」的坑：
//      1) 底座是一块 1000×800 的大四边形，它的**质心**深度可能比后排料盘还小，
//         混在一起排序会让底座盖住后排料盘；
//      2) 靠近相机那几条地面网格线的质心深度可能比底座顶面还小，
//         混在一起排序会让网格线「划」到台面上，像台面被刮了几道。
//    把网格 / 底座 / 实体分成三档、档内再按深度排，这两个坑就都不存在了。
//
//  【每帧只动 C 层和芯片颜色】
//    相机不动时 A/B 层几何完全不变；每帧只重算 C 层那 18 个面 + 芯片的 Fill，
//    所以 20fps 的动画开销可以忽略。换视角才重算全场（点按钮触发）。
//    排序不重排 ItemsControl，而是改 DiagramItem.Z（绑到 Canvas.ZIndex），
//    避免每次换视角都重建 1000 个容器。
//
//  【自动取景 + 手动缩放】
//    任意视角都先按机台的**真实几何包络点集**（FitPts）算一次投影范围，
//    再自动调 Dist 和画面偏移，保证机台永远刚好装进画布（不会转两下就跑出画面），
//    而且长边稳定占到画布的 92%（用一个外包大盒子只能占到 82%，白浪费一圈）。
//    用户的手动缩放记在 View3D.Zoom 上，取景算完再除以它 —— 这样「拉近 / 拉远」
//    不会被紧接着的取景抵消，而且换视角时缩放级别保持不变。
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using HandlerGuide.Motion;

namespace HandlerGuide.ViewModels
{
    // ─────────────────────────────────────────────────
    //  温启志 18719361399 · 源码署名标记（Layout3D）
    // ─────────────────────────────────────────────────
    internal static class WenQiZhiTag_Layout3D
    {
        /// <summary>作者水印：温启志 18719361399。</summary>
        internal const string WenQiZhiAuthor = Owner.WenQiZhiMark;

        /// <summary>作者电话水印：18719361399。</summary>
        internal const string WenQiZhiPhone = Owner.WenQiZhiPhone;

        /// <summary>本文件署名：温启志 18719361399 · Layout3D。</summary>
        internal const string WenQiZhiFile = "Layout3D";
    }

    /// <summary>三维点（右手系，Z 朝上）。温启志 18719361399</summary>
    internal struct V3
    {
        public readonly double X, Y, Z;

        public V3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static V3 operator +(V3 a, V3 b) { return new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static V3 operator -(V3 a, V3 b) { return new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static V3 operator *(V3 a, double s) { return new V3(a.X * s, a.Y * s, a.Z * s); }

        public static double Dot(V3 a, V3 b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z; }

        public static V3 Cross(V3 a, V3 b)
        {
            return new V3(a.Y * b.Z - a.Z * b.Y,
                          a.Z * b.X - a.X * b.Z,
                          a.X * b.Y - a.Y * b.X);
        }

        public double Len { get { return Math.Sqrt(X * X + Y * Y + Z * Z); } }

        public V3 Unit()
        {
            double l = Len;
            return l < 1e-9 ? new V3(0, 0, 0) : new V3(X / l, Y / l, Z / l);
        }
    }

    /// <summary>
    /// 一个四边形面。四个顶点按任意绕向给都行 —— 渲染时会看法线是否背对相机，
    /// 背对就翻过来（所以不用小心翼翼地保证顶点顺序，少一类低级错误）。
    /// </summary>
    internal sealed class Quad
    {
        public V3 A, B, C, D;
        public Color Col;

        /// <summary>属于哪一组芯片（-1 = 普通面）；组内序号 <see cref="Slot"/>。</summary>
        public int Group = -1;
        public int Slot = -1;

        public Quad(V3 a, V3 b, V3 c, V3 d, Color col) { A = a; B = b; C = c; D = d; Col = col; }

        public V3 Center() { return (A + B + C + D) * 0.25; }
    }

    /// <summary>一组芯片（一张盘上的全部格子）。</summary>
    internal sealed class ChipGroup
    {
        public readonly List<Quad> Quads = new List<Quad>();

        /// <summary>有料时的颜色。</summary>
        public Color On;

        /// <summary>true = 加热盘：斜向扫掠；false = 料盘：按格顺序推进。</summary>
        public bool Heat;

        /// <summary>顺序波方向：+1 空→满（缓存盘 / 出料盘），-1 满→空（待测盘）。</summary>
        public int Sign = 1;

        /// <summary>相位序号，用来把各张盘的节拍错开。</summary>
        public int Index;

        /// <summary>列数（加热盘的斜向波按 (行 + 列) 算相位）。</summary>
        public int Cols;

        /// <summary>有料时用的画刷（全组共用一支 —— 同组芯片法线都是 +Z，着色结果一样）。</summary>
        public Brush OnBrush;
    }

    /// <summary>整个场景的几何（温启志 18719361399）。</summary>
    internal sealed class Scene3D
    {
        /// <summary>A1 层：地面网格（渲染时永远第一个画）。</summary>
        public readonly List<Quad> Grid = new List<Quad>();

        /// <summary>A2 层：机台底座 + 台面（压在网格之上）。</summary>
        public readonly List<Quad> Ground = new List<Quad>();

        /// <summary>B 层：料盘 / 加热盘 / 立柱 / 导轨（静态）。</summary>
        public readonly List<Quad> Static = new List<Quad>();

        /// <summary>芯片（B 层的一部分，但颜色每帧变）。</summary>
        public readonly List<ChipGroup> Chips = new List<ChipGroup>();

        /// <summary>C 层：横梁 / 滑座 / 吸嘴 —— 顶点每帧重算，Quad 对象本身复用。</summary>
        public readonly Quad[] Dynamic = new Quad[18];

        public Scene3D()
        {
            for (int i = 0; i < Dynamic.Length; i++)
                Dynamic[i] = new Quad(new V3(0, 0, 0), new V3(0, 0, 0), new V3(0, 0, 0), new V3(0, 0, 0), Colors.White);
        }
    }

    /// <summary>相机 / 视角参数（温启志 18719361399）。</summary>
    internal sealed class View3D
    {
        /// <summary>方位角（度）。0 = 相机在 +Y，146 = 右前方 3/4 视角（本页默认）。</summary>
        public double Az = 146;

        /// <summary>仰角（度）：0 = 平视，90 = 正俯视。</summary>
        public double El = 30;

        /// <summary>相机到目标点的距离（自动取景每次都会重算它，这里只是初值）。</summary>
        public double Dist = 1400;

        /// <summary>
        /// 用户手动缩放倍数（1 = 自动取景刚好装满；&gt; 1 拉近，&lt; 1 拉远）。
        /// 自动取景算出的距离最后会再除以它 —— 否则点「拉近」会被紧接着的取景立刻抵消，
        /// 按钮看起来完全没反应。温启志 18719361399
        /// </summary>
        public double Zoom = 1.0;

        /// <summary>垂直视场角（度）。</summary>
        public double Fov = 40;

        public V3 Target = new V3(500, 400, 110);

        public void Reset()
        {
            Az = 146; El = 30; Dist = 1400; Fov = 40; Target = new V3(500, 400, 110);
            Zoom = 1.0;
        }
    }

    /// <summary>
    /// 软件三维渲染器：把 <see cref="Scene3D"/> 投到 <see cref="CanvasElement"/> 上。
    /// 温启志 18719361399
    /// </summary>
    internal sealed class Renderer3D
    {
        private readonly CanvasElement _cv;
        private readonly Scene3D _sc;
        private readonly View3D _view;

        // 相机基向量（每次重算前先 UpdateCamera）
        private V3 _eye, _fwd, _right, _up;
        private double _f;
        private double _offX, _offY;      // 自动取景的画面偏移

        // 自动取景用的「真实几何包络」点集（不含地面网格 —— 网格故意让它出血）。
        //
        // 为什么不用一个 (FitLo, FitHi) 大盒子：盒子的**顶面四个角**在 z = 440 上，
        // 可那里其实什么实体都没有（只有一根 44 宽的横梁），这些「幽灵角点」
        // 会把投影包围盒撑大一大截 —— 实测按大盒子取景，机台只占画布的 82%×75%，
        // 白白浪费一圈。改成按下面这些**真实存在的极值点**取景后，长边稳定在 92%。
        private static readonly V3[] FitPts =
        {
            // 底座（最低的实体，z = 0）
            new V3(0, 0, 0), new V3(Layout3D.MX, 0, 0),
            new V3(0, Layout3D.MY, 0), new V3(Layout3D.MX, Layout3D.MY, 0),
            new V3(0, 0, Layout3D.BASE_H), new V3(Layout3D.MX, 0, Layout3D.BASE_H),
            new V3(0, Layout3D.MY, Layout3D.BASE_H), new V3(Layout3D.MX, Layout3D.MY, Layout3D.BASE_H),
            // 台面（比底座外扩 14，是横向最宽的地方）
            new V3(-14, -14, Layout3D.BASE_H), new V3(Layout3D.MX + 14, -14, Layout3D.BASE_H),
            new V3(-14, Layout3D.MY + 14, Layout3D.BASE_H), new V3(Layout3D.MX + 14, Layout3D.MY + 14, Layout3D.BASE_H),
            new V3(-14, -14, Layout3D.BASE_TOP), new V3(Layout3D.MX + 14, -14, Layout3D.BASE_TOP),
            new V3(-14, Layout3D.MY + 14, Layout3D.BASE_TOP), new V3(Layout3D.MX + 14, Layout3D.MY + 14, Layout3D.BASE_TOP),
            // 立柱顶 + 前后导轨顶面（静态实体的最高点）
            new V3(26, 40, Layout3D.POST_TOP), new V3(Layout3D.MX - 26, 40, Layout3D.POST_TOP),
            new V3(26, Layout3D.MY - 40, Layout3D.POST_TOP), new V3(Layout3D.MX - 26, Layout3D.MY - 40, Layout3D.POST_TOP),
            new V3(26, 40, Layout3D.POST_TOP + Layout3D.RAIL_H), new V3(Layout3D.MX - 26, 40, Layout3D.POST_TOP + Layout3D.RAIL_H),
            new V3(26, Layout3D.MY - 40, Layout3D.POST_TOP + Layout3D.RAIL_H),
            new V3(Layout3D.MX - 26, Layout3D.MY - 40, Layout3D.POST_TOP + Layout3D.RAIL_H),
            // 龙门包络：横梁 / 滑座 / 吸嘴的行程范围（最高点到 z = 434）
            new V3(130, 34, 360), new V3(914, 34, 360),
            new V3(130, 766, 360), new V3(914, 766, 360),
            new V3(130, 34, 434), new V3(914, 34, 434),
            new V3(130, 766, 434), new V3(914, 766, 434),
        };
        private const double FitMargin = 0.92;

        // 每个面 → 它的图元（引用相等，Quad 是 class）
        private readonly Dictionary<Quad, PolyItem> _item = new Dictionary<Quad, PolyItem>();
        // 芯片图元：_chipItems[组号][组内序号]
        private readonly List<List<PolyItem>> _chipItems = new List<List<PolyItem>>();
        // C 层图元
        private readonly List<PolyItem> _dynItems = new List<PolyItem>();

        private int _dynZ;      // C 层的起始层号
        private double _t;      // 当前动画时刻（秒）

        public Renderer3D(CanvasElement cv, Scene3D sc, View3D view)
        {
            _cv = cv; _sc = sc; _view = view;
        }

        // ==============================================================
        //  一次性建图元（不投影，只挂对象）
        // ==============================================================
        public void Build()
        {
            _cv.Items.Clear();
            _item.Clear();
            _chipItems.Clear();
            _dynItems.Clear();

            // 背景：竖直渐变（浅色 CAD 视口，和整机俯视图的浅色底图同一风格）
            var bg = new LinearGradientBrush(
                Color.FromRgb(0xF2, 0xF5, 0xF9), Color.FromRgb(0xC6, 0xCD, 0xD8), 90.0);
            _cv.Items.Add(new RectItem { X = 0, Y = 0, W = Layout3D.CW, H = Layout3D.CH, Fill = bg, Z = -10000 });

            foreach (var q in _sc.Grid) AddItem(q);
            foreach (var q in _sc.Ground) AddItem(q);
            foreach (var q in _sc.Static) AddItem(q);
            foreach (var g in _sc.Chips)
            {
                var list = new List<PolyItem>();
                foreach (var q in g.Quads) list.Add(AddItem(q));
                _chipItems.Add(list);
            }
            for (int i = 0; i < _sc.Dynamic.Length; i++) _dynItems.Add(AddItem(_sc.Dynamic[i]));

            // 前景文字（最高层）
            AddLabel("3D 整机仿真（真实三维 · 可换视角）", 12, 10, 8, Color.FromRgb(0x2A, 0x30, 0x3A));
            AddLabel("世界坐标 → 相机坐标 → 透视投影 → 画家算法 → 逐面朗伯着色；"
                   + "料盘位置与「整机俯视图」共用同一份真机量测数据",
                10.5, 10, 28, Color.FromRgb(0x5A, 0x62, 0x70));
            AddLabel("温启志 18719361399 · 3D 整机仿真（自算三维，非贴图）",
                10, 10, Layout3D.CH - 22, Color.FromRgb(0x70, 0x78, 0x86));
        }

        private PolyItem AddItem(Quad q)
        {
            var it = new PolyItem { Stroke = null, StrokeThickness = 0 };
            _cv.Items.Add(it);
            _item[q] = it;
            return it;
        }

        private void AddLabel(string text, double size, double x, double y, Color c)
        {
            _cv.Items.Add(new TextItem
            {
                Text = text, X = x, Y = y, TextSize = size, TextWidth = Layout3D.CW - 20,
                Align = TextAlignment.Left,
                Foreground = new SolidColorBrush(c),
                Z = 990000,
            });
        }

        // ==============================================================
        //  相机
        // ==============================================================
        private void UpdateCamera()
        {
            double az = _view.Az * Math.PI / 180.0;
            double el = _view.El * Math.PI / 180.0;
            double ce = Math.Cos(el);

            // 从目标点指向相机的方向
            V3 dir = new V3(ce * Math.Sin(az), ce * Math.Cos(az), Math.Sin(el));
            _eye = _view.Target + dir * _view.Dist;
            _fwd = (_view.Target - _eye).Unit();
            _right = V3.Cross(_fwd, new V3(0, 0, 1)).Unit();
            _up = V3.Cross(_right, _fwd);

            _f = (Layout3D.CH / 2.0) / Math.Tan(_view.Fov * Math.PI / 360.0);
        }

        /// <summary>机台几何包络点集投到「相对画面中心」的屏幕范围。</summary>
        private bool FitBounds(out double minX, out double maxX, out double minY, out double maxY)
        {
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            bool any = false;
            for (int i = 0; i < FitPts.Length; i++)
            {
                V3 p = FitPts[i];
                V3 d = p - _eye;
                double dep = V3.Dot(d, _fwd);
                if (dep < 30) continue;
                double rx = _f * V3.Dot(d, _right) / dep;
                double ry = -_f * V3.Dot(d, _up) / dep;
                if (rx < minX) minX = rx;
                if (rx > maxX) maxX = rx;
                if (ry < minY) minY = ry;
                if (ry > maxY) maxY = ry;
                any = true;
            }
            return any;
        }

        /// <summary>
        /// 自动取景：先迭代调 Dist 让机台刚好装进画布，再算画面偏移把机台居中。
        /// 这样任意视角（顺时针 / 抬高 / 拉近）都不会把机台转出画面。
        /// </summary>
        private void AutoFit()
        {
            // 第一步：迭代求「刚好装下机台」的相机距离。
            // 这个距离只跟视角（Az / El）有关，从任意初值出发都会收敛到同一个值 ——
            // 所以先把它单独算出来，不要直接写回 _view.Dist，免得吃掉用户的手动缩放。
            double dist = _view.Dist;
            for (int iter = 0; iter < 6; iter++)
            {
                _view.Dist = dist;
                UpdateCamera();
                double a, b, c, d;
                if (!FitBounds(out a, out b, out c, out d))
                {
                    dist *= 1.4;            // 有角点跑到相机背后了，先退远
                    continue;
                }
                double w = b - a, h = d - c;
                if (w < 1 || h < 1) break;
                double s = Math.Min(Layout3D.CW * FitMargin / w, Layout3D.CH * FitMargin / h);
                if (Math.Abs(s - 1.0) < 0.004) break;
                dist /= s;
            }

            // 第二步：把手动缩放叠上去（Zoom > 1 = 拉近）
            _view.Dist = dist / Math.Max(0.25, Math.Min(6.0, _view.Zoom));
            UpdateCamera();
            double mx0, mx1, my0, my1;
            if (FitBounds(out mx0, out mx1, out my0, out my1))
            {
                // FitBounds 给的是「相对画面中心」的偏移，Project 里已经加了 CW/2、CH/2，
                // 所以这里只要把包围盒中心挪到 0 即可（不要再加半个画布）。
                _offX = -(mx0 + mx1) / 2.0;
                _offY = -(my0 + my1) / 2.0;
            }
            else { _offX = 0; _offY = 0; }
        }

        private bool Project(V3 p, out double sx, out double sy, out double depth)
        {
            V3 d = p - _eye;
            depth = V3.Dot(d, _fwd);
            if (depth < 30) { sx = 0; sy = 0; return false; }   // 近裁剪：整面丢弃
            sx = Layout3D.CW / 2.0 + _offX + _f * V3.Dot(d, _right) / depth;
            sy = Layout3D.CH / 2.0 + _offY - _f * V3.Dot(d, _up) / depth;
            return true;
        }

        // ==============================================================
        //  投影一个面 → 写进它的图元
        // ==============================================================
        private void Push(Quad q, PolyItem it, int z)
        {
            double ax, ay, bx, by, cx, cy, dx, dy, d0;
            if (!Project(q.A, out ax, out ay, out d0)
                || !Project(q.B, out bx, out by, out d0)
                || !Project(q.C, out cx, out cy, out d0)
                || !Project(q.D, out dx, out dy, out d0))
            {
                it.Fill = null;          // 有顶点跑到相机背后：整个面不画
                return;
            }

            it.Points = new PointCollection(4)
            {
                new Point(ax, ay), new Point(bx, by), new Point(cx, cy), new Point(dx, dy),
            };
            it.Z = z;

            if (q.Group >= 0) return;    // 芯片：颜色由 Animate 决定，这里只保证几何和层号

            // 法线朝相机翻转，再算光照
            V3 n = V3.Cross(q.B - q.A, q.C - q.A).Unit();
            if (V3.Dot(n, q.Center() - _eye) > 0) n = n * -1.0;

            var br = new SolidColorBrush(Layout3D.Shade(q.Col, n));
            br.Freeze();
            it.Fill = br;
        }

        /// <summary>按相机空间深度从远到近排序（远的先画 = 画家算法）。</summary>
        private List<Quad> Sorted(List<Quad> src)
        {
            int n = src.Count;
            var idx = new int[n];
            var dep = new double[n];
            for (int i = 0; i < n; i++)
            {
                idx[i] = i;
                dep[i] = V3.Dot(src[i].Center() - _eye, _fwd);
            }
            Array.Sort(idx, (a, b) =>
            {
                int c = dep[b].CompareTo(dep[a]);
                return c != 0 ? c : a.CompareTo(b);   // 同深度用序号兜底，保证稳定
            });
            var outList = new List<Quad>(n);
            for (int i = 0; i < n; i++) outList.Add(src[idx[i]]);
            return outList;
        }

        // ==============================================================
        //  整场重算（首次 + 每次换视角）
        // ==============================================================
        public void Render()
        {
            AutoFit();

            // ---- A1 层：地面网格（永远最先画）----
            int z = 0;
            foreach (var q in Sorted(_sc.Grid))
                Push(q, _item[q], z++);

            // ---- A2 层：底座 + 台面（压在网格之上）----
            foreach (var q in Sorted(_sc.Ground))
                Push(q, _item[q], z++);

            // ---- B 层：静态面 + 芯片，统一按深度排序 ----
            var b = new List<Quad>(_sc.Static.Count + 128);
            b.AddRange(_sc.Static);
            foreach (var g in _sc.Chips) b.AddRange(g.Quads);
            foreach (var q in Sorted(b))
                Push(q, _item[q], z++);

            // ---- C 层：永远在最上面 ----
            _dynZ = z;
            Animate(_t);
        }

        // ==============================================================
        //  逐帧动画：只动 C 层 + 芯片颜色
        // ==============================================================
        public void Animate(double t)
        {
            _t = t;

            // ---- C 层：横梁沿 X 走，滑座沿 Y 走，吸嘴沿 Z 升降 ----
            double beamX = 130 + Layout3D.Triangle(t * 0.10) * 740;             // 130..870
            double headY = 120 + Layout3D.Triangle(t * 0.17 + 0.30) * 560;      // 120..680
            double nz = 150 + Layout3D.Triangle(t * 0.33) * 220;                // 吸嘴下端 150..370

            Layout3D.SetBox(_sc.Dynamic, 0,
                new V3(beamX, 34, 360), new V3(beamX + 44, 766, 390), Layout3D.ColBeam);
            Layout3D.SetBox(_sc.Dynamic, 6,
                new V3(beamX - 10, headY - 30, 390), new V3(beamX + 54, headY + 30, 434), Layout3D.ColCarriage);
            Layout3D.SetBox(_sc.Dynamic, 12,
                new V3(beamX + 8, headY - 9, nz), new V3(beamX + 36, headY + 9, 396), Layout3D.ColNozzle);

            for (int i = 0; i < _sc.Dynamic.Length; i++)
                Push(_sc.Dynamic[i], _dynItems[i], _dynZ + 1 + i);

            // ---- 芯片：与「整机俯视图」同一套节拍 ----
            for (int gi = 0; gi < _sc.Chips.Count; gi++)
            {
                var g = _sc.Chips[gi];
                var items = _chipItems[gi];
                int n = g.Quads.Count;
                if (n == 0) continue;

                if (g.Heat)
                {
                    // 加热盘：一条斜向（左下→右上）的料带扫过去
                    int cols = g.Cols > 0 ? g.Cols : 1;
                    for (int j = 0; j < n; j++)
                    {
                        int r = j / cols, c = j % cols;
                        double ph = t * 0.55 + g.Index * 0.5 - (r + c) * 0.13;
                        items[j].Fill = Layout3D.Triangle(ph) > 0.5 ? g.OnBrush : null;
                    }
                }
                else
                {
                    // 料盘：按格推进。上排 6 张待测盘「满→空」，其余「空→满」。
                    double k = Layout3D.Triangle(t * 0.08 + g.Index * 0.19);
                    int target = g.Sign < 0
                        ? (int)Math.Round(n * (1.0 - k))
                        : (int)Math.Round(n * k);
                    for (int j = 0; j < n; j++)
                        items[j].Fill = j < target ? g.OnBrush : null;
                }
            }
        }
    }

    /// <summary>
    /// 「3D整机图」页签：视角工具条 + 真三维画布（温启志 18719361399）。
    /// </summary>
    public static class Layout3D
    {
        // ─────────────────────────────────────────────────
        //  画布与机台尺寸
        // ─────────────────────────────────────────────────

        /// <summary>画布宽：与「整机俯视图」同宽，切换页签时画面不跳。</summary>
        internal const double CW = 1010;

        /// <summary>画布高：与「整机俯视图」「2D平面图」同高。</summary>
        internal const double CH = 819;

        // 注意：这些尺寸是 internal 而不是 private —— Renderer3D（另一个类）
        // 的自动取景点集 FitPts 要用它们，private 取不到。
        internal const double MX = 1000;          // 机台宽（X）
        internal const double MY = 800;           // 机台深（Y）
        internal const double BASE_H = 40;        // 底座厚
        internal const double TABLE_H = 8;        // 台面厚
        internal const double TRAY_H = 16;        // 料盘厚
        internal const double PLATE_H = 30;       // 加热盘厚（比料盘厚，真机就是凸出来的）
        internal const double POST_TOP = 330;     // 立柱顶面 Z
        internal const double RAIL_H = 26;        // 导轨高
        internal const double BASE_TOP = BASE_H + TABLE_H;   // 台面上表面 Z = 48

        // ─────────────────────────────────────────────────
        //  配色（未打光的基色，实际颜色 = 基色 × 朗伯系数）
        // ─────────────────────────────────────────────────

        private static readonly Color ColGrid = Color.FromRgb(0x9A, 0xA3, 0xB0);
        private static readonly Color ColBaseSide = Color.FromRgb(0x8C, 0x93, 0x9E);
        private static readonly Color ColTable = Color.FromRgb(0xC2, 0xC8, 0xD2);
        private static readonly Color ColTrayDark = Color.FromRgb(0x3C, 0x41, 0x4B);
        private static readonly Color ColTrayGreen = Color.FromRgb(0x2E, 0x4A, 0x42);
        private static readonly Color ColPlate = Color.FromRgb(0x6E, 0x76, 0x81);
        private static readonly Color ColPost = Color.FromRgb(0x79, 0x80, 0x8B);
        private static readonly Color ColRail = Color.FromRgb(0x8A, 0x92, 0x9D);
        internal static readonly Color ColBeam = Color.FromRgb(0x3A, 0x7C, 0xBF);
        internal static readonly Color ColCarriage = Color.FromRgb(0x4E, 0x93, 0xD4);
        internal static readonly Color ColNozzle = Color.FromRgb(0xC8, 0xCE, 0xD8);
        private static readonly Color ColChipDark = Color.FromRgb(0x3C, 0xC0, 0xF0);
        private static readonly Color ColChipGreen = Color.FromRgb(0x3A, 0xD3, 0x8E);
        private static readonly Color ColChipHeat = Color.FromRgb(0xF2, 0xA9, 0x3B);

        // ─────────────────────────────────────────────────
        //  着色：朗伯 + 一点背光，顶面最亮（像 CAD 的默认打光）
        // ─────────────────────────────────────────────────

        /// <summary>平行光方向（左前上方）。</summary>
        private static readonly V3 Light = new V3(-0.35, 0.45, 0.82).Unit();

        /// <summary>把一个基色按面法线打光。顶面最亮，背面给一点补偿，避免死黑。</summary>
        internal static Color Shade(Color c, V3 n)
        {
            double d = V3.Dot(n, Light);
            double k = 0.42 + 0.58 * Math.Max(0.0, d) + 0.10 * Math.Max(0.0, -d);
            return Color.FromRgb(Cl(c.R * k), Cl(c.G * k), Cl(c.B * k));
        }

        private static byte Cl(double v)
        {
            if (v <= 0) return 0;
            if (v >= 255) return 255;
            return (byte)(v + 0.5);
        }

        // ─────────────────────────────────────────────────
        //  小工具
        // ─────────────────────────────────────────────────

        /// <summary>始终为正的取模。</summary>
        internal static double Mod(double a, double b)
        {
            if (b <= 0) return 0;
            double r = a % b;
            return r < 0 ? r + b : r;
        }

        /// <summary>0..1 三角波（周期 1）—— 和 2D / 俯视图共用同一条节拍曲线。</summary>
        internal static double Triangle(double x)
        {
            double p = Mod(x, 1.0);
            return p < 0.5 ? p * 2.0 : (1.0 - p) * 2.0;
        }

        /// <summary>把一个轴对齐长方体的 6 个面写进 <paramref name="dst"/> 的 offset 起的 6 个槽位。</summary>
        internal static void SetBox(Quad[] dst, int offset, V3 lo, V3 hi, Color c)
        {
            double x0 = lo.X, y0 = lo.Y, z0 = lo.Z, x1 = hi.X, y1 = hi.Y, z1 = hi.Z;
            Put(dst, offset + 0, new V3(x0, y0, z1), new V3(x1, y0, z1), new V3(x1, y1, z1), new V3(x0, y1, z1), c); // 顶
            Put(dst, offset + 1, new V3(x0, y0, z0), new V3(x0, y1, z0), new V3(x1, y1, z0), new V3(x1, y0, z0), c); // 底
            Put(dst, offset + 2, new V3(x0, y0, z0), new V3(x1, y0, z0), new V3(x1, y0, z1), new V3(x0, y0, z1), c); // 后 (y0)
            Put(dst, offset + 3, new V3(x1, y1, z0), new V3(x0, y1, z0), new V3(x0, y1, z1), new V3(x1, y1, z1), c); // 前 (y1)
            Put(dst, offset + 4, new V3(x0, y1, z0), new V3(x0, y0, z0), new V3(x0, y0, z1), new V3(x0, y1, z1), c); // 左 (x0)
            Put(dst, offset + 5, new V3(x1, y0, z0), new V3(x1, y1, z0), new V3(x1, y1, z1), new V3(x1, y0, z1), c); // 右 (x1)
        }

        private static void Put(Quad[] dst, int i, V3 a, V3 b, V3 c, V3 d, Color col)
        {
            var q = dst[i];
            q.A = a; q.B = b; q.C = c; q.D = d; q.Col = col;
        }

        /// <summary>新建一个长方体，6 个面直接加进 <paramref name="list"/>。</summary>
        private static void AddBox(List<Quad> list, V3 lo, V3 hi, Color c)
        {
            double x0 = lo.X, y0 = lo.Y, z0 = lo.Z, x1 = hi.X, y1 = hi.Y, z1 = hi.Z;
            list.Add(new Quad(new V3(x0, y0, z1), new V3(x1, y0, z1), new V3(x1, y1, z1), new V3(x0, y1, z1), c));
            list.Add(new Quad(new V3(x0, y0, z0), new V3(x0, y1, z0), new V3(x1, y1, z0), new V3(x1, y0, z0), c));
            list.Add(new Quad(new V3(x0, y0, z0), new V3(x1, y0, z0), new V3(x1, y0, z1), new V3(x0, y0, z1), c));
            list.Add(new Quad(new V3(x1, y1, z0), new V3(x0, y1, z0), new V3(x0, y1, z1), new V3(x1, y1, z1), c));
            list.Add(new Quad(new V3(x0, y1, z0), new V3(x0, y0, z0), new V3(x0, y0, z1), new V3(x0, y1, z1), c));
            list.Add(new Quad(new V3(x1, y0, z0), new V3(x1, y1, z0), new V3(x1, y1, z1), new V3(x1, y0, z1), c));
        }

        /// <summary>
        /// 在 Z = <paramref name="z"/> 的平面上加**一个**水平矩形（地面网格线用）。
        /// 网格线如果用 AddBox，会多出 4 个零面积的面 + 1 个和顶面重合的底面，
        /// 白占图元还容易打架；这里只给 1 个面。温启志 18719361399
        /// </summary>
        private static void AddFlat(List<Quad> list, double x0, double y0, double x1, double y1, double z, Color c)
        {
            list.Add(new Quad(new V3(x0, y0, z), new V3(x1, y0, z),
                              new V3(x1, y1, z), new V3(x0, y1, z), c));
        }

        /// <summary>
        /// 俯视图归一化纵坐标 v → 世界 Y。
        /// v 在照片里是**向下**增大的，而世界 Y 是「越大越远」，
        /// 所以这里要取 (1 - v)；写成 v 会让整台机器前后颠倒。
        /// </summary>
        private static double WY(double v) { return (1.0 - v) * MY; }

        // ─────────────────────────────────────────────────
        //  场景
        // ─────────────────────────────────────────────────

        private static Scene3D BuildScene()
        {
            var sc = new Scene3D();

            // ---- A1 层：地面网格（帮眼睛看出透视，CAD 视口的常规做法）----
            // 故意让它比机台大一圈（出血），构图更像 CAD 视口。
            const double gx0 = -260, gx1 = MX + 260, gy0 = -260, gy1 = MY + 260;
            const double step = 130, lw = 1.6;
            for (double x = gx0; x <= gx1 + 0.1; x += step)
                AddFlat(sc.Grid, x - lw / 2, gy0, x + lw / 2, gy1, -1, ColGrid);
            for (double y = gy0; y <= gy1 + 0.1; y += step)
                AddFlat(sc.Grid, gx0, y - lw / 2, gx1, y + lw / 2, -1, ColGrid);

            // ---- A2 层：底座 + 台面（台面外扩 14，做出「机台压边」的立体感）----
            AddBox(sc.Ground, new V3(0, 0, 0), new V3(MX, MY, BASE_H), ColBaseSide);
            AddBox(sc.Ground, new V3(-14, -14, BASE_H), new V3(MX + 14, MY + 14, BASE_TOP), ColTable);

            // ---- B 层：14 个料盘（位置来自 TrayPlan，和俯视图同一份数据）----
            var plan = LayoutPages.TrayPlan;
            var trayTop = new List<double[]>();
            for (int i = 0; i < plan.Length; i++)
            {
                var tp = plan[i];
                double x0 = tp.U * MX, y0 = WY(tp.V + tp.DV);
                double x1 = (tp.U + tp.DU) * MX, y1 = WY(tp.V);
                AddBox(sc.Static, new V3(x0, y0, BASE_TOP), new V3(x1, y1, BASE_TOP + TRAY_H),
                    tp.Green ? ColTrayGreen : ColTrayDark);
                trayTop.Add(new double[] { x0, y0, x1, y1, tp.Green ? 1 : 0 });
            }

            // ---- B 层：2 块加热盘 ----
            // 真机俯视图量测：列 x 1356.1..1468.6；两块盘行 y 547.2..817.4 / 857.2..1126.5；
            // 机台外框 x 26..1586(1560)、y 19..1281(1262) → 归一化再乘 (MX, MY)。
            double hpx0 = (1356.1 - 26) / 1560.0 * MX;    // ≈ 852.6
            double hpx1 = (1468.6 - 26) / 1560.0 * MX;    // ≈ 924.7
            double hpv0 = (547.2 - 19) / 1262.0, hpv1 = (817.4 - 19) / 1262.0;
            double hpv2 = (857.2 - 19) / 1262.0, hpv3 = (1126.5 - 19) / 1262.0;
            double hpya = Math.Min(WY(hpv0), WY(hpv1)), hpyb = Math.Max(WY(hpv0), WY(hpv1));
            double hpyc = Math.Min(WY(hpv2), WY(hpv3)), hpyd = Math.Max(WY(hpv2), WY(hpv3));
            AddBox(sc.Static, new V3(hpx0, hpya, BASE_TOP), new V3(hpx1, hpyb, BASE_TOP + PLATE_H), ColPlate);
            AddBox(sc.Static, new V3(hpx0, hpyc, BASE_TOP), new V3(hpx1, hpyd, BASE_TOP + PLATE_H), ColPlate);

            // ---- B 层：龙门四立柱 + 前后导轨 ----
            const double pw = 34;
            double[] px = { 26, MX - 26 - pw };
            double[] py = { 40, MY - 40 - pw };
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    AddBox(sc.Static, new V3(px[i], py[j], BASE_TOP),
                        new V3(px[i] + pw, py[j] + pw, POST_TOP), ColPost);
            AddBox(sc.Static, new V3(26, 40, POST_TOP), new V3(MX - 26, 40 + 26, POST_TOP + RAIL_H), ColRail);
            AddBox(sc.Static, new V3(26, MY - 66, POST_TOP), new V3(MX - 26, MY - 40, POST_TOP + RAIL_H), ColRail);

            // ---- 芯片 ----
            // 深盘 5×10、绿盘 4×8（和俯视图完全一致）；加热盘 8×11，方形芯片正对点位居中。
            double trayTopZ = BASE_TOP + TRAY_H + 1.5;    // 抬 1.5，避免和盘面共面打架
            for (int i = 0; i < trayTop.Count; i++)
            {
                var t = trayTop[i];
                bool green = t[4] > 0.5;
                int cols = green ? 4 : 5;
                int rows = green ? 8 : 10;
                // 上排 6 张 = 待测盘（满→空，Sign=-1）；中排缓存盘 + 左下出料盘（空→满，Sign=+1）
                int sign = i < 6 ? -1 : 1;
                sc.Chips.Add(MakeChips(t[0], t[1], t[2], t[3], trayTopZ, cols, rows,
                    green ? ColChipGreen : ColChipDark, sign, i, cols, false));
            }
            double plateTopZ = BASE_TOP + PLATE_H + 1.5;
            sc.Chips.Add(MakeChips(hpx0, hpya, hpx1, hpyb, plateTopZ, 8, 11, ColChipHeat, 1, 14, 8, true));
            sc.Chips.Add(MakeChips(hpx0, hpyc, hpx1, hpyd, plateTopZ, 8, 11, ColChipHeat, 1, 15, 8, true));

            // 芯片画刷：同组法线都是 +Z，着色结果一样，所以全组共用一支
            foreach (var g in sc.Chips)
            {
                var br = new SolidColorBrush(Shade(g.On, new V3(0, 0, 1)));
                br.Freeze();
                g.OnBrush = br;
            }

            return sc;
        }

        /// <summary>
        /// 在某块盘的上表面铺一组芯片格。
        /// <paramref name="square"/> = true 时做正方形芯片（加热盘：格子高瘦，等比铺料会变竖条）。
        /// </summary>
        private static ChipGroup MakeChips(double x0, double y0, double x1, double y1, double z,
            int cols, int rows, Color on, int sign, int index, int waveCols, bool square)
        {
            var g = new ChipGroup { On = on, Sign = sign, Index = index, Cols = waveCols, Heat = square };
            double cw = (x1 - x0) / cols, ch = (y1 - y0) / rows;
            double ix = cw * 0.22, iy = ch * 0.22;
            double cs = Math.Min(cw, ch) * 0.66;      // 正方形芯片边长

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double ax, ay, bx, by;
                    // 行序要和俯视图一致：第 0 行落在**大 Y（远）**那一侧，
                    // 这样「从上到下逐格清空」在两个页签里是同一个方向。
                    if (square)
                    {
                        double mx = x0 + c * cw + cw / 2, my = y1 - r * ch - ch / 2;
                        ax = mx - cs / 2; bx = mx + cs / 2;
                        ay = my - cs / 2; by = my + cs / 2;
                    }
                    else
                    {
                        ax = x0 + c * cw + ix; bx = x0 + (c + 1) * cw - ix;
                        ay = y1 - (r + 1) * ch + iy; by = y1 - r * ch - iy;
                    }
                    var q = new Quad(new V3(ax, ay, z), new V3(bx, ay, z),
                                     new V3(bx, by, z), new V3(ax, by, z), on);
                    q.Group = index;
                    q.Slot = g.Quads.Count;
                    g.Quads.Add(q);
                }
            }
            return g;
        }

        // ─────────────────────────────────────────────────
        //  页面
        // ─────────────────────────────────────────────────

        /// <summary>「3D整机图」页签内容：视角工具条 + 真三维画布。</summary>
        public static Element BuildTab()
        {
            // ClipContent = true：地面网格比机台大一圈，投影后会伸到画布外，
            // 必须裁掉，否则会画到旁边的视角工具栏上。
            var cv = new CanvasElement { CanvasWidth = CW, CanvasHeight = CH, ClipContent = true };
            var sc = BuildScene();
            var view = new View3D();
            var r = new Renderer3D(cv, sc, view);

            r.Build();
            r.Render();     // 先整场算一遍，保证首次显示就有内容

            long last = Environment.TickCount64;
            double t = 0;

            cv.Draw = c =>
            {
                long now = Environment.TickCount64;
                double dt = (now - last) / 1000.0;
                last = now;
                if (dt <= 0 || dt > 0.5) dt = 0.05;
                if (MotionManager.Instance.AutoCycle) t += dt;
                r.Animate(t);
            };

            cv.Refresh();

            ButtonElement B(string text, Action act)
                => E.Btn(text, act, "PrimaryButton", 12);

            var bar = E.H(
                E.T("视角", 12, E.P, true),
                E.SpacerW(8),
                B("◀ 顺时针", () => { view.Az += 15; r.Render(); }),
                B("逆时针 ▶", () => { view.Az -= 15; r.Render(); }),
                B("↑ 抬高", () => { view.El = Math.Min(84, view.El + 7); r.Render(); }),
                B("↓ 降低", () => { view.El = Math.Max(6, view.El - 7); r.Render(); }),
                B("拉近", () => { view.Zoom = Math.Min(4.0, view.Zoom * 1.3); r.Render(); }),
                B("拉远", () => { view.Zoom = Math.Max(0.35, view.Zoom / 1.3); r.Render(); }),
                B("正俯视", () => { view.Az = 180; view.El = 84; r.Render(); }),
                B("复位", () => { view.Reset(); r.Render(); }),
                E.SpacerW(12),
                E.T("自算三维：世界坐标 → 相机坐标 → 透视投影 → 画家算法 → 逐面朗伯着色（非贴图）",
                    11, E.Sec));

            return E.V(bar, E.Spacer(6), cv);
        }
    }
}
