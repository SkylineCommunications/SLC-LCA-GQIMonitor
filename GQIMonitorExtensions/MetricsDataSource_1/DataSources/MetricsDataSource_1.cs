namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Request durations")]
    public sealed class MetricsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIDateTimeColumn _timeColumn = new GQIDateTimeColumn("Time");
        private static readonly GQIStringColumn _requestColumn = new GQIStringColumn("Request");
        private static readonly GQIStringColumn _userColumn = new GQIStringColumn("User");
        //private static readonly GQITimeSpanColumn _durationColumn = new GQITimeSpanColumn("Duration");
        private static readonly GQIIntColumn _durationColumn = new GQIIntColumn("Duration (ms)");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _timeColumn,
                _requestColumn,
                _userColumn,
                _durationColumn,
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var metrics = Cache.Instance.Metrics.GetMetrics(_logger);
            var rows = metrics.RequestDurations
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow ToRow(RequestDurationMetric metric)
        {
            var cells = new[]
            {
                new GQICell { Value = metric.Time },
                new GQICell { Value = metric.Request },
                new GQICell { Value = metric.User },
                new GQICell { Value = metric.Duration.Milliseconds },
            };
            return new GQIRow(cells);
        }
    }
}
