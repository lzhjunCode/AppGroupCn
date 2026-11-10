using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace AppGroup {
    /// <summary>
    /// 让应用窗口归到所属分组的任务栏图标下面：
    /// 1) 分组自己的 .lnk（以及任务栏已固定的副本）写入 AppUserModelID = AppGroup.Group.&lt;分组名&gt;；
    /// 2) 从弹层启动应用时，改写走一个带相同 AUMID 的启动快捷方式，
    ///    Windows 会把该应用的窗口并进分组图标，而不是新增按钮。
    /// 注意：应用自身若显式设置过 AUMID（如 Chrome），会覆盖快捷方式继承值，此时仍会独立显示。
    /// </summary>
    internal static class GroupAumidHelper {
        private const string AumidPrefix = "AppGroup.Group.";

        public static string GetGroupAumid(string groupName) {
            if (string.IsNullOrWhiteSpace(groupName)) return null;
            var sb = new StringBuilder();
            foreach (char c in groupName)
                if (!char.IsControl(c)) sb.Append(c);
            string clean = sb.ToString().Trim();
            return clean.Length == 0 ? null : AumidPrefix + clean;
        }

        /// <summary>写入分组快捷方式及任务栏固定副本的 AUMID（幂等）。</summary>
        public static void EnsureGroupShortcutAumid(string groupName) {
            try {
                string aumid = GetGroupAumid(groupName);
                if (aumid == null) return;

                string lnk = Path.Combine(AppPaths.BaseDataPath, "Groups", groupName, groupName + ".lnk");
                if (File.Exists(lnk)) ShellInterop.SetShortcutAumid(lnk, aumid);

                EnsurePinnedShortcutAumid(groupName, aumid);
            }
            catch (Exception ex) { Debug.WriteLine($"[Aumid] EnsureGroupShortcutAumid({groupName}) failed: {ex.Message}"); }
        }

        /// <summary>任务栏“固定”目录里的副本必须写同一个 AUMID，窗口才会并到固定按钮上。</summary>
        public static void EnsurePinnedShortcutAumid(string groupName, string aumid) {
            try {
                string taskbarPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                if (!Directory.Exists(taskbarPath)) return;

                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                bool changed = false;
                IWshShell wsh = new WshShell();
                try {
                    foreach (string file in Directory.EnumerateFiles(taskbarPath, "*.lnk")) {
                        try {
                            IWshShortcut sc = (IWshShortcut)wsh.CreateShortcut(file);
                            string target = sc.TargetPath ?? "";
                            if (!target.Equals(exePath, StringComparison.OrdinalIgnoreCase)) continue;

                            string arguments = sc.Arguments ?? "";
                            string description = sc.Description ?? "";
                            bool ours = arguments.Contains($"\"{groupName}\"")
                                        || description.Equals($"{groupName} - AppGroup Shortcut", StringComparison.OrdinalIgnoreCase);
                            if (!ours) continue;

                            if (ShellInterop.SetShortcutAumid(file, aumid)) changed = true;
                        }
                        catch { }
                    }
                }
                finally { Marshal.ReleaseComObject(wsh); }

                if (changed) {
                    TaskbarManager.TryRefreshTaskbarWithoutRestartAsync();
                    Debug.WriteLine($"[Aumid] pinned shortcut AUMID updated for '{groupName}'");
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[Aumid] EnsurePinnedShortcutAumid({groupName}) failed: {ex.Message}"); }
        }

        /// <summary>启动时同步所有分组的 AUMID（兼容已存在的分组/已固定图标）。</summary>
        public static void SyncAll() {
            try {
                string groupsDir = Path.Combine(AppPaths.BaseDataPath, "Groups");
                if (!Directory.Exists(groupsDir)) return;
                foreach (string dir in Directory.GetDirectories(groupsDir))
                    EnsureGroupShortcutAumid(Path.GetFileName(dir));
            }
            catch (Exception ex) { Debug.WriteLine($"[Aumid] SyncAll failed: {ex.Message}"); }
        }

        /// <summary>
        /// 生成（或复用）带分组 AUMID 的启动快捷方式。
        /// 返回 null 表示不适用（UWP/shell: 项、目标 exe 不存在等），调用方按原路径启动。
        /// </summary>
        public static string GetOrCreateLaunchShortcut(string groupName, string appPath, string args) {
            try {
                string aumid = GetGroupAumid(groupName);
                if (aumid == null || string.IsNullOrWhiteSpace(appPath)) return null;
                if (appPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)) return null; // UWP 走身份启动，无法改写

                string exe = RunningInstanceHelper.ResolveExecutablePath(appPath);
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return null;

                string folder = Path.Combine(AppPaths.BaseDataPath, "LaunchShortcuts");
                Directory.CreateDirectory(folder);
                string key = Hash($"{aumid}|{exe}|{args}");
                string lnkPath = Path.Combine(folder, key + ".lnk");

                if (File.Exists(lnkPath) && ShellInterop.GetShortcutAumid(lnkPath) == aumid)
                    return lnkPath;
                var link = (ShellInterop.IShellLinkW)new ShellInterop.CShellLink();
                try {
                    link.SetPath(exe);
                    if (!string.IsNullOrWhiteSpace(args)) link.SetArguments(args);
                    string dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(dir)) link.SetWorkingDirectory(dir);
                    link.SetDescription(Path.GetFileNameWithoutExtension(exe));
                    ((ShellInterop.IPersistFile)link).Save(lnkPath, true);
                }
                finally { Marshal.ReleaseComObject(link); }

                ShellInterop.SetShortcutAumid(lnkPath, aumid);

                // 校验：写不进去（某些环境属性处理器不可用）就清理掉，回退为直接启动
                if (ShellInterop.GetShortcutAumid(lnkPath) != aumid) {
                    try { File.Delete(lnkPath); } catch { }
                    return null;
                }
                return lnkPath;
            }
            catch (Exception ex) {
                Debug.WriteLine($"[Aumid] GetOrCreateLaunchShortcut({appPath}) failed: {ex.Message}");
                return null;
            }
        }

        private static string Hash(string s) {
            using var sha = System.Security.Cryptography.SHA1.Create();
            byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(b).Replace("-", "").Substring(0, 16).ToLowerInvariant();
        }
    }
}
