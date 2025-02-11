namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Caches;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Snapshots")]
    public sealed class SnapshotsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIUpdateable
    {
        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Name"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = Cache.Instance.Snapshots
                .GetSnapshots()
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private IGQIUpdater _updater;

        public void OnStartUpdates(IGQIUpdater updater)
        {
            _updater = updater;
            Cache.Instance.Snapshots.Added += OnSnapshotAdded;
            Cache.Instance.Snapshots.Removed += OnSnapshotRemoved;
        }

        public void OnStopUpdates()
        {
            Cache.Instance.Snapshots.Added -= OnSnapshotAdded;
            Cache.Instance.Snapshots.Removed -= OnSnapshotRemoved;
            _updater = null;
        }

        private void OnSnapshotAdded(string snapshot)
        {
            var row = ToRow(snapshot);
            _updater?.AddRow(row);
        }

        private void OnSnapshotRemoved(string snapshot)
        {
            _updater?.RemoveRow(snapshot);
        }

        private GQIRow ToRow(string name)
        {
            var cells = new GQICell[]
            {
                new GQICell { Value = name },
            };
            return new GQIRow(name, cells);
        }
    }
}
