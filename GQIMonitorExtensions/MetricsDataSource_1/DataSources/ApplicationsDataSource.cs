namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;

    [GQIMetaData(Name = "GQI Monitor - Applications")]
    public sealed class ApplicationsDataSource : IGQIDataSource, IGQIOnInit
    {
        private GQIDMS _dms;
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _dms = args.DMS;
            _logger = args.Logger;

            return default;
        }

        public GQIColumn[] GetColumns()
        {
            return ApplicationsCache.Columns;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = Cache.Instance.Applications.GetApplications(_dms, _logger);

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }
    }
}
