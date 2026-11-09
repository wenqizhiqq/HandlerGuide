# HandlerGuide

## 验证管线
- 构建 `dotnet build HandlerGuide.csproj -c Debug`；自检 `HG_SELFTEST=1` 跑 `bin\Debug\net8.0-windows\HandlerGuide.exe`。
- 绿线：`selftest done ok=52 bad=0 bind=0`、`clock ratio≈1.0 PASS`、`cycle 偏差<20% PASS`、`flow 被拒=0 异常=0`、`ops ALL PASS ok=245`。日志 `%TEMP%\handlerguide.log`。
- 只用 PowerShell；⚠️ 改完 `.lua` **必须手动** `Copy-Item Scripts\x.lua bin\Debug\net8.0-windows\Scripts\`，构建不刷新运行目录脚本。
- Lua 后台真跑 `python -X utf8 artifacts\req64_lua_harness.py --cycles N --out f.txt`；`--yield-rate 1.0` 额定口径、`--tray-cap 16` 压换盘；`artifacts\req66_regress.py` 17 场景。
- ⚠️ `artifacts/*.py` 只能用 Write/Edit 改，PowerShell 写回会变 DLP 密文。

## 架构
- 轴 45 根不连续：4/5 梭车X、10/11 下料X/Y、18..25 下料吸嘴Z、26/27 上料X/搬运X、28..30 三仓Z、32 上料Y、35..37 仓Y、39..46 上料吸嘴Z。按名取轴。
- 安全：`BuildRules()` 只供显示，判定在 `SafetyCheck()`，两处必须同改。
  走廊 = `{上料X, 搬运X}`（下料X 在分支4 独立导轨、home 900）；home 上料0/搬运442.3/下料900，写错=永久占道。
- SR-08 前后梭车/压头联动、SR-11 后梭车压头、SR-12 两臂走廊互斥；`ShuttlePressClear` 判梭车在压区时压头必须抬起。`AxisParamVersion` 3。
- `SimMotionCard`：`Step` 内部截 dt≤0.2s；首次 `Configure` 播种 `HomeMm`；噪声仅运动时叠加。`SyncVacuumFeedback()` 回显真空否则投料恒 0。
- 时钟：快速通道用 `Stopwatch` 真实间隔，禁压 dt；关窗 `_timer.Stop()`。cycle/梭车理论值从 `m.Cfg(ax)` 现算，**禁止写死速度**。

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

## 其它
- 画布三臂只有 X 自由度，t=0 与照片重合；横梁 sprite 长度不许加长。文件头尾署名「温启志 18719361399」。
- `.workbuddy/memory/*.md` 本机透明加密，PowerShell 读密文，只能用 Read/Edit。
- ⚠️ 一切理论值从轴表现算；`--slow-ui`（time_scale<1）场景不做规格判定；良率<100% 不按额定口径判 UPH，否则「良率差⇒UPH 不合格」是同义反复。
