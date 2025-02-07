using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsDataSource_1
{
    internal static class GQIMonitor
    {
        public const string AppName = "GQI Monitor";
        public const string DocumentsPath = @"C:\Skyline DataMiner\Documents\GQI Monitor";

        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
        };
    }
}
