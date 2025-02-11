namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System.Collections.Generic;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Executions")]
    public sealed class ExecutionsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit, IGQIInputArguments
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private const string OptimizationType_Any = "Any";
        private const string OptimizationType_FirstPage = "First page";
        private const string OptimizationType_AllPages = "All pages";

        private static readonly string[] _optimizationTypeOptions = new[]
        {
            OptimizationType_Any,
            OptimizationType_FirstPage,
            OptimizationType_AllPages,
        };

        private static readonly GQIArgument<string> _optimizationTypeArg = new GQIStringDropdownArgument("Optimization type", _optimizationTypeOptions)
        {
            IsRequired = true,
            DefaultValue = OptimizationType_Any,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _optimizationTypeArg,
            };
        }

        private string _optimizationType;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _optimizationType = args.GetArgumentValue(_optimizationTypeArg);

            return default;
        }



        private static readonly GQIDateTimeColumn _timeColumn = new GQIDateTimeColumn("Time");
        private static readonly GQIStringColumn _userColumn = new GQIStringColumn("User");
        private static readonly GQIStringColumn _queryColumn = new GQIStringColumn("Query");
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
                _queryColumn,
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

        private IEnumerable<GQIRow> GetRows(IMetricCollection metrics)
        {
            switch (_optimizationType)
            {
                case OptimizationType_FirstPage:
                    return metrics.FirstPageDurations.Select(ToRow);
                case OptimizationType_AllPages:
                    return metrics.AllPagesDurations.Select(ToRow);
                default:
                    var firstPageRows = metrics.FirstPageDurations.Select(ToRow);
                    var allPagesRows = metrics.AllPagesDurations.Select(ToRow);
                    return firstPageRows
                        .Concat(allPagesRows)
                        .OrderBy(row => row.Cells[0].Value);
            }
        }

        private GQIRow ToRow(FirstPageDurationMetric metric)
        {
            var cells = new[]
            {
                new GQICell { Value = metric.Time },
                new GQICell { Value = metric.User },
                new GQICell { Value = metric.Query },
                new GQICell { Value = MetricCollection.GetAppId(metric.Query) },
                new GQICell { Value = metric.Duration.Milliseconds },
                new GQICell { Value = metric.Rows },
                new GQICell { Value = 1 },
                new GQICell { Value = true },
            };
            return new GQIRow(cells);
        }

        private GQIRow ToRow(AllPagesDurationMetric metric)
        {
            var cells = new[]
            {
                new GQICell { Value = metric.Time },
                new GQICell { Value = metric.User },
                new GQICell { Value = metric.Query },
                new GQICell { Value = MetricCollection.GetAppId(metric.Query) },
                new GQICell { Value = metric.Duration.Milliseconds },
                new GQICell { Value = metric.Rows },
                new GQICell { Value = metric.Pages },
                new GQICell { Value = false },
            };
            return new GQIRow(cells);
        }
    }
}
