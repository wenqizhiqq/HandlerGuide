# 温启志 18719361399 · 临时脚本：将 PageCatalog.cs 中所有 Build 委托引用置空
# 所有页面已有 Pages/*.xaml，PageLoader.Load 优先，Build 委托成为死代码。
import io

path = r"D:\wqz\Handler\HandlerGuide\ViewModels\PageCatalog.cs"
with open(path, encoding="utf-8") as f:
    src = f.read()

# 全部 49 个非 null 的 Build 委托方法引用（maint_safe/toffset/light 已是 null）
methods = [
    "RecipePages.RecipeMaint",
    "RecipePages.TestMode",
    "RecipePages.TrayPage",
    "RecipePages.TestArmPage",
    "RecipePages.TestInterface",
    "RecipePages.BinSetting",
    "RecipePages.TempRecipe",
    "RecipePages.VisionPage",
    "RecipePages.CleanPage",
    "UnitPages.UnitSpeed",
    "UnitPages.UnitArm",
    "UnitPages.UnitCart",
    "UnitPages.UnitTestArm",
    "UnitPages.UnitTransfer",
    "UnitPages.UnitIO",
    "UnitPages.UnitOffset",
    "UnitPages.UnitApplyOthers",
    "DiagPages.DiagMotor",
    "DiagPages.DiagLoader",
    "DiagPages.DiagUnloader",
    "DiagPages.DiagArea",
    "DiagPages.DiagTrayIO",
    "DiagPages.DiagPanelIO",
    "DiagPages.DiagTestIO",
    "MaintPages.MaintBase",
    "MaintPages.MaintForce",
    "MaintPages.MaintUser",
    "LayoutPages.LayoutPage",
    "MotionPages.SimPage",
    "MotionPages.CardPage",
    "ToolPages.ModePage",
    "ToolPages.EnablePage",
    "ToolPages.TempControlPage",
    "ToolPages.MotorHomePage",
    "ToolPages.InfoLogPage",
    "ToolPages.ManualPage",
    "BoardPages.BoardTestArea",
    "BoardPages.BoardInArea",
    "BoardPages.BoardOutArea",
    "BoardPages.BoardBatch",
    "BoardPages.BoardSite",
    "BoardPages.BoardBin",
    "BoardPages.BoardHistory",
    "BoardPages.BoardTemp",
    "BoardPages.BoardTrayClass",
    "BoardPages.BoardSpc",
    "BoardPages.BoardAlarm",
    "HomePages.MainScreen",
]

count = 0
for m in methods:
    n = src.count(m)
    if n == 0:
        print("MISSING:", m)
    else:
        src = src.replace(m, "null")
        count += n
        print("NULL", m, "x%d" % n)

with open(path, "w", encoding="utf-8") as f:
    f.write(src)
print("TOTAL replaced:", count)
