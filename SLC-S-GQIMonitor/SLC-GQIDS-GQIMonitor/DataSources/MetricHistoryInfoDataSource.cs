namespace GQI.DataSources
{
    using GQI.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;

    [GQIMetaData(Name = "GQI Monitor - Metric history info")]
    public sealed class MetricHistoryInfoDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        public static readonly GQIDateTimeColumn _startTimeColumn = new GQIDateTimeColumn("Start time");
        public static readonly GQIDateTimeColumn _endTimeColumn = new GQIDateTimeColumn("End time");
        public static readonly GQIDateTimeColumn _lastUpdatedColumn = new GQIDateTimeColumn("Snapshot time");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _startTimeColumn,
                _endTimeColumn,
                _lastUpdatedColumn,
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var metrics = Cache.Instance.Metrics.GetMetrics(_logger);
            var row = CreateInfoRow(metrics);
            var rows = new[] { row };

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow CreateInfoRow(MetricCollection metrics)
        {
            var cells = new[]
            {
                new GQICell { Value = metrics.StartTime },
                new GQICell { Value = metrics.EndTime },
                new GQICell { Value = metrics.CreatedAt },
            };

            return new GQIRow("0", cells);
        }
    }
}
