using System;
using System.Diagnostics;
using System.IO;

namespace AppGroup {
    /// <summary>
    /// 已运行实例激活：点击应用时若目标 exe 已在运行，则把它的主窗口切到前台，
    /// 不再启动新进程，避免任务栏出现重复图标（类似 Chrome 点已固定的图标只聚焦窗口）。
    /// </summary>
    internal static class RunningInstanceHelper {

        /// <summary>尝试激活已运行的实例。返回 true 表示已激活，调用方无需再启动新进程。</summary>
        public static bool TryActivateRunningInstance(string launchPath) =>
            TryActivateRunningInstance(launchPath, out _);

        /// <summary>尝试激活已运行的实例，并回传被激活的窗口句柄。</summary>
        public static bool TryActivateRunningInstance(string launchPath, out IntPtr activatedWindow) {
            activatedWindow = IntPtr.Zero;
            try {
                string? exePath = ResolveExecutablePath(launchPath);
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return false;

                string processName = Path.GetFileNameWithoutExtension(exePath);
                if (string.IsNullOrEmpty(processName)) return false;

                foreach (Process p in Process.GetProcessesByName(processName)) {
                    try {
                        string? procPath = null;
                        try { procPath = p.MainModule?.FileName; }
                        catch { /* 提权进程读不到路径，退化为按进程名匹配 */ }

                        if (procPath != null && !string.Equals(procPath, exePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        IntPtr hWnd = p.MainWindowHandle;
                        if (hWnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(hWnd))
                            continue;

                        if (NativeMethods.IsIconic(hWnd))
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

                        NativeMethods.ForceForegroundWindow(hWnd);
                        activatedWindow = hWnd;
                        return true;
                    }
                    finally {
                        p.Dispose();
                    }
                }
            }
            catch { /* 激活失败则照常走启动流程 */ }
            return false;
        }

        /// <summary>解析 .lnk 指向的目标 exe；普通路径原样返回。</summary>
        public static string? ResolveExecutablePath(string path) {
            try {
                if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
                    return path;

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(path);
                string? target = shortcut.TargetPath as string;
                return string.IsNullOrEmpty(target) ? null : target;
            }
            catch { return null; }
        }
    }
}
