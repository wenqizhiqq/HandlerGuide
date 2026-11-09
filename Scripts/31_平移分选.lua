-- ============================================================================
--  31_平移分选：整条芯片流（Tray 上料 → 视觉定位 → 压测 → 按 Bin 分选 → 换盘）
--  绑定：上料X/Y轴、上料吸嘴1..8Z轴、上料变距X/Y、上料旋转U轴、
--        前/后梭车X轴、前/后压头Z轴、下料X/Y轴、下料吸嘴1..8Z轴、
--        下料变距X/Y、Auto1..3 Z轴、进料仓Z/Y轴、搬运X轴 + 夹爪/上下气缸
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距 / 梭车与压头联动都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
--
--  ⚠ 这条是「整链路」流程：只在变量 分选.启用 = 1 时才跑，且生产期间独占
--     上料臂（ARM_X 令牌按批 take/give）。01_上料XY / 10_下料XY 这类单模块
--     调试流程请在生产时关掉，否则会抢同一根轴。
--
--  温启志 18719361399
--  ────────────────────────────────────────────────────────────────────────
--  【需求 66~70】真机标准流程与判定口径（已全部落地，逐条见下方注释）
--    ① Tray 堆叠自动供料 ② 视觉定位纠偏 ③ 真空吸取（逐路真空开关确认）
--    ④ 高速平移至测试站 ⑤ 压头下压接触 ATE 电测 ⑥ 抬起
--    ⑦ 下料臂取料 ⑧ 按 Bin 等级分选入 Auto1/2/3 + NG1/2 ⑨ 满盘换盘
--    ⑩ 数据收集（UPH / Yield / JAM / MTBA / Contact Count / per-site）
--  资料参数对照（长川 C6800C / 万福达 WFD-CG12000G / 深科达 SKD308H 公开样本）：
--    测试工位 8 site（2×4）        → BATCH = 8，吸嘴组 2×4          ✔
--    2×4 pitch X 40 / Y 60mm       → 变距轴收拢（默认 -12.5 / -8）  ✔
--    Auto Unload ×3                → Auto1/2/3 三座收料塔            ✔
--    料仓容量 35 张 Tray           → MAG_CAP = 35                    ✔
--    Tray 规格 **14×35**（= 490 颗）→ 料盘.容量（默认 490）          ✔ 需求 73
--    test time = 1.5s（UPH 11K 的前提）→ 压头.测试时间（默认 1500） ✔ 需求 73
--    per-site 结果 / per-site bin  → 分选.结果1..8，逐 bin 停靠落料  ✔
--    Retest 1~5 次（救假 NG）      → 分选.重测次数 / 重测挽回         ✔
--    温控 ±1℃ 到温才允许开测       → 加热盘.温度 互锁（默认关闭）    ✔
--    Contact Count / 清针周期      → 分选.接触次数 / 分选.清针间隔    ✔
--    掉料检测（落料前复核真空）    → 分选.掉料次数（不记账）          ✔
--    Index Time ≤0.6s（接触力≤120kgf）→ 实测梭车换位 0.29s（峰值 0.50）✔
--    UPH Max. 11K（2×4, QFN5×5, 14×35 Tray, 常温, test 1.5s）
--                                  → 实测额定 11,495（35 圈，无重测）  ✔
--    Jam Rate ≤1/5000              → 分选.JAM率 实测 0/万            ✔
--
--  ============================================================================
--  【需求 73】把「和网上资料对不上」的最后两处参数改正过来
--
--  查长川 C6800C 官方样本 PDF，UPH 11K 那一行的原文是：
--      Max. UPH 11K (2*4模式, Base on QFN5*5, **14*35 Tray**, 常温测试, **test time=1.5s**)
--  这句话里有两个我们一直写错的硬参数：
--    ① Tray 是 **14×35 = 490 颗/盘**。上一版写死 TRAY_CAP = 120，比真表小 4 倍，
--       换盘次数跟着 ×4，长跑 UPH 被耗材动作吃掉 18%（那 18.6s 的换盘占用就是这么来的）。
--       现在走参数「料盘.容量」，默认 490；存过旧容量的会按新容量自动重设三本账。
--    ② **test time = 1.5s** 是 11K 的前提。上一版压测只给了 500ms，等于拿「测得更快」
--       去比手册的 11K，口径不成立。现在默认 1500ms，与资料同口径。
--
--  改完这两条后额定（无重测、连续供料）节拍反而冲上去了，因为瓶颈从「换盘」换回了
--  真正的搬运。逐项量出来再压：
--    · 料座压测段 3.68s → 2.53s：真空建立与压头 Z 下压**并行**、破真空与抬起并行
--      （真机本来就是边下压边建真空，先等满 100ms 再动 Z 是白丢的）；接触稳定 80→50ms。
--    · 吸嘴真空：死等 120/90ms → **轮询真空开关**（SMC ZZB08 就是干这个的），
--      建立下限 40ms、超时 300ms 才判漏吸/没释放。一批 4 次建立/释放，省 0.26s。
--    · 轴到位轮询 20ms → 10ms（伺服位置更新周期就是 4~10ms，20ms 每段白丢 10ms）。
--    · 停机打断后 gmove 一律不下发（否则吸嘴悬空时水平轴被安全层连拒，污染「无拒绝」）。
--
--
--  ============================================================================
--  【需求 71】把串行节拍改成真机的「双梭车 + 双压头」流水线
--
--  上一版的实测（8.46s/圈、UPH 3.4K）离样本手册的 11K 差了 3 倍。原因**不是
--  脚本慢**，是架构错了：上一版把 上料/测试/下料 串成一条线，一圈里 88% 的时间
--  在搬料，压测只占 8%，两边互相等。
--
--  查真表（HandoverTables）发现之前有一处硬错误：
--    · 上料X轴(26) 在 **分支2**、下料X轴(10) 在 **分支4** —— 是两条独立的
--      EtherCAT 分支 / 独立龙门，**机械上不共轨**；
--    · 真机还有 前梭车X(4) / 后梭车X(5) + 前压头(0/2) / 后压头(1/3) ——
--      就是样本手册里那套「双料座轮流进出压头」的并行架构。
--  旧版把它们挂在一条虚构的 ARM_X 导轨上（31 轴旧仿真表遗留），SR-08/SR-11
--  于是要求「同一时刻只有一根臂能离开停放位」⇒ 上料、测试、下料被迫串行。
--  需求 71 起：安全层拆掉这条假共轨（只保留 上料臂 ↔ 料盘搬运臂 的真互斥），
--  脚本改成三条常驻任务并行：
--
--      上料任务 ──► 料座F/B（空）──► 前梭车/后梭车 送进压头
--                                     │
--      压头任务（前后各一，可同时在压）◄┘ 测完退回交接位
--                                     │
--      下料任务 ◄──────────────────────┘ 取料 → 按 bin 分选 → 落料
--
--  一圈（= 一批 8 颗）的时间不再是三段相加，而是**取三者最大值**：
--      上料搬运 ≈ 1.9s ｜ 下料分选 ≈ 2.1s ｜ 压测（前后并行）≈ 0.9s
--  两条料座交错 ⇒ 上料臂与下料臂各自满负荷、互不等待，理论 UPH ≈ 8×3600/2.1
--  ≈ 13.7K；实测（后台测试台真跑，含视觉飞拍、真空确认、重测、换盘）见
--  artifacts/req71_regress.txt。
--
--  与真机一致的另几处：
--    · 八嘴整批取放，**不再按行分度** —— 2×4 吸嘴组一次覆盖两行，上料Y 全程
--      停在工作位（旧版每批 Y 往返 4 次白花 1.0s，那是单行吸嘴的做法）；
--    · 料座是「上料位 = 下料位」同一个工位：上料臂放、下料臂取都在梭车交接位，
--      中间由梭车送进压头 —— 真机双梭车就是这么走的（前后各一个料座）；
--    · 前梭车测试位 500 / 后梭车 700，共轨间隙 200 > 120，两个料座可以同时在压；
--    · 换盘、补空盘、清针、温控、重测全部保留，但都挪到「不挡节拍」的位置
--      （换盘在上料任务里顺手做，补盘在两臂都空出来的时候做）。
-- ============================================================================

