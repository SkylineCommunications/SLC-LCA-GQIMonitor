using GQIMonitor;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.IO;
using System.Linq;

namespace GQI.Caches
{
    internal sealed class SnapshotsCache : IDisposable
    {
        private const string SnapshotsPath = Info.DocumentsPath + @"\Snapshots";

        private readonly object _lock = new object();

        private string[] _snapshots = null;
        private FileSystemWatcher _watcher = null;

        public event Action<string> Added;
        public event Action<string> Removed;

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
            _snapshots = null;
        }

        public string[] GetSnapshots()
        {
            if (_snapshots != null)
                return _snapshots;

            lock (_lock)
            {
                if (_snapshots != null)
                    return _snapshots;

                _snapshots = ReadSnapshots();
                _watcher = WatchChanges();
            }

            return _snapshots;
        }

        private string[] ReadSnapshots()
        {
            try
            {
                if (!Directory.Exists(SnapshotsPath))
                    return Array.Empty<string>();

                var snapshotPaths = Directory.GetDirectories(SnapshotsPath);
                return snapshotPaths
                    .Select(path => Path.GetFileName(path))
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new GenIfException("Failed to read config file.", ex);
            }
        }

        private void UpdateSnapshots()
        {
            try
            {
                lock (_lock)
                {
                    _snapshots = ReadSnapshots();
                }
            }
            catch
            {
                // Ignore exceptions
            }
        }

        private FileSystemWatcher WatchChanges()
        {
            if (!Directory.Exists(SnapshotsPath))
                return null;

            var watcher = new FileSystemWatcher();

            watcher.Path = SnapshotsPath;
            watcher.Filter = "*";
            watcher.NotifyFilter = NotifyFilters.DirectoryName;

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            UpdateSnapshots();
            Added?.Invoke(e.Name);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            UpdateSnapshots();
            Removed?.Invoke(e.Name);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            UpdateSnapshots();
            Removed?.Invoke(e.OldName);
            Added?.Invoke(e.Name);
        }
    }
}
