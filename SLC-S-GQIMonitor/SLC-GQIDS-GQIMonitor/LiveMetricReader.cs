using GQI.Caches;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GQI
{
    internal sealed class LiveMetricReader : IDisposable
    {
        private const string FilePattern = "metrics*.txt";
        private readonly object _lock = new object();
        private readonly FileSystemWatcher _directoryWatcher;
        private readonly List<string> _newFilePaths;

        private long _lastFileSize = 0;
        private string _lastFilePath = null;
        private bool _isDisposed = false;

        public LiveMetricReader(string metricsFolderPath)
        {
            _directoryWatcher = new FileSystemWatcher(metricsFolderPath);
            _directoryWatcher.Filter = FilePattern;
            _directoryWatcher.NotifyFilter = NotifyFilters.CreationTime;
            _directoryWatcher.Created += OnNewFileCreated;
            _directoryWatcher.EnableRaisingEvents = true;

            _newFilePaths = GetInitialMetricFilePaths(metricsFolderPath);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _directoryWatcher.Dispose();
        }

        private List<string> GetInitialMetricFilePaths(string metricsFolderPath)
        {
            return Directory.GetFiles(metricsFolderPath, FilePattern)
                .OrderBy(File.GetLastWriteTimeUtc)
                .ToList();
        }

        private void OnNewFileCreated(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _newFilePaths.Add(e.FullPath);
            }
        }

        public List<string> ReadLines()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LiveMetricReader));

            var lines = new List<string>();
            lock (_lock)
            {
                _lastFileSize = ReadLines(_lastFilePath, _lastFileSize, lines);
                foreach (var filePath in _newFilePaths)
                {
                    _lastFilePath = filePath;
                    _lastFileSize = ReadLines(filePath, 0, lines);
                }
                _newFilePaths.Clear();
            }
            return lines;

        }

        private static long ReadLines(string filePath, long offset, List<string> lines)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return 0;

                if (!File.Exists(filePath))
                    return 0;

                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;
                if (fileSize <= offset)
                    return offset;

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    stream.Seek(offset, SeekOrigin.Begin);

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        lines.Add(line);
                    }
                }
                return fileSize;
            }
            catch (Exception ex)
            {
                throw new GenIfException($"Failed to read metrics", ex);
            }
        }
    }
}
