import ctypes, ctypes.wintypes as wt, os, subprocess, time, sys

u32 = ctypes.windll.user32
g32 = ctypes.windll.gdi32
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    u32.SetProcessDPIAware()

# ---- correct 64-bit handle return types (avoids truncation bug) ----
u32.FindWindowW.restype = ctypes.c_void_p
u32.GetForegroundWindow.restype = ctypes.c_void_p
u32.GetWindowDC.restype = ctypes.c_void_p
u32.GetDC.restype = ctypes.c_void_p
u32.GetWindowRect.argtypes = [ctypes.c_void_p, ctypes.POINTER(wt.RECT)]
u32.GetWindowDC.argtypes = [ctypes.c_void_p]
u32.ReleaseDC.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
u32.PrintWindow.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint]
g32.CreateCompatibleDC.argtypes = [ctypes.c_void_p]
g32.CreateCompatibleDC.restype = ctypes.c_void_p
g32.CreateCompatibleBitmap.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_int]
g32.CreateCompatibleBitmap.restype = ctypes.c_void_p
g32.SelectObject.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
g32.SelectObject.restype = ctypes.c_void_p
g32.GetDIBits.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint, ctypes.c_uint, ctypes.c_void_p, ctypes.POINTER(BIH) if False else ctypes.c_void_p, ctypes.c_uint]
g32.GetDIBits.restype = ctypes.c_int
g32.DeleteObject.argtypes = [ctypes.c_void_p]
g32.DeleteDC.argtypes = [ctypes.c_void_p]

EXE = r'D:\wqz\Handler\HandlerGuide\bin\Debug\net8.0-windows\HandlerGuide.exe'
OUT = r'D:\wqz\Handler\HandlerGuide\verify_layout.png'

# 0b. desktop precondition
pt = wt.POINT()
u32.GetCursorPos(ctypes.byref(pt))
h = u32.OpenInputDesktop(0, False, 0x0100)
print('precondition: cursor=', pt.x, pt.y, 'inputDesktop=', h)

import uiautomation as auto

subprocess.run(['taskkill', '/F', '/IM', 'HandlerGuide.exe'], capture_output=True)
time.sleep(1)
p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))

def find_main_hwnd():
    try:
        for w in auto.GetRootControl().GetChildren():
            try:
                name = w.Name
            except Exception:
                name = ''
            if name and 'Handler' in name:
                return w.NativeWindowHandle
    except Exception as e:
        print('find err', e)
    return 0

hwnd, end = 0, time.time() + 30
while time.time() < end:
    hwnd = find_main_hwnd()
    if hwnd and u32.IsWindowVisible(hwnd):
        break
    time.sleep(0.5)
print('hwnd=', hwnd)
if not hwnd:
    raise SystemExit('window not found')

u32.ShowWindow(hwnd, 3)
time.sleep(1.5)

def invokable_ancestor(ctrl, levels=6):
    cur = ctrl
    for _ in range(levels):
        try:
            t = cur.ControlTypeName
        except Exception:
            return None
        if t == 'ButtonControl':
            return cur
        cur = cur.GetParentControl()
        if cur is None:
            return None
    return None

# navigate to the layout page via UIA
def walk(ctrl, hit, d=0):
    if hit[0] or d > 14:
        return
    try:
        ch = ctrl.GetChildren()
    except Exception:
        return
    for k in ch:
        try:
            nm = k.Name
        except Exception:
            nm = ''
        if '模块平面仿真' in nm:
            hit[0] = k
            return
        walk(k, hit, d + 1)

for attempt in range(8):
    try:
        hit = [None]
        for top in auto.GetRootControl().GetChildren():
            walk(top, hit)
        if hit[0]:
            btn = invokable_ancestor(hit[0])
            if btn is not None:
                ok = btn.GetInvokePattern().Invoke()
                print('invoked nav:', ok, 'name=', hit[0].Name)
                break
    except Exception as e:
        print('nav walk err', e)
    time.sleep(0.6)

time.sleep(2.0)

# capture
r = wt.RECT()
u32.GetWindowRect(hwnd, ctypes.byref(r))
w, h = r.right - r.left, r.bottom - r.top
print('window size:', w, 'x', h)
hdc = u32.GetWindowDC(hwnd)
mem = g32.CreateCompatibleDC(hdc)
bmp = g32.CreateCompatibleBitmap(hdc, w, h)
g32.SelectObject(mem, bmp)
u32.PrintWindow(hwnd, mem, 0x2)

class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', ctypes.c_long), ('biHeight', ctypes.c_long),
                ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD),
                ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', ctypes.c_long),
                ('biYPelsPerMeter', ctypes.c_long), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]

bi = BIH()
bi.biSize = ctypes.sizeof(BIH)
bi.biWidth = w
bi.biHeight = -h
bi.biPlanes = 1
bi.biBitCount = 32
buf = ctypes.create_string_buffer(w * h * 4)
g32.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), 0)
g32.DeleteObject(bmp)
g32.DeleteDC(mem)
u32.ReleaseDC(hwnd, hdc)

from PIL import Image
img = Image.frombuffer('RGBA', (w, h), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
img.save(OUT)
print('saved', OUT, img.size)

u32.PostMessageW(hwnd, 0x0010, 0, 0)
time.sleep(1)
if p.poll() is None:
    p.kill()
print('done')
