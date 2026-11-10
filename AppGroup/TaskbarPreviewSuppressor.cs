using System;
using System.Collections.Generic;
using System.Text;

namespace AppGroup {
    /// <summary>
    /// 分组里有 ≥2 个运行窗口时，Win11 会在点/悬停分组图标时弹出“窗口缩略图预览”飞出层
    /// （explorer 的置顶 XamlIsland 窗口）。它正好盖在弹层的位置上，把弹层的点击全部拦截。
    /// 弹层打开期间，定时找到与弹层区域相交的预览飞出层并隐藏，保证弹层可点。
    /// 弹层收起后停止检测，不影响任务栏其它浮层（托盘溢出、音量等）。
    /// </summary>
    internal static class TaskbarPreviewSuppressor {

        private static Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;
        // Win11 任务栏浮层（缩略图预览等）宿主窗口类名
        private static readonly string[] FlyoutClasses = {
            "XamlExplorerHostIslandWindow",          // 分组图标点击/悬停时弹出的窗口预览飞出层（实测全屏大小）
            "TopLevelWindowForOverflowXamlIsland",
        };
        private static IntPtr _explorerPidCache = IntPtr.Zero;
        // 上一次心跳里压住弹层的窗口集合（去重，避免日志刷屏）
        private static string _lastCoveringKey;

        public static void Start() {
            if (_timer != null) return; // 已在运行
            _tickCount = 0;
            var queue = PopupWindow.Instance?.DispatcherQueue;
            if (queue == null) return;
            _timer = queue.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += (_, __) => Tick();
            _timer.Start();
            Tick(); // 立刻压一次：飞出层往往比弹层先出现
        }

        public static void Stop() {
            _timer?.Stop();
            _timer = null;
        }

        private static int _tickCount;

        private static void Tick() {
            try {
                if (!PopupWindow._isPopupCurrentlyOpen) return;
                IntPtr popup = PopupWindow.Instance == null ? IntPtr.Zero
                    : WinRT.Interop.WindowNative.GetWindowHandle(PopupWindow.Instance);
                if (popup == IntPtr.Zero || !NativeMethods.IsWindowVisible(popup)) return;

                // 弹层必须压在所有应用窗口之上（不抢焦点）：
                // Win11 点分组图标会激活组内窗口，激活的窗口会盖住重新显示的弹层
                NativeMethods.SetWindowPos(popup, new IntPtr(-1) /* HWND_TOPMOST */, 0, 0, 0, 0,
                    0x1 /* NOSIZE */ | 0x2 /* NOMOVE */ | 0x10 /* NOACTIVATE */);

                // Z 序验证：弹层之上若还有可见窗口（被应用窗口压住），记录现场便于定位。
                // 同一批覆盖窗口只记一次，避免 250ms 心跳刷屏
                IntPtr above = NativeMethods.GetWindow(popup, 3 /* GW_HWNDPREV：Z 序中位于弹层之上的窗口 */);
                if (!NativeMethods.GetWindowRect(popup, out NativeMethods.RECT zr)) return;
                var covering = new List<string>();
                while (above != IntPtr.Zero && covering.Count < 3) {
                    if (NativeMethods.IsWindowVisible(above) &&
                        NativeMethods.GetWindowRect(above, out NativeMethods.RECT ar) &&
                        Intersects(zr, ar, 0)) {
                        var csb = new StringBuilder(128);
                        NativeMethods.GetClassName(above, csb, 128);
                        NativeMethods.GetWindowThreadProcessId(above, out uint apid);
                        covering.Add($"hwnd={above} pid={apid} class='{csb}' rect=({ar.left},{ar.top})-({ar.right},{ar.bottom})");
                    }
                    above = NativeMethods.GetWindow(above, 3);
                }
                string coveringKey = string.Join("|", covering);
                if (covering.Count > 0 && coveringKey != _lastCoveringKey) {
                    TaskbarDebugLog.Write("popup covered by " + coveringKey);
                    _lastCoveringKey = coveringKey;
                }
                else if (covering.Count == 0) {
                    _lastCoveringKey = null;
                }

                // 弹层开着期间始终隐藏与其相交的预览飞出层。
                // 不能加“只压前 3 秒”的时间门：点弹层启动/激活应用后，explorer 随时可能
                // 重新显示全屏飞出层（XamlExplorerHostIslandWindow rect 覆盖全屏），正好压住弹层。
                // 误伤风险低：点弹层外任何地方都会先收起弹层（压制器随之停止），开始菜单等浮层不受影响。
                if (!NativeMethods.GetWindowRect(popup, out NativeMethods.RECT pr)) return;

                IntPtr explorerPid = GetExplorerPid();
                var victims = new List<IntPtr>();
                NativeMethods.EnumWindows((h, _) => {
                    var sb = new StringBuilder(128);
                    if (NativeMethods.GetClassName(h, sb, 128) <= 0) return true;
                    string cls = sb.ToString();
                    bool classMatch = false;
                    foreach (string c in FlyoutClasses) {
                        if (string.Equals(cls, c, StringComparison.Ordinal)) { classMatch = true; break; }
                    }
                    if (!classMatch) return true;
                    NativeMethods.GetWindowThreadProcessId(h, out uint pid);
                    if (!NativeMethods.IsWindowVisible(h)) return true;
                    if ((IntPtr)pid != explorerPid) return true;
                    if (!NativeMethods.GetWindowRect(h, out NativeMethods.RECT fr)) return true;
                    if (!Intersects(pr, fr, 60)) return true;
                    victims.Add(h);
                    return true;
                }, IntPtr.Zero);

                foreach (IntPtr h in victims) {
                    // SW_HIDE=0：只隐藏，explorer 下次悬停会自己重建，无破坏性
                    NativeMethods.ShowWindow(h, 0);
                    TaskbarDebugLog.Write($"suppressed preview flyout hwnd={h}");
                }
            }
            catch (Exception ex) {
                TaskbarDebugLog.Write($"suppressor error: {ex.Message}");
            }
        }

        private static bool Intersects(NativeMethods.RECT a, NativeMethods.RECT b, int inflate) {
            return a.left - inflate < b.right && a.right + inflate > b.left &&
                   a.top - inflate < b.bottom && a.bottom + inflate > b.top;
        }

        private static IntPtr GetExplorerPid() {
            if (_explorerPidCache != IntPtr.Zero) return _explorerPidCache;
            IntPtr tray = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (tray != IntPtr.Zero) {
                NativeMethods.GetWindowThreadProcessId(tray, out uint pid);
                if (pid != 0) { _explorerPidCache = (IntPtr)pid; return _explorerPidCache; }
            }
            return IntPtr.Zero;
        }
    }
}
