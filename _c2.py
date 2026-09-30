# -*- coding: utf-8 -*-
# 温启志 18719361399
import os, shutil
root = r"D:\wqz\Handler\HandlerGuide"
for p in [
    os.path.join(root, "_cleanup_tmp.py"),
    os.path.join(root, "_cleanup_result.txt"),
    os.path.join(root, "artifacts", "selftest_shots"),
    os.path.join(root, "artifacts", "selftest_shots2"),
]:
    try:
        if os.path.isfile(p):
            os.remove(p)
        elif os.path.isdir(p):
            shutil.rmtree(p, ignore_errors=True)
    except Exception as e:
        print("ERR", p, e)
print("done")
