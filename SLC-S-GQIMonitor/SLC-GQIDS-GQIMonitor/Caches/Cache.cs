using GQIMonitor;
using System;

namespace GQI.Caches
{
    internal sealed class Cache
    {
        public static Cache Instance { get; }

        static Cache()
        {
            try
            {
                Instance = new Cache();
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed initializing cache: {ex.Message}");
            }
        }

        private Cache()
        {
            GQIProvider = GQIProviders.GetCurrent();
            Config = new ConfigCache();
            Metrics = new MetricsCache(GQIProvider, Config);
            Applications = new ApplicationsCache(Config);
            Logs = new LogsCache(Config);
            Snapshots = new SnapshotsCache();
            LiveMetrics = new RefCountCache<LiveMetricCollection>(() => new LiveMetricCollection(GQIProvider, Config));
        }

        public IGQIProvider GQIProvider { get; }

        public ConfigCache Config { get; }

        public MetricsCache Metrics { get; }

        public ApplicationsCache Applications { get; }

        public LogsCache Logs { get; }

        public SnapshotsCache Snapshots { get; }

        public RefCountCache<LiveMetricCollection> LiveMetrics { get; }
    }
}
