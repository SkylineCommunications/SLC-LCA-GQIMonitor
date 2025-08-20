using System;
using System.IO;
using System.Text;

namespace GQI
{
    internal static class Logger
    {
        // TODO: should not be in Documents
        private const string LogFilePath = GQIMonitor.Info.DocumentsPath + @"\debug.txt";

        public static void Log(params string[] messages)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                using (var stream = File.Open(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    foreach (var message in messages)
                    {
                        var logEntry = $"[{timestamp}] {message}";
                        writer.WriteLine(logEntry);
                    }
                }
            }
            catch { }
        }
    }
}
