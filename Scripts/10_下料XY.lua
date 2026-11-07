-- ============================================================================
--  10_下料XY：下料XY 工站
--  绑定：下料X轴, 下料Y轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 10
local PERIOD = 50

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
    if not take("ARM_X", OWNER) then
        step("让行：ARM_X 被占用"); pause(PERIOD); return
    end
    step("下料臂 → 取料位")
    go("下料X轴", pt("下料.取料X", 620))
    go("下料Y轴", pt("下料.取料Y", 0))
    pause(200)
    step("下料臂 → 出料位")
    go("下料X轴", pt("下料.放料X", 880))
    go("下料Y轴", pt("下料.放料Y", 0))
    pause(200)
    step("下料臂回停放位")
    go("下料X轴", pt("下料.停放X", 900))
    give("ARM_X", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 then
        run_cycle()
        boxinc("循环计数.10_下料XY", 1)
    else
        step("让行：未安全（SafeOk=0）")
        pause(PERIOD)
    end
end
