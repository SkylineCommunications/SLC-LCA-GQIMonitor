using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class MetricsCache
    {
        public const string GQIProvider_Local_Any = "Local GQI (Any)";
        public const string GQIProvider_Local_SLHelper = "Local GQI (SLHelper)";
        public const string GQIProvider_Local_DxM = "Local GQI (DxM)";
        public const string GQIProvider_Other = @"From Documents/GQI Monitor";

        public static readonly TimeSpan DefaultMaxCacheAge = TimeSpan.FromSeconds(60);

        private const string SLHelperMetricsFolderPath = @"C:\Skyline DataMiner\Logging\GQI\Metrics";
        private const string DxMMetricsFolderPath = @"C:\ProgramData\Skyline Communications\DataMiner GQI\Metrics";
        private const string CustomMetricsFolderPath = @"C:\Skyline DataMiner\Documents\GQI Monitor\Metrics";

        private readonly ProviderMetricsCache _slHelperMetrics = new ProviderMetricsCache(SLHelperMetricsFolderPath);
        private readonly ProviderMetricsCache _dxmMetrics = new ProviderMetricsCache(DxMMetricsFolderPath);
        private readonly ProviderMetricsCache _otherMetrics = new ProviderMetricsCache(CustomMetricsFolderPath);

        public IMetricCollection GetMetrics(Context context)
        {
            switch (context.Provider)
            {
                case GQIProvider_Local_SLHelper:
                    return _slHelperMetrics.GetMetrics(context);
                case GQIProvider_Local_DxM:
                    return _dxmMetrics.GetMetrics(context);
                case GQIProvider_Other:
                    return _otherMetrics.GetMetrics(context);
                case GQIProvider_Local_Any:
                default:
                    var slHelperMetrics = _slHelperMetrics.GetMetrics(context);
                    var dxmMetrics = _dxmMetrics.GetMetrics(context);
                    return new CombinedMetricCollection(slHelperMetrics, dxmMetrics);
            }
        }

        public sealed class Context
        {
            public IGQILogger Logger { get; set; }
            public string Provider { get; set; }
            public TimeSpan MaxCacheAge { get; set; }

            public Context(IGQILogger logger)
            {
                Logger = logger;
                Provider = GQIProvider_Local_Any;
                MaxCacheAge = DefaultMaxCacheAge;
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

            public MetricCollection GetMetrics(Context context)
            {
                if (IsValid(context.MaxCacheAge))
                    return _metrics;

                lock (this)
                {
                    if (IsValid(context.MaxCacheAge))
                        return _metrics;

                    _cacheTime = DateTime.UtcNow;
                    _metrics = MetricCollection.Parse(_folderPath, context.Logger);
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
