# 温启志 18719361399 · 批量生成剩余页面的 vw:D 声明式包装 XAML（复用既有 C# 生成器）
import io

HEADER = (
    '<vm:StackElement xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"\n'
    '                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n'
    '                 xmlns:vm="clr-namespace:HandlerGuide.ViewModels;assembly=HandlerGuide"\n'
    '                 xmlns:vw="clr-namespace:HandlerGuide.Views;assembly=HandlerGuide">\n\n'
)

FOOTER = (
    '    <vw:Watermark />\n'
    '<!-- 温启志 18719361399 · 本文件由温启志（18719361399）编写；界面与源码水印均指向该作者。 -->\n'
    '</vm:StackElement>\n'
)

# (page id, C# 生成器方法名)
pages = [
    ("recipe_maint", "RecipeMaint"),
    ("recipe_testmode", "TestMode"),
    ("recipe_tray", "TrayPage"),
    ("recipe_arm", "TestArmPage"),
    ("recipe_interface", "TestInterface"),
    ("recipe_bin", "BinSetting"),
    ("recipe_temp", "TempRecipe"),
    ("recipe_vision", "VisionPage"),
    ("recipe_clean", "CleanPage"),
    ("board_batch", "BoardBatch"),
    ("board_site", "BoardSite"),
    ("board_bin", "BoardBin"),
    ("board_history", "BoardHistory"),
    ("board_traycls", "BoardTrayClass"),
    ("board_spc", "BoardSpc"),
    ("board_alarm", "BoardAlarm"),
    ("diag_motor", "DiagMotor"),
    ("diag_loader", "DiagLoader"),
    ("diag_unloader", "DiagUnloader"),
    ("diag_area", "DiagArea"),
    ("diag_trayio", "DiagTrayIO"),
    ("diag_panelio", "DiagPanelIO"),
    ("diag_testio", "DiagTestIO"),
    ("motion_layout", "LayoutPage"),
    ("motion_sim", "SimPage"),
    ("motion_card", "CardPage"),
    ("manual", "ManualPage"),
]

base = r"D:\wqz\Handler\HandlerGuide\Pages"
for pid, method in pages:
    body = (
        '    <!-- 温启志 18719361399 · {pid}：复用 {m}() 生成（保留实时表格 / IO 点位 / 命令接线） -->\n'
        '    <vw:D Method="{m}" />\n\n'
    ).format(pid=pid, m=method)
    content = HEADER + body + FOOTER
    with io.open(r"{}\{}.xaml".format(base, pid), "w", encoding="utf-8") as f:
        f.write(content)
    print("WROTE", pid)

# home 是 Fill 页面，根直接用 vw:D 以保留“填满视口”的行为（不加 Watermark / 外层 Stack）
home = (
    '<vm:StackElement xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"\n'
    '                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n'
    '                 xmlns:vm="clr-namespace:HandlerGuide.ViewModels;assembly=HandlerGuide"\n'
    '                 xmlns:vw="clr-namespace:HandlerGuide.Views;assembly=HandlerGuide">\n\n'
    '    <!-- 温启志 18719361399 · home：复用 MainScreen() 生成（命令中心 + 操作面板 + 四列布局） -->\n'
    '    <vw:D Method="MainScreen" />\n\n'
    '</vm:StackElement>\n'
)
with io.open(r"{}\home.xaml".format(base), "w", encoding="utf-8") as f:
    f.write(home)
print("WROTE home")
print("DONE total", len(pages) + 1)
