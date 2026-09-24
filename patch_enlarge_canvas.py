# -*- coding: utf-8 -*-
"""放大左侧仿真画布（K=1.45 → 2.2），保留命令中心/批次管理自动换行多行显示。"""
import os

LAY = r'D:\wqz\Handler\HandlerGuide\ViewModels\LayoutPages.cs'

raw = open(LAY, 'rb').read()
text = raw.decode('utf-8-sig')
bom = raw[:3] == b'\xef\xbb\xbf'

# 当前状态（上一轮已改成 K=1.45）
old_const = (
    "            const double K = 1.45;     // 缩放（原图 540×438 → 画布 783×635）\r\n"
    "            const double OX = -45.0;   // 水平偏移（按 K 同比）\r\n"
    "            const double OY = -68.0;   // 垂直偏移（按 K 同比）\r\n"
    "            const double CW = 783;\r\n"
    "            const double CH = 635;\r\n"
)
# 目标：放大（K=2.2），按 K 同比缩放 OX/OY/CW/CH
new_const = (
    "            const double K = 2.2;      // 缩放（原图 540×438 → 画布 1188×964，左侧尽量放大）\r\n"
    "            const double OX = -68.0;   // 水平偏移（按 K 同比）\r\n"
    "            const double OY = -103.0;  // 垂直偏移（按 K 同比）\r\n"
    "            const double CW = 1188;\r\n"
    "            const double CH = 964;\r\n"
)

assert text.count(old_const) == 1, f'常量锚点异常 old={text.count(old_const)}'
text = text.replace(old_const, new_const)
print('画布缩放 K: 1.45 → 2.2（CW/CH/OX/OY 同步放大）')

# 顺手更新文件头注释里那行「原图 540×438 → 画布 952×728」的描述，避免误导
old_hdr = "// ---- 原图 540×438 → 画布 952×728 ----\r\n"
new_hdr = "// ---- 原图 540×438 → 画布 1188×964（K=2.2，左侧尽量放大）----\r\n"
if text.count(old_hdr) == 1:
    text = text.replace(old_hdr, new_hdr)
    print('文件头注释同步更新')

open(LAY, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))
print('LayoutPages 画布放大完成')