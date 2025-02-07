using Newtonsoft.Json.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MetricsDataSource_1
{
    internal interface IMetricCollection
    {
        IEnumerable<RequestDurationMetric> RequestDurations { get; }
        IEnumerable<FirstPageDurationMetric> FirstPageDurations { get; }
        IEnumerable<AllPagesDurationMetric> AllPagesDurations { get; }
    }

    internal sealed class MetricCollection : IMetricCollection
    {
        public static MetricCollection Parse(string directoryPath, IGQILogger logger)
        {
            logger.Information($"Parsing metrics in directory \"{directoryPath}\"");
            var collection = new MetricCollection();

            if (!Directory.Exists(directoryPath))
            {
                logger.Information($"Metrics directory \"{directoryPath}\" does not exist");
                return collection;
            }

            var filePaths = Directory.GetFiles(directoryPath, "*.txt");
            logger.Information($"Found {filePaths.Length} metric files");

            foreach (var filePath in filePaths)
            {
                collection.AddMetricFile(filePath, logger);
            }

            return collection;
        }

        public IEnumerable<RequestDurationMetric> RequestDurations => _requestDurations;
        public IEnumerable<FirstPageDurationMetric> FirstPageDurations => _firstPageDurations;
        public IEnumerable<AllPagesDurationMetric> AllPagesDurations => _allPagesDurations;

        private List<RequestDurationMetric> _requestDurations;
        private List<FirstPageDurationMetric> _firstPageDurations;
        private List<AllPagesDurationMetric> _allPagesDurations;

        private MetricCollection()
        {
            _requestDurations = new List<RequestDurationMetric>();
            _firstPageDurations = new List<FirstPageDurationMetric>();
            _allPagesDurations = new List<AllPagesDurationMetric>();
        }

        private void AddMetricFile(string filePath, IGQILogger logger)
        {
            var fileName = Path.GetFileName(filePath);
            logger.Information($"Parsing metrics from \"{fileName}\"");

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line is null)
                            break;
                        AddMetricLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new GenIfException($"Failed parsing metrics from \"{fileName}\": {ex.Message}");
            }
        }

        private void AddMetricLine(string line)
        {
            try
            {
                var jsonObject = JObject.Parse(line);
                var metricType = jsonObject["Metric"].Value<string>();

                switch (metricType)
                {
                    case "RequestDuration":
                        _requestDurations.Add(jsonObject.ToObject<RequestDurationMetric>());
                        break;
                    case "FirstPageDuration":
                        _firstPageDurations.Add(jsonObject.ToObject<FirstPageDurationMetric>());
                        break;
                    case "AllPagesDuration":
                        _allPagesDurations.Add(jsonObject.ToObject<AllPagesDurationMetric>());
                        break;
                }
            }
            catch { /* Ignore line */ }
        }
    }

    internal sealed class CombinedMetricCollection : IMetricCollection
    {
        public IEnumerable<RequestDurationMetric> RequestDurations => _requestDurations;

        public IEnumerable<FirstPageDurationMetric> FirstPageDurations => _firstPageDurations;

        public IEnumerable<AllPagesDurationMetric> AllPagesDurations => _allPagesDurations;

        private readonly RequestDurationMetric[] _requestDurations;
        private readonly FirstPageDurationMetric[] _firstPageDurations;
        private readonly AllPagesDurationMetric[] _allPagesDurations;

        public CombinedMetricCollection(params IMetricCollection[] collections)
        {
            _requestDurations = collections
                .SelectMany(c => c.RequestDurations)
                .OrderBy(m => m.Time)
                .ToArray();
            _firstPageDurations = collections
                .SelectMany(c => c.FirstPageDurations)
                .OrderBy(m => m.Time)
                .ToArray();
            _allPagesDurations = collections
                .SelectMany(c => c.AllPagesDurations)
                .OrderBy(m => m.Time)
                .ToArray();
        }
    }
}
