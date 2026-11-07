-- ============================================================================
--  24_Fix3仓：Fix3 盘位（走虚轴+气缸）
--  绑定：无真实 Y/Z 轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 24
local PERIOD = 100

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

-- Fix3：固定盘位，没有伺服轴 —— 由「料盘搬运夹爪 + 顶升气缸」代走，
-- 这里只维护盘位状态并驱动气缸，位置量全部记在 VarBox 里。
local function run_cycle()
    if not take("Fix3盘位", OWNER) then
        step("让行：Fix3盘位被占用"); pause(PERIOD); return
    end
    if boxget("Fix3.层数") < 1 then
        step("Fix3 缺盘：等搬运臂送盘")
        boxset("Fix3.缺盘", 1)
        pause(500)
        give("Fix3盘位", OWNER)
        return
    end
    step("Fix3 夹爪夹紧")
    outp("料盘搬运夹爪松", false)
    outp("料盘搬运夹爪夹", true)
    pause(400)
    step("Fix3 夹爪松开")
    outp("料盘搬运夹爪夹", false)
    outp("料盘搬运夹爪松", true)
    pause(300)
    give("Fix3盘位", OWNER)
end

while running() do
    if boxget("SafeOk") == 1 or boxget("仿真.演示") == 1 then
        run_cycle()
        boxinc("循环计数.24_Fix3仓", 1)
    else
        step("等待：未安全（等演示节拍或安全扫描）")
        pause(PERIOD)
    end
end
