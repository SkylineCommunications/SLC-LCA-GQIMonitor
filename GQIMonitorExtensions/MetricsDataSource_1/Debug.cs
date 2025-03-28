﻿using System.IO;

namespace MetricsDataSource_1
{
    internal static class Debug
    {
        private const string LogFilePath = GQIMonitor.Info.DocumentsPath + @"\debug.txt";

        public static void Log(params string[] messages)
        {
            try
            {
                File.AppendAllLines(LogFilePath, messages);
            }
            catch { }
        }
    }
}
