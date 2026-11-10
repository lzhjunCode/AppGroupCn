using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// 用法:
//   AumidTool windows                 枚举所有可见顶层窗口及其 AUMID
//   AumidTool window <pid>            只看指定进程的窗口
//   AumidTool getlnk <lnk路径>        读快捷方式的 System.AppUserModel.ID
//   AumidTool setlnk <lnk路径> <aumid> 给快捷方式写 System.AppUserModel.ID

class TeeWriter : System.IO.TextWriter {
    private readonly System.IO.TextWriter _out;
    private readonly string _path;
    public TeeWriter(System.IO.TextWriter o, string path) { _out = o; _path = path; }
    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
    public override void WriteLine(string value) {
        try { _out.WriteLine(value); } catch { }
        try { System.IO.File.AppendAllText(_path, value + Environment.NewLine, System.Text.Encoding.UTF8); } catch { }
    }
    public override void Write(string value) { WriteLine(value); }
}

class Program {
    // ---- property store ----
    [StructLayout(LayoutKind.Sequential)]
    struct PROPERTYKEY { public Guid fmtid; public uint pid; }

    static readonly PROPERTYKEY PKEY_AUMID = new PROPERTYKEY {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5
    };

    [StructLayout(LayoutKind.Sequential)]
    struct PropVariant {
        public ushort vt;
        public ushort reserved1, reserved2, reserved3;
        public IntPtr p1, p2, p3, p4;
    }

    [ComImport, Guid("886D8EEB-8CF2-444E-8DDE-C5F1C2C0B806"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore {
        void GetCount(out uint c);
        void GetAt(uint i, out PROPERTYKEY key);
        void GetValue(ref PROPERTYKEY key, out PropVariant v);
        void SetValue(ref PROPERTYKEY key, ref PropVariant v);
        void Commit();
    }

    [DllImport("shell32.dll")]
    static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, out IPropertyStore store);

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr pv, uint coInit);

