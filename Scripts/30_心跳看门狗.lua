-- ============================================================================
--  30_心跳看门狗：给界面一个活着的信号
--  绑定：VarBox 心跳
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里。
--  温启志 18719361399
-- ============================================================================

local OWNER  = 30
local PERIOD = 500

-- 点位一律走 point("名字")，现场改点位表即可，脚本不写死数字
local function pt(name, def)
    local v = point(name)
    if v == 0 then return def end
    return v
end

-- 走一根轴并等它停稳；轴表里没有就跳过（不算错），被安全层拒绝要报出来
local function go(axis, v, ms)
    if ax(axis) < 0 then return false end
    if not moveabs(axis, v) then
        alarm(axis .. " 被安全层拒绝：" .. lasterr())
        return false
    end
    pause(40)
    local t = 0
    while running() and t < (ms or 4000) do
        if inpos(axis) then return true end
        pause(20)
        t = t + 20
    end
    return inpos(axis)
end

local function outp(name, on)
    if not writeout(name, on) then alarm("输出点不存在：" .. name) end
end

-- 心跳：界面 / 外部系统看到这个数在涨，就知道脚本线程还活着
local maxGap = 3          -- 连续 3 秒没被 Tick 就算假死（本脚本自己就是 Tick）
local lastBeat = boxget("心跳.计数")
local stuck = 0
local function run_cycle()
    local n = boxget("心跳.计数")
    if n == lastBeat then
        stuck = stuck + 1
        if stuck >= maxGap then
            alarm("心跳停了：" .. stuck .. " 拍没有推进")
            stuck = 0
        end
    else
        stuck = 0
    end
    lastBeat = n
    boxinc("心跳.计数", 1)
    step("心跳 " .. n)
    pause(PERIOD)
end

while running() do
    run_cycle()
end
