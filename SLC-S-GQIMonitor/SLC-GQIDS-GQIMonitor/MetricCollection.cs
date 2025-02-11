using Newtonsoft.Json.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GQI
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

            collection.CalculateBounds();
            return collection;
        }

        public DateTime CreatedAt => _createdAt;
        public DateTime StartTime => _bounds.Start;
        public DateTime EndTime => _bounds.End;
        public IEnumerable<RequestDurationMetric> RequestDurations => _requestDurations;
        public IEnumerable<FirstPageDurationMetric> FirstPageDurations => _firstPageDurations;
        public IEnumerable<AllPagesDurationMetric> AllPagesDurations => _allPagesDurations;

        private readonly DateTime _createdAt;
        private readonly List<RequestDurationMetric> _requestDurations;
        private readonly List<FirstPageDurationMetric> _firstPageDurations;
        private readonly List<AllPagesDurationMetric> _allPagesDurations;
        private Bounds _bounds;

        private MetricCollection()
        {
            _createdAt = DateTime.UtcNow;
            _requestDurations = new List<RequestDurationMetric>();
            _firstPageDurations = new List<FirstPageDurationMetric>();
            _allPagesDurations = new List<AllPagesDurationMetric>();
        }

        private void CalculateBounds()
        {
            _bounds = new Bounds(_createdAt, _createdAt);
            _bounds = Bounds.GetOuterBounds(_bounds, GetBounds(_requestDurations));
            _bounds = Bounds.GetOuterBounds(_bounds, GetBounds(_firstPageDurations));
            _bounds = Bounds.GetOuterBounds(_bounds, GetBounds(_allPagesDurations));
        }

        private Bounds GetBounds(IReadOnlyList<Metric> metrics)
        {
            if (metrics.Count == 0)
                return default;

            var firstMetric = metrics[0];
            var lastMetric = metrics[metrics.Count - 1];

            return new Bounds(firstMetric.Time, lastMetric.Time);
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

        public static QueryDurationMetric ParseQueryDurationMetric(string line)
        {
            try
            {
                var jsonObject = JObject.Parse(line);
                var metricType = jsonObject["Metric"].Value<string>();

                switch (metricType)
                {
                    case "FirstPageDuration":
                        return jsonObject.ToObject<FirstPageDurationMetric>();
                    case "AllPagesDuration":
                        return jsonObject.ToObject<AllPagesDurationMetric>();
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static string GetAppId(string queryTag)
        {
            if (queryTag.Length < 40)
                return null;

            if (!queryTag.StartsWith("app/"))
                return null;

            return queryTag.Substring(4, 36);
        }

        public readonly struct Bounds
        {
            public DateTime Start { get; }
            public DateTime End { get; }

            public Bounds(DateTime start, DateTime end)
            {
                Start = start;
                End = end;
            }

            public static Bounds GetOuterBounds(Bounds a, Bounds b)
            {
                var start = a.Start <= b.Start ? a.Start : b.Start;
                var end = a.End >= b.End ? a.End : b.End;
                return new Bounds(start, end);
            }
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
