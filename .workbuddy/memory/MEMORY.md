# HandlerGuide · 项目长期备忘

## 验证管线（项目自带，改动后必跑）
- 构建：`dotnet build HandlerGuide.csproj -c Debug`（dotnet 在 `C:\Program Files\dotnet\dotnet.exe`）。
- 自检：设 `HG_SELFTEST=1` 跑 `bin\Debug\net8.0-windows\HandlerGuide.exe`，配 `HG_SHOT=<目录>` 会把每页渲染成 PNG。
  - 日志 `%TEMP%\handlerguide.log`；判读 `selftest done ok=52 bad=0 bind=0`（52 = `Pages/*.xaml` 文件数）+ `ops selftest done ALL PASS ok=223`。
  - ⚠️ 日志里的 `layout guard: 未拿到画布…跳过包含性校验` 是**正常现状**（`LastLayoutCanvas` 由已无入口的 `BuildLayoutCanvas()` 设置）；若看到 `guards=12` 那是旧构建残留。
  - 截图产物：`HG_SHOT` 指定目录，主界面页 id = `home`，`_layout_t1/_layout_t4.png` = motion_layout 两帧。
- 备选：根目录 `verify_layout.py`（uiautomation + PrintWindow 抓真实窗口）。
- ⚠️ 脱离 `HG_SELFTEST` 直接启动 exe 在本机会被杀（退出码 `0x80000053`，EDR 类干扰）→ 实机交互验证需换机器。

## 架构约定
- ViewModel 层用 `Element` 视图模型树描述 UI（`E.Grid/E.Box/E.Btn/E.LiveLamp...`），Views/Elements.xaml 用 DataTemplate 渲染；禁止在 ViewModel 里 `new Button()` 等具体控件。
- 实时刷新：`LiveRegistry` + MainWindow 两条 DispatcherTimer（500ms `RefreshAll` / 50ms `RefreshFast`）；`ButtonElement.Enabled` / `LiveLampElement.Fill` 每 tick 自动刷新。
- `Element.VAlign` 默认 `VerticalAlignment.Center` —— 放进整列 Stretch 容器会垂直居中；要与其它卡片顶对齐需显式 `VAlign = Top`。
- 操作面板/命令中心的命令都落在 `Ops`（Start/Pause/StopProduction、HomeAll、ClearAlarm、EmergencyStop、StopAllAxes、SingleStep、ResetCounters、DisassembleArm、SnapMachineState…）与 `LotOps`（OpenLot/CloseLot/AbortLot/PurgeTrays/FinishPurge…），底层经 `MotionManager` 单例（`Mm.ToggleOut/ReadOut/ResetAllOutputs/AnyAlarm/AnyBusy/EStop`）。
- 每个文件头尾保留作者署名水印「温启志 18719361399」。

## motion_layout 整机俯视图（LayoutPages.BuildLayoutCanvasTop）
- 底图 `image/handler_top.jpg`，坐标来自 `TrayPlan`（14 个盘：上排 6 深盘 / 中排 5 深盘 / 左下 3 绿盘）+ 2 块加热盘。
- **上排 6 个深盘 = 料盘堆叠，画「叠层」不画格子**（2026-10-12 改）：
  - 数据源 `MachineModel.TopTrayLayers[6]`（模拟值，`MaxTrayLayers=20`、`MinTrayAlarmLayers=3`）；<3 层整摞转黄 + 盘内写「需增加料盘」。
  - 其余 8 个盘仍走原「格子层（DrawingBrush 平铺）+ 物料格」路径。
  - 动画只在 `MotionManager.AutoCycle` 为真时推进（点「启动演示节拍」或 `StartProduction`）。
- `LastLayoutCanvas` / `BuildLayoutCanvas()`（11 模组 2D 工程图）**已无入口**，仅在自检里被读；不要把 guard 校验当必过项。

## 改 UI 前先做像素取证（好用）
- 用户只给截图、不确定是哪一页时：先用 Pillow 统计 top-N 颜色 + 对特征色做行/列投影，反查代码里的**字面色值**（如 `devBlue #3CC0F0`、`darkBody #26282E`），再按 `TrayPlan` 归一化坐标反算对齐 —— 比盲翻代码快得多。

## 主界面布局（HomePages.MainScreen）
- 网格列：`460 | * | 260 | 320` —— 左列运行状态/品种/测试位矩阵、中列统计录入、右列批次数据统计（Counter/UPH/Timer 页签）、最右列「操作面板」（4×5=20 键 + LED，`OperationPanel()`）。
- 底部：拆卸/SNAP 迷你按钮 + 信号灯 + JHT 版权条（注意：拆卸/SNAP 与操作面板里的同名键是重复入口，保留现状）。

## 环境坑（本机）
- Git Bash 已损坏 → 只用 PowerShell；前台 dotnet 被沙箱拦截、前台 stdout 被吞 → 构建后台跑 + 输出 `Set-Content -Encoding UTF8` 落盘再 Read。
- `*>` 重定向是 UTF-16；`DOTNET_CLI_UI_LANGUAGE=en` + `VSLANG=1033` 防 GBK 乱码。
- 有 fail-closed 安全删除 hook（Remove-Item 走回收站，找不到文件会抛错）→ 临时文件别硬删。
- 临时 `.py` 里的**中文字面量会被当 GBK 解码**（`# -*- coding: utf-8 -*-` 也压不住）→ 探针脚本用 `''.join(chr(c) for c in [...])` 按码点构造中文。
- UIA 抓窗口**不能按标题**（VS 标题含 HandlerGuide 会误命中）→ 按 `ProcessId == p.pid` 匹配；`GetDIBits` 必须显式写 `argtypes`，否则 64 位句柄 `OverflowError`。
- `Write-Host "x" | Set-Content` 不落盘（Write-Host 不进管道）；用纯字符串管道或 `Add-Content`。
- 详细手法见 skill：`windows-sandbox-dotnet-build-verify`。
