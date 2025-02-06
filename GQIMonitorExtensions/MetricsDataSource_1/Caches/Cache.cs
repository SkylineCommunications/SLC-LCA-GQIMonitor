namespace MetricsDataSource_1.Caches
{
    internal sealed class Cache
    {
        public static Cache Instance { get; } = new Cache();

        static Cache() { }
        private Cache() { }

        public MetricsCache Metrics { get; } = new MetricsCache();
        public ApplicationsCache Applications { get; } = new ApplicationsCache();

    }
}
