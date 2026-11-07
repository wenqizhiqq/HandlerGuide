-- ============================================================================
--  27_面板485：COM2 面板 9 站刷新
--  绑定：RS485
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 27
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

-- COM2 面板 9 站刷新：把运行态写进 VarBox，供面板页 500ms 读数
local function run_cycle()
    boxset("面板.刷新计数", boxget("面板.刷新计数") + 1)
    boxset("面板.安全", boxget("SafeOk"))
    boxset("面板.轴数", axiscount())
    boxset("面板.节拍", boxget("循环计数.01_上料XY"))
    step("面板刷新 #" .. boxget("面板.刷新计数"))
    pause(PERIOD)
end

while running() do
    run_cycle()
end
