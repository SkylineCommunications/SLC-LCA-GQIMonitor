using GQIMonitor;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Linq;

namespace MetricsDataSource_1.Caches
{
    internal sealed class ApplicationsCache
    {
        private const string FilePath = Info.DocumentsPath + @"\applications.json";

        public static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIStringColumn("ID"),
            new GQIStringColumn("Name"),
        };

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
                _applications = GetApplicationRows(config, dms, logger);
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

        private GQIRow[] GetApplicationRows(Config config, GQIDMS dms, IGQILogger logger)
        {
            logger.Information("Retrieving applications...");
            try
            {

                var applications = GetApplications(config, dms, logger);
                return GetRows(applications);
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve applications.", ex);
            }
        }

        private ApplicationCollection GetApplications(Config config, GQIDMS dms, IGQILogger logger)
        {
            switch (config.Mode)
            {
                case Mode.Snapshot:
                    return Applications.ReadFromFile(FilePath, logger);
                default:
                    var connection = dms.GetConnection();
                    return _fetcher.GetFromWebAPI(connection);
            }
        }

        private static GQIRow[] GetRows(ApplicationCollection collection)
        {
            var applications = collection?.Applications;
            if (applications is null)
                return Array.Empty<GQIRow>();

            return applications
                .Select(ToRow)
                .ToArray();
        }

        private static GQIRow ToRow(Application application)
        {
            var cells = new[]
            {
                new GQICell { Value = application.ID },
                new GQICell { Value = application.Name },
            };

            return new GQIRow(application.ID, cells);
        }
    }
}
