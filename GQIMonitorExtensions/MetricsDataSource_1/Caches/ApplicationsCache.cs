﻿using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class ApplicationsCache
    {
        private readonly ConfigCache _configCache;
        private readonly ApplicationsFetcher _fetcher = new ApplicationsFetcher();
        private readonly object _lock = new object();

        private GQIRow[] _applications = null;
        private DateTime _cacheTime = DateTime.MinValue;

        public ApplicationsCache(ConfigCache configCache)
        {
            _configCache = configCache;
        }

        public GQIRow[] GetApplications(GQIDMS dms, IGQILogger logger)
        {
            var config = _configCache.GetConfig();
            if (IsValid(config.ApplicationsCacheTTL))
                return _applications;

            lock (_lock)
            {
                if (IsValid(config.ApplicationsCacheTTL))
                    return _applications;

                _cacheTime = DateTime.UtcNow;
                _applications = _fetcher.GetApplicationRows(config, dms, logger);
            }

            return _applications;
        }

        private bool IsValid(TimeSpan maxCacheAge)
        {
            if (_applications is null)
                return false;

            var minCacheTime = DateTime.UtcNow - maxCacheAge;
            return _cacheTime > minCacheTime;
        }
    }
}
