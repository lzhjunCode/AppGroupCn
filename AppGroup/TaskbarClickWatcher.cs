using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppGroup {
    /// <summary>
    /// 监听“任务栏上的左键按下”，如果点在某个分组的固定图标上，就通知弹层打开该分组。
    /// 只处理“分组里已经有运行窗口”的情况——没运行时 Windows 本来就会启动分组快捷方式，
    /// 由 Program/App 的常规流程弹层；有运行时 Windows 只激活窗口，没人告诉我们点击事件。
    /// </summary>
    internal static class TaskbarClickWatcher {

        private static readonly object _sync = new object();
        private static DateTime _lastHandledUtc = DateTime.MinValue;
        private const int DebounceMs = 400;

        /// <summary>由弹层的全局鼠标钩子在 WM_LBUTTONDOWN 时调用（UI 线程，必须快速返回）。</summary>
        public static void OnMouseDown(NativeMethods.POINT pt) {
            try {
                bool onTaskbar = IsPointOnTaskbar(pt);
                lock (_sync) {
                    if ((DateTime.UtcNow - _lastHandledUtc).TotalMilliseconds < DebounceMs) return;
                    _lastHandledUtc = DateTime.UtcNow;
                }
                TaskbarDebugLog.Write($"mouse down ({pt.X},{pt.Y}) onTaskbar={onTaskbar}");
                if (!onTaskbar) return;

                _ = Task.Run(async () => {
                    try {
                        // 给 Windows 一点时间完成窗口激活/还原，弹层再叠上去
                        await Task.Delay(140);
                        string group = TaskbarUia.IdentifyGroupAt(pt);
                        TaskbarDebugLog.Write($"click ({pt.X},{pt.Y}) identified='{group ?? "(null)"}'");
                        if (string.IsNullOrEmpty(group)) return;
                        bool hasWindow = GroupHasRunningWindow(group);
                        TaskbarDebugLog.Write($"group '{group}' hasRunningWindow={hasWindow}");
                        if (!hasWindow) return;
                        PopupWindow.ShowGroupFromTaskbar(group, pt);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine($"[TaskbarClick] detect failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex) {
                Debug.WriteLine($"[TaskbarClick] hook failed: {ex.Message}");
            }
        }

        private static bool IsPointOnTaskbar(NativeMethods.POINT pt) {
            IntPtr main = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (main != IntPtr.Zero && Contains(main, pt)) return true;

            IntPtr prev = IntPtr.Zero;
            while (true) {
                IntPtr next = NativeMethods.FindWindowEx(IntPtr.Zero, prev, "Shell_SecondaryTrayWnd", null);
                if (next == IntPtr.Zero) break;
                if (Contains(next, pt)) return true;
                prev = next;
            }
            return false;
        }

        private static bool Contains(IntPtr hwnd, NativeMethods.POINT pt) {
            if (!NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT r)) return false;
            return pt.X >= r.left && pt.X < r.right && pt.Y >= r.top && pt.Y < r.bottom;
        }

        /// <summary>该分组里是否已经有一个带窗口的进程在跑。</summary>
        private static bool GroupHasRunningWindow(string groupName) {
            try {
                var group = FindGroup(groupName);
                if (group?.Path == null) return false;
                foreach (string path in group.Path.Keys) {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    if (path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)) continue;
                    string exe = RunningInstanceHelper.ResolveExecutablePath(path);
                    if (string.IsNullOrEmpty(exe)) continue;
                    string name = Path.GetFileNameWithoutExtension(exe);
                    if (string.IsNullOrEmpty(name)) continue;
                    foreach (Process p in Process.GetProcessesByName(name)) {
                        try {
                            if (p.MainWindowHandle != IntPtr.Zero && NativeMethods.IsWindowVisible(p.MainWindowHandle))
                                return true;
                        }
                        finally { p.Dispose(); }
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"[TaskbarClick] running check failed: {ex.Message}");
            }
            return false;
        }

        private static GroupData FindGroup(string groupName) {
            try {
                string json = JsonConfigHelper.ReadJsonFromFile(JsonConfigHelper.GetDefaultConfigPath());
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var groups = JsonSerializer.Deserialize<Dictionary<string, GroupData>>(json, options);
                if (groups == null) return null;
                foreach (var kv in groups) {
                    if (string.Equals(kv.Value.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"[TaskbarClick] read config failed: {ex.Message}");
            }
            return null;
        }
    }
}
