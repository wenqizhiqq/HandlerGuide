-- ============================================================================
--  13_下料后排吸嘴：下料后排吸嘴工站
--  绑定：下料吸嘴5Z轴..8Z轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 13
local PERIOD = 40

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

local NOZ = { "下料吸嘴5Z轴", "下料吸嘴6Z轴", "下料吸嘴7Z轴", "下料吸嘴8Z轴" }
local function run_cycle()
    if not take("下料吸嘴", OWNER) then
        step("让行：下料吸嘴被占用"); pause(PERIOD); return
    end
    step("下料后排下降")
    for i = 1, 4 do go(NOZ[i], pt("吸嘴.Z下", 5)) end
    pause(150)
    step("下料后排吸真空")
    for i = 5, 8 do outp("右下料吸真空" .. i, true) end
    pause(300)
    step("下料后排抬起")
    for i = 1, 4 do go(NOZ[i], pt("吸嘴.Z上", 30)) end
    pause(150)
    step("下料后排破真空")
    for i = 5, 8 do
        outp("右下料吸真空" .. i, false)
        outp("右下料破真空" .. i, true)
    end
    pause(120)
    for i = 5, 8 do outp("右下料破真空" .. i, false) end
    give("下料吸嘴", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 or boxget("仿真.演示") == 1 then
        run_cycle()
        boxinc("循环计数.13_下料后排吸嘴", 1)
    else
        step("等待：未安全（等演示节拍或安全扫描）")
        pause(PERIOD)
    end
end
