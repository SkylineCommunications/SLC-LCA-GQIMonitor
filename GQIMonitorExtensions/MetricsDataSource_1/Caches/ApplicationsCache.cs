using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Caches
{
    internal sealed class ApplicationsCache
    {
        private readonly object _lock = new object();

        private GQIRow[] _applications = null;
        private DateTime _cacheTime = DateTime.MinValue;

        public GQIRow[] GetApplications(TimeSpan maxCacheAge, IGQILogger logger)
        {
            if (IsValid(maxCacheAge))
                return _applications;

            lock (_lock)
            {
                if (IsValid(maxCacheAge))
                    return _applications;

                _cacheTime = DateTime.UtcNow;
                _applications = Applications.GetApplications(logger);
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
