using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AppGroup {
    /// <summary>
    /// 从分组打开应用后去掉它自己的任务栏按钮（ITaskbarList.DeleteTab），
    /// 让任务栏只剩分组图标；窗口仍可正常使用，切换走分组弹层（会还原并切到前台）。
    /// </summary>
    internal static class TaskbarButtonHelper {

        [ComImport, Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
        private class CTaskbarList { }

        [ComImport, Guid("56FDF342-FD6D-11D0-958A-006097C9A090"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
        }

        /// <summary>启动前快照：记录该 exe 当前已有的进程 ID。</summary>
        public static HashSet<int> SnapshotProcessIds(string exePath) {
            var ids = new HashSet<int>();
            try {
                string name = Path.GetFileNameWithoutExtension(exePath ?? string.Empty);
                if (string.IsNullOrEmpty(name)) return ids;
                foreach (Process p in Process.GetProcessesByName(name)) {
                    try { ids.Add(p.Id); } finally { p.Dispose(); }
                }
            }
            catch { }
            return ids;
        }

        /// <summary>删除指定窗口的任务栏按钮。</summary>
        public static void DeleteTab(IntPtr hwnd) {
            if (hwnd == IntPtr.Zero) return;
            try {
                var t = new Thread(() => {
                    ITaskbarList list = null;
                    try {
                        list = (ITaskbarList)new CTaskbarList();
                        list.HrInit();
                        list.DeleteTab(hwnd);
                    }
                    catch (Exception ex) { Debug.WriteLine($"[Taskbar] DeleteTab failed: {ex.Message}"); }
                    finally { if (list != null) Marshal.ReleaseComObject(list); }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                t.Join(3000);
            }
            catch (Exception ex) { Debug.WriteLine($"[Taskbar] DeleteTab thread failed: {ex.Message}"); }
        }

        /// <summary>等待本次启动产生的新进程主窗口出现，然后删掉它的任务栏按钮。</summary>
        public static void HideNewWindowAsync(string exePath, HashSet<int> pidsBefore, int timeoutMs = 25000) {
            if (string.IsNullOrEmpty(exePath)) return;
            string name = Path.GetFileNameWithoutExtension(exePath);
            if (string.IsNullOrEmpty(name)) return;

            _ = Task.Run(async () => {
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs) {
                    try {
                        foreach (Process p in Process.GetProcessesByName(name)) {
                            try {
                                if (pidsBefore != null && pidsBefore.Contains(p.Id)) continue;
                                IntPtr h = p.MainWindowHandle;
                                if (h != IntPtr.Zero && NativeMethods.IsWindowVisible(h)) {
                                    DeleteTab(h);
                                    return;
                                }
                            }
                            finally { p.Dispose(); }
                        }
                    }
                    catch { }
                    await Task.Delay(250);
                }
            });
        }
    }
}
