HandlerGuide 软件总结（约 1000 字）

HandlerGuide 是一套用 WPF / .NET 8（net8.0-windows）编写的半导体三温标准机上位机控制软件，界面按 EXCEED9000 复刻，采用苹果风格。源码约 2.07 万行 C#，配 52 个 XAML 页面，全部署名温启志 18719361399。它覆盖完整的料流流程：待测芯片从进料盘被吸嘴阵列整排取走，经加热盘加温与测压，再按 Bin 分选进收料盘，满盘后回仓并换新盘。

架构没有采用常规 MVVM 框架，而是自行开发了一套 Element 视图模型树（E.T / E.V / E.Grid…）和画布图元（RectItem / TextItem / PolyItem）。52 个页面用 XAML 声明，运行时交给 PageLoader + XamlReader 解析，改界面不必重新编译；LiveRegistry 让动画走 20fps 快速刷新通道，并与 500ms 的普通读数分开，避免拖慢整机界面。全局状态集中在 MachineModel 与 MotionManager 两个单例中。

功能按 7 组 52 页组织：品种设定（测试模式、料盘、测试臂、测试接口、Bin、温度、视觉、清洁盘等配方项）、单元设定（速度、上下料臂、运料小车、料盘传送、偏移、参数下发）、诊断、维护配置（安全、接触力校准、温度偏移、用户管理）、运动仿真、通讯（SECS/GEM 与测试接口）、显示控制板与各类看板（Site/Bin 统计、SPC、报警、批次、结果历史）。

运动层是核心部分：MotionManager 管理 31 根轴、32 路输入和 32 路输出，通过 P/Invoke 调用雷赛 LTDMC.dll；没有插入真实控制卡时，dmc_board_init 返回 0，系统会自动降级为逻辑一致的内置仿真卡，因此现场和办公室运行的是同一套代码。所有指令都要经过安全判断：11 条 SR 规则覆盖急停、安全门、气压、伺服报警、软限位、Z 轴未抬起、共享导轨最小间距和三臂分时互斥，被拦截的指令及其原因会一并记入日志。内置 22 步取放料循环可以自动运行。

仿真页投入的工作量最多。2D 工程图是 11 个模组的平面示意；整机俯视图以真机照片为底图（手臂和横梁已从底图擦除），14 个料盘按照片逐像素量测的坐标定位——上排 6 个叠层盘、中排 5 个缓存盘、左下 3 个出料盘，右侧两块加热盘各 176 个工位；三根手臂、两根横梁和两个红色滑座全部从照片抠图，且各自只沿一个自由度运动。料流全部事件化：上料吸嘴 2×4 吸料 → 加热盘 → 小车 B（右侧上料位、中间测试位、左侧下料位，4×8 格）→ 缓存盘 → 下料吸嘴按 Bin 落盘；出料盘满 32 颗后整盘升进出空盘塔，空位由搬运料盘手臂从进空盘补一片，补上之前下料臂持料等待。每 24 秒输出一行总量守恒自检。

质量保障依靠一套开箱自检：HG_SELFTEST 运行 52 项界面与绑定检查、31 轴运动检查和 223 项操作校验，再逐像素比对两帧确认动画确实在动；离线还有 Python 镜像回归，检查料流守恒、容量上限与无死锁。整体而言，它是一个“界面复刻 + 运动仿真 + 安全互锁 + 自检回归”四位一体的机器控制软件原型。
<img width="2559" height="1527" alt="6293166af4fbb4fafbf9702bb9eb42f2" src="https://github.com/user-attachments/assets/4fd773ba-97b3-4fa7-bda2-6d92df292fba" />


<img width="1280" height="766" alt="9e81b00ed1e227df01babf80c5677f73" src="https://github.com/user-attachments/assets/bd555a33-4865-43f5-b84a-ec470ddfbf51" />
<img width="2559" height="1530" alt="8e9f80d3a8d72decfa77c4ca1aa2ca7b" src="https://github.com/user-attachments/assets/1ecc461f-e725-46cc-a662-7ed0ba0c3707" />
<img width="2559" height="1529" alt="8ad374d20d8abc9ea86059fb78e9f291" src="https://github.com/user-attachments/assets/27f8ca26-dfc1-4050-a6f7-831bf8b308bd" />
<img width="2048" height="1120" alt="b0201473545c0b1043addad40a40aba6" src="https://github.com/user-attachments/assets/32bb1107-095e-4c24-821e-9a8cb0744bc8" />
<img width="2559" height="1528" alt="4eabd6d368762a3608850d794e0e15dc" src="https://github.com/user-attachments/assets/63798490-6518-4545-8906-e7a182cbedd8" />
<img width="1051" height="1121" alt="23ce149fdef5e8fd669315f54a2b7209" src="https://github.com/user-attachments/assets/b9d1471c-3f33-4b50-a9c9-e764b2274f19" />
<img width="2559" height="1521" alt="96c0e5eb8af241f9e98d8b2b205b8bbe" src="https://github.com/user-attachments/assets/7b9bf080-5f82-452f-8790-f0a9da6350c4" />
<img width="2559" height="1530" alt="d4f69e9c69ccb94de4000a9287f23a3d" src="https://github.com/user-attachments/assets/85ed0310-f7ce-4535-b919-abd14d3a2790" />
<img width="2559" height="1524" alt="231f91bcfe82ebc08e85ec2913e14904" src="https://github.com/user-attachments/assets/51e5f9a2-6e0b-4b50-a734-3a09062a6124" />
<img width="2559" height="1520" alt="bb80a808ed2e3b977e40fd0a780f057b" src="https://github.com/user-attachments/assets/df74d776-6f4a-46d9-9bd9-88bd73d57ceb" />
<img width="2559" height="1525" alt="6b88b1bbd9ce4a2c0dba879234e9046f" src="https://github.com/user-attachments/assets/70192d50-4cd1-48b9-8dfe-8b3192e3d499" />
