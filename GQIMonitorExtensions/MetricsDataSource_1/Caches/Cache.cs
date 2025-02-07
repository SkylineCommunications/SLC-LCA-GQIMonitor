namespace MetricsDataSource_1.Caches
{
    internal sealed class Cache
    {
        public static Cache Instance { get; } = new Cache();

        static Cache() { }

        private Cache()
        {
            Config = new ConfigCache();
            Metrics = new MetricsCache(Config);
            Applications = new ApplicationsCache(Config);
            Logs = new LogsCache(Config);
        }

        public ConfigCache Config { get; }

        public MetricsCache Metrics { get; }

        public ApplicationsCache Applications { get; }

        public LogsCache Logs { get; }
    }
}
