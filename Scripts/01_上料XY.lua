-- ============================================================================
--  01_上料XY：上料XY 工站
--  绑定：上料X轴, 上料Y轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 1   -- 本流程的令牌持有人编号，全流程唯一
local PERIOD = 50  -- 扫描周期 ms

while running() do
    if boxwait("SafeOk", 1) == 1 then
        if take("ARM_X", OWNER) then
            step("工作中")
            -- TODO 运动：在此动 上料X轴, 上料Y轴，每步先 home/inpos 再 moveabs
            pause(PERIOD)
            give("ARM_X", OWNER)     -- 必须归还；异常时宿主 finally 会兜底清
        else
            pause(PERIOD)         -- 令牌被别的流程持有：让行，绝不阻塞
        end
    else
        pause(PERIOD)              -- 未安全：原地让行，不下任何指令
    end
    boxinc("循环计数.01_上料XY", 1)
end
