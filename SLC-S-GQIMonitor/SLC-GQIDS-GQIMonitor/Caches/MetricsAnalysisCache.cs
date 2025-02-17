using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GQI.Caches
{
    internal sealed class MetricsAnalysisCache
    {
        public const string MetricProperty_App = "App";
        public const string MetricProperty_User = "User";

        private readonly MetricsCache _metricsCache;
        private readonly object _lock = new object();

        private Dictionary<string, Result> _analysis;
        private Options _options;

        public MetricsAnalysisCache(MetricsCache metricsCache)
        {
            _metricsCache = metricsCache;
        }

        public Dictionary<string, Result> GetAnalysis(Options options, IGQILogger logger)
        {
            lock (_lock)
            {
                if (IsCacheValid(options))
                    return _analysis;

                _analysis = CreateAnalysis(options, logger);
                _options = options;
                return _analysis;
            }
        }

        private bool IsCacheValid(Options filter)
        {
            return _analysis != null && IsSameOptions(_options, filter);
        }

        private Dictionary<string, Result> CreateAnalysis(Options options, IGQILogger logger)
        {
            var metrics = _metricsCache.GetMetrics(logger).QueryDurations;
            metrics = FilterMetrics(metrics, options);

            return AnalyzeMetrics(metrics, options);
        }

        private IReadOnlyList<QueryDurationMetric> FilterMetrics(IReadOnlyList<QueryDurationMetric> metrics, Options options)
        {
            if (!IsFiltered(options))
                return metrics;

            int startIndex = 0;
            int endIndex = metrics.Count;
            if (options.StartTime != DateTime.MinValue)
            {
                for (; startIndex < endIndex; startIndex++)
                {
                    var metric = metrics[startIndex];
                    if (metric.Time >= options.StartTime)
                        break;
                }
            }
            if (options.EndTime != DateTime.MaxValue)
            {
                for (; endIndex > startIndex; endIndex--)
                {
                    var metric = metrics[endIndex - 1];
                    if (metric.Time <= options.EndTime)
                        break;
                }
            }

            var filteredMetrics = metrics
                .Skip(startIndex)
                .Take(endIndex - startIndex);

            if (string.IsNullOrEmpty(options.Filter))
                return filteredMetrics.ToArray();

            var predicate = GetFilterPredicate(options);
            return filteredMetrics.Where(predicate).ToArray();
        }

        private Dictionary<string, Result> AnalyzeMetrics(IReadOnlyList<QueryDurationMetric> metrics, Options options)
        {
            var keySelector = GetKeySelector(options.GroupBy);
            var analysis = new Dictionary<string, Result>();

            foreach (var metric in metrics)
            {
                var key = keySelector(metric);
                if (!analysis.TryGetValue(key, out var result))
                {
                    analysis[key] = new Result(metric);
                }
                else
                {
                    result.AddMetric(metric);
                }
            }
            return analysis;
        }

        private Func<QueryDurationMetric, string> GetKeySelector(string groupBy)
        {
            switch (groupBy)
            {
                case MetricProperty_App:
                    return metric => metric.App;
                case MetricProperty_User:
                    return metric => metric.User;
                default:
                    throw new GenIfException($"Invalid group by property '{groupBy}'.");
            }
        }

        private Func<QueryDurationMetric, bool> GetFilterPredicate(Options options)
        {
            switch (options.GroupBy)
            {
                case MetricProperty_App:
                    return metric => metric.User == options.Filter;
                case MetricProperty_User:
                    return metric => metric.App == options.Filter;
                default:
                    throw new GenIfException($"Invalid group by property '{options.GroupBy}'.");
            }
        }

        private bool IsFiltered(Options filter)
        {
            if (filter.StartTime != DateTime.MinValue)
                return true;
            if (filter.EndTime != DateTime.MaxValue)
                return true;
            return !string.IsNullOrEmpty(filter.Filter);
        }

        private static bool IsSameOptions(Options options1, Options options2)
        {
            if (options1 == options2)
                return true;

            if (options1.StartTime != options2.StartTime) return false;
            if (options1.EndTime != options2.EndTime) return false;
            if (options1.GroupBy != options2.GroupBy) return false;
            if (options1.Filter != options2.Filter) return false;

            return true;
        }

        public sealed class Options
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string GroupBy { get; set; }
            public string Filter { get; set; }
        }

        public sealed class Result
        {
            public int QueryCount { get; private set; }
            public double TotalDuration { get; private set; }
            public double MaxDuration { get; private set; }
            public HashSet<string> DistinctUsers { get; }

            public double AvgDuration => TotalDuration / QueryCount;
            public int UserCount => DistinctUsers.Count;

            public Result(QueryDurationMetric metric)
            {
                QueryCount = 1;
                TotalDuration = metric.Duration.TotalMilliseconds;
                MaxDuration = TotalDuration;
                DistinctUsers = new HashSet<string>
                {
                    metric.User
                };
            }

            public void AddMetric(QueryDurationMetric metric)
            {
                QueryCount++;

                var duration = metric.Duration.TotalMilliseconds;
                TotalDuration += duration;

                if (duration > MaxDuration)
                    MaxDuration = duration;

                DistinctUsers.Add(metric.User);
            }
        }
    }
}
