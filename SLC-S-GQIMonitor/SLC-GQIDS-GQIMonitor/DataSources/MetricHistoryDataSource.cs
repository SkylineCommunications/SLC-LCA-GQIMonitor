namespace GQI.DataSources
{
    using GQI.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Metric history")]
    public sealed class MetricHistoryDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit, IGQIInputArguments
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIArgument<string> _appIdArg = new GQIStringArgument("App ID")
        {
            IsRequired = false,
            DefaultValue = string.Empty,
        };

        private static readonly GQIArgument<string> _userArg = new GQIStringArgument("User")
        {
            IsRequired = false,
            DefaultValue = string.Empty,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _appIdArg,
                _userArg,
            };
        }

        private string _appId = string.Empty;
        private string _user = string.Empty;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            args.TryGetArgumentValue(_appIdArg, out _appId);
            args.TryGetArgumentValue(_userArg, out _user);

            return default;
        }

        private static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIDateTimeColumn("Start time"),
            new GQIDateTimeColumn("End time"),
            new GQIIntColumn("Query executions"),
            new GQIIntColumn("Average duration (ms)"),
            new GQIIntColumn("Maximum duration (ms)"),
            new GQIIntColumn("Active users"),
        };

        public GQIColumn[] GetColumns()
        {
            return Columns;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var metrics = Cache.Instance.Metrics.GetMetrics(_logger);
            var buckets = CreateBuckets(metrics);
            var rows = buckets
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private static readonly TimeSpan BucketInterval = TimeSpan.FromHours(1);

        private Bucket[] CreateBuckets(MetricCollection metrics)
        {
            var bucketSize = BucketInterval.Ticks;
            var bucketOffset = (metrics.StartTime.Ticks / bucketSize) * bucketSize;
            var bucketCount = GetBucketIndex(bucketOffset, bucketSize, metrics.EndTime.Ticks) + 1;

            var buckets = Enumerable.Range(0, bucketCount)
                .Select(bucketIndex => new Bucket(GetBucketBounds(bucketOffset, bucketSize, bucketIndex)))
                .ToArray();

            // Adjust bounds of the first and last bucket
            var firstBucket = buckets.First();
            var lastBucket = buckets.Last();
            firstBucket.Bounds = firstBucket.Bounds.StartAt(metrics.StartTime);
            lastBucket.Bounds = lastBucket.Bounds.EndAt(metrics.EndTime);

            foreach (var metric in metrics.QueryDurations)
            {
                var bucketIndex = GetBucketIndex(bucketOffset, bucketSize, metric.Time.Ticks);
                if (bucketIndex < 0 || bucketIndex >= bucketCount)
                {
                    _logger.Warning($"Unexpected bucket index: {bucketIndex}");
                    continue;
                }

                buckets[bucketIndex].Metrics.Add(metric);
            }

            return buckets;
        }

        private int GetBucketIndex(long bucketOffset, long bucketSize, long value)
        {
            return (int)((value - bucketOffset) / bucketSize);
        }

        private BucketBounds GetBucketBounds(long bucketOffset, long bucketSize, int bucketIndex)
        {
            var start = bucketOffset + (bucketIndex * bucketSize);
            var end = start + bucketSize;
            return new BucketBounds(start, end);
        }

        private GQIRow ToRow(Bucket bucket)
        {
            var metrics = FilterMetrics(bucket.Metrics);

            var count = metrics.Count;
            var avgDuration = (int)Math.Round(GetAvgDuration(metrics));
            var maxDuration = (int)Math.Round(GetMaxDuration(metrics));
            var activeUsers = GetActiveUsers(metrics);

            var cells = new GQICell[]
            {
                new GQICell { Value = bucket.Bounds.Start },
                new GQICell { Value = bucket.Bounds.End },
                new GQICell { Value = count },
                new GQICell { Value = avgDuration },
                new GQICell { Value = maxDuration },
                new GQICell { Value = activeUsers },
            };
            return new GQIRow(bucket.Key, cells);
        }
        
        private ICollection<QueryDurationMetric> FilterMetrics(List<QueryDurationMetric> metrics)
        {
            bool filterOnApp = !string.IsNullOrEmpty(_appId);
            bool filterOnUser = !string.IsNullOrEmpty(_user);

            if (!filterOnApp && !filterOnUser)
                return metrics;

            IEnumerable<QueryDurationMetric> filteredMetrics = metrics;
            if (filterOnUser)
                filteredMetrics = filteredMetrics.Where(metric => metric.User == _user);

            if (filterOnApp)
                filteredMetrics = filteredMetrics.Where(metric => MetricCollection.GetAppId(metric.Query) == _appId);

            return filteredMetrics.ToArray();
        }

        private static double GetAvgDuration(ICollection<QueryDurationMetric> metrics)
        {
            if (metrics.Count == 0)
                return 0;
            return metrics.Average(metric => metric.Duration.TotalMilliseconds);
        }

        private static double GetMaxDuration(ICollection<QueryDurationMetric> metrics)
        {
            if (metrics.Count == 0)
                return 0;
            return metrics.Max(metric => metric.Duration.TotalMilliseconds);
        }

        private static int GetActiveUsers(ICollection<QueryDurationMetric> metrics)
        {
            if (metrics.Count == 0)
                return 0;

            var users = new HashSet<string>();
            foreach (var metric in metrics)
            {
                users.Add(metric.User);
            }
            return users.Count;
        }
    }
}
