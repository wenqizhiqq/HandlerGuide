-- ============================================================================
--  28_安全巡检：急停 / 门禁 / 气压 逐点巡检
--  绑定：急停按钮, 左门禁1, 右门禁1
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里。
--  温启志 18719361399
-- ============================================================================

local OWNER  = 28
local PERIOD = 200

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

-- 只读巡检：把安全点状态汇总进 VarBox，不写任何输出
local PTS = { "急停按钮", "左门禁1", "右门禁1", "后左门禁1", "后右门禁1" }
local function run_cycle()
    local bad = 0
    for i = 1, #PTS do
        if readin(PTS[i]) then
            bad = bad + 1
            step("安全点触发：" .. PTS[i])
        end
    end
    boxset("安全.触发点数", bad)
    if bad == 0 then step("安全巡检：全部正常") end
    pause(PERIOD)
end

while running() do
    run_cycle()
end
