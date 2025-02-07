using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class MetricsCache
    {
        public static readonly TimeSpan DefaultMaxCacheAge = TimeSpan.FromSeconds(60);

        private const string SLHelperMetricsFolderPath = @"C:\Skyline DataMiner\Logging\GQI\Metrics";
        private const string DxMMetricsFolderPath = @"C:\ProgramData\Skyline Communications\DataMiner GQI\Metrics";
        private const string SnapshotMetricsFolderPath = GQIMonitor.DocumentsPath + @"\Metrics";

        private readonly ConfigCache _configCache;
        private readonly ProviderMetricsCache _slHelperMetrics = new ProviderMetricsCache(SLHelperMetricsFolderPath);
        private readonly ProviderMetricsCache _dxmMetrics = new ProviderMetricsCache(DxMMetricsFolderPath);
        private readonly ProviderMetricsCache _snapshotMetrics = new ProviderMetricsCache(SnapshotMetricsFolderPath);

        public MetricsCache(ConfigCache configCache)
        {
            _configCache = configCache;
        } 

        public IMetricCollection GetMetrics(IGQILogger logger)
        {
            var config = _configCache.GetConfig();
            switch (config.Mode)
            {
                case Mode.LocalSLHelper:
                    return _slHelperMetrics.GetMetrics(config, logger);
                case Mode.LocalDxM:
                    return _dxmMetrics.GetMetrics(config, logger);
                case Mode.Snapshot:
                    return _snapshotMetrics.GetMetrics(config, logger);
                case Mode.Local:
                default:
                    var slHelperMetrics = _slHelperMetrics.GetMetrics(config, logger);
                    var dxmMetrics = _dxmMetrics.GetMetrics(config, logger);
                    return new CombinedMetricCollection(slHelperMetrics, dxmMetrics);
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
    }
}
