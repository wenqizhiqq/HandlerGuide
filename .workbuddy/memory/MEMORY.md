# HandlerGuide

## 验证管线
- 构建 `dotnet build HandlerGuide.csproj -c Debug`；自检 `HG_SELFTEST=1` 跑 `bin\Debug\net8.0-windows\HandlerGuide.exe`。
- 绿线：`selftest done ok=52 bad=0 bind=0`、`clock ratio≈1.0 PASS`、`cycle 偏差<20% PASS`、`flow 被拒=0 异常=0`、`ops ALL PASS ok=245`。日志 `%TEMP%\handlerguide.log`。
- 只用 PowerShell；⚠️ 改完 `.lua` **必须手动** `Copy-Item Scripts\x.lua bin\Debug\net8.0-windows\Scripts\`，构建不刷新运行目录脚本。
- Lua 后台真跑 `python -X utf8 artifacts\req64_lua_harness.py --cycles N --out f.txt`；`--yield-rate 1.0` 额定口径、`--tray-cap 16` 压换盘；`artifacts\req66_regress.py` 17 场景。
- ⚠️ `artifacts/*.py` 只能用 Write/Edit 改，PowerShell 写回会变 DLP 密文。
- ⚠️ **`.lua` 不能用 Edit/Write 改**：本机 DLP 会把它们透明加密（头 `88 7D 1C`），Lua 读到密文报 `unexpected symbol near '?'`。
  只能 Python 写 `.tmp` + `os.replace`（`artifacts/_fix31*.py` 是范例）；改完查 `head=45,45,32`(=`-- `) 才算明文。
- 宿主链路后台验证（**不启动 WPF**）：`%TEMP%\hg_lua_probe`（NLua 1.7.9 控制台），跑 `luahost_probe.exe <脚本> <报告>`，
  A1 = `DoString(string)`、A2 = 字节级加载，判定「脚本能不能加载 / 中文键是否命中 / 是否进入开工并下发 moveabs」。

## 架构
- 轴 45 根不连续：4/5 梭车X、10/11 下料X/Y、18..25 下料吸嘴Z、26/27 上料X/搬运X、28..30 三仓Z、32 上料Y、35..37 仓Y、39..46 上料吸嘴Z。按名取轴。
- 安全：`BuildRules()` 只供显示，判定在 `SafetyCheck()`，两处必须同改。
  走廊 = `{上料X, 搬运X}`（下料X 在分支4 独立导轨、home 900）；home 上料0/搬运442.3/下料900，写错=永久占道。
- SR-08 前后梭车/压头联动、SR-11 后梭车压头、SR-12 两臂走廊互斥；`ShuttlePressClear` 判梭车在压区时压头必须抬起。`AxisParamVersion` 3。
- `SimMotionCard`：`Step` 内部截 dt≤0.2s；首次 `Configure` 播种 `HomeMm`；噪声仅运动时叠加。`SyncVacuumFeedback()` 回显真空否则投料恒 0。
- 时钟：快速通道用 `Stopwatch` 真实间隔，禁压 dt；关窗 `_timer.Stop()`。cycle/梭车理论值从 `m.Cfg(ax)` 现算，**禁止写死速度**。
- **IO 两套表并存（需求 86）**：主表 `HandoverTables.Inputs/Outputs`（303/123，U0_X2.0 编号）驱动仿真；
  附表「上料/下料-输入输出」是 GL20 远端 IO 另一版接线，存 `Gl20Inputs/Gl20Outputs/Gl20Analog`（301/145/4，
  表内点号 输入216·输出200 + 块内位号），`Gl20Blocks` 26 块、`Gl20FastIn` 8 路、`Gl20OriginAxes` 20 根、
  `BusBranches`(Sheet2 X2~X6)、`SpareParts`(Sheet1 型号)。界面只读页 `MotionPages.TabGl20()`。
  解析要点：附表块标题可能在 col0/col7，同行两个标题=左右两个独立块，否则左右共用一个块。

## 31_平移分选.lua
- 66 八嘴+真空确认+飞拍+三本账+连败锁存；67 Auto塔/NG仓/压头真空/变距旋转/进料仓换盘；68 per-site→逐 bin 停靠+UPH上限；69 重测/温控/清针/落料真空复核。
- 约束：一线程一文件、`while running()`、`need()`+`stage()` pcall、改向前 `settle()`。
- **71 双梭车+双压头**：`前/后梭车X(4/5)` + `前/后压头Z`，同一 SHUTTLE 导轨（间隙120），测试位前500/后700。`上料X(26)` 分支2、`下料X(10)` 分支4 = 两条独立导轨（旧 `Rail="ARM_X"` 是假共轨，UPH 只有 3.3K 的根因）。
  协程调度：spawn 上料/压前/压后/下料四常驻任务，`sleep=coroutine.yield`，节拍=max(各段)。`NEST[F/B]={state,batch}`，`batch` 独立表（放下新批就换表）；`ARMX_OWNER` 任务级互斥。
- **72**：换盘挪到本批交付完那一瞬；`free_nest()` 用 FIFO（固定优先会相位锁定）；变距复位 gneed 挪到下一批压吸嘴前（上料 2.31→1.98s）。
- **73 参数改回资料原值 → 同口径跨过 11K**（`artifacts/req73_report.md`、回归 `req73_regress.txt`）：
  - 长川 C6800C 原文 `Max. UPH 11K (2*4, QFN5*5, 14*35 Tray, 常温, test time=1.5s)`
    ⇒ `料盘.容量` 默认 **490**（原写死 120 ⇒ 换盘 ×4、长跑被耗材吃掉 18%）、`压头.测试时间` 默认 **1500**（原 500 ⇒ 拿更快的测试比手册 11K，口径不成立）。
  - 存档容量变了按新容量重设三本账（`料盘.容量版本`）。
  - 真空建立/Z 下压并行、破真空/Z 抬起并行；接触 80→50ms；吸嘴真空从死等 120/90ms 改**轮询真空开关**（下限 40/超时 300）；轴到位轮询 20→10ms。
  - 停机后 `gmove` 一律不下发（`nozzles()` 被 StopDemo 打断时 return false 不抛错，后续水平轴会被安全层连拒，污染「无拒绝」判据）。
  - **结果：额定 35 圈稳态 11,495**（Index 0.29s/峰值 0.50s、JAM 0/万、料账 288=280+8+0）；17 场景全 PASS。
  - 口径：11,495 是**额定**（无重测/无换盘）；97% 良率下 10.3K（每 4 批整批重压 1.5s，是质量动作不是产能）。`换盘压测（盘容16）` 净节拍 16,050 ⇒ 节拍已不是瓶颈。
  - ⚠️ `seg_add` 必须 `(SEG[k] or 0)+d`，新分类第一次是 nil 会抛「算术 nil」。
- **74 常驻循环 + 慢放**（日志 `2026-11-09.md`）：
  - 「仿真刚开始就结束」两个根因：① 全工程没人把 `分选.启用` 置 1，而旧条件要求它=1 ⇒ 一启动就进待机；
    ② 布局页〔启动演示节拍〕不调 `Scan()`，`flows` 空时 `StartAll` 启动 0 条。
    修法：启动条件 → `demo or (en and safe)`（`仿真.演示` 由 `StartDemo/StopDemo` 置）；`StartAll()` 空列表自动 `Scan()`。
  - 主循环 `pcall(start_line)`：出错只锁存这一轮、正常收尾自动接下一轮，只有 `running()` 变假才退出（宿主 `running()` = `f.Running && !stopAll`，`DoString` 返回即「已退出」）。
  - 慢放 `点位.仿真.慢放倍率`（1=真机速度，2=半速，5=1/5，0.5=两倍）：`SimMotionCard.SpeedScale` 缩 dt（**先除再截 0.2**）+ `drive()` 只放大真实 `pause`，`NOW/RUN_MS` 不变 ⇒ 观感慢、UPH 口径不变。
  - `Flow/LuaFlowHost.cs` 新增 `simspeed(v)`；`MotionManager.SimSpeedScale` 每帧同步给仿真卡。
  - ⚠️ `dotnet build` 出现 `Stack overflow (Copy.DoCopyIfNecessary)` 时：先手动 `Copy-Item` 那个 .lua 再 build；**不要**改 `BaseOutputPath`（bin\Debug 不被排除 ⇒ 52 条 MC3074）。
- **76 中文跨边界（这才是「脚本没跑起来」的真根因）**：探针 `artifacts/_luahost_final.txt`
  - `NLua.DoString(string)` 把源码按 ANSI(GBK) 编码后才交给 `luaL_loadbuffer` ⇒ **源码进 Lua 之前**每个中文已是 `?`(0x3F)。
    于是 `boxget("仿真.演示")` 恒 0 ⇒ 主循环恒落待机 ⇒ 界面「刚开始就结束 / 轴一直在原位」。转义、C# 侧解码都救不回来。
  - 修 ①：宿主改 `e.State.LoadBuffer(Encoding.UTF8.GetBytes(text), name)` + `st.PCall(0,0,0)`（`LuaFlowHost.Run`），
    `ReadScriptText` 保留密文检测。修 ②：字符串 interop 同样走 ANSI（Lua→C# 也变 `?`），
    脚本顶层 `__wrapNames(...)` 把所有 API 名字编成 `h:`+UTF-8 十六进制，C# `LuaFlowHost.Unhex()` 解回；无 `h:` 前缀原样返回（旧脚本不受影响）。
  - ⚠️ NLua 注册的委托在 Lua 里是 **userdata 不是 function**（`type(f)=="function"` 会把包装全跳过）；
    转发必须**保参数个数**（`table.unpack(args,1,n)`），多传一个 nil 就 `Cannot invoke delegate (invalid arguments)`。
  - 测试台 `req64_lua_harness.py` 的每个 stub 都要 `_uh()` 解码（含 `boxinc`，漏它 = 0 产出、料账 746 颗/批）。
  - 验证：A1(DoString) FAIL / A2(字节加载) PASS；17 场景回归 `req76_regress.txt` 全 PASS，额定 35 圈 11,495 不变。

## 其它
- **⚠️ 11-11 11:33 整树还原（用户确认「有意的」）**：`git reflog` 显示
  `HEAD=50eb4b0 "x移动" 11:35:36`（提交在还原后的树上，工作区 clean），
  `ViewModels / Scripts / Editing / Flow` 目录 mtime 全停在 11:33:12。
  今天未提交的需求 **79（14×35=490 格 + 料账）、80（上料Y逐行 + 皮带相位）、
  81（满盘搬运 + `料盘.臂载`）、82（料盘 469）全部作废**，磁盘回到 11-09 那一版
  （`FEED_COLS=5/FEED_ROWS=8`、`feedY≈292`、31.lua 无 `feed_cell`）。
  用户口径：**画布只要 X 移动**，不要按旧笔记恢复这些功能；`Flow/LuaFlowHost.cs`
  需求 76 的修复属于早先提交，仍有效。教训：改前改后先 `git status`、及时 commit，C# 没有备份。
- **75 画布轴速度 = 1 秒 10px**（`点位.仿真.画布速度`，MotionManager 构造登记默认 10）：
  只改 `LayoutPages` 的**时隙长度算法**（`SlotByPx = 行程px ÷ (px速度 × 0.35)`，0.35=`(1−PhaseHold)/2`），
  轴表 MaxVel 一律不动。三时隙不再等长（下料/上料 113.9s、搬运 215.4s，一轮 443s），
  槽定位改按累计边界 `cyc<SLOT_D ? 0 : ...`，demo 日志 `(3.0*SLOT_SEC)` 全换 `(CYC_SEC)`。
  ⚠️ 各机构画布比例差 7 倍（上料 0.19 / 下料 0.32 / 横梁 1.33 / 搬运 1.08 px/mm），
  **不可能所有轴同时 10px/s** ⇒ 口径 = 每槽「行程最长的那根」= 设定值，其余按真机比例更慢。
  填 0/负数 = 退回需求 64 的真机时间（8.2s/轮）。脚本运行时的慢放仍看 `仿真.慢放倍率`，两者不互相覆盖。
- 画布三臂只有 X 自由度，t=0 与照片重合；横梁 sprite 长度不许加长。文件头尾署名「温启志 18719361399」。
- `.workbuddy/memory/*.md` 本机透明加密，PowerShell 读密文，只能用 Read/Edit。
- ⚠️ 一切理论值从轴表现算；`--slow-ui`（time_scale<1）场景不做规格判定；良率<100% 不按额定口径判 UPH，否则「良率差⇒UPH 不合格」是同义反复。
