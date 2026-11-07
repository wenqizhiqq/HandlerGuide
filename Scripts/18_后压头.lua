-- ============================================================================
--  18_后压头：后压头（测试位2）
--  绑定：后压头Z轴, 后压头Y轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 18
local PERIOD = 30

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

-- 后压头：测试位下压 → 保压 → 抬起（压到位后由 C# 侧按真实 Z 判定）
local function run_cycle()
    if not take("后压头在干涉区", OWNER) then
        step("让行：后压头在干涉区"); pause(PERIOD); return
    end
    step("后压头 → 对位")
    go("后压头Y轴", pt("后压头.Y对位", 0))
    step("后压头 下压")
    go("后压头Z轴", pt("后压头.Z压合", 5))
    pause(800)
    step("后压头 抬起")
    go("后压头Z轴", pt("后压头.Z抬起", 30))
    pause(200)
    give("后压头在干涉区", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 then
        run_cycle()
        boxinc("循环计数.18_后压头", 1)
    else
        step("让行：未安全（SafeOk=0）")
        pause(PERIOD)
    end
end
