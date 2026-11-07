-- ============================================================================
--  16_后梭车：后梭车（真机流程 12）
--  绑定：后梭车X轴
--  一条流程 = 一条线程 = 一个独立 Lua 状态；流程之间只通过 box 变量和
--  take/give 令牌交互，不允许互相调用。循环条件必须写 running()，
--  否则停止请求杀不掉当前这一圈（NLua 状态不能从外部强杀）。
--  软限位 / 急停 / 门禁 / 气压 / 共享导轨间距都在 C# 的安全层里，
--  脚本只能被拒绝（moveabs 返回 false，原因看 lasterr()），不能绕过。
-- ============================================================================

local OWNER  = 16   -- 本流程的令牌持有人编号，全流程唯一
local PERIOD = 40  -- 扫描周期 ms

while running() do
    if boxwait("SafeOk", 1) == 1 then
        if take("SHUTTLE", OWNER) then
            step("工作中")
            -- TODO 运动：在此动 后梭车X轴，每步先 home/inpos 再 moveabs
            pause(PERIOD)
            give("SHUTTLE", OWNER)     -- 必须归还；异常时宿主 finally 会兜底清
        else
            pause(PERIOD)         -- 令牌被别的流程持有：让行，绝不阻塞
        end
    else
        pause(PERIOD)              -- 未安全：原地让行，不下任何指令
    end
    boxinc("循环计数.16_后梭车", 1)
end
