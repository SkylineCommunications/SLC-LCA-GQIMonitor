using System;
using System.Diagnostics;
using System.IO;

namespace GQIMonitor
{
    public interface IGQIProvider
    {
        string LogPath { get; }
        string MetricsPath { get; }

        void TakeSnapshot(string snapshotPath);
    }

    public static class GQIProviders
    {
        public static readonly IGQIProvider SLHelper = new GQISLHelperProvider();
        public static readonly IGQIProvider DxM = new GQIDxMProvider();

        public static IGQIProvider GetCurrent()
        {
            var currentProcess = Process.GetCurrentProcess();
            if (currentProcess.ProcessName == "SLHelper")
                return SLHelper;

            return DxM;
        }

        private sealed class GQISLHelperProvider : IGQIProvider
        {
            public string LogPath => @"C:\Skyline DataMiner\Logging\GQI";
            public string MetricsPath => @"C:\Skyline DataMiner\Logging\GQI\Metrics";

            public void TakeSnapshot(string snapshotPath)
            {
                var logSnapshotPath = Path.Combine(snapshotPath, "SLHelper");
                FileSystem.CopyDirectory(LogPath, logSnapshotPath);
            }
        }

        private sealed class GQIDxMProvider : IGQIProvider
        {
            public string LogPath => @"C:\ProgramData\Skyline Communications\DataMiner GQI";
            public string MetricsPath => @"C:\ProgramData\Skyline Communications\DataMiner GQI\Metrics";

            public void TakeSnapshot(string snapshotPath)
            {
                // TODO
                throw new NotSupportedException("Taking a snapshot from GQI DxM logging is not supported yet.");
            }
        }
    }
}
