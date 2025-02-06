namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System;

    [GQIMetaData(Name = "GQI Monitor - Applications")]
    public sealed class ApplicationsDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments
    {
        private static readonly TimeSpan DefaultMaxCacheAge = TimeSpan.FromMinutes(15);

        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIArgument<int> _maxCacheAgeArg = new GQIIntArgument("Maximum cache age (seconds)")
        {
            IsRequired = false,
            DefaultValue = DefaultMaxCacheAge.Seconds,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _maxCacheAgeArg,
            };
        }

        private TimeSpan _maxCacheAge;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            int maxCacheAgeSeconds = args.GetArgumentValue(_maxCacheAgeArg);
            _maxCacheAge = TimeSpan.FromSeconds(maxCacheAgeSeconds);

            return default;
        }


        public GQIColumn[] GetColumns()
        {
            return Applications.Columns;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = Cache.Instance.Applications.GetApplications(_maxCacheAge, _logger);

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }
    }
}
