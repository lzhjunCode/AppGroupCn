using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AppGroup {
    public class SystemTrayManager {
        private static IntPtr _hwnd = IntPtr.Zero;
        private static IntPtr _hIcon = IntPtr.Zero;
        private static IntPtr _hMenu = IntPtr.Zero;
        private static NativeMethods.WndProcDelegate _wndProcDelegate;
        private static Action _onShowCallback;
        private static Action _onExitCallback;
        private static bool _isInitialized = false;
        private static bool _isVisible = false;
        private static int WM_TASKBARCREATED;
        private const string WndClassName = "WinUI3AppGroupTrayWndClass";
        private const uint ID_GROUP_BASE = 4000;   // “打开分组：<名称>”菜单项 ID 起点
        private const uint MF_SEPARATOR = 0x800;

        // ── Public API ────────────────────────────────────────────────────────

        public static void Initialize(Action showCallback, Action exitCallback) {
            _onShowCallback = showCallback;
            _onExitCallback = exitCallback;
            _isInitialized = true;

            WM_TASKBARCREATED = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            _ = InitializeBasedOnSettingsAsync();
        }

        private static async System.Threading.Tasks.Task InitializeBasedOnSettingsAsync() {
            try {
                var settings = await SettingsHelper.LoadSettingsAsync();
                if (settings.ShowSystemTrayIcon)
                    ShowSystemTray();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error loading settings for system tray: {ex.Message}");
                ShowSystemTray();
            }
        }

        public static void ShowSystemTray() {
            if (!_isInitialized) return;
            if (_isVisible) return;

            EnsureWindow();
            CreateTrayIcon();
            _isVisible = true;
        }

        public static void HideSystemTray() {
            if (!_isVisible) return;

            // Remove icon only — keep window alive so message pump stays intact
            var nid = new NativeMethods.NOTIFYICONDATA {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1
            };
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref nid);

            if (_hIcon != IntPtr.Zero) {
                NativeMethods.DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }

            if (_hMenu != IntPtr.Zero) {
                NativeMethods.DestroyMenu(_hMenu);
                _hMenu = IntPtr.Zero;
            }

            _isVisible = false;
        }

        public static void Cleanup() {
            HideSystemTray();

            if (_hwnd != IntPtr.Zero) {
                NativeMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            _isVisible = false;
        }

        public static void UpdateTooltip(string tooltip) {
            if (_hwnd == IntPtr.Zero || !_isVisible) return;

            var nid = new NativeMethods.NOTIFYICONDATA {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NativeMethods.NIF_TIP,
                szTip = tooltip
            };
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref nid);
        }

        // ── Window ────────────────────────────────────────────────────────────

        private static void EnsureWindow() {
            if (_hwnd != IntPtr.Zero) return;

            // Create delegate fresh — keep reference in static field to prevent GC
            _wndProcDelegate = new NativeMethods.WndProcDelegate(WndProc);

            var wc = new NativeMethods.WNDCLASSEX {
                cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.WNDCLASSEX)),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = NativeMethods.GetModuleHandle(null),
                hIcon = IntPtr.Zero,
                hCursor = NativeMethods.LoadCursor(IntPtr.Zero, 32512u),
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = WndClassName,
                hIconSm = IntPtr.Zero
            };

            NativeMethods.RegisterClassEx(ref wc);

            _hwnd = NativeMethods.CreateWindowEx(
                0, WndClassName, "WinUI3 AppGroup Tray Window",
                0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero,
                NativeMethods.GetModuleHandle(null), IntPtr.Zero);
        }

        // ── Icon & Menu ───────────────────────────────────────────────────────

        private static void CreateTrayIcon() {
            if (_hIcon == IntPtr.Zero) {
                int w = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSMICON);
                int h = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSMICON);

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppGroup.ico");
                if (File.Exists(iconPath)) {
                    _hIcon = NativeMethods.LoadImage(IntPtr.Zero, iconPath,
                        NativeMethods.IMAGE_ICON, w, h, NativeMethods.LR_LOADFROMFILE);
                }
                if (_hIcon == IntPtr.Zero) {
                    _hIcon = NativeMethods.LoadImage(IntPtr.Zero, "#32516",
                        NativeMethods.IMAGE_ICON, w, h, 0);
                }
            }

            var nid = new NativeMethods.NOTIFYICONDATA {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
                uCallbackMessage = NativeMethods.WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "AppGroup"
            };

            bool ok = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref nid);
            if (!ok) {
                // May already exist — try modify
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref nid);
            }

            // Create menu fresh each time
            if (_hMenu == IntPtr.Zero) {
                _hMenu = NativeMethods.CreatePopupMenu();
                NativeMethods.AppendMenu(_hMenu, 0, (uint)NativeMethods.ID_SHOW, "显示");
                NativeMethods.AppendMenu(_hMenu, 0, (uint)NativeMethods.ID_EXIT, "退出");
            }
        }

        // ── WndProc ───────────────────────────────────────────────────────────

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            if ((int)msg == WM_TASKBARCREATED && _isVisible) {
                // Explorer restarted — recreate icon
                _hIcon = IntPtr.Zero;
                CreateTrayIcon();
                return IntPtr.Zero;
            }

            switch (msg) {
                case NativeMethods.WM_TRAYICON:
                    HandleTrayIconMessage(lParam);
                    break;

                case NativeMethods.WM_COMMAND:
                    HandleMenuCommand(wParam.ToInt32() & 0xFFFF);
                    break;
            }

            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private static void HandleTrayIconMessage(IntPtr lParam) {
            switch (lParam.ToInt32()) {
                case (int)NativeMethods.WM_LBUTTONDBLCLK:
                    _onShowCallback?.Invoke();
                    break;

                case (int)NativeMethods.WM_RBUTTONUP:
                    ShowContextMenu();
                    break;
            }
        }

        private static void HandleMenuCommand(int command) {
            // “打开分组：<名称>”：重新走一遍任务栏快捷方式的启动路径（AppGroup.exe "<组名>"），
            // 已在运行的老进程会收到 WM_COPYDATA 并把弹层弹出来
            if (command >= (int)ID_GROUP_BASE && command < (int)ID_GROUP_BASE + 64) {
                int idx = command - (int)ID_GROUP_BASE;
                var groups = GetGroupNames();
                if (idx >= 0 && idx < groups.Count) {
                    try {
                        string exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                        if (!string.IsNullOrEmpty(exe)) {
                            Process.Start(new ProcessStartInfo {
                                FileName = exe,
                                Arguments = "\"" + groups[idx] + "\"",
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine($"[Tray] open group popup failed: {ex.Message}"); }
                }
                return;
            }

            switch (command) {
                case NativeMethods.ID_SHOW:
                    _onShowCallback?.Invoke();
                    break;

                case NativeMethods.ID_EXIT:
                    _onExitCallback?.Invoke();
                    break;
            }
        }

        private static void ShowContextMenu() {
            if (_hMenu == IntPtr.Zero) return;

            RebuildMenu();

            NativeMethods.GetCursorPos(out NativeMethods.POINT pt);
            NativeMethods.SetForegroundWindow(_hwnd);

            uint result = NativeMethods.TrackPopupMenu(
                _hMenu,
                NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_RIGHTBUTTON,
                pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);

            NativeMethods.PostMessage(_hwnd, NativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero);

            if (result != 0)
                HandleMenuCommand((int)result);
        }

        /// <summary>每次打开菜单都重建：把“打开分组：<名称>”项加进去。
        /// 应用窗口并入分组图标后，点任务栏图标只会激活窗口，托盘菜单是重新打开弹层的入口。</summary>
        private static void RebuildMenu() {
            if (_hMenu != IntPtr.Zero) {
                NativeMethods.DestroyMenu(_hMenu);
                _hMenu = IntPtr.Zero;
            }
            _hMenu = NativeMethods.CreatePopupMenu();
            NativeMethods.AppendMenu(_hMenu, 0, (uint)NativeMethods.ID_SHOW, "显示");

            var groups = GetGroupNames();
            if (groups.Count > 0) {
                NativeMethods.AppendMenu(_hMenu, MF_SEPARATOR, 0, null);
                for (int i = 0; i < groups.Count; i++)
                    NativeMethods.AppendMenu(_hMenu, 0, ID_GROUP_BASE + (uint)i, "打开分组：" + groups[i]);
            }

            NativeMethods.AppendMenu(_hMenu, MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(_hMenu, 0, (uint)NativeMethods.ID_EXIT, "退出");
        }

        private static System.Collections.Generic.List<string> GetGroupNames() {
            var list = new System.Collections.Generic.List<string>();
            try {
                string path = Path.Combine(AppPaths.BaseDataPath, "appgroups.json");
                if (!File.Exists(path)) return list;
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
                foreach (var prop in doc.RootElement.EnumerateObject()) {
                    string name = prop.Value.TryGetProperty("groupName", out var gn) ? gn.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(name)) list.Add(name);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[Tray] GetGroupNames failed: {ex.Message}"); }
            return list;
        }
    }
}