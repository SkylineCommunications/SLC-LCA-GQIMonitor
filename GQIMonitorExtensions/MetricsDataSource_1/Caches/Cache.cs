namespace MetricsDataSource_1.Caches
{
    internal sealed class Cache : GQIMonitorLoader
    {
        public static Cache Instance { get; } = new Cache();

        static Cache() { }

        private Cache()
        {
            Config = new ConfigCache();
            Metrics = new MetricsCache(Config);
            Applications = new ApplicationsCache(Config);
            Logs = new LogsCache(Config);
            Snapshots = new SnapshotsCache();
            LiveMetrics = new RefCountCache<LiveMetricCollection>(() => new LiveMetricCollection());
        }

        public ConfigCache Config { get; }

        public MetricsCache Metrics { get; }

        public ApplicationsCache Applications { get; }

        public LogsCache Logs { get; }

        public SnapshotsCache Snapshots { get; }

        public RefCountCache<LiveMetricCollection> LiveMetrics { get; }
    }
}
