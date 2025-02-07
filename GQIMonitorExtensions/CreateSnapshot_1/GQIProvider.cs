using Skyline.DataMiner.Automation;
using System;
using System.IO;

namespace CreateSnapshot_1
{
    internal interface IGQIProvider
    {
        void TakeSnapshot(string snapshotPath);
    }

    internal static class GQIProviders
    {
        public static readonly IGQIProvider SLHelper = new GQISLHelperProvider();
        public static readonly IGQIProvider DxM = new GQIDxMProvider();

        private sealed class GQISLHelperProvider : IGQIProvider
        {
            private const string LogPath = @"C:\Skyline DataMiner\Logging\GQI";

            public void TakeSnapshot(string snapshotPath)
            {
                var logSnapshotPath = Path.Combine(snapshotPath, "SLHelper");
                FileSystem.CopyDirectory(LogPath, logSnapshotPath);
            }
        }

        private sealed class GQIDxMProvider : IGQIProvider
        {
            private const string GQI_DxM_LogPath = @"C:\ProgramData\Skyline Communications\DataMiner GQI";

            public void TakeSnapshot(string snapshotPath)
            {
                // TODO
                throw new NotSupportedException("Taking a snapshot from GQI DxM logging is not supported yet.");
            }
        }
    }
}
