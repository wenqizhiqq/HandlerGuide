-- ============================================================================
--  29_产量计数：OK / NG 计数与良率
--  绑定：VarBox 产量.OK / 产量.NG
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里。
--  温启志 18719361399
-- ============================================================================

local OWNER  = 29
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

local function run_cycle()
    local ok = boxget("产量.OK")
    local ng = boxget("产量.NG")
    local tot = ok + ng
    boxset("产量.总数", tot)
    if tot > 0 then
        boxset("产量.良率", ok * 100.0 / tot)
    end
    step(string.format("产量 %d 良率 %.2f%%", tot, boxget("产量.良率")))
    pause(PERIOD)
end

while running() do
    run_cycle()
end
