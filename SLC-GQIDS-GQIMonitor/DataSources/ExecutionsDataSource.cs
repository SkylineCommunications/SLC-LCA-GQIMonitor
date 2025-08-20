namespace GQI.DataSources
{
    using GQI.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Executions")]
    public sealed class ExecutionsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIDateTimeColumn _timeColumn = new GQIDateTimeColumn("Time");
        private static readonly GQIStringColumn _userColumn = new GQIStringColumn("User");
        private static readonly GQIStringColumn _appIdColumn = new GQIStringColumn("App ID");
        //private static readonly GQITimeSpanColumn _durationColumn = new GQITimeSpanColumn("Duration");
        private static readonly GQIIntColumn _durationColumn = new GQIIntColumn("Duration (ms)");
        private static readonly GQIIntColumn _rowsColumn = new GQIIntColumn("# rows");
        private static readonly GQIIntColumn _pagesColumn = new GQIIntColumn("# pages");
        private static readonly GQIBooleanColumn _firstPageOnlyColumn = new GQIBooleanColumn("First page only");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _timeColumn,
                _userColumn,
                _appIdColumn,
                _durationColumn,
                _rowsColumn,
                _pagesColumn,
                _firstPageOnlyColumn,
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var metrics = Cache.Instance.Metrics.GetMetrics(_logger);
            var rows = GetRows(metrics).ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private IEnumerable<GQIRow> GetRows(MetricCollection metrics)
        {
            return metrics.QueryDurations.Select(ToRow);
        }

        private GQIRow ToRow(QueryDurationMetric metric)
        {
            var pageCount = 1;
            var isFirstPageOnly = true;
            if (metric is AllPagesDurationMetric allPagesMetric)
            {
                pageCount = allPagesMetric.Pages;
                isFirstPageOnly = false;
            }

            var duration = (int)Math.Round(metric.Duration.TotalMilliseconds);
            var cells = new[]
            {
                new GQICell { Value = metric.Time },
                new GQICell { Value = metric.User },
                new GQICell { Value = metric.App },
                new GQICell { Value = duration },
                new GQICell { Value = metric.Rows },
                new GQICell { Value = pageCount },
                new GQICell { Value = isFirstPageOnly },
            };
            return new GQIRow(cells);
        }
    }
}
