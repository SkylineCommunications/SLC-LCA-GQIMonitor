namespace MetricsDataSource_1.DataSources
{
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Request durations")]
    public sealed class MetricsDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }



        private static readonly string[] _providerOptions = new[]
        {
            MetricsCache.GQIProvider_Local_Any,
            MetricsCache.GQIProvider_Local_SLHelper,
            MetricsCache.GQIProvider_Local_DxM,
            MetricsCache.GQIProvider_Other,
        };

        private static readonly GQIArgument<string> _providerArg = new GQIStringDropdownArgument("Metrics provider", _providerOptions)
        {
            IsRequired = true,
            DefaultValue = _providerOptions[0],
        };

        private static readonly GQIArgument<int> _maxCacheAgeArg = new GQIIntArgument("Maximum cache age (seconds)")
        {
            IsRequired = false,
            DefaultValue = MetricsCache.DefaultMaxCacheAge.Seconds,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _providerArg,
                _maxCacheAgeArg,
            };
        }

        private string _provider;
        private TimeSpan _maxCacheAge;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _provider = args.GetArgumentValue(_providerArg);

            int maxCacheAgeSeconds = args.GetArgumentValue(_maxCacheAgeArg);
            _maxCacheAge = TimeSpan.FromSeconds(maxCacheAgeSeconds);

            return default;
        }



        private static readonly GQIDateTimeColumn _timeColumn = new GQIDateTimeColumn("Time");
        private static readonly GQIStringColumn _requestColumn = new GQIStringColumn("Request");
        private static readonly GQIStringColumn _userColumn = new GQIStringColumn("User");
        private static readonly GQITimeSpanColumn _durationColumn = new GQITimeSpanColumn("Duration");

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
            var context = new MetricsCache.Context(_logger)
            {
                Provider = _provider,
                MaxCacheAge = _maxCacheAge,
            };
            var metrics = MetricsCache.Instance.GetRequestDurationMetrics(context);
            var rows = metrics
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
                new GQICell { Value = metric.Duration },
            };
            return new GQIRow(cells);
        }
    }
}
