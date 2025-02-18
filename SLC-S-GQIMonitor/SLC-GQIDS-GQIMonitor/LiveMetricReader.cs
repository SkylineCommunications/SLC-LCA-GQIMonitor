using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.IO;

namespace GQI
{
    internal sealed class LiveMetricReader : IDisposable
    {
        private const string FilePattern = "metrics*.txt";
        private readonly object _lock = new object();
        private readonly FileSystemWatcher _directoryWatcher;
        private readonly HashSet<string> _changedFilePaths;
        private readonly Dictionary<string, long> _fileSizes;

        private bool _isDisposed = false;

        public LiveMetricReader(string metricsFolderPath)
        {
            _changedFilePaths = new HashSet<string>();
            _fileSizes = GetInitialFileSizes(metricsFolderPath);

            _directoryWatcher = new FileSystemWatcher(metricsFolderPath);
            _directoryWatcher.Filter = FilePattern;
            _directoryWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            _directoryWatcher.Changed += OnFileChanged;
            _directoryWatcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _directoryWatcher.Dispose();
        }

        private Dictionary<string, long> GetInitialFileSizes(string metricsFolderPath)
        {
            var fileSizes = new Dictionary<string, long>();

            var filePaths = Directory.GetFiles(metricsFolderPath, FilePattern);
            foreach (var filePath in filePaths)
            {
                if (!TryGetFileInfo(filePath, out var fileInfo))
                    continue;

                fileSizes[filePath] = fileInfo.Length;
            }

            return fileSizes;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _changedFilePaths.Add(e.FullPath);
            }
        }

        public List<string> ReadLines()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LiveMetricReader));

            var lines = new List<string>();
            lock (_lock)
            {
                foreach (var filePath in _changedFilePaths)
                {
                    if (!_fileSizes.TryGetValue(filePath, out var oldFileSize))
                        oldFileSize = 0;

                    long newFileSize = ReadLines(filePath, oldFileSize, lines);
                    _fileSizes[filePath] = newFileSize;
                }
                _changedFilePaths.Clear();
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

                if (!TryGetFileInfo(filePath, out var fileInfo))
                    return 0;

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

        private static bool TryGetFileInfo(string filePath, out FileInfo fileInfo)
        {
            try
            {
                fileInfo = new FileInfo(filePath);
                return true;
            }
            catch
            {
                fileInfo = null;
                return false;
            }
        }
    }
}
