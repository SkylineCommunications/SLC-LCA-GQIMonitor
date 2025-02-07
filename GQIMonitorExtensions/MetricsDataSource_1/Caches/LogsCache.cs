using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class LogsCache
    {
        private const string LogFolderPath = @"C:\Skyline DataMiner\Logging\GQI";

        private readonly ConfigCache _configCache;
        private readonly object _lock = new object();

        private LogCollection _logs = null;
        private DateTime _cacheTime = DateTime.MinValue;

        public LogsCache(ConfigCache configCache)
        {
            _configCache = configCache;
        }

        public LogCollection GetLogs(IGQILogger logger)
        {
            var config = _configCache.GetConfig();
            if (IsValid(config.LogsCacheTTL))
                return _logs;

            lock (_lock)
            {
                if (IsValid(config.LogsCacheTTL))
                    return _logs;

                _cacheTime = DateTime.UtcNow;
                _logs = LogCollection.Parse(LogFolderPath, logger);
            }

            return _logs;
        }

        private bool IsValid(TimeSpan maxCacheAge)
        {
            if (_logs is null)
                return false;

            var minCacheTime = DateTime.UtcNow - maxCacheAge;
            return _cacheTime > minCacheTime;
        }
    }
}
