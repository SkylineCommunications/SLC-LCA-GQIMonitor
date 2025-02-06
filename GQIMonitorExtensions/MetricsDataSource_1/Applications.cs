using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.IO;
using System.Linq;

namespace MetricsDataSource_1
{
    internal static class Applications
    {
        public static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIStringColumn("ID"),
            new GQIStringColumn("Name"),
        };

        private const string FilePath = @"C:\Skyline DataMiner\Documents\GQI Monitor\applications.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
        };

        public static GQIRow[] GetApplications(IGQILogger logger)
        {
            try
            {
                var json = GetJsonFromFile(FilePath, logger);
                if (json is null)
                    return Array.Empty<GQIRow>();

                var apiResult = JsonConvert.DeserializeObject<APIResult<ApplicationCollection>>(json, SerializerSettings);
                return GetRows(apiResult);
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve applications.", ex);
            }
        }

        private static string GetJsonFromFile(string filePath, IGQILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.Warning($"Applications file \"{FilePath}\" does not exist");
                return null;
            }

            return File.ReadAllText(filePath);
        }

        private static GQIRow[] GetRows(APIResult<ApplicationCollection> apiResult)
        {
            var applications = apiResult?.Value.Applications;
            if (applications is null)
                return Array.Empty<GQIRow>();

            return applications
                .Select(ToRow)
                .ToArray();
        }

        private static GQIRow ToRow(Application application)
        {
            var cells = new[]
            {
                new GQICell { Value = application.ID },
                new GQICell { Value = application.Name },
            };

            return new GQIRow(application.ID, cells);
        }

        private sealed class APIResult<T>
        {
            [JsonProperty("d")]
            public T Value { get; set; }
        }

        private sealed class ApplicationCollection
        {
            [JsonProperty("DynamicApplications")]
            public Application[] Applications { get; set; }
        }

        private sealed class Application
        {
            [JsonProperty("ID")]
            public string ID { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }
        }
    }
}
