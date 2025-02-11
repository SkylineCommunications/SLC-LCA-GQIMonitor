using GQIMonitor;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class MetricsCache
    {
        public static readonly TimeSpan DefaultMaxCacheAge = TimeSpan.FromSeconds(60);

        private readonly ConfigCache _configCache;
        private readonly ProviderMetricsCache _liveMetrics;
        private readonly SnapshotMetricsCache _snapshotMetrics;

        public MetricsCache(IGQIProvider gqiProvider, ConfigCache configCache)
        {
            _configCache = configCache;
            _liveMetrics = new ProviderMetricsCache(gqiProvider.MetricsPath);
            _snapshotMetrics = new SnapshotMetricsCache();
        }

        public MetricCollection GetMetrics(IGQILogger logger)
        {
            var config = _configCache.GetConfig();
            switch (config.Mode)
            {
                case Mode.Snapshot:
                    return _snapshotMetrics.GetMetrics(config.Snapshot, logger);
                case Mode.Live:
                default:
                    return _liveMetrics.GetMetrics(config, logger);
            }
        }

        private sealed class ProviderMetricsCache
        {
            private readonly string _folderPath;

            private MetricCollection _metrics = null;
            private DateTime _cacheTime = DateTime.MinValue;

            public ProviderMetricsCache(string folderPath)
            {
                _folderPath = folderPath;
            }

            public MetricCollection GetMetrics(Config config, IGQILogger logger)
            {
                if (IsValid(config.MetricsCacheTTL))
                    return _metrics;

                lock (this)
                {
                    if (IsValid(config.MetricsCacheTTL))
                        return _metrics;

                    _cacheTime = DateTime.UtcNow;
                    _metrics = MetricCollection.Parse(_folderPath, logger);
                }

                return _metrics;
            }

            private bool IsValid(TimeSpan maxCacheAge)
            {
                if (_metrics is null)
                    return false;

                var minCacheTime = DateTime.UtcNow - maxCacheAge;
                return _cacheTime > minCacheTime;
            }
        }

        private sealed class SnapshotMetricsCache
        {
            private string _snapshot;
            private MetricCollection _metrics = null;

            public MetricCollection GetMetrics(string snapshot, IGQILogger logger)
            {
                if (IsValid(snapshot))
                    return _metrics;

                lock (this)
                {
                    if (IsValid(snapshot))
                        return _metrics;

                    _snapshot = snapshot;
                    var snapshotPath = GetMetricsPath();
                    _metrics = MetricCollection.Parse(snapshotPath, logger);
                }

                return _metrics;
            }

            private bool IsValid(string snapshot)
            {
                if (_metrics is null)
                    return false;

                return _snapshot == snapshot;
            }

            private string GetMetricsPath()
            {
                return $"{Info.DocumentsPath}/Snapshots/{_snapshot}/Metrics";
            }
        }
    }
}