    [DllImport("ole32.dll")]
    static extern void CoTaskMemFree(IntPtr pv);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr lp);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    delegate bool EnumProc(IntPtr h, IntPtr lp);

    static Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-444E-8DDE-C5F1C2C0B806");

    // ---- shell link (minimal) ----
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class CShellLink { }

    [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLinkW {
        void GetPath_Placeholder();
        void GetIDList_Placeholder();
        void SetIDList_Placeholder();
        void GetDescription_Placeholder();
        void SetDescription_Placeholder();
        void GetWorkingDirectory_Placeholder();
        void SetWorkingDirectory_Placeholder();
        void GetArguments_Placeholder();
        void SetArguments_Placeholder();
        void GetHotkey_Placeholder();
        void SetHotkey_Placeholder();
        void GetShowCmd_Placeholder();
        void SetShowCmd_Placeholder();
        void GetIconLocation_Placeholder();
        void SetIconLocation_Placeholder();
        void SetRelativePath_Placeholder();
        void Resolve_Placeholder();
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }

    [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersistFile {
        void GetClassID_Placeholder();
        void IsDirty_Placeholder();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string file, uint mode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string file, bool remember);
    }

    static string GetWindowAumid(IntPtr hwnd) {
        Guid iid = IID_IPropertyStore;
        int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out IPropertyStore store);
        if (hr != 0 || store == null) return null;
        try {
            var key = PKEY_AUMID;
            store.GetValue(ref key, out PropVariant v);
            if (v.vt == 31 && v.p1 != IntPtr.Zero) {   // VT_LPWSTR
                string s = Marshal.PtrToStringUni(v.p1);
                Marshal.FreeCoTaskMem(v.p1);
                return s;
            }
            if (v.vt == 31 && v.p1 == IntPtr.Zero) return "";
            return null; // 属性不存在
        } catch { return null; }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    static readonly Guid IID_IShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

    [ComImport, Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem2Raw {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IntPtr ppsi);
        void GetDisplayName(uint sigdnName, out IntPtr ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IntPtr psi, uint hint, out int piOrder);
        void GetProperty(ref PROPERTYKEY key, IntPtr pv);   // IShellItem2.GetProperty —— 参数数量必须真实
        void GetPropertyStore(uint flags, ref Guid riid, out IntPtr ppv);
    }

    [DllImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName")]
    static extern int SHCreateItemFromParsingName2(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    [ComImport, Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
    class CTaskbarList { }

    [ComImport, Guid("56FDF342-FD6D-11D0-958A-006097C9A090"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITaskbarList {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
    }

    // ---- GDI 抓屏 ----
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr d, int x, int y, int w, int h, IntPtr s, int sx, int sy, uint rop);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, ref RECT r);

    static void CaptureScreen(string outPath, int cropY = -1, int cropH = -1, int cropX = -1, int cropW = -1) {
        int x0s = 0;
        int y0s = 0;
        int sw = GetSystemMetrics(0);
        int sh = GetSystemMetrics(1);
        int y0 = cropY >= 0 ? y0s + cropY : y0s;
        int h = cropH > 0 ? cropH : sh;
        int x0 = cropX >= 0 ? x0s + cropX : x0s;
        int w = cropW > 0 ? cropW : sw;
        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);
        IntPtr bmp = CreateCompatibleBitmap(screenDc, w, h);
        IntPtr old = SelectObject(memDc, bmp);
        BitBlt(memDc, 0, 0, w, h, screenDc, x0, y0, 0x00CC0020); // SRCCOPY

        int stride = ((w * 3 + 3) / 4) * 4;
        int imgSize = stride * h;
        byte[] pixels = new byte[imgSize];

        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize = 40;
        bmi.bmiHeader.biWidth = w;
        bmi.bmiHeader.biHeight = -h; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 24;
        bmi.bmiHeader.biCompression = 0;
        GetDIBits(memDc, bmp, 0, (uint)h, pixels, ref bmi, 0);

        using (var fs = new System.IO.FileStream(outPath, System.IO.FileMode.Create)) {
            using var bw = new System.IO.BinaryWriter(fs);
            int fileSize = 54 + imgSize;
            bw.Write((byte)'B'); bw.Write((byte)'M');
            bw.Write(fileSize); bw.Write(0); bw.Write(54);
            bw.Write(40); bw.Write(w); bw.Write(-h);
            bw.Write((short)1); bw.Write((short)24); bw.Write(0); bw.Write(imgSize);
            bw.Write(2835); bw.Write(2835); bw.Write(0); bw.Write(0);
            bw.Write(pixels);
        }

        SelectObject(memDc, old);
        DeleteObject(bmp);
        DeleteDC(memDc);
        ReleaseDC(IntPtr.Zero, screenDc);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO {
        public BITMAPINFOHEADER bmiHeader;
        public int bmiColors;
    }

    [DllImport("gdi32.dll")]
    static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, byte[] bits, ref BITMAPINFO bmi, uint usage);

    static IPropertyStore OpenFileStore(string path, bool writable, uint flagsOverride = 99) {
        if (!string.IsNullOrEmpty(path) && path.IndexOf('/') >= 0 && path.Length > 2 && path[1] == ':')
            path = path.Replace('/', '\\');
        if (!System.IO.File.Exists(path)) { Console.WriteLine($"[dbg] not found: {path}"); return null; }

        // 路线1: SHGetPropertyStoreFromParsingName（文档推荐）
        Guid iidPS = IID_IPropertyStore;
        uint flags = flagsOverride != 99 ? flagsOverride : (writable ? 2u : 0u);
        int hr = SHGetPropertyStoreFromParsingName(path, IntPtr.Zero, flags, ref iidPS, out IntPtr pStore);
        if (hr == 0 && pStore != IntPtr.Zero) {
            return (IPropertyStore)Marshal.GetObjectForIUnknown(pStore);
        }
        Console.WriteLine($"[dbg] route1 hr=0x{hr:X8}");

        // 路线2: IShellItem2::GetPropertyStore（vtable 修正版）
        Guid iid2 = new Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93");
        hr = SHCreateItemFromParsingName2(path, IntPtr.Zero, ref iid2, out IntPtr pItem);
        if (hr != 0) { Console.WriteLine($"item hr=0x{hr:X8}"); return null; }
        try {
            var item2 = (IShellItem2Raw)Marshal.GetObjectForIUnknown(pItem);
            Guid iid = IID_IPropertyStore;
            item2.GetPropertyStore(writable ? 2u : 0u, ref iid, out IntPtr ppv);
            if (ppv == IntPtr.Zero) { Console.WriteLine("GetPropertyStore returned null"); return null; }
            return (IPropertyStore)Marshal.GetObjectForIUnknown(ppv);
        }
        catch (Exception e) { Console.WriteLine("route2 failed: " + e.Message); return null; }
        finally { Marshal.Release(pItem); }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetPropertyStoreFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, uint flags, ref Guid riid, out IntPtr ppv);

    static IPropertyStore OpenFileStoreUnused(string path) => null;

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINTP p);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
    [DllImport("user32.dll")] static extern int GetWindowLongW(IntPtr h, int idx);
    struct POINTP { public int X, Y; }

    [STAThread]
    static int Main(string[] args) {
        try { SetProcessDPIAware(); } catch { }
        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aumidtool.log");
        try { System.IO.File.WriteAllText(logPath, ""); } catch { }
        var origOut = Console.Out;
        var tee = new TeeWriter(origOut, logPath);
        Console.SetOut(tee);

        int coHr = CoInitializeEx(IntPtr.Zero, 2); // COINIT_APARTMENTTHREADED
        Console.WriteLine($"[dbg] CoInitializeEx hr=0x{coHr:X8}");

        if (args.Length == 0) { Console.WriteLine("用法: windows | window <pid> | getlnk <lnk> | setlnk <lnk> <aumid>"); return 1; }

        switch (args[0]) {
            case "windows": {
                    uint? filterPid = args.Length >= 2 ? uint.Parse(args[1]) : (uint?)null;
                    var rows = new List<string>();
                    EnumWindows((h, lp) => {
                        if (!IsWindowVisible(h)) return true;
                        GetWindowThreadProcessId(h, out uint pid);
                        if (filterPid.HasValue && pid != filterPid.Value) return true;
                        var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
                        string title = sb.ToString();
                        if (title.Length == 0) return true;
                        var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                        rows.Add($"{h} pid={pid} class={cls} title={title} aumid={GetWindowAumid(h) ?? "(未设置)"}");
                        return true;
                    }, IntPtr.Zero);
                    foreach (var r in rows) Console.WriteLine(r);
                    return 0;
                }
            case "getlnk": {
                    var store = OpenFileStore(args[1], false, args.Length > 2 ? uint.Parse(args[2]) : 0u);
                    if (store == null) { Console.WriteLine("(无法打开 property store)"); return 2; }
                    var key = PKEY_AUMID;
                    store.GetValue(ref key, out PropVariant v);
                    if (v.vt == 31 && v.p1 != IntPtr.Zero) { Console.WriteLine(Marshal.PtrToStringUni(v.p1)); Marshal.FreeCoTaskMem(v.p1); }
                    else Console.WriteLine("(未设置)");
                    return 0;
                }
            case "setlnk": {
                    var store = OpenFileStore(args[1], true);
                    if (store == null) { Console.WriteLine("(无法打开 property store)"); return 2; }
                    var key = PKEY_AUMID;
                    var pv = new PropVariant { vt = 31 };
                    pv.p1 = Marshal.StringToCoTaskMemUni(args[2]);
                    store.SetValue(ref key, ref pv);
                    store.Commit();
                    Console.WriteLine("ok");
                    return 0;
                }
            case "propcount": {
                    // 用 jpg 等文件测属性系统整体是否可用
                    var store = OpenFileStore(args[1], false, args.Length > 2 ? uint.Parse(args[2]) : 0u);
                    if (store == null) { Console.WriteLine("(无法打开)"); return 2; }
                    store.GetCount(out uint c);
                    Console.WriteLine("property count=" + c);
                    return 0;
                }
            case "tbrect": {
                    IntPtr tb = FindWindow("Shell_TrayWnd", null);
                    RECT r = new RECT();
                    GetWindowRect(tb, ref r);
                    Console.WriteLine($"taskbar hwnd={tb} rect=({r.Left},{r.Top})-({r.Right},{r.Bottom})");
                    Console.WriteLine($"primary={GetSystemMetrics(0)}x{GetSystemMetrics(1)} virtual=({GetSystemMetrics(76)},{GetSystemMetrics(77)}) {GetSystemMetrics(78)}x{GetSystemMetrics(79)}");
                    return 0;
                }
            case "shot": {
                    string outPath = args.Length > 1 ? args[1] : System.IO.Path.Combine(System.IO.Path.GetTempPath(), "shot.bmp");
                    int cropY = args.Length > 2 ? int.Parse(args[2]) : -1;   // 起始 Y（相对虚拟桌面）
                    int cropH = args.Length > 3 ? int.Parse(args[3]) : -1;   // 高度
                    int cropX = args.Length > 4 ? int.Parse(args[4]) : -1;   // 起始 X
                    int cropW = args.Length > 5 ? int.Parse(args[5]) : -1;   // 宽度
                    CaptureScreen(outPath, cropY, cropH, cropX, cropW);
                    Console.WriteLine("saved " + outPath);
                    return 0;
                }
            case "click": {
                    // click <x> <y> [shift]  —— 模拟鼠标左键点击（物理像素）
                    int cx = int.Parse(args[1]), cy = int.Parse(args[2]);
                    bool shift = args.Length > 3 && args[3] == "shift";
                    SetCursorPos(cx, cy);
                    System.Threading.Thread.Sleep(120);
                    if (shift) { keybd_event(0x10, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(80); }
                    mouse_event(0x0002, 0, 0, 0, IntPtr.Zero); // LEFTDOWN
                    System.Threading.Thread.Sleep(90);
                    mouse_event(0x0004, 0, 0, 0, IntPtr.Zero); // LEFTUP
                    if (shift) { System.Threading.Thread.Sleep(80); keybd_event(0x10, 0, 2, IntPtr.Zero); }
                    Console.WriteLine($"clicked {cx},{cy} shift={shift}");
                    return 0;
                }
            case "hover": {
                    // hover <x> <y> [ms] —— 把鼠标移到指定点停留，然后列出附近可见窗口（找任务栏预览用）
                    int hx = int.Parse(args[1]), hy = int.Parse(args[2]);
                    int waitMs = args.Length > 3 ? int.Parse(args[3]) : 900;
                    SetCursorPos(hx, hy);
                    System.Threading.Thread.Sleep(waitMs);
                    Console.WriteLine($"--- windows near ({hx},{hy}) after hover {waitMs}ms ---");
                    EnumWindows((h, lp) => {
                        if (!IsWindowVisible(h)) return true;
                        RECT wr = new RECT(); GetWindowRect(h, ref wr);
                        if (wr.Right - wr.Left < 40 || wr.Bottom - wr.Top < 40) return true;
                        // 只关心屏幕下半区（任务栏上方）的窗口
                        if (wr.Bottom < 1400) return true;
                        var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                        var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
                        GetWindowThreadProcessId(h, out uint pid);
                        string pname = "";
                        try { pname = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; } catch { }
                        Console.WriteLine($"{h} pid={pid} proc={pname} class={cls} title={sb} rect=({wr.Left},{wr.Top})-({wr.Right},{wr.Bottom})");
                        return true;
                    }, IntPtr.Zero);
                    return 0;
                }
            case "wfp": {
                    // wfp <x> <y> —— WindowFromPoint：这个点上的点击会落到谁头上
                    int wx = int.Parse(args[1]), wy = int.Parse(args[2]);
                    IntPtr h = WindowFromPoint(new POINTP { X = wx, Y = wy });
                    for (int i = 0; i < 6 && h != IntPtr.Zero; i++) {
                        var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                        var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
                        GetWindowThreadProcessId(h, out uint pid);
                        RECT wr = new RECT(); GetWindowRect(h, ref wr);
                        Console.WriteLine($"[{i}] hwnd={h} pid={pid} class={cls} title={sb} rect=({wr.Left},{wr.Top})-({wr.Right},{wr.Bottom})");
                        IntPtr parent = GetAncestor(h, 1 /* GA_ROOT */);
                        if (parent == h) break;
                        h = parent;
                    }
                    return 0;
                }
            case "style": {
                    // style <hwnd> —— 打印窗口的 style/exstyle
                    IntPtr sh = new IntPtr(long.Parse(args[1].StartsWith("0x") ? args[1].Substring(2) : args[1], System.Globalization.NumberStyles.Integer));
                    int st = GetWindowLongW(sh, -16), ex = GetWindowLongW(sh, -20);
                    Console.WriteLine($"style=0x{st:X8} exstyle=0x{ex:X8}");
                    Console.WriteLine($"TRANSPARENT={(ex & 0x20) != 0} LAYERED={(ex & 0x80000) != 0} TOPMOST={(ex & 8) != 0} NOACTIVATE={(ex & 0x8000000) != 0} TOOLWINDOW={(ex & 0x80) != 0}");
                    return 0;
                }
            case "winall": {
                    // winall <pid> —— 列出该进程所有顶层窗口（含隐藏/无标题）
                    uint target = uint.Parse(args[1]);
                    EnumWindows((h, lp) => {
                        GetWindowThreadProcessId(h, out uint pid);
                        if (pid != target) return true;
                        var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                        var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
                        RECT wr = new RECT(); GetWindowRect(h, ref wr);
                        Console.WriteLine($"{h} visible={IsWindowVisible(h)} class={cls} title={sb} rect=({wr.Left},{wr.Top})-({wr.Right},{wr.Bottom})");
                        return true;
                    }, IntPtr.Zero);
                    return 0;
                }
            case "wininfo": {
                    // wininfo <标题> —— 查窗口是否存在/可见/位置
                    IntPtr h = FindWindow(null, args[1]);
                    if (h == IntPtr.Zero) { Console.WriteLine("not found: " + args[1]); return 0; }
                    RECT wr = new RECT();
                    GetWindowRect(h, ref wr);
                    Console.WriteLine($"hwnd={h} visible={IsWindowVisible(h)} iconic={IsIconic(h)} rect=({wr.Left},{wr.Top})-({wr.Right},{wr.Bottom})");
                    return 0;
                }
            case "mclick": {
                    // mclick <x> <y> —— 模拟鼠标中键点击
                    int cx = int.Parse(args[1]), cy = int.Parse(args[2]);
                    SetCursorPos(cx, cy);
                    System.Threading.Thread.Sleep(120);
                    mouse_event(0x0020, 0, 0, 0, IntPtr.Zero); // MIDDLEDOWN
                    System.Threading.Thread.Sleep(90);
                    mouse_event(0x0040, 0, 0, 0, IntPtr.Zero); // MIDDLEUP
                    Console.WriteLine($"midclicked {cx},{cy}");
                    return 0;
                }
            case "hidetab": {
                    int pid = int.Parse(args[1]);
                    var proc = System.Diagnostics.Process.GetProcessById(pid);
                    IntPtr h = proc.MainWindowHandle;
                    Console.WriteLine($"pid={pid} hwnd={h}");
                    var list = (ITaskbarList)new CTaskbarList();
                    list.HrInit();
                    list.DeleteTab(h);
                    Console.WriteLine("DeleteTab called");
                    return 0;
                }
            case "getext": {
                    string dir = System.IO.Path.GetDirectoryName(args[1]);
                    string name = System.IO.Path.GetFileName(args[1]);
                    Type shellType = Type.GetTypeFromProgID("Shell.Application");
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic folder = shell.Namespace(dir);
                    dynamic item = folder.ParseName(name);
                    object v = item.ExtendedProperty("System.AppUserModel.ID");
                    Console.WriteLine(v == null ? "(未设置)" : v.ToString());
                    return 0;
                }
            case "mklnk": {
                    var link = (IShellLinkW)new CShellLink();
                    link.SetPath(args[2]);
                    ((IPersistFile)link).Save(args[1], true);
                    Console.WriteLine("lnk created: " + args[1]);
                    return 0;
                }
            case "pin": {
                    bool ok = InvokePinVerb(args[1], unpin: false);
                    Console.WriteLine(ok ? "PIN-OK" : "PIN-FAIL");
                    return ok ? 0 : 3;
                }
            case "unpin": {
                    bool ok = InvokePinVerb(args[1], unpin: true);
                    Console.WriteLine(ok ? "UNPIN-OK" : "UNPIN-FAIL");
                    return ok ? 0 : 3;
                }
            case "pins": {
                    string dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                    foreach (var f in System.IO.Directory.GetFiles(dir, "*.lnk"))
                        Console.WriteLine(System.IO.Path.GetFileName(f));
                    return 0;
                }
            case "uia": {
                    // uia <x> <y> —— UIA 命中测试：从命中的元素逐级向上打印属性
                    double x = double.Parse(args[1]), y = double.Parse(args[2]);
                    DumpUiaAt(x, y);
                    return 0;
                }
            case "uiabtn": {
                    DumpUiaTaskbar();
                    return 0;
                }
            case "uiaidents": {
                    DumpUiaIids();
                    return 0;
                }
            default:
                Console.WriteLine("未知命令");
                return 1;
        }
    }

    static void DumpUiaAt(double x, double y) {
        var pt = new System.Windows.Point(x, y);
        var el = System.Windows.Automation.AutomationElement.FromPoint(pt);
        if (el == null) { Console.WriteLine("(no element)"); return; }
        DumpUiaUp(el, 0);
    }

    static void DumpUiaTaskbar() {        var root = System.Windows.Automation.AutomationElement.RootElement;
        var cond = new System.Windows.Automation.PropertyCondition(
            System.Windows.Automation.AutomationElement.ClassNameProperty, "Shell_TrayWnd");
        var tb = root.FindFirst(System.Windows.Automation.TreeScope.Children, cond);
        if (tb == null) { Console.WriteLine("no Shell_TrayWnd child"); return; }
        var btnCond = new System.Windows.Automation.PropertyCondition(
            System.Windows.Automation.AutomationElement.ControlTypeProperty,
            System.Windows.Automation.ControlType.Button);
        var buttons = tb.FindAll(System.Windows.Automation.TreeScope.Descendants, btnCond);
        Console.WriteLine("button count=" + buttons.Count);
        foreach (System.Windows.Automation.AutomationElement b in buttons) {
            var c = b.Current;
            var r = c.BoundingRectangle;
            Console.WriteLine($"name='{c.Name}' aid='{c.AutomationId}' class='{c.ClassName}' rect=({(int)r.X},{(int)r.Y})-({(int)(r.X + r.Width)},{(int)(r.Y + r.Height)})");
        }
    }

    static void DumpUiaUp(System.Windows.Automation.AutomationElement e, int depth) {
        if (e == null || depth > 8) return;
        var c = e.Current;
        var r = c.BoundingRectangle;
        Console.WriteLine($"{new string(' ', depth * 2)}type={c.ControlType.ProgrammaticName} name='{c.Name}' aid='{c.AutomationId}' class='{c.ClassName}' frame='{c.FrameworkId}' rect=({(int)r.X},{(int)r.Y})-({(int)(r.X + r.Width)},{(int)(r.Y + r.Height)})");
        var p = System.Windows.Automation.TreeWalker.ControlViewWalker.GetParent(e);
        DumpUiaUp(p, depth + 1);
    }

    static bool InvokePinVerb(string lnkPath, bool unpin) {
        try {
            if (!System.IO.File.Exists(lnkPath)) { Console.WriteLine("lnk not found: " + lnkPath); return false; }
            string dir = System.IO.Path.GetDirectoryName(lnkPath);
            string name = System.IO.Path.GetFileName(lnkPath);
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic folder = shell.Namespace(dir);
            dynamic item = folder.ParseName(name);
            bool done = false;
            foreach (dynamic v in item.Verbs()) {
                string n = (v.Name ?? "").Replace("&", "");
                Console.WriteLine("verb: " + n);
                if (done) continue;
                if (!unpin) {
                    if ((n.Contains("任务栏") && n.Contains("固定") && !n.Contains("取消"))
                        || n.Equals("TaskbarPin", StringComparison.OrdinalIgnoreCase)
                        || n.Contains("Pin to taskbar")) {
                        v.DoIt(); done = true;
                    }
                }
                else {
                    if ((n.Contains("任务栏") && (n.Contains("取消") || n.Contains("从任务栏")))
                        || n.Equals("TaskbarUnpin", StringComparison.OrdinalIgnoreCase)
                        || n.Contains("Unpin from taskbar")) {
                        v.DoIt(); done = true;
                    }
                }
            }
            return done;
        }
        catch (Exception ex) { Console.WriteLine("verb error: " + ex.Message); return false; }
    }

    static void DumpUiaIids() {
        var asm = typeof(System.Windows.Automation.AutomationElement).Assembly;
        Console.WriteLine("asm=" + asm.Location);
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (System.Reflection.ReflectionTypeLoadException ex) { types = ex.Types; Console.WriteLine("(partial load)"); }
        foreach (var t in types) {
            if (t == null) continue;
            if (t.FullName != null && t.FullName.Contains("IUIAutomation")) {
                var ga = (System.Runtime.InteropServices.GuidAttribute)System.Attribute.GetCustomAttribute(t, typeof(System.Runtime.InteropServices.GuidAttribute));
                Console.WriteLine($"{t.FullName} iid={ga?.Value}");
                var ms = t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                for (int i = 0; i < ms.Length; i++)
                    Console.WriteLine($"  [{i}] {ms[i].ReturnType.Name} {ms[i].Name}({string.Join(", ", Array.ConvertAll(ms[i].GetParameters(), p => p.ParameterType.Name + " " + p.Name))})");
            }
        }
    }

    // IShellLinkW 对象实现 IPersistFile, 而 property store 要走 IShellLink 上的 IPropertyStore 接口 (QI)
    static IPropertyStore GetLinkPropertyStore(string lnkPath, bool writable) {
        var link = (IShellLinkW)new CShellLink();
        ((IPersistFile)link).Load(lnkPath, 0);
        // STGM: 0x00000001 = STGM_READ? 用 QI 拿 IPropertyStore
        var iid = IID_IPropertyStore;
        int hr = Marshal.QueryInterface(Marshal.GetIUnknownForObject(link), ref iid, out IntPtr pStore);
        if (hr != 0) return null;
        var store = (IPropertyStore)Marshal.GetObjectForIUnknown(pStore);
        Marshal.Release(pStore);
        return store;
    }
}
