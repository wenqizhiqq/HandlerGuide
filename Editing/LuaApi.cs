// ============================================================================
//  温启志 18719361399 · 本文件由温启志（18719361399）编写与维护
//  保留所有权利；界面与源码中的署名水印均指向温启志 18719361399。
// ============================================================================
//  Editing/LuaApi.cs —— Lua 脚本编辑器的智能提示数据表
//  从 NoCodeMotion 的 Editing/LuaApi.cs 移植进来，做了两件事：
//    1. 剥掉 MoonSharp / HandyControl / 源项目专有 API（AxisMove / ReadIO / CommSend …）；
//    2. 把「宿主注册的全局函数」那一栏整表重写成 HandlerGuide 自己的流程宿主
//       （Flow/LuaFlowHost.cs 的 Install()）真正挂进 Lua 环境的那些名字与签名。
//  补全必须和宿主一致，否则操作员按提示写的函数在运行时是 nil，比不提示更坏。
//  改 Flow/LuaFlowHost.cs 的 Install() 时，这里必须同步改（对齐表见本文件末尾注释）。
// ============================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HandlerGuide.Editing
{
    /// <summary>补全项的类别（决定补全列表里那个小方块的字与颜色）。</summary>
    public enum SymbolKind
    {
        Keyword,
        Function,
        Module,
        Field,
        Variable,
        Snippet
    }

    /// <summary>一个可补全的符号：名字 / 类别 / 签名 / 中文说明 / 可插入的代码片段。</summary>
    public sealed class LuaSymbol
    {
        public LuaSymbol(string name, SymbolKind kind, string signature, string description, string insertText = null)
        {
            Name = name;
            Kind = kind;
            Signature = signature;
            Description = description;
            InsertText = insertText;
        }

        public string Name { get; }
        public SymbolKind Kind { get; }
        public string Signature { get; }
        public string Description { get; }
        public string InsertText { get; }
    }

    /// <summary>
    /// Lua 标准库 + HandlerGuide 流程宿主注册的全局函数的智能提示数据。
    /// 运行时是 NLua（KeraLua，Lua 5.4），不是 MoonSharp：
    /// 所以 MoonSharp 才有的 json 库、string.split / string.contains 之类一律不列。
    /// </summary>
    public static class LuaApi
    {
        /// <summary>代码片段里光标落点的占位符（插入前会被去掉）。</summary>
        public const char CaretMarker = '\u0001';

        /// <summary>Lua 5.4 关键字。</summary>
        public static readonly string[] Keywords =
        {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto",
            "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
        };

        // ==================================================================
        //  一、宿主注册的全局函数（Flow/LuaFlowHost.cs 的 Install() 逐条对齐）
        //  一条流程 = 一条线程 = 一个独立 Lua 状态，所以下面这些函数都是
        //  「对本流程自己」说话：step / log / alarm 记到本流程，ax 解析本流程的轴名。
        // ==================================================================

        /// <summary>宿主挂进 Lua 环境的全局函数；补全与悬停提示的第一优先来源。</summary>
        public static readonly List<LuaSymbol> HostList = new List<LuaSymbol>
        {
            // ---- 循环 / 节拍 / 记录 ----
            H("running", "running()", "本流程该继续跑吗（true / false）。主循环必须写成 while running() do ... end，返回假才退出 —— Lua 死循环从外部杀不掉，循环条件只能由脚本自己写。"),
            H("pause", "pause(ms)", "分段睡 ms 毫秒（内部每 20ms 检查一次 running()），是所有等待的基本单位。ms 填 0 或负数按 1ms 处理。"),
            H("step", "step(text)", "把本流程「当前步」那一列写成 text，供界面左侧流程表显示现在走到哪一步。"),
            H("log", "log(text)", "写一条「信息」级记录到《信息记录》页，自动加 [流程名] 前缀。"),
            H("alarm", "alarm(text)", "写一条「警告」级记录到《信息记录》页，用来报本流程自己发现的异常（不走机台报警链）。"),

            // ---- 轴（轴名一律用真表里的名字） ----
            H("ax", "ax(轴名)", "轴名 → 真表里的轴号（找不到返回 -1）；一般只用它判断这根轴在不在表上。轴名用真表名字，如 上料X轴。"),
            H("lasterr", "lasterr()", "返回本流程最近一次失败 / 被安全层拒绝的原因文本（没错时返回空串）。"),
            H("moveabs", "moveabs(轴名, 位置)", "绝对定位到位置（单位 mm），返回 true / false。轴名用真表名字，如 上料X轴；填数字轴号或表外名字直接返回 false，原因看 lasterr()。"),
            H("moverel", "moverel(轴名, 距离)", "相对当前位移距离（mm，可负），返回 true / false。轴名用真表名字，如 上料X轴。"),
            H("home", "home(轴名)", "该轴回零，返回 true / false。轴名用真表名字，如 上料X轴；被软限位 / 急停 / 气压互锁拒绝时返回 false。"),
            H("pos", "pos(轴名)", "该轴当前位置（mm）；轴名不在真表里返回 nan。轴名用真表名字，如 上料X轴。"),
            H("inpos", "inpos(轴名)", "该轴是否「不忙且无报警」（到位）。轴名用真表名字，如 上料X轴。"),
            H("axiscount", "axiscount()", "真表里的轴总数，用来在脚本里做越界自检。"),

            // ---- IO（点名同样用真表名字） ----
            H("readin", "readin(点名)", "读一路输入 IO 的电平（true / false）；点名不在真表里返回 false。点名用真表名字，如 左门禁1。"),
            H("writeout", "writeout(点名, 值)", "写一路输出 IO（值 = true / false）；点名找不到返回 false 并把原因写进 lasterr()。点名用真表名字，如 真空电磁阀1。"),
            H("outval", "outval(点名)", "读一路输出 IO 的当前回读电平（true / false）。点名用真表名字，如 真空电磁阀1。"),
            H("stable", "stable(点名, ms)", "输入 IO 连续为真 ms 毫秒才算稳并返回 true（中途变假就重新计时；停流程或点名不存在返回 false）。点名用真表名字，如 左门禁1。"),

            // ---- 具名变量黑板（流程之间唯一的共享内存） ----
            H("boxget", "boxget(变量名)", "读一个具名变量的数值（D 值），没登记过返回 0。"),
            H("boxset", "boxset(变量名, 值)", "写一个具名变量的数值（D 值）。约定 0 = 假、1 = 真。"),
            H("boxinc", "boxinc(变量名, 增量)", "给具名变量加上增量并返回新值；每圈末尾用 boxinc(\"循环计数.流程名\", 1) 计圈数。"),
            H("boxwait", "boxwait(变量名, 期望值)", "等具名变量等于期望值，最多等 5 秒：返回 1 等到了，返回 0 超时（并把原因写进 lasterr()）。"),
            H("point", "point(点位名)", "读一个点位的数值（内部取具名变量「点位.点位名」的 D 值），供定位目标使用。"),

            // ---- 令牌（互斥：共享导轨 / 干涉区一次只许一条流程占） ----
            H("take", "take(令牌名, 持有人)", "抢令牌：抢到返回 true 并登记本流程持有（持有人编号是本流程固定编号，非 0 时只允许本人重入）；被占返回 false，必须让行、不得阻塞。"),
            H("give", "give(令牌名, 持有人)", "归还令牌；正常退出必须显式 give，异常退出时宿主的 finally 会兜底归还本流程持有的全部令牌。"),
            H("held", "held()", "返回本流程当前持有的令牌名数组（没持有时是空表）。")
        };

        /// <summary>按名字取宿主函数说明（供悬停提示）；不是宿主函数返回 null。</summary>
        public static LuaSymbol FindHost(string name) =>
            string.IsNullOrEmpty(name) ? null : HostList.FirstOrDefault(s => s.Name == name);

        // ==================================================================
        //  二、Lua 5.4 标准库（NLua / KeraLua 开放的部分）
        // ==================================================================

        private static readonly List<LuaSymbol> GlobalsList = new List<LuaSymbol>
        {
            F("print", "print(...)", "把参数输出到控制台"),
            F("type", "type(v)", "返回值的类型名：nil/boolean/number/string/function/table"),
            F("tostring", "tostring(v)", "把任意值转换成字符串"),
            F("tonumber", "tonumber(v [, base])", "把字符串转成数字，失败返回 nil"),
            F("pairs", "pairs(t)", "遍历表的所有键值对：for k, v in pairs(t) do"),
            F("ipairs", "ipairs(t)", "按 1..n 顺序遍历数组部分：for i, v in ipairs(t) do"),
            F("next", "next(t [, key])", "返回表中的下一个键值对"),
            F("select", "select(index, ...)", "取变长参数的第 index 个；select('#', ...) 取个数"),
            F("assert", "assert(v [, message])", "断言：v 为假时抛出错误"),
            F("error", "error(message [, level])", "抛出一个错误"),
            F("pcall", "pcall(f, ...)", "保护调用，返回 ok, result 或 ok, errmsg"),
            F("xpcall", "xpcall(f, handler, ...)", "带错误处理函数的保护调用"),
            F("setmetatable", "setmetatable(t, meta)", "为表设置元表"),
            F("getmetatable", "getmetatable(t)", "获取表的元表"),
            F("rawget", "rawget(t, k)", "绕过元表直接读取字段"),
            F("rawset", "rawset(t, k, v)", "绕过元表直接写入字段"),
            F("rawequal", "rawequal(a, b)", "绕过元表比较是否相等"),
            F("rawlen", "rawlen(t)", "绕过元表取长度"),
            F("load", "load(chunk [, chunkname])", "把字符串 / 函数编译成 Lua 代码块"),
            F("require", "require(module)", "加载模块（NLua 里除标准库外没有预置模块）"),
            F("collectgarbage", "collectgarbage([opt])", "控制垃圾回收器"),
            M("string", "字符串库：format / find / gsub / sub …"),
            M("table", "表操作库：insert / remove / concat / sort / unpack …"),
            M("math", "数学库：floor / ceil / abs / max / random …"),
            M("os", "系统库：time / date / clock / getenv …"),
            M("io", "输入输出库：open / read / write / lines …"),
            M("coroutine", "协程库：create / resume / yield …"),
            new LuaSymbol("_G", SymbolKind.Variable, "_G", "全局环境表"),
            new LuaSymbol("_VERSION", SymbolKind.Variable, "_VERSION", "Lua 版本字符串（本机由 NLua 带的 Lua 5.4）")
        };

        private static readonly Dictionary<string, List<LuaSymbol>> Members =
            new Dictionary<string, List<LuaSymbol>>(StringComparer.Ordinal)
            {
                ["string"] = new List<LuaSymbol>
                {
                    F("format", "string.format(fmt, ...)", "格式化字符串，如 string.format('%d 分', 90)"),
                    F("len", "string.len(s)", "字符串长度，等价于 #s"),
                    F("sub", "string.sub(s, i [, j])", "取子串，索引从 1 开始，支持负数"),
                    F("upper", "string.upper(s)", "转大写"),
                    F("lower", "string.lower(s)", "转小写"),
                    F("rep", "string.rep(s, n [, sep])", "重复字符串 n 次"),
                    F("reverse", "string.reverse(s)", "反转字符串"),
                    F("find", "string.find(s, pattern [, init [, plain]])", "查找模式，返回起止位置"),
                    F("match", "string.match(s, pattern [, init])", "按模式匹配并返回捕获"),
                    F("gmatch", "string.gmatch(s, pattern)", "迭代所有匹配：for w in string.gmatch(s, '%a+') do"),
                    F("gsub", "string.gsub(s, pattern, repl [, n])", "替换匹配内容，返回新串和替换次数"),
                    F("byte", "string.byte(s [, i [, j]])", "取字符的数值编码"),
                    F("char", "string.char(...)", "由数值编码构造字符串")
                },
                ["table"] = new List<LuaSymbol>
                {
                    F("insert", "table.insert(t, [pos,] value)", "插入元素，默认追加到末尾"),
                    F("remove", "table.remove(t [, pos])", "移除并返回元素，默认移除最后一个"),
                    F("concat", "table.concat(t [, sep [, i [, j]]])", "把数组元素连接成字符串"),
                    F("sort", "table.sort(t [, comp])", "排序，comp(a, b) 返回 a 是否应排在前面"),
                    F("unpack", "table.unpack(t [, i [, j]])", "展开数组为多返回值（Lua 5.4 是 table.unpack，没有全局 unpack）"),
                    F("pack", "table.pack(...)", "把变长参数打包成表，字段 n 为个数"),
                    F("move", "table.move(a1, e, f, a2 [, ...])", "把一段元素搬到另一张表")
                },
                ["math"] = new List<LuaSymbol>
                {
                    F("floor", "math.floor(x)", "向下取整"),
                    F("ceil", "math.ceil(x)", "向上取整"),
                    F("abs", "math.abs(x)", "绝对值"),
                    F("max", "math.max(...)", "最大值"),
                    F("min", "math.min(...)", "最小值"),
                    F("sqrt", "math.sqrt(x)", "平方根"),
                    F("sin", "math.sin(x)", "正弦（弧度）"),
                    F("cos", "math.cos(x)", "余弦（弧度）"),
                    F("tan", "math.tan(x)", "正切（弧度）"),
                    F("asin", "math.asin(x)", "反正弦"),
                    F("acos", "math.acos(x)", "反余弦"),
                    F("atan", "math.atan(y [, x])", "反正切"),
                    F("exp", "math.exp(x)", "自然指数"),
                    F("log", "math.log(x [, base])", "对数"),
                    F("fmod", "math.fmod(x, y)", "取余"),
                    F("modf", "math.modf(x)", "拆分整数与小数部分"),
                    F("deg", "math.deg(x)", "弧度转角度"),
                    F("rad", "math.rad(x)", "角度转弧度"),
                    F("random", "math.random([m [, n]])", "随机数：无参 [0,1)，一参 [1,m]，两参 [m,n]"),
                    F("randomseed", "math.randomseed(x)", "设置随机种子"),
                    F("tointeger", "math.tointeger(x)", "能精确转成整数就返回整数，否则返回 nil"),
                    F("type", "math.type(x)", "返回 integer / float / nil"),
                    new LuaSymbol("pi", SymbolKind.Field, "math.pi", "圆周率 3.1415926…"),
                    new LuaSymbol("huge", SymbolKind.Field, "math.huge", "正无穷大"),
                    new LuaSymbol("maxinteger", SymbolKind.Field, "math.maxinteger", "整数上限"),
                    new LuaSymbol("mininteger", SymbolKind.Field, "math.mininteger", "整数下限")
                },
                ["os"] = new List<LuaSymbol>
                {
                    F("time", "os.time([table])", "取时间戳"),
                    F("date", "os.date([format [, time]])", "格式化日期，如 os.date('%Y-%m-%d')"),
                    F("clock", "os.clock()", "程序占用的 CPU 时间，常用于计时"),
                    F("difftime", "os.difftime(t2, t1)", "两个时间戳之差（秒）"),
                    F("getenv", "os.getenv(name)", "读取环境变量"),
                    F("exit", "os.exit([code])", "退出解释器（流程脚本里别用，会带走整条流程线程）"),
                    F("tmpname", "os.tmpname()", "生成一个临时文件名")
                },
                ["io"] = new List<LuaSymbol>
                {
                    F("open", "io.open(filename [, mode])", "打开文件，返回文件句柄"),
                    F("close", "io.close([file])", "关闭文件"),
                    F("read", "io.read([format])", "读取输入"),
                    F("write", "io.write(...)", "写出内容（不换行）"),
                    F("lines", "io.lines([filename])", "按行迭代文件"),
                    F("type", "io.type(obj)", "判断是否为文件句柄")
                },
                ["coroutine"] = new List<LuaSymbol>
                {
                    F("create", "coroutine.create(f)", "创建协程"),
                    F("resume", "coroutine.resume(co, ...)", "启动 / 继续协程"),
                    F("yield", "coroutine.yield(...)", "挂起当前协程"),
                    F("status", "coroutine.status(co)", "协程状态：running/suspended/dead"),
                    F("wrap", "coroutine.wrap(f)", "把协程包装成函数"),
                    F("running", "coroutine.running()", "返回当前协程"),
                    F("isyieldable", "coroutine.isyieldable()", "当前协程能否 yield")
                }
            };

        /// <summary>常用代码片段（补全列表里标 S）。片子里的 \u0001 是光标落点。</summary>
        public static readonly List<LuaSymbol> Snippets = new List<LuaSymbol>
        {
            S("flow", "流程主循环骨架（running + 令牌 + 计圈）",
              "while running() do\r\n\tif take(\"ARM_X\", OWNER) then\r\n\t\tstep(\"" + CaretMarker + "\")\r\n\t\t-- TODO 运动：每步先 home() / inpos() 再 moveabs()\r\n\t\tpause(PERIOD)\r\n\t\tgive(\"ARM_X\", OWNER)\r\n\telse\r\n\t\tpause(PERIOD)   -- 令牌被别的流程占了：让行，绝不阻塞\r\n\tend\r\n\tboxinc(\"循环计数.流程名\", 1)\r\nend"),
            S("if", "if 条件语句", "if " + CaretMarker + " then\r\n\r\nend"),
            S("ifelse", "if-else 语句", "if " + CaretMarker + " then\r\n\r\nelse\r\n\r\nend"),
            S("for", "数值 for 循环", "for i = 1, " + CaretMarker + " do\r\n\r\nend"),
            S("forp", "pairs 遍历", "for k, v in pairs(" + CaretMarker + ") do\r\n\r\nend"),
            S("fori", "ipairs 遍历", "for i, v in ipairs(" + CaretMarker + ") do\r\n\r\nend"),
            S("while", "while 循环", "while " + CaretMarker + " do\r\n\r\nend"),
            S("function", "函数定义", "function " + CaretMarker + "()\r\n\r\nend"),
            S("local function", "局部函数定义", "local function " + CaretMarker + "()\r\n\r\nend"),
            S("repeat", "repeat-until 循环", "repeat\r\n\t" + CaretMarker + "\r\nuntil ")
        };

        public static IReadOnlyList<LuaSymbol> Globals => GlobalsList;

        /// <summary>标准库模块成员表（供 t. 之后的成员提示）。</summary>
        public static bool TryGetMembers(string module, out List<LuaSymbol> members) =>
            Members.TryGetValue(module, out members);

        /// <summary>所有标准库表名。</summary>
        public static IEnumerable<string> ModuleNames => Members.Keys;

        /// <summary>按名字查找（先宿主函数、再标准库全局、再模块成员），用于悬停提示。</summary>
        public static LuaSymbol Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var hit = HostList.FirstOrDefault(s => s.Name == name);
            if (hit != null) return hit;

            hit = GlobalsList.FirstOrDefault(s => s.Name == name);
            if (hit != null) return hit;

            foreach (var kv in Members)
            {
                hit = kv.Value.FirstOrDefault(s => s.Name == name);
                if (hit != null) return hit;
            }

            if (Keywords.Contains(name))
                return new LuaSymbol(name, SymbolKind.Keyword, name, "Lua 关键字");

            return null;
        }

        /// <summary>查找 module.member 形式的符号。</summary>
        public static LuaSymbol FindMember(string module, string member)
        {
            if (module != null && Members.TryGetValue(module, out var list))
                return list.FirstOrDefault(s => s.Name == member);
            return null;
        }

        // ---------------- 造表用的小工厂 ----------------

        private static LuaSymbol H(string name, string signature, string desc) =>
            new LuaSymbol(name, SymbolKind.Function, signature, desc);

        private static LuaSymbol F(string name, string signature, string desc) =>
            new LuaSymbol(name, SymbolKind.Function, signature, desc);

        private static LuaSymbol M(string name, string desc) =>
            new LuaSymbol(name, SymbolKind.Module, name, desc);

        private static LuaSymbol S(string name, string desc, string body) =>
            new LuaSymbol(name, SymbolKind.Snippet, name + " …", desc, body);
    }
}
// ============================================================================
//  宿主 API 对齐表（Flow/LuaFlowHost.cs 的 Install()，共 25 条，与 HostList 一一对应）
//   1 running   2 pause    3 step     4 log      5 alarm
//   6 ax        7 lasterr  8 moveabs  9 moverel  10 home
//   11 pos      12 inpos   13 axiscount          14 readin   15 writeout
//   16 outval   17 boxget  18 boxset  19 boxinc  20 boxwait  21 stable
//   22 take     23 give    24 held    25 point
//  这就是「宿主有 / 补全有」的对照基准：两边都齐 25 条，一个不多一个不少。
// ============================================================================

// ---------------------------------------------------------------------------
//  温启志 18719361399 · 文件结束（署名水印）
// ---------------------------------------------------------------------------
