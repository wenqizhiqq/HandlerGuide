# 温启志 18719361399 · 临时脚本：删除已无引用的页面生成器（被 XAML 覆盖的死代码）
import re

base = r"D:\wqz\Handler\HandlerGuide\ViewModels"
targets = {
    "UnitPages.cs": ["UnitSpeed","UnitArm","UnitCart","UnitTestArm","UnitTransfer","UnitIO","UnitOffset","UnitApplyOthers"],
    "BoardPages.cs": ["BoardTestArea","BoardInArea","BoardOutArea","BoardTemp","Region"],
    "ToolPages.cs": ["ModePage","EnablePage","TempControlPage","MotorHomePage","InfoLogPage"],
}

def find_method_span(src, name):
    sig = "static Element %s(" % name
    idx = src.find(sig)
    if idx < 0:
        return None
    # 找到签名所在行首
    line_start = src.rfind("\n", 0, idx) + 1
    # 找到方法体开始的 '{'
    bopen = src.find("{", idx)
    # 字符串感知的括号匹配
    depth = 0; in_str = False; i = bopen
    while i < len(src):
        c = src[i]
        if c == '"':
            # 跳过转义
            if i+1 < len(src) and src[i+1] == '"':
                i += 2; continue
            in_str = not in_str
        elif not in_str:
            if c == '{': depth += 1
            elif c == '}':
                depth -= 1
                if depth == 0:
                    end = i
                    # 行尾
                    line_end = src.find("\n", end)
                    if line_end < 0: line_end = len(src)
                    return (line_start, line_end)
        i += 1
    return None

def collapse_blanks(src):
    # 超过 2 个连续空行压成 2 个
    return re.sub(r"\n{4,}", "\n\n\n", src)

for fname, names in targets.items():
    path = base + "\\" + fname
    with open(path, encoding="utf-8") as f:
        src = f.read()
    for name in names:
        span = find_method_span(src, name)
        if span is None:
            print("NOT FOUND:", fname, name); continue
        s, e = span
        # 同时剔除紧邻签名上方的单行 // 注释（装饰注释）
        pre = src.rfind("\n", 0, s)
        # pre 是该注释行之前的位置；检查 s 上方一行是否为 // 注释
        above = src[pre+1:s].rstrip("\r\n")
        above = above.strip()
        if above.startswith("//"):
            s = pre + 1
        src = src[:s] + src[e:]
        print("REMOVED", fname, name)
    src = collapse_blanks(src)
    with open(path, "w", encoding="utf-8") as f:
        f.write(src)
print("DONE")
