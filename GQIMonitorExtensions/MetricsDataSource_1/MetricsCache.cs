using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetricsDataSource_1
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
        private const string CustomMetricsFolderPath = @"C:\Skyline DataMiner\Documents\GQI Monitor";

        public static MetricsCache Instance { get; } = new MetricsCache();

        private readonly ProviderMetricsCache _slHelperMetrics = new ProviderMetricsCache(SLHelperMetricsFolderPath);
        private readonly ProviderMetricsCache _dxmMetrics = new ProviderMetricsCache(DxMMetricsFolderPath);
        private readonly ProviderMetricsCache _otherMetrics = new ProviderMetricsCache(CustomMetricsFolderPath);

        static MetricsCache() { }
        private MetricsCache() { }

        public IEnumerable<RequestDurationMetric> GetRequestDurationMetrics(Context context)
        {
            switch (context.Provider)
            {
                case GQIProvider_Local_SLHelper:
                    return _slHelperMetrics.GetMetrics(context).RequestDurations;
                case GQIProvider_Local_DxM:
                    return _dxmMetrics.GetMetrics(context).RequestDurations;
                case GQIProvider_Other:
                    return _otherMetrics.GetMetrics(context).RequestDurations;
                case GQIProvider_Local_Any:
                default:
                    var slHelperMetrics = _slHelperMetrics.GetMetrics(context).RequestDurations;
                    var dxmMetrics = _dxmMetrics.GetMetrics(context).RequestDurations;
                    return slHelperMetrics.Concat(dxmMetrics);
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
