-- ============================================================================
--  02_上料变距XY：上料变距工站
--  绑定：上料变距X, 上料变距Y
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 2
local PERIOD = 60

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

-- 变距：把 2×4 吸嘴组按料盘 pitch 张开 / 收拢
local function run_cycle()
    if not take("上料变距", OWNER) then
        step("让行：上料变距被占用"); pause(PERIOD); return
    end
    step("变距展开")
    go("上料变距X", pt("上料.变距X开", 40))
    go("上料变距Y", pt("上料.变距Y开", 30))
    pause(300)
    step("变距收拢")
    go("上料变距X", pt("上料.变距X合", 0))
    go("上料变距Y", pt("上料.变距Y合", 0))
    pause(300)
    give("上料变距", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 then
        run_cycle()
        boxinc("循环计数.02_上料变距XY", 1)
    else
        step("让行：未安全（SafeOk=0）")
        pause(PERIOD)
    end
end
