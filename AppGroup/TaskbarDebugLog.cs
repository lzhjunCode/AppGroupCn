using System;
using System.IO;

namespace AppGroup {
    /// <summary>
    /// 任务栏点击识别链路的调试日志（%LOCALAPPDATA%\AppGroup\taskbar-click.log）。
    /// 量很小（只在真实点击任务栏时写一行），方便现场排查“点了图标但弹错分组”。
    /// </summary>
    internal static class TaskbarDebugLog {
        private const long MaxBytes = 256 * 1024;

        public static void Write(string message) {
            try {
                string dir = AppPaths.BaseDataPath;
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "taskbar-click.log");
                try {
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > MaxBytes) File.WriteAllText(path, "");
                }
                catch { }
                File.AppendAllText(path,
                    $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
