using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using System.IO;

namespace GQIMonitor
{
    public static class Applications
    {
        public static ApplicationCollection ReadFromFile(string filePath, IGQILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.Warning($"Applications file \"{filePath}\" does not exist");
                return null;
            }

            var json = File.ReadAllText(filePath);
            var apiResult = JsonConvert.DeserializeObject<ApplicationCollection>(json, Info.JsonSerializerSettings);
            return apiResult;
        }

        public static void WriteToFile(string filePath, ApplicationCollection applications)
        {
            var json = JsonConvert.SerializeObject(applications, Info.JsonSerializerSettings);
            File.WriteAllText(filePath, json);
        }
    }
}
