# 温启志 18719361399 · 清理被 XAML 覆盖的 MaintPages 死代码（按大括号配对删除方法 + 其节头注释）
import io, sys

path = r"D:\wqz\Handler\HandlerGuide\ViewModels\MaintPages.cs"
with io.open(path, "r", encoding="utf-8") as f:
    lines = f.readlines()

def remove_method(lines, sig_prefix):
    si = None
    for i, l in enumerate(lines):
        if l.strip().startswith(sig_prefix):
            si = i
            break
    if si is None:
        print("NOT FOUND:", sig_prefix)
        return lines
    # 向上删除紧邻的注释/空行（节头 //=== / // N. name / //===）
    j = si - 1
    while j >= 0 and (lines[j].strip() == "" or lines[j].lstrip().startswith("//")):
        j -= 1
    start = j + 1
    # 向下按大括号配对找到方法结束的 }
    depth = 0
    started = False
    k = si
    while k < len(lines):
        for ch in lines[k]:
            if ch == "{":
                depth += 1; started = True
            elif ch == "}":
                depth -= 1
        k += 1
        if started and depth == 0:
            break
    end = k  # exclusive
    del lines[start:end]
    print("REMOVED:", sig_prefix, "lines", start, "->", end)
    return lines

for sig in [
    "public static Element MaintSafe()",
    "private static Element SafeGeneral()",
    "private static Element SafeTestArea()",
    "private static Element SafeArm()",
    "private static Element SafeTray()",
    "public static Element MaintTempOffset()",
    "public static Element MaintLight()",
]:
    lines = remove_method(lines, sig)

with io.open(path, "w", encoding="utf-8") as f:
    f.writelines(lines)
print("DONE")
