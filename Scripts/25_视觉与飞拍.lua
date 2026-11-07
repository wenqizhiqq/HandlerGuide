-- ============================================================================
--  25_视觉与飞拍：视觉与飞拍触发
--  绑定：上料X/前梭车/后梭车 通道
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 25
local PERIOD = 20

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

-- 飞拍：上料 X 轴走过触发位就记一次（真机是编码器比较输出，这里按位置读数模拟）
local last = 0
local function run_cycle()
    local x = pos("上料X轴")
    if x ~= x then pause(PERIOD) return end   -- NaN：轴表里没有，静默跳过
    local trig = pt("视觉.飞拍X", 400)
    if last < trig and x >= trig then
        step("飞拍触发 @ " .. string.format("%.1f", x))
        boxinc("视觉.飞拍次数", 1)
        boxset("视觉.最近X", x)
        pause(50)
    end
    last = x
    if boxinc("视觉.轮询", 1) % 500 == 0 then
        step("视觉心跳 " .. boxget("视觉.飞拍次数") .. " 次")
    end
    pause(PERIOD)
end

while running() do
    run_cycle()
end
