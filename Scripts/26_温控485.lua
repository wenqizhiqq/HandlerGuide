-- ============================================================================
--  26_温控485：COM1 温控 12 站轮询
--  绑定：RS485
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 26
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

-- 温控：读设定 / 当前温度，偏差超过死区就报警；加热使能由这里统一写
local function run_cycle()
    local set = boxget("温度.设定")
    local cur = boxget("温度.当前")
    if set <= 0 then set = 50 end
    if cur > 0 and math.abs(cur - set) > 5 then
        alarm(string.format("温度偏差过大：设定 %.0f / 当前 %.0f", set, cur))
    end
    outp("加热使能", cur < set)
    boxset("温度.偏差", cur - set)
    step(string.format("温控 设定 %.0f / 当前 %.0f", set, cur))
    pause(PERIOD)
end

while running() do
    run_cycle()
end
