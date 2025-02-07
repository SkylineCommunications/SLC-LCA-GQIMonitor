namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;

    [GQIMetaData(Name = "GQI Monitor - Live metric info")]
    public sealed class LiveMetricInfoDataSource : IGQIDataSource, IGQIOnInit, IGQIUpdateable, IGQIOnPrepareFetch, IGQIOnDestroy
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        public static readonly GQIDateTimeColumn _startTimeColumn = new GQIDateTimeColumn("Start time");
        public static readonly GQIDateTimeColumn _endTimeColumn = new GQIDateTimeColumn("End time");
        public static readonly GQIDateTimeColumn _lastUpdatedColumn = new GQIDateTimeColumn("Last updated");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _startTimeColumn,
                _endTimeColumn,
                _lastUpdatedColumn,
            };
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
            _handle.Value.Updated += OnUpdated;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var row = CreateInfoRow(_handle.Value);
            var rows = new[] { row };

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow CreateInfoRow(LiveMetricCollection liveMetrics)
        {
            var cells = new[]
            {
                new GQICell { Value = liveMetrics.StartTime },
                new GQICell { Value = liveMetrics.EndTime },
                new GQICell { Value = liveMetrics.LastUpdated },
            };

            return new GQIRow("0", cells);
        }

        private void OnUpdated()
        {
            var updater = _updater;
            if (updater is null)
                return;

            var row = CreateInfoRow(_handle.Value);
            updater.UpdateRow(row);
        }

        public void OnStopUpdates()
        {
            _handle.Value.Updated -= OnUpdated;
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
