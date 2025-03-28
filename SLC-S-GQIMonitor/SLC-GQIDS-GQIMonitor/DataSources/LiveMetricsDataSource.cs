namespace GQI.DataSources
{
    using GQI.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Live metrics")]
    public sealed class LiveMetricsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIUpdateable, IGQIOnPrepareFetch, IGQIOnDestroy
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIArgument<string> _appIdsArg = new GQIStringArgument("App IDs")
        {
            IsRequired = false,
            DefaultValue = string.Empty,
        };

        private static readonly GQIArgument<string> _usersArg = new GQIStringArgument("Users")
        {
            IsRequired = false,
            DefaultValue = string.Empty,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _appIdsArg,
                _usersArg,
            };
        }

        private HashSet<string> _appIds = null;
        private HashSet<string> _users = null;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            if (args.TryGetArgumentValue(_appIdsArg, out var appIds) && !string.IsNullOrEmpty(appIds))
            {
                _appIds = new HashSet<string>(appIds.Split(','));
            }

            if (args.TryGetArgumentValue(_usersArg, out var users) && !string.IsNullOrEmpty(users))
            {
                _users = new HashSet<string>(users.Split(','));
            }

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

        private RefCountCache<LiveMetricCollection>.Handle _handle;

        public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
        {
            _handle = Cache.Instance.LiveMetrics.GetHandle();
            return default;
        }

        private IGQIUpdater _updater;

        public void OnStartUpdates(IGQIUpdater updater)
        {
            _updater = updater;
            _handle.Value.BucketRemoved += OnBucketRemoved;
            _handle.Value.BucketAdded += OnBucketAdded;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = _handle.Value.GetBuckets()
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
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
            bool filterOnApp = _appIds?.Count > 0;
            bool filterOnUser = _users?.Count > 0;

            if (!filterOnApp && !filterOnUser)
                return metrics;

            IEnumerable<QueryDurationMetric> filteredMetrics = metrics;
            if (filterOnUser)
                filteredMetrics = filteredMetrics.Where(metric => _users.Contains(metric.User));

            if (filterOnApp)
                filteredMetrics = filteredMetrics.Where(metric => _appIds.Contains(metric.App));

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

        private void OnBucketRemoved(string bucketKey)
        {
            var updater = _updater;
            if (updater is null)
                return;

            updater.RemoveRow(bucketKey);
        }

        private void OnBucketAdded(Bucket bucket)
        {
            var updater = _updater;
            if (updater is null)
                return;

            var row = ToRow(bucket);
            updater.AddRow(row);
        }

        public void OnStopUpdates()
        {
            _handle.Value.BucketAdded -= OnBucketAdded;
            _handle.Value.BucketRemoved -= OnBucketRemoved;
            _updater = null;
        }

        public OnDestroyOutputArgs OnDestroy(OnDestroyInputArgs args)
        {
            _handle?.Dispose();
            _handle = null;

            return default;
        }
    }
}
