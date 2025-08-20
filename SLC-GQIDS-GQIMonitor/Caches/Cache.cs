using GQIMonitor;
using System;

namespace GQI.Caches
{
    internal sealed class Cache : IDisposable
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
                Logger.Log($"Failed initializing cache: {ex.Message}");
            }
        }

        private readonly string _assemblyName;
        private readonly CommandReceiver _commandReceiver;

        private Cache()
        {
            _assemblyName = typeof(Cache).Assembly.GetName().Name;
            Logger.Log($"Initializing cache for {_assemblyName}...");

            GQIProvider = GQIProviders.GetCurrent();
            Config = new ConfigCache();
            Metrics = new MetricsCache(GQIProvider, Config);
            MetricsAnalysis = new MetricsAnalysisCache(Metrics);
            Applications = new ApplicationsCache(Config);
            Logs = new LogsCache(Config);
            Snapshots = new SnapshotsCache();
            LiveMetrics = new RefCountCache<LiveMetricCollection>(() => new LiveMetricCollection(GQIProvider, Config));
            _commandReceiver = new CommandReceiver(this);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            Logger.Log($"Initialized cache for {_assemblyName}");
        }

        public IGQIProvider GQIProvider { get; }

        public ConfigCache Config { get; }

        public MetricsCache Metrics { get; }

        public ApplicationsCache Applications { get; }

        public LogsCache Logs { get; }

        public SnapshotsCache Snapshots { get; }

        public RefCountCache<LiveMetricCollection> LiveMetrics { get; }

        public MetricsAnalysisCache MetricsAnalysis { get; }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assemblyName = args?.LoadedAssembly?.GetName();
            if (assemblyName is null)
                return;

            if (assemblyName.Name.StartsWith("SLC-GQIDS-GQIMonitor"))
                Dispose();
        }

        public void Dispose()
        {
            Logger.Log($"Disposing cache for {_assemblyName}...");
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            _commandReceiver.Dispose();
            LiveMetrics.Dispose();
            Snapshots.Dispose();
            Config.Dispose();
            Logger.Log($"Disposed cache for {_assemblyName}");
        }
    }
}
