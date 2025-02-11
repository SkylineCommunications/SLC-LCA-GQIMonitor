using Newtonsoft.Json;

namespace GQIMonitor
{
    public static class Info
    {
        public const string AppName = "GQI Monitor";
        public const string DocumentsPath = @"C:\Skyline DataMiner\Documents\GQI Monitor";

        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
        };
    }
}
