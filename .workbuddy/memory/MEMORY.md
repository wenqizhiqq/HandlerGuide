# HandlerGuide · 项目长期备忘

## 验证管线（项目自带，改动后必跑）
- 构建：`dotnet build HandlerGuide.csproj -c Debug`（dotnet 在 `C:\Program Files\dotnet\dotnet.exe`）。
- 自检：设 `HG_SELFTEST=1` 跑 `bin\Debug\net8.0-windows\HandlerGuide.exe`，配 `HG_SHOT=<目录>` 会把每页渲染成 PNG。
  - 日志 `%TEMP%\handlerguide.log`；判读 `selftest done ok=N bad=0 bind=0` + `ops selftest done ALL PASS ok=218`。
  - 截图产物在 `artifacts/selftest_shots/`，主界面页 id = `home`。
- 备选：根目录 `verify_layout.py`（uiautomation + PrintWindow 抓真实窗口）。

## 架构约定
- ViewModel 层用 `Element` 视图模型树描述 UI（`E.Grid/E.Box/E.Btn/E.LiveLamp...`），Views/Elements.xaml 用 DataTemplate 渲染；禁止在 ViewModel 里 `new Button()` 等具体控件。
- 实时刷新：`LiveRegistry` + MainWindow 两条 DispatcherTimer（500ms `RefreshAll` / 50ms `RefreshFast`）；`ButtonElement.Enabled` / `LiveLampElement.Fill` 每 tick 自动刷新。
- `Element.VAlign` 默认 `VerticalAlignment.Center` —— 放进整列 Stretch 容器会垂直居中；要与其它卡片顶对齐需显式 `VAlign = Top`。
- 操作面板/命令中心的命令都落在 `Ops`（Start/Pause/StopProduction、HomeAll、ClearAlarm、EmergencyStop、StopAllAxes、SingleStep、ResetCounters、DisassembleArm、SnapMachineState…）与 `LotOps`（OpenLot/CloseLot/AbortLot/PurgeTrays/FinishPurge…），底层经 `MotionManager` 单例（`Mm.ToggleOut/ReadOut/ResetAllOutputs/AnyAlarm/AnyBusy/EStop`）。
- 每个文件头尾保留作者署名水印「温启志 18719361399」。

## 主界面布局（HomePages.MainScreen）
- 网格列：`460 | * | 260 | 320` —— 左列运行状态/品种/测试位矩阵、中列统计录入、右列批次数据统计（Counter/UPH/Timer 页签）、最右列「操作面板」（4×5=20 键 + LED，`OperationPanel()`）。
- 底部：拆卸/SNAP 迷你按钮 + 信号灯 + JHT 版权条（注意：拆卸/SNAP 与操作面板里的同名键是重复入口，保留现状）。

## 环境坑（本机）
- Git Bash 已损坏 → 只用 PowerShell；前台 dotnet 被沙箱拦截、前台 stdout 被吞 → 构建后台跑 + 输出 `Set-Content -Encoding UTF8` 落盘再 Read。
- `*>` 重定向是 UTF-16；`DOTNET_CLI_UI_LANGUAGE=en` + `VSLANG=1033` 防 GBK 乱码。
- 有 fail-closed 安全删除 hook（Remove-Item 走回收站，找不到文件会抛错）→ 临时文件别硬删。
- 详细手法见 skill：`windows-sandbox-dotnet-build-verify`。
