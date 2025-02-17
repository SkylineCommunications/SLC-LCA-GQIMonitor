namespace GQI.DataSources
{
    using GQI.Caches;
    using GQIMonitor;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Applications")]
    public sealed class ApplicationsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit
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
            var rows = Cache.Instance.Applications
                .GetApplications(_dms, _logger)
                .Values
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private static GQIRow ToRow(Application application)
        {
            var cells = new[]
            {
                new GQICell { Value = application.ID },
                new GQICell { Value = application.Name },
            };

            return new GQIRow(application.ID, cells);
        }
    }
}
