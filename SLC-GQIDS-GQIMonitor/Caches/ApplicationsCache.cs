using GQIMonitor;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GQI.Caches
{
    internal sealed class ApplicationsCache
    {
        public static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIStringColumn("ID"),
            new GQIStringColumn("Name"),
        };

        private readonly ConfigCache _configCache;
        private readonly ApplicationsFetcher _fetcher = new ApplicationsFetcher();
        private readonly object _lock = new object();

        private Dictionary<string, Application> _applications = null;
        private DateTime _cacheTime = DateTime.MinValue;
        private string _mode = string.Empty;

        public ApplicationsCache(ConfigCache configCache)
        {
            _configCache = configCache;
        }

        public Dictionary<string, Application> GetApplications(GQIDMS dms, IGQILogger logger)
        {
            var config = _configCache.GetConfig();
            if (IsValid(config))
                return _applications;

            lock (_lock)
            {
                if (IsValid(config))
                    return _applications;

                _cacheTime = DateTime.UtcNow;
                _mode = config.Mode;
                _applications = GetApplications(config, dms, logger);
            }

            return _applications;
        }

        private bool IsValid(Config config)
        {
            if (_applications is null)
                return false;

            if (_mode != config.Mode)
                return false;

            var minCacheTime = DateTime.UtcNow - config.ApplicationsCacheTTL;
            return _cacheTime > minCacheTime;
        }

        private Dictionary<string, Application> GetApplications(Config config, GQIDMS dms, IGQILogger logger)
        {
            logger.Information("Retrieving applications...");
            try
            {
                var collection = GetApplicationCollection(config, dms, logger);
                var dictionary = new Dictionary<string, Application>();
                var applications = collection?.Applications;
                if (applications is null)
                    return dictionary;

                foreach (var application in applications)
                {
                    if (application is null) 
                        continue;
                    if (application.ID is null)
                        continue;

                    dictionary[application.ID] = application;
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to retrieve applications.", ex);
            }
        }

        private ApplicationCollection GetApplicationCollection(Config config, GQIDMS dms, IGQILogger logger)
        {
            switch (config.Mode)
            {
                case Mode.Snapshot:
                    var filePath = GetApplicationsPath(config.Snapshot);
                    return Applications.ReadFromFile(filePath, logger);
                case Mode.Live:
                default:
                    var connection = dms.GetConnection();
                    return _fetcher.GetFromWebAPI(connection);
            }
        }

        private static string GetApplicationsPath(string snapshot)
        {
            return $"{Info.DocumentsPath}/Snapshots/{snapshot}/applications.json";
        }
    }
}
