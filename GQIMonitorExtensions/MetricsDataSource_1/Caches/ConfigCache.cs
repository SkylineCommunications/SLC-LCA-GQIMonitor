using Newtonsoft.Json;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.IO;

namespace MetricsDataSource_1.Caches
{
    internal sealed class ConfigCache
    {
        private const string ConfigFilePath = GQIMonitor.DocumentsPath + @"\config.json";

        private readonly object _lock = new object();
        private readonly Config DefaultConfig = new Config();

        private Config _config = null;
        private FileSystemWatcher _watcher = null;

        public Config GetConfig()
        {
            if (_config != null)
                return _config;

            lock (_lock)
            {
                if (_config != null)
                    return _config;

                _config = ReadConfig();
                _watcher = WatchConfigChanges();
            }

            return _config;
        }

        private Config ReadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return DefaultConfig;

                var jsonConfig = File.ReadAllText(ConfigFilePath);
                var config = JsonConvert.DeserializeObject<Config>(jsonConfig, GQIMonitor.JsonSerializerSettings);
                return config ?? DefaultConfig;
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to read config file.", ex);
            }
        }

        private void UpdateConfig()
        {
            try
            {
                lock (_lock)
                {
                    _config = ReadConfig();
                }
            }
            catch
            {
                // Ignore exceptions
            }
        }

        private FileSystemWatcher WatchConfigChanges()
        {
            var watcher = new FileSystemWatcher();

            watcher.Path = GQIMonitor.DocumentsPath;
            watcher.Filter = Path.GetFileName(ConfigFilePath);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime;

            watcher.Created += OnCreated;
            watcher.Changed += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Deleted += OnDeleted;

            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void OnChanged(object sender, FileSystemEventArgs e) => UpdateConfig();

        private void OnCreated(object sender, FileSystemEventArgs e) => UpdateConfig();

        private void OnDeleted(object sender, FileSystemEventArgs e)=> UpdateConfig();

        private void OnRenamed(object sender, RenamedEventArgs e) => UpdateConfig();
    }
}