local OWNER   = 31
local OWNER_P = 32
local PERIOD  = 20

-- 2×4 = 8 嘴整批（真机吸嘴组是 2 行 × 4 列，画布按 8 颗一拍）
local NOZ_L = { "上料吸嘴1Z轴", "上料吸嘴2Z轴", "上料吸嘴3Z轴", "上料吸嘴4Z轴",
                "上料吸嘴5Z轴", "上料吸嘴6Z轴", "上料吸嘴7Z轴", "上料吸嘴8Z轴" }
local NOZ_U = { "下料吸嘴1Z轴", "下料吸嘴2Z轴", "下料吸嘴3Z轴", "下料吸嘴4Z轴",
                "下料吸嘴5Z轴", "下料吸嘴6Z轴", "下料吸嘴7Z轴", "下料吸嘴8Z轴" }

local BATCH     = 8      -- 每批颗数（2×4）
-- 一盘容量。**资料口径**：长川 C6800C 样本手册的 UPH 11K 备注写的是
--   「2*4模式, Base on QFN5*5, 14*35 Tray, 常温测试, test time=1.5s」
-- ⇒ 14×35 = 490 颗/盘。上一版写死 120（「JEDEC 小尺寸典型 100~200」，纯拍），
--   比真表小 4 倍 ⇒ 换盘次数 ×4，长跑 UPH 被耗材动作吃掉 18%。这是错的。
-- 走参数「料盘.容量」，现场换别的 Tray 规格只改点位，不用改脚本；
-- 回归里压换盘逻辑时把这个参数调小（8 / 16）即可，不必跑几百圈。
local TRAY_DEF  = 490    -- 默认一盘（14×35 Tray）
local MAG_CAP   = 35     -- 料仓容量（资料口径：35 张 Tray/仓）
local TOWER_MAX = 25     -- 收料塔最多叠多少张（500mm 行程 / 20mm 层高）

local Z_UP, Z_DOWN = 30, 5

-- 真空 IO：输出「吸真空n / 破真空n」，输入「真空n」= 真空开关（真机 SMC ZZB08）
local SIDE = {
    L = { vac = "左上料吸真空", blow = "左上料破真空", sen = "左上料真空" },
    U = { vac = "右下料吸真空", blow = "右下料破真空", sen = "右下料真空" },
}
-- 压头真空：前 / 后压头各 16 路（真表 U22 Bit16.. / U23 Bit16..），脚本用前 8 路
local PVAC = {
    F = { vac = "前压头吸真空", blow = "前压头破真空", sen = "前压头真空" },
    B = { vac = "后压头吸真空", blow = "后压头破真空", sen = "后压头真空" },
}

-- ============================================================================
--  需求 71：双料座（前 / 后梭车）
--
--  梭车坐标与手臂坐标是**两套坐标系**，不能混：
--    · 梭车：交接位 / 测试位 —— 前 300 / 500，后 900 / 700（前 后共一条 1200mm
--      导轨，间隙 120mm；两个测试位相距 200mm ⇒ 两个料座能同时在压头下方）；
--    · 手臂：料座在 上料X / 下料X 上的位置 —— 前 620 / 后 740（上料臂放下料、
--      下料臂取料都在这里，真机也是同一个交接工位）。
-- ============================================================================
local LANE = {
    F = { key = "F", name = "前", shuttle = "前梭车X轴", press = "前压头Z轴",
          hand = 300, test = 500, nestX = 620 },
    B = { key = "B", name = "后", shuttle = "后梭车X轴", press = "后压头Z轴",
          hand = 900, test = 700, nestX = 740 },
}
local LANES = { "F", "B" }

-- 料座状态机：0 = 空（可以上料）｜1 = 已上料待测 ｜2 = 测完待下料
-- batch 是一张**独立的表**：上料臂放下新一批时就换一张新表，下料任务只消费
-- 自己那张 —— 否则「下料还在分选上一批、上料已经放下新一批」时批颗数会互相覆盖
-- （实测会把 N.n 抹成 0，下一批直接报「料座账上没有料」）。
local NEST = {
    F = { state = 0, batch = nil, tFree = 0 },
    B = { state = 0, batch = nil, tFree = 0 },
}

-- 分选落料点（真表：Auto1/2/3 三座收料塔 + NG料仓1/2，共 5 个）
local BIN_N      = 5
local AUTO_TOP   = 500
local AUTO_LAYER = 20
local AUTO_X     = { 700, 755, 810 }
local NG_X       = { 850, 885 }

-- 2×4 吸嘴组的变距：Tray 上的列距 ≠ 测试座列距，取料后必须收拢
local PITCH_TRAY   = 0
local PITCH_SITE   = -12.5
local PITCH_SITE_Y = -8.0

local FEED_Z_WORK = 500      -- 进料仓取料高度：顶到顶（顶层 Tray 在取料面）
local FEED_Z_CHG  = 475      -- 换盘高度：必须 ≥ 抬起阈值 470，否则 进料仓Y轴 被拒

-- ============================================================================
--  协作式调度器
--
--  为什么需要：真机 上料臂 / 下料臂 / 前压头 / 后压头 / 前后梭车 是**独立硬件**，
--  同时都在动。Lua 单线程里模拟「同时」，靠协程把「等轴到位」变成让出而不是阻塞：
--    · 任务里 sleep(ms) = coroutine.yield(ms)，不占时间；
--    · 真正推进时间只发生在调度器的 drive() → pause(ms)，
--      所以「脚本时间 == 真机时间」，UPH 才有意义；
--    · 仍然是一条流程一条线程一个 Lua 状态，不跨流程，不引入竞态。
-- ============================================================================
local NOW       = 0        -- 调度器时间（ms）
local RUN_MS    = 0        -- 累计运行时间（算 UPH / MTBA 用）
local IN_CYCLE  = false    -- 生产计时开关
local IN_TASK   = false    -- 在任务协程里？（决定 sleep 是让出还是直接 pause）
local TASKS     = {}
local FAULT     = false    -- 任一任务出错 = 锁存停机
local HOLD      = false    -- 首件确认 / 清针等人为暂停（全流水线一起停）
local STOPPING  = false

local SEG = { load = 0, contact = 0, test = 0, unload = 0, tray = 0, wait = 0 }

local function drive(ms)
    ms = math.floor(tonumber(ms) or 1)
    if ms < 1 then ms = 1 end
    pause(ms)
    NOW = NOW + ms
    if IN_CYCLE then RUN_MS = RUN_MS + ms end
end

local function sleep(ms)
    ms = math.floor(tonumber(ms) or 0)
    if ms < 0 then ms = 0 end
    if IN_TASK then
        coroutine.yield(ms)
    else
        drive(ms)
    end
end

local function seg_add(k, t0)
    local d = RUN_MS - t0
    -- 新分类第一次出现时 SEG[k] 还是 nil，必须兜底（否则整段以「算术 nil」中止）
    if d > 0 then SEG[k] = (SEG[k] or 0) + d end
end

local function alive()
    return running() and not FAULT
end

