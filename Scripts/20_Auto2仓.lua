-- ============================================================================
--  20_Auto2仓：Auto2 盘位
--  绑定：Auto2Z轴, Auto2Y轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 20
local PERIOD = 80

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

-- Auto2 料仓：Z 顶升一层 → 换列 → 落回
local function run_cycle()
    if not take("Auto2", OWNER) then
        step("让行：Auto2 被占用"); pause(PERIOD); return
    end
    step("Auto2 顶升一层")
    go("Auto2Z轴", pt("Auto2.Z顶升", 100))
    pause(300)
    step("Auto2 换列")
    go("Auto2Y轴", pt("Auto2.Y下一列", 40))
    pause(200)
    go("Auto2Y轴", pt("Auto2.Y原点", 0))
    step("Auto2 落回")
    go("Auto2Z轴", pt("Auto2.Z原点", 0))
    give("Auto2", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 or boxget("仿真.演示") == 1 then
        run_cycle()
        boxinc("循环计数.20_Auto2仓", 1)
    else
        step("等待：未安全（等演示节拍或安全扫描）")
        pause(PERIOD)
    end
end
