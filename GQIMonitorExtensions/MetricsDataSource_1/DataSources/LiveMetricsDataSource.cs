namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;

    [GQIMetaData(Name = "GQI Monitor - Live metrics")]
    public sealed class LiveMetricsDataSource : IGQIDataSource, IGQIOnInit, IGQIUpdateable, IGQIOnPrepareFetch, IGQIOnDestroy
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        public GQIColumn[] GetColumns()
        {
            return LiveMetricCollection.Columns;
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
            _handle.Value.RowRemoved += OnRowRemoved;
            _handle.Value.RowAdded += OnRowAdded;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = _handle.Value.Metrics;
            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private void OnRowRemoved(string rowKey)
        {
            var updater = _updater;
            if (updater is null)
                return;

            updater.RemoveRow(rowKey);
        }

        private void OnRowAdded(GQIRow row)
        {
            var updater = _updater;
            if (updater is null)
                return;

            updater.AddRow(row);
        }

        public void OnStopUpdates()
        {
            _handle.Value.RowAdded -= OnRowAdded;
            _handle.Value.RowRemoved -= OnRowRemoved;
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