local function spawn(name, fn)
    TASKS[#TASKS + 1] = { name = name, co = coroutine.create(fn), wake = NOW, dead = false }
end

-- 调度一圈：把到点的任务各推进一次，然后按最近唤醒时间推进时钟。
local function sched_run()
    while alive() do
        for i = #TASKS, 1, -1 do
            local tk = TASKS[i]
            if tk.dead then
                table.remove(TASKS, i)
            elseif tk.wake <= NOW then
                IN_TASK = true
                local ok, w = coroutine.resume(tk.co)
                IN_TASK = false
                if not ok then
                    alarm("任务「" .. tk.name .. "」异常中止：" .. tostring(w))
                    tk.dead = true
                    FAULT = true
                elseif coroutine.status(tk.co) == "dead" then
                    tk.dead = true
                else
                    tk.wake = NOW + (tonumber(w) or 0)
                end
            end
        end
        local near
        for _, tk in ipairs(TASKS) do
            if not tk.dead and (near == nil or tk.wake < near) then near = tk.wake end
        end
        if near == nil then return end
        local d = near - NOW
        if d < 1 then d = 1 end
        if d > 50 then d = 50 end     -- 最多 50ms 一跳，贴近界面 50ms 的刷新节拍
        drive(d)
    end
end

-- ============================================================================
--  基本动作
-- ============================================================================

-- 点位一律走 point("名字")，现场改点位表即可，脚本不写死数字
local function pt(name, def)
    local v = point(name)
    if v == 0 then return def end
    return v
end

-- 一盘能放几颗（14×35 Tray = 490，见文件头 TRAY_DEF 的出处）
-- 下限锁到一批：容量比一批还小就没有「放得下」的时刻，会一直换盘。
local function tray_cap()
    local v = math.floor(pt("料盘.容量", TRAY_DEF))
    if v < BATCH then v = BATCH end
    return v
end

local function outp(name, on)
    if not writeout(name, on) then
        alarm("输出点不存在：" .. name)
        return false
    end
    return true
end

-- 走一根轴并等它停稳。true = 到位；false = 轴表没有 / 被安全层拒 / 真卡住
-- 等待判据是「**还在动就续期，停住不动才算失败**」，不是死超时。
local STALL_MS  = 1000
local STALL_EPS = 1e-5
local WAIT_MAX  = 30000

local function go(axis, v, ms)
    if ax(axis) < 0 then
        alarm("轴表里没有 " .. axis .. "，动作被跳过（检查 HandoverTables）")
        return false
    end
    -- 已经在目标位就直接算到位：省掉起步等待 + 至少一轮轮询
    if inpos(axis) and math.abs(pos(axis) - v) <= 0.01 then return true end
    if not moveabs(axis, v) then
        alarm(axis .. " 被安全层拒绝：" .. lasterr())
        return false
    end
    sleep(30)
    local limit = ms or WAIT_MAX
    local t, stall = 0, 0
    local last = pos(axis)
    -- 轮询粒度 10ms：伺服位置更新周期就是 4~10ms，用 20ms 轮询每段动作平均白丢 10ms，
    -- 一批要过 4~6 段轴动作，累积 0.05~0.08s 的纯测量误差（不是机构时间）。
    while running() do
        if inpos(axis) then return true end
        local p = pos(axis)
        if math.abs(p - last) < STALL_EPS then
            stall = stall + 10
            if stall >= STALL_MS then
                alarm(axis .. " 停在 " .. string.format("%.2f", p) .. "（目标 "
                      .. string.format("%.2f", v) .. "）超过 " .. STALL_MS
                      .. "ms 没动，判定卡住（仿真是否停摆？）")
                return false
            end
        else
            stall = 0
            last = p
        end
        sleep(10)
        t = t + 10
        if t >= limit then break end
    end
    if not running() then return false end
    if inpos(axis) then return true end
    alarm(axis .. " 等 " .. limit .. "ms 仍未到位（当前 " .. string.format("%.2f", pos(axis))
          .. "，目标 " .. string.format("%.2f", v) .. "）")
    return false
end

-- 必须成功：失败就抛错，由 stage() 捕获后中止本圈
local function need(axis, v, ms)
    if not go(axis, v, ms) then
        error(axis .. " → " .. v .. " 未完成", 0)
    end
end

-- 吸嘴组并行下发：8 个同时走，再统一等到位
local function nozzles(noz, z, tag)
    tag = tag or "吸嘴"
    local any = false
    for i = 1, #noz do
        if ax(noz[i]) >= 0
           and not (inpos(noz[i]) and math.abs(pos(noz[i]) - z) <= 0.01) then
            any = true
            break
        end
    end
    if not any then return true end
    local n = 0
    for i = 1, #noz do
        if ax(noz[i]) >= 0 then
            if moveabs(noz[i], z) then
                n = n + 1
            else
                alarm(noz[i] .. " 被安全层拒绝：" .. lasterr())
            end
        end
    end
    if n == 0 then error(tag .. " 没有一个下发出去", 0) end
    sleep(30)
    local t, stall = 0, 0
    local last = {}
    for i = 1, #noz do last[i] = (ax(noz[i]) >= 0) and pos(noz[i]) or 0 end
    while running() do
        local done, moved = true, false
        for i = 1, #noz do
            if ax(noz[i]) >= 0 then
                if not inpos(noz[i]) then
                    done = false
                    local p = pos(noz[i])
                    if math.abs(p - last[i]) >= STALL_EPS then
                        moved = true
                        last[i] = p
                    end
                end
            end
        end
        if done then return true end
        if moved then stall = 0 else stall = stall + 20 end
        if stall >= STALL_MS then
            error(tag .. " 全部停住不动，判定卡住（仿真是否停摆？）", 0)
        end
        sleep(20)
        t = t + 20
        if t >= WAIT_MAX then break end
    end
    if not running() then return false end
    error(tag .. " 等 " .. WAIT_MAX .. "ms 未全部到位", 0)
end

-- ============================================================================
--  真空：吸取 / 释放 / 确认
-- ============================================================================
local function idxs(x)
    if type(x) == "table" then return x end
    local t = {}
    for i = 1, (tonumber(x) or 0) do t[i] = i end
    return t
end

-- 开/关真空。吸与破互锁（真机是同一个阀岛，两路不能同时给）
local function vac_set(side, on, which)
    local s = SIDE[side]
    local l = idxs(which)
    for k = 1, #l do
        local i = l[k]
        outp(s.vac .. i, on)
        outp(s.blow .. i, not on)
    end
end

local function vac_clear(side)
    local s = SIDE[side]
    for i = 1, 8 do
        outp(s.blow .. i, false)
        outp(s.vac .. i, false)
    end
end

local function vac_miss(side, which)
    local s = SIDE[side]
    local l = idxs(which)
    local miss = 0
    for k = 1, #l do
        if not readin(s.sen .. l[k]) then miss = miss + 1 end
    end
    return miss
end

-- 真空建立/释放：真空开关（SMC ZZB08）从给阀到稳定典型 80~150ms。
-- 【需求 73】真空最短等待 = 真空发生器本身的建立时间下限（小吸嘴腔体 40ms）。
-- 上一版是死等 120 / 90ms 再读开关 —— 一批要等 4 次（上料吸/放 + 下料吸/放），
-- 白丢 0.26s 节拍。真机的做法是**给阀后轮询真空开关**，开关一到就走，
-- 超时（默认 300ms）才判漏吸/没释放。这才是 ZZB08 这类开关的用法。
local VAC_MIN = 40
-- 只在「漏吸后关真空重吸」这条异常路径上用（正常路径不等待）
local VAC_BLOW = 90

-- 轮询真空开关：want = true 等「全部吸住」，false 等「全部释放」。
-- 返回 0 = 达到目标；>0 = 超时后仍不满足的路数
local function vac_poll(side, which, want, timeout)
    local s = SIDE[side]
    local l = idxs(which)
    local t = 0
    while running() do
        local bad = 0
        for k = 1, #l do
            local on = readin(s.sen .. l[k]) and true or false
            if on ~= want then bad = bad + 1 end
        end
        if bad == 0 then return 0 end
        if t >= timeout then return bad end
        sleep(10)
        t = t + 10
    end
    return #l
end

-- 吸取并确认：给真空 → 等建立 → 逐路读开关 → 有漏吸就重吸一次 → 还漏就停机
local function vac_pick(side, n, tag)
    for pass = 1, 2 do
        vac_set(side, true, n)
        sleep(pt("真空.最短等待", VAC_MIN))
        local miss = vac_poll(side, n, true, pt("真空.建立超时", 300))
        if miss == 0 then
            boxinc("分选.真空确认次数", 1)
            return true
        end
        alarm(tag .. " 第 " .. pass .. " 次吸取漏吸 " .. miss .. " 路（应有 " .. n
              .. " 路），关真空重吸")
        boxinc("分选.漏吸次数", 1)
        vac_set(side, false, n)
        sleep(pt("真空.释放时间", VAC_BLOW))
    end
    error(tag .. " 连续两次漏吸，已中止（查吸嘴堵塞 / 盘上缺料 / 真空压力）", 0)
end

-- 释放并确认破真空（真空开关全灭才说明料真的放下了）
-- 同样走轮询：破真空比建立快，开关全灭立即走，超时才判「料还挂着」
local function vac_release(side, which, tag)
    vac_set(side, false, which)
    sleep(pt("真空.最短等待", VAC_MIN))
    local stuck = vac_poll(side, which, false, pt("真空.释放超时", 300))
    if stuck > 0 then
        alarm(tag .. " 破真空后仍有 " .. stuck .. " 路真空未释放（料可能还挂在吸嘴上）")
        boxinc("分选.掉料次数", 1)
    end
    return stuck == 0
end

-- ============================================================================
--  可选轴（变距 / 旋转U / 仓类升降）：轴表里没有就跳过，不报错、不中止
-- ============================================================================
-- ⚠ 停机后一律不下发：nozzles() 在 StopDemo 打断时会 return false 而**不抛错**，
--   后面这些并行下发照跑的话，吸嘴还停在中间，安全层会以「同组垂直轴未抬起」拒掉
--   一串运动（慢速场景实测 4 次），被拒记录会污染「无拒绝」判据。
local function gmove(axis, v)
    if not running() then return false end
    if ax(axis) < 0 then return false end
    if not moveabs(axis, v) then
        alarm(axis .. " 被安全层拒绝：" .. lasterr())
        return false
    end
    return true
end

local function gneed(axis, v)
    if ax(axis) < 0 then return end
    need(axis, v)
end

-- 整组转向角：0 / 90 / 180 / -90（轴软限位是 -180..180，270° 必须写 -90）
local ROT_STEPS = { 0, 90, 180, -90 }
local function rot_angle()
    local k = math.floor(boxget("分选.旋转档位")) % 4 + 1
    return ROT_STEPS[k]
end

-- 压头真空：吸住 / 释放 / 确认（前 / 后压头各一套）
local function press_vac(laneKey, on, n)
    local p = PVAC[laneKey]
    for i = 1, n do
        outp(p.vac .. i, on)
        outp(p.blow .. i, not on)
    end
end

local function press_miss(laneKey, n)
    local p = PVAC[laneKey]
    local miss = 0
    for i = 1, n do
        if not readin(p.sen .. i) then miss = miss + 1 end
    end
    return miss
end

-- 视觉定位（飞拍）：取料后、平移中拍一次，把纠偏量写进变量
local function vision_align(tag)
    step("视觉定位 · " .. tag)
    sleep(pt("视觉.定位时间", 260))
    boxset("视觉.纠偏X", (math.random() - 0.5) * 0.02)   -- ±10μm
    boxset("视觉.纠偏Y", (math.random() - 0.5) * 0.02)
    boxinc("视觉.拍照次数", 1)
end

-- ============================================================================
--  ARM_X 令牌 + 任务内互斥
--
--  take/give 是**按 owner 号**判定的（同一 owner 重复 take 永远成功），所以
--  同一条流程里的两个任务（上料 / 换盘）不能只靠令牌区分 —— 实测会出现
--  「上料任务在换料盘、换盘任务同时在动搬运臂」⇒ 上料X 被走廊规则拒。
--  这里加一层任务级的持有者标记：一个任务拿着时，另一个任务必须等。
-- ============================================================================
local ARMX_OWNER = nil

local function armx_take(who)
    if ARMX_OWNER ~= nil then return ARMX_OWNER == who end
    if not take("ARM_X", OWNER) then return false end
    ARMX_OWNER = who
    return true
end

local function armx_give(who)
    if ARMX_OWNER == who then
        ARMX_OWNER = nil
        give("ARM_X", OWNER)
    end
end

-- ============================================================================
--  料况三本账
-- ============================================================================
local function ensure_state()
    local cap = tray_cap()
    -- 容量变了（比如存档里还是旧的 120、现场改成 14×35 的 490）就按新容量重设三本账，
    -- 否则「仓余位 = 120」会一直卡在换盘阈值上，跑出来的 UPH 全是耗材动作。
    if boxget("料盘.已初始化") == 1 and math.floor(boxget("料盘.容量版本")) == cap then return end
    boxset("料盘.上料余料", cap)
    boxset("料盘.空盘库存", MAG_CAP)
    for i = 1, BIN_N do boxset("分选.仓余位" .. i, cap) end
    for i = 1, 3 do boxset("分选.Auto层" .. i, 0) end
    boxset("分选.梭车Index秒", 0)
    boxset("分选.梭车Index峰值秒", 0)
    boxset("料盘.容量版本", cap)
    boxset("料盘.已初始化", 1)
    step("料况初始化：上料盘 " .. cap .. " 颗 / 空盘 " .. MAG_CAP
         .. " 张 / 3 座收料塔 + 2 个 NG 仓")
end

-- 一段 = 一次 pcall：段内任一 need 失败就把机构停在安全位并锁存停机
local function stage(name, fn)
    local ok, msg = pcall(fn)
    if ok then return true end
    if not running() then return false end         -- 正常停机，不算故障
    alarm(name .. " 中止：" .. tostring(msg))
    FAULT = true
    return false
end

-- 失败收尾 = 安全复位：吸嘴抬起 → Y 抬起 → X 回停放位
-- 顺序不能反：安全层规定「同组垂直轴没抬起就禁止水平轴动」
local function recover(noz, yAxis, yUp, xAxis, xHome, why)
    local ok, err = pcall(function()
        nozzles(noz, pt("吸嘴.Z上", Z_UP), "收尾抬吸嘴")
        if ax(yAxis) >= 0 then need(yAxis, yUp) end
        if ax(xAxis) >= 0 then need(xAxis, xHome) end
    end)
    boxinc("分选.异常次数", 1)
    if ok then
        alarm("安全复位完成，机构已回停放位（" .. why .. "）")
    else
        alarm("安全复位未完成：" .. tostring(err) .. "（" .. why .. "），请人工确认")
    end
end

-- ============================================================================
--  换盘
-- ============================================================================

-- 上料盘空了 → 进料仓换盘（降堆 · 出空盘 · 进新盘）
local function change_feed_tray()
    step("上料盘取空 → 进料仓换盘（降堆 · 出空盘 · 进新盘）")
    local t0 = RUN_MS
    boxinc("料盘.上料换盘次数", 1)
    gneed("进料仓Z轴", pt("进料仓.换盘Z", FEED_Z_CHG))
    gneed("进料仓Y轴", pt("进料仓.换盘Y", 200))
    sleep(pt("料盘.换盘时间", 1200))
    gneed("进料仓Y轴", pt("进料仓.取料Y", 0))
    gneed("进料仓Z轴", pt("进料仓.取料Z", FEED_Z_WORK))
    sleep(pt("料盘.扫码时间", 200))
    boxinc("料盘.上料Tray号", 1)
    boxset("料盘.上料余料", tray_cap())
    boxinc("料盘.进料层", 1)
    seg_add("tray", t0)
end

local function cyl(onName, offName)
    outp(onName, true)
    outp(offName, false)
end

-- Auto n 塔顶升高度：每满一盘降一层，顶层 Tray 始终停在落料高度
local function auto_z(bin)
    local layer = math.floor(boxget("分选.Auto层" .. bin))
    local z = AUTO_TOP - layer * pt("料盘.层高", AUTO_LAYER)
    if z < 0 then z = 0 end
    return z
end

-- 收料塔满盘 → 搬运臂从「进空盘」抓一张空盘放上去
-- ⚠ 搬运X轴 与 上料X轴 同属分支2 走廊，只有 上料X 回停放位时才允许动
local function refill_tower(bin)
    local pick = pt("料盘.进空盘X", 361)
    local drop = pt("料盘.收料" .. bin .. "X", ({ 19, 129, 239 })[bin])
    local grab = pt("料盘.夹爪时间", 180)
    local lift = pt("料盘.定位时间", 220)

    step("自动盘" .. bin .. " 满盘 → 搬运臂从进空盘补一张空盘")
    boxinc("料盘.收料换盘次数", 1)
    boxinc("料盘.收料Tray号", 1)
    sleep(pt("料盘.扫码时间", 200))

    need("搬运X轴", pick)
    cyl("料盘搬运上下下", "料盘搬运上下上")
    sleep(lift)
    cyl("料盘搬运夹爪夹", "料盘搬运夹爪松")
    sleep(grab)
    cyl("料盘搬运上下上", "料盘搬运上下下")
    sleep(lift)

    need("搬运X轴", drop)
    cyl("料盘搬运上下下", "料盘搬运上下上")
    sleep(lift)
    cyl("料盘搬运夹爪松", "料盘搬运夹爪夹")
    sleep(grab)
    cyl("料盘搬运上下上", "料盘搬运上下下")
    sleep(lift)

    need("搬运X轴", pt("料盘.停放X", 442.3))
end

-- 这一仓还放得下一批吗？放不下就换盘。返回 true = 可以放
local function ensure_bin_space(bin)
    local left = boxget("分选.仓余位" .. bin)
    if left >= BATCH then return true end
    if bin <= 3 then
        if math.floor(boxget("分选.Auto层" .. bin)) >= TOWER_MAX then
            alarm("自动盘" .. bin .. " 收料塔已满 " .. TOWER_MAX .. " 层，请取走整塔后再继续")
            boxinc("分选.塔满报警", 1)
            boxinc("分选.异常次数", 1)
            return false
        end
        if boxget("料盘.空盘库存") <= 0 then
            alarm("进空盘仓没有空盘了，自动盘" .. bin .. " 无法换盘 —— 请补空盘后继续")
            boxinc("分选.异常次数", 1)
            return false
        end
        refill_tower(bin)
        boxinc("料盘.空盘库存", -1)
        boxinc("分选.Auto层" .. bin, 1)
        gneed("Auto" .. bin .. "Z轴", auto_z(bin))
    else
        step("NG 仓" .. (bin - 3) .. " 满盘 → 换盘")
        boxinc("料盘.NG换盘次数", 1)
        sleep(pt("料盘.换盘时间", 1200))
    end
    boxset("分选.仓余位" .. bin, tray_cap())
    return true
end

-- ============================================================================
--  测试判定
-- ============================================================================
-- 结果消费：**逐 site**。分选.结果序号 电测每测完一批 +1；
-- 分选.结果1..8 各 site 结果（1 = OK，0 = NG）。
-- ⚠ 序号没变 = 没有新结果（仿真 / 手动模式）→ 退回按良率随机。
local lastSeq = { F = 0, B = 0 }

local function site_results(laneKey, n)
    local seq = boxget("分选.结果序号")
    local fresh = (seq ~= lastSeq[laneKey])
    if fresh then lastSeq[laneKey] = seq end
    local r = {}
    if fresh then
        local allZero = true
        for i = 1, n do
            r[i] = (boxget("分选.结果" .. i) == 1) and 1 or 0
            if r[i] == 1 then allZero = false end
        end
        if allZero and boxget("分选.结果") == 1 then
            for i = 1, n do r[i] = 1 end
        end
        return r
    end
    local y = boxget("分选.良率")
    if y <= 0 or y > 1 then y = 0.97 end
    for i = 1, n do
        r[i] = (math.random() < y) and 1 or 0
    end
    return r
end

-- 单颗结果 → 落料点编号（1..3 = Auto1..3 收料塔，4..5 = NG料仓1/2）
local function bin_index(ok)
    local n
    if ok == 1 then
        n = math.floor(boxget("分选.良品等级"))
        if n < 1 or n > 3 then n = 1 end
        return n
    end
    n = math.floor(boxget("分选.不良类型"))
    if n < 1 then n = 1 end
    if n > 2 then
        boxinc("分选.超类不良", 1)
        n = 2
    end
    return 3 + n
end

local function bin_x(bin)
    if bin <= 3 then return pt("分选.Auto" .. bin .. "X", AUTO_X[bin]) end
    return pt("分选.NG" .. (bin - 3) .. "X", NG_X[bin - 3])
end

-- ============================================================================
--  需求 69：测量与维护环节（重测 / 温控 / 清针 / 掉料复核）
-- ============================================================================
local function temp_gate()
    if pt("分选.温控启用", 0) ~= 1 then return end
    local set = pt("分选.设定温度", 90)
    local tol = pt("分选.温度允差", 1.0)
    if tol <= 0 then tol = 1.0 end
    local t0 = RUN_MS
    local saidAt = -10000
    while running() do
        local cur = boxget("加热盘.温度")
        if math.abs(cur - set) <= tol then
            boxset("分选.测试座就绪", 1)
            return
        end
        if RUN_MS - saidAt >= 3000 then
            saidAt = RUN_MS
            step(string.format("等温控：测试座 %.1f℃ → 设定 %.1f℃（允差 ±%.1f）",
                               cur, set, tol))
        end
        local tw = RUN_MS
        sleep(200)
        SEG.wait = SEG.wait + (RUN_MS - tw)
        if RUN_MS - t0 >= pt("分选.等温超时", 120000) then
            error(string.format("测试座温度 %.1f℃ 未到设定 %.1f±%.1f℃，禁止开测", cur, set, tol), 0)
        end
    end
end

local lastCleanSlot = 0
local function contact_service(cnt)
    local every = pt("分选.清针间隔", 0)
    if every <= 0 then return end
    local slot = math.floor(cnt / every)
    if slot <= lastCleanSlot then return end
    lastCleanSlot = slot
    boxset("分选.需清针", 1)
    boxinc("分选.清针次数", 1)
    step("接触次数 " .. cnt .. " 到清针周期（每 " .. every .. " 次）—— 清针后置 分选.清针完成 = 1")
    HOLD = true
    local t0 = RUN_MS
    while running() and boxget("分选.清针完成") ~= 1 do
        sleep(200)
        if RUN_MS - t0 >= pt("分选.清针超时", 600000) then
            HOLD = false
            error("等清针确认超时，请人工清针后置 分选.清针完成 = 1", 0)
        end
    end
    boxset("分选.需清针", 0)
    boxset("分选.清针完成", 0)
    sleep(pt("清针.时间", 800))
    boxset("分选.上次清针接触", cnt)
    HOLD = false
    step("清针完成，继续生产")
end

local function socket_life(n)
    local life = pt("分选.socket寿命", 0)
    if life <= 0 then return end
    for i = 1, n do
        local c = boxget("分选.site" .. i .. "接触")
        if c >= life then
            error("site" .. i .. " 探针已接触 " .. c .. " 次（寿命 " .. life .. "），换针后再生产", 0)
        end
    end
end

-- 一次完整的压测（吸持 → 下压 → 接触 → 保压 → 抬起 → 取结果）
-- 重测就是把它再跑一遍，不重新搬运（真机的 retest 也是原地再压）
local function test_once(laneKey, n)
    local L = LANE[laneKey]
    -- 真空建立（~100ms）与 Z 下压（20mm ≈ 0.17s）**并行**。
    -- 真机的压头是「一边下压一边建立真空」，先等满 100ms 再动 Z 完全是白丢的；
    -- test time 1.5s 是资料硬指标不能动，能压的只有这类机构开销。
    -- 真空确认放在 Z 走到一半时读（此时还未压到位，吸不住就立刻停，不会压坏芯片）。
    step(L.name .. "压头下压 · 真空吸持确认（" .. n .. " 路）")
    local tC = RUN_MS
    press_vac(laneKey, true, n)
    gmove(L.press, pt("压头.Z压合", 5))
    sleep(pt("压头.真空建立", 100))
    local missP = press_miss(laneKey, n)
    if missP > 0 then
        press_vac(laneKey, false, n)
        gneed(L.press, pt("压头.Z抬起", 25))
        error(L.name .. "压头真空只吸住 " .. (n - missP) .. "/" .. n
              .. " 路（芯片没在测试座中心），禁止下压", 0)
    end
    boxinc("分选.压头真空确认次数", 1)
    gneed(L.press, pt("压头.Z压合", 5))
    seg_add("contact", tC)                        -- 接触建立 = max(真空, Z 下压)
    sleep(pt("压头.接触时间", 50))                -- 到位后稳定（探针落座 30~50ms）
    local tT = RUN_MS
    sleep(pt("压头.测试时间", 1500))              -- 保压电测（资料 test time=1.5s）
    seg_add("test", tT)
    step(L.name .. "压头抬起 · 破真空释放")
    -- 破真空与 Z 抬起并行：真机抬起的同时破真空，芯片落回料座，不用等
    press_vac(laneKey, false, n)
    gmove(L.press, pt("压头.Z抬起", 25))
    gneed(L.press, pt("压头.Z抬起", 25))

    local cnt = boxinc("分选.接触次数", n)
    for i = 1, n do boxinc("分选.site" .. i .. "接触", 1) end
    socket_life(n)
    return site_results(laneKey, n), cnt
end

local function measure(laneKey, n)
    temp_gate()
    local res, cnt = test_once(laneKey, n)
    contact_service(cnt)

    local rtMax = math.floor(pt("分选.重测次数", 1))
    local rtCap = math.floor(pt("分选.重测上限颗数", 4))
    local rt = 0
    while running() and rt < rtMax do
        local ng = 0
        for i = 1, n do if res[i] == 0 then ng = ng + 1 end end
        if ng == 0 then break end
        if ng > rtCap then
            step("本批 " .. ng .. " 颗 NG（超过重测上限 " .. rtCap .. "）—— 不重测，直接落 NG 仓")
            break
        end
        rt = rt + 1
        boxinc("分选.重测次数实际", 1)
        step("重测 " .. rt .. "/" .. rtMax .. "：上一轮 " .. ng .. " 颗 NG，就地再压一次")
        local r2, c2 = test_once(laneKey, n)
        contact_service(c2)
        for i = 1, n do
            if res[i] == 0 and r2[i] == 1 then
                res[i] = 1
                boxinc("分选.重测挽回", 1)
            end
        end
    end
    local ngLeft = 0
    for i = 1, n do if res[i] == 0 then ngLeft = ngLeft + 1 end end
    boxset("分选.末批NG颗数", ngLeft)
    return res
end

-- ============================================================================
--  流水线任务 1：上料（取料 → 视觉 → 放到空料座）
-- ============================================================================

-- 找一个空料座：**FIFO**（谁先空出来就先用谁）。
-- 原来按固定顺序 F 优先，会相位锁定 —— F 老是被优先补料，B 只能等 F 占满，
-- 两条线越跑越同相，两个料座同时被占 ⇒ 上料臂空转（实测每批白等 0.52s）。
local function free_nest()
    local pick, best
    for _, k in ipairs(LANES) do
        if NEST[k].state == 0 then
            local t = NEST[k].tFree or 0
            if pick == nil or t < best then pick, best = k, t end
        end
    end
    return pick
end

local function load_batch(laneKey)
    local L = LANE[laneKey]
    local N = NEST[laneKey]

    -- 料况：上料盘空了先换盘（进料仓 Z 降堆 → Y 分度 → Z 升回）
    local remain = math.floor(boxget("料盘.上料余料"))
    if remain < 1 then
        change_feed_tray()
        remain = math.floor(boxget("料盘.上料余料"))
    end
    local n = remain < BATCH and remain or BATCH
    if n <= 0 then error("上料盘空且换盘失败", 0) end
    local batch = { n = n, res = {}, lane = laneKey }
    boxset("分选.批颗数", n)

    local t0 = RUN_MS
    step("上料臂 → 进料盘取料（" .. n .. " 颗，送" .. L.name .. "料座）")

    -- 取料：八嘴整批，2×4 吸嘴组一次覆盖两行，Y 全程停在工作位
    need("上料X轴", pt("上料.取料X", 200))
    -- 变距要回到 Tray pitch 才能对准盘上的格子；**在压吸嘴前这一瞬才等**，
    -- 该动作上一批放完料就已经下发了，一路与平移重叠，不额外占节拍。
    gneed("上料变距X", pt("变距.料盘X", PITCH_TRAY))
    gneed("上料变距Y", pt("变距.料盘Y", PITCH_TRAY))
    nozzles(NOZ_L, pt("吸嘴.Z下", Z_DOWN), "上料吸嘴下压")
    vac_pick("L", n, "上料臂")
    nozzles(NOZ_L, pt("吸嘴.Z上", Z_UP), "上料吸嘴抬起")
    boxset("料盘.上料余料", remain - n)

    -- 变距 + 整组转向（与 X 平移并行下发，不额外占节拍）
    gmove("上料变距X", pt("变距.测试位X", PITCH_SITE))
    gmove("上料变距Y", pt("变距.测试位Y", PITCH_SITE_Y))
    gmove("上料旋转U轴", rot_angle())

    -- 平移到料座（途中飞拍定位：真机相机做 on-the-fly，不停下来拍）
    local nx = pt("上料.料座" .. laneKey .. "X", L.nestX)
    if not running() then return end     -- 停机打断：吸嘴还悬在半空，不再下发水平轴
    if not moveabs("上料X轴", nx) then
        error("上料X轴 被安全层拒绝：" .. lasterr(), 0)
    end
    vision_align("上料 → " .. L.name .. "料座")
    need("上料X轴", nx)
    -- 变距 / 旋转 都是「上料臂」这一组的水平轴：没停稳就下压吸嘴会被安全层拒
    gneed("上料变距X", pt("变距.测试位X", PITCH_SITE))
    gneed("上料变距Y", pt("变距.测试位Y", PITCH_SITE_Y))
    gneed("上料旋转U轴", rot_angle())

    nozzles(NOZ_L, pt("吸嘴.Z下", Z_DOWN), "上料吸嘴下压")
    boxset("分选.批颗数", n)
    vac_release("L", n, "上料臂")
    nozzles(NOZ_L, pt("吸嘴.Z上", Z_UP), "上料吸嘴抬起")

    -- 回程把变距放回 Tray pitch（下一批取料要用）：**只下发、不在这里等** ——
    -- 等的时机挪到下一批压吸嘴之前（见函数开头），这段时间正好被平移吃掉。
    gmove("上料变距X", pt("变距.料盘X", PITCH_TRAY))
    gmove("上料变距Y", pt("变距.料盘Y", PITCH_TRAY))

    seg_add("load", t0)
    N.batch = batch        -- 交给压头任务
    N.state = 1
    N.tFill = RUN_MS       -- 诊断：料座被填满的时刻（从这里开始算「料座占用」）
    boxinc("分选.上料批数", 1)

    -- 【需求 72】盘取空了就在**交付完的这一瞬**补盘，而不是等下一批开头再补。
    -- 差别是「和谁重叠」：此刻这批已经在压头上、压测/下料会自己往下走，换盘那
    -- 4~5s 被流水线吸收；放到下一批开头换，两条料座都已经放空，恢复出料还要
    -- 再等一个上料周期（实测每圈白丢 4.8s，长跑 UPH 少 1.4K）。
    if math.floor(boxget("料盘.上料余料")) < 1 then
        change_feed_tray()
    end
end

local function task_loader()
    while alive() do
        if HOLD then sleep(50)
        else
            local laneKey = free_nest()
            if not laneKey then
                -- 上料臂空转 = 两条料座都被占着（压测/下料还没腾出位置）。
                -- 这段是真正的「等料座」，单独记账，否则会算进上料段节拍里看不出瓶颈
                local tw = RUN_MS
                sleep(20)
                seg_add("wait", tw)
            elseif not armx_take("load") then
                -- 别的流程（01_上料XY 这类）或换盘任务正在用上料臂：让行，不报警
                sleep(30)
            else
                local ok = stage("上料段", function() load_batch(laneKey) end)
                armx_give("load")
                if not ok then
                    recover(NOZ_L, "上料Y轴", pt("上料.抬起Y", 0),
                            "上料X轴", pt("上料.停放X", 0), "上料段")
                    break
                end
            end
        end
    end
end

-- ============================================================================
--  流水线任务 2/3：压测（前 / 后各一条，可同时在压）
-- ============================================================================
local function task_press(laneKey)
    local L = LANE[laneKey]
    local N = NEST[laneKey]
    while alive() do
        if HOLD or N.state ~= 1 then
            sleep(20)
        else
            local ok = stage(L.name .. "压头段", function()
                local batch = N.batch
                if not batch then error("料座上没有待测批次", 0) end
                -- 梭车送料：前梭车 300 → 500，后梭车 900 → 700
                step(L.name .. "梭车 → 测试位 " .. L.test .. "mm")
                -- 资料口径 Index Time（≤0.6s）= 梭车把一个料座从一个位号送到下一个
                -- 位号的净走位时间；这是「平移式」机台最核心的节拍指标，单独量出来
                local tFill0 = N.tFill
                local t0 = RUN_MS
                need(L.shuttle, L.test)
                seg_add("pin", tFill0)          -- 填满 → 梭车到测试位（含等调度）
                local t1 = RUN_MS
                local iSec = (t1 - t0) / 1000
                boxset("分选.梭车Index秒", iSec)
                if iSec > boxget("分选.梭车Index峰值秒") then
                    boxset("分选.梭车Index峰值秒", iSec)
                end

                batch.res = measure(laneKey, batch.n)
                seg_add("tst", t1)              -- 梭车到位 → 测完（保压 + 压头 Z + 真空）

                step(L.name .. "梭车 → 交接位 " .. L.hand .. "mm")
                local t2 = RUN_MS
                need(L.shuttle, L.hand)
                seg_add("pout", t2)             -- 测完 → 回交接位
                N.state = 2
                -- 诊断：料座从「填满」到「测完」被占了多久（梭车送进 + 压测 + 退回）
                if tFill0 then seg_add("press", tFill0) end
                N.tFill = nil
                N.tTest = RUN_MS
            end)
            if not ok then
                -- 芯片还在料座上：抬压头、把梭车退回交接位，等人工处理
                pcall(function()
                    gneed(L.press, pt("压头.Z抬起", 25))
                    need(L.shuttle, L.hand)
                end)
                recover(NOZ_U, "下料Y轴", pt("下料.抬起Y", 0),
                        "下料X轴", pt("下料.停放X", 900), L.name .. "压头段")
                break
            end
        end
    end
end

-- ============================================================================
--  流水线任务 4：下料 + 按 Bin 分选
-- ============================================================================
local function unload_batch(laneKey)
    local L = LANE[laneKey]
    local N = NEST[laneKey]
    local batch = N.batch
    if not batch then error("料座账上没有料", 0) end
    local n = batch.n
    if n <= 0 then error("料座账上没有料", 0) end

    local t0 = RUN_MS
    step("下料臂 → " .. L.name .. "料座取料（" .. n .. " 颗）")

    -- 去料座前先把变距收拢到测试座 pitch（与 X 平移并行下发）
    gmove("下料变距X轴", pt("变距.测试位X", PITCH_SITE))
    gmove("下料变距Y轴", pt("变距.测试位Y", PITCH_SITE_Y))
    local nx = pt("下料.料座" .. laneKey .. "X", L.nestX)
    need("下料X轴", nx)
    gneed("下料变距X轴", pt("变距.测试位X", PITCH_SITE))
    gneed("下料变距Y轴", pt("变距.测试位Y", PITCH_SITE_Y))

    nozzles(NOZ_U, pt("吸嘴.Z下", Z_DOWN), "下料吸嘴下压")
    boxset("分选.批颗数", n)
    vac_pick("U", n, "下料臂")
    nozzles(NOZ_U, pt("吸嘴.Z上", Z_UP), "下料吸嘴抬起")
    N.state = 0                       -- 料座交出：上料任务马上可以补下一批
    N.tFree = RUN_MS                  -- FIFO 排队时间戳（见 free_nest）
    -- 诊断：料座从「测完」到「下料取完」被占多久（= 等下料臂 + 取下料）
    if N.tTest then seg_add("unld", N.tTest) end
    N.tTest = nil
    if N.batch == batch then N.batch = nil end

    -- 结果在压头段就拿到了，这里只消费
    local res = batch.res
    local groups = {}
    for i = 1, n do
        local b = bin_index(res[i] or 0)
        if not groups[b] then groups[b] = {} end
        groups[b][#groups[b] + 1] = i
    end

    -- 落料：按 bin 升序逐组停靠。8 路吸嘴各自一个真空阀，只对目标 site 破真空，
    -- 其余继续吸住 —— 一批分几个 bin 就停靠几次（良率高时只停 1 次）
    local stopped, lost = 0, 0
    for b = 1, BIN_N do
        local sites = groups[b]
        if sites then
            stopped = stopped + 1
            if b <= 3 then
                step("分选：良品 → Auto" .. b .. " 收料塔（" .. #sites .. " 颗）")
            else
                step("分选：不良 → NG料仓" .. (b - 3) .. "（" .. #sites .. " 颗）")
            end
            -- 落到收料盘上是 Tray pitch：回程把变距放回原位（与 X 平移并行）
            gmove("下料变距X轴", pt("变距.料盘X", PITCH_TRAY))
            gmove("下料变距Y轴", pt("变距.料盘Y", PITCH_TRAY))
            if stopped == 1 then
                if not moveabs("下料X轴", bin_x(b)) then
                    error("下料X轴 被安全层拒绝：" .. lasterr(), 0)
                end
                sleep(pt("视觉.复检时间", 120))     -- 测试后复检：飞拍外观/引脚
                boxinc("视觉.复检次数", 1)
            end
            need("下料X轴", bin_x(b))
            gneed("下料变距X轴", pt("变距.料盘X", PITCH_TRAY))
            gneed("下料变距Y轴", pt("变距.料盘Y", PITCH_TRAY))
            if b <= 3 then gneed("Auto" .. b .. "Z轴", auto_z(b)) end

            -- 落料前复核真空：搬送途中掉了的那几颗既不能落、也不能记账
            local keep = {}
            for k = 1, #sites do
                if readin(SIDE.U.sen .. sites[k]) then
                    keep[#keep + 1] = sites[k]
                else
                    lost = lost + 1
                end
            end
            if #keep > 0 then
                nozzles(NOZ_U, pt("吸嘴.Z下", Z_DOWN), "下料吸嘴下压")
                vac_release("U", keep, "下料臂")       -- 只放这一组，且只放还在的
                nozzles(NOZ_U, pt("吸嘴.Z上", Z_UP), "下料吸嘴抬起")
            end
            groups[b] = keep
        end
    end
    boxset("分选.停靠次数", stopped)
    boxinc("分选.停靠累计", stopped)
    seg_add("unload", t0)

    if lost > 0 then
        boxinc("分选.掉料次数", lost)
        boxinc("分选.异常次数", 1)
        alarm("搬送途中掉料 " .. lost .. " 颗（落料前真空丢失），已从本批账上剔除")
    end

    -- 记账：只记真的落到盘上的
    local nOK, nNG = 0, 0
    for b = 1, BIN_N do
        local keep = groups[b]
        if keep then
            for k = 1, #keep do
                local i = keep[k]
                if res[i] == 1 then
                    nOK = nOK + 1
                    boxinc("分选.site" .. i .. "OK", 1)
                else
                    nNG = nNG + 1
                    boxinc("分选.site" .. i .. "NG", 1)
                end
            end
            if #keep > 0 then boxinc("分选.仓余位" .. b, -#keep) end
        end
    end
    if nOK > 0 then boxinc("分选.计数.OK", nOK) end
    if nNG > 0 then boxinc("分选.计数.NG", nNG) end
    return nOK, nNG, lost
end

-- 补盘：任一条收料塔余位不足一批就先换好（落料时走廊被占，绝不能等满了才换）
-- ⚠ 搬运X轴 要动，必须先把上料臂收回停放位（同一走廊）；所以这一段要拿 ARM_X 令牌
local function tray_service()
    local any = false
    for b = 1, BIN_N do
        if boxget("分选.仓余位" .. b) < BATCH then any = true end
    end
    if not any then return end                  -- 没满盘需求就立刻返回，不占走廊

    local waited = 0
    while running() do
        if armx_take("tray") then
            local ok, err = pcall(function()
                for b = 1, BIN_N do
                    if boxget("分选.仓余位" .. b) < BATCH then
                        local t0 = RUN_MS
                        need("上料X轴", pt("上料.停放X", 0))
                        if not ensure_bin_space(b) then
                            error("落料点 " .. b .. " 无法换盘，需人工处理", 0)
                        end
                        seg_add("tray", t0)
                    end
                end
            end)
            armx_give("tray")
            if not ok then error(tostring(err), 0) end
            return
        end
        sleep(30)
        waited = waited + 30
        if waited > 60000 then
            error("等上料臂让出走廊超时（换盘做不了）", 0)
        end
    end
end

-- ============================================================================
--  一圈（= 一批）完成后的数据收集
-- ============================================================================
local lastBatchAt = 0
local firstBatchAt = 0
local batchNo = 0

local function book_batch(nOK, nNG)
    batchNo = batchNo + 1
    local cyc = (lastBatchAt > 0) and (RUN_MS - lastBatchAt) or 0
    lastBatchAt = RUN_MS
    if firstBatchAt == 0 then firstBatchAt = RUN_MS end
    boxset("分选.本批节拍秒", cyc / 1000)
    if cyc > 0 then boxset("分选.瞬时节拍UPH", math.floor(3600 * (nOK + nNG) / (cyc / 1000))) end
    boxinc("分选.循环计数", 1)

    -- 连续整批全 NG = 系统性故障（探针脏 / 弹簧针老化 / 程序错 / 来料全坏）
    if nOK == 0 and (nOK + nNG) > 0 then
        local c = boxinc("分选.连续全NG批", 1)
        if c >= pt("分选.全NG报警批数", 3) then
            alarm("连续 " .. c .. " 批全部不良 —— 查测试座探针 / 测试程序 / 来料批次")
        end
    else
        boxset("分选.连续全NG批", 0)
    end

    local okN = boxget("分选.计数.OK")
    local ngN = boxget("分选.计数.NG")
    local tot = okN + ngN
    local abn = boxget("分选.异常次数")
    if tot > 0 then
        boxset("分选.良率实际", okN / tot)
        boxset("分选.JAM率", abn / (tot + abn))
        boxset("分选.MTBA秒", abn > 0 and (RUN_MS / 1000) / abn or 0)
        if RUN_MS > 1000 then
            boxset("分选.UPH", math.floor(3600 * tot / (RUN_MS / 1000)))
            -- 稳态 UPH：从第一批**产出的那一刻**起算，把开机充满两条料座的
            -- 那 4~5s（真机也一样有）排除掉 —— 样本手册上的 11K 就是稳态口径。
            local steady = RUN_MS - firstBatchAt
            if batchNo > 1 and steady > 1000 then
                boxset("分选.UPH稳态", math.floor(3600 * (tot - BATCH) / (steady / 1000)))
                -- 【需求 72】净值节拍 UPH = 稳态口径**再扣除换盘/补盘占用**。
                -- 样本手册上的 11K 就是这个口径（连续供料、不含换盘）；换盘是耗材
                -- 动作，真机也一样停机，把它算进 UPH 会得到「机器比手册慢 20%」的假象。
                local net = steady - SEG.tray
                if net > 1000 then
                    boxset("分选.UPH净节拍", math.floor(3600 * (tot - BATCH) / (net / 1000)))
                end
            end
            local busy = RUN_MS - SEG.wait
            if busy > 1000 then
                boxset("分选.UPH上限", math.floor(3600 * tot / (busy / 1000)))
            end
        end
    end
    boxset("分选.累计运行秒", RUN_MS / 1000)
    for k, v in pairs(SEG) do
        boxset("分选.段." .. k .. "秒", v / 1000)
    end
end

local first_article_gate          -- 前向声明（定义在下面，下料任务收尾时调用）

local function task_unloader()
    while alive() do
        if HOLD then
            sleep(50)
        else
            local laneKey
            for _, k in ipairs(LANES) do
                if NEST[k].state == 2 then laneKey = k; break end
            end
            if laneKey == nil then
                sleep(20)
            else
                local ok = stage("下料段", function()
                    local nOK, nNG, lost = unload_batch(laneKey)
                    book_batch(nOK or 0, nNG or 0)
                end)
                if not ok then
                    recover(NOZ_U, "下料Y轴", pt("下料.抬起Y", 0),
                            "下料X轴", pt("下料.停放X", 900), "下料段")
                    break
                end
                -- 首件：第一批跑完停下来等人工确认（开机/换批次/换程序的标准动作）
                if batchNo == 1 then first_article_gate() end
                -- 换盘 / 补盘：不挡落料节拍，放在一批收尾做
                if alive() then stage("换盘段", function() tray_service() end) end
            end
        end
    end
end

-- ============================================================================
--  首件检查（First Article）：开机 / 换批次 / 换程序后第一颗必须由人确认
-- ============================================================================
function first_article_gate()
    if boxget("分选.首件模式") ~= 1 then return end
    if boxget("分选.首件确认") == 1 then return end
    boxset("分选.状态", 3)
    step("首件已分选（Auto1 第 1 颗）—— 确认外观/电性后置 分选.首件确认 = 1")
    HOLD = true
    while running() and boxget("分选.首件确认") ~= 1 do sleep(200) end
    HOLD = false
    if running() then boxinc("分选.首件确认批", 1) end
end

-- ============================================================================
--  主循环
-- ============================================================================
local function reset_state()
    FAULT = false
    HOLD = false
    TASKS = {}
    NOW = 0
    RUN_MS = 0
    IN_CYCLE = false
    lastBatchAt = 0
    firstBatchAt = 0
    batchNo = 0
    ARMX_OWNER = nil
    for k, _ in pairs(SEG) do SEG[k] = 0 end
    for _, k in ipairs(LANES) do
        NEST[k].state = 0
        NEST[k].batch = nil
        NEST[k].tFree = 0
    end
end

local function start_line()
    reset_state()
    ensure_state()                 -- 三本账（上料盘 / 空盘仓 / 5 个落料点）
    IN_CYCLE = true
    boxset("分选.状态", 1)
    step("开工：双梭车 + 双压头流水线启动（上料 / 压测前 / 压测后 / 下料 四任务并行）")
    spawn("上料", task_loader)
    spawn("压测-前", function() task_press("F") end)
    spawn("压测-后", function() task_press("B") end)
    spawn("下料", task_unloader)
    sched_run()
    IN_CYCLE = false
    -- 停机时把还攥着的令牌交回去，否则 01/10 会一直让行
    ARMX_OWNER = nil
    give("ARM_X", OWNER)
end

boxset("分选.状态", 0)
local started = false

while running() do
    if boxget("分选.故障停") == 1 then
        if boxget("分选.启用") ~= 1 then boxset("分选.故障停", 0) end
        boxset("分选.状态", 2)
        step("已锁存停机：排除故障后把 分选.启用 置 0 再置 1 复位")
        sleep(PERIOD)
    elseif FAULT then
        -- 任务里出的错：锁存停机，等人工复位
        boxset("分选.故障停", 1)
        boxset("分选.状态", 2)
        alarm("流水线任务出错，已锁存停机（排除后把 分选.启用 置 0 再置 1 复位）")
        FAULT = false
        sleep(PERIOD)
    elseif boxget("分选.启用") == 1 and (boxget("SafeOk") == 1 or boxget("仿真.演示") == 1) then
        started = true
        start_line()
        if running() then
            -- 流水线正常结束（停止请求）或锁存停机
            if boxget("分选.故障停") == 1 then boxset("分选.状态", 2) else boxset("分选.状态", 0) end
        end
    else
        boxset("分选.状态", 0)
        step("待机：置 分选.启用 = 1 后开始平移分选")
        sleep(PERIOD)
    end
end

-- ============================================================================
--  停机收尾
--  ⚠ running() 已经是假，sleep/pause 的等待循环会立即退出，所以这里**只
--  记录状态、不假装把机构收干净**。真空保持（关掉料会掉），机构位置交给
--  操作面板的〔全部回零〕或〔停止全部轴〕处理。
-- ============================================================================
boxset("分选.状态", boxget("分选.故障停") == 1 and 2 or 0)
step("已退出：真空保持，机构请用〔全部回零〕收干净")
