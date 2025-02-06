using MetricsDataSource_1.Caches;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Linq;

namespace MetricsDataSource_1.DataSources
{
    [GQIMetaData(Name = "GQI Monitor - Logs")]
    public sealed class LogsDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments
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
        private static readonly GQIStringColumn _levelColumn = new GQIStringColumn("Level");
        private static readonly GQIStringColumn _messageColumn = new GQIStringColumn("Message");
        private static readonly GQIIntColumn _requestIdColumn = new GQIIntColumn("Request ID");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _timeColumn,
                _levelColumn,
                _messageColumn,
                _requestIdColumn,
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var logFolderPath = @"C:\Skyline DataMiner\Logging\GQI";
            var logs = LogCollection.Parse(logFolderPath, _logger).Logs;
            var rows = logs
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow ToRow(LogCollection.Log log)
        {
            var cells = new[]
            {
                new GQICell { Value = log.Time },
                new GQICell { Value = log.Level },
                new GQICell { Value = log.Message },
                new GQICell { Value = log.RequestId },
            };
            return new GQIRow(cells);
        }

    }
}
