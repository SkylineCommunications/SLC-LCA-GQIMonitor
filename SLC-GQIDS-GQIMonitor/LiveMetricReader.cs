using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;

namespace GQI
{
	internal sealed class LiveMetricReader
	{
		private const int MaxFileRead = 3;

		private const string FilePattern = "metrics*.txt";
		private readonly string _metricsFolderPath;

		private Dictionary<string, long> _oldFileSizes;
		private Dictionary<string, long> _newFileSizes;

		public LiveMetricReader(string metricsFolderPath)
		{
			_metricsFolderPath = metricsFolderPath;
			_oldFileSizes = GetFileSizes();
			_newFileSizes = new Dictionary<string, long>();
		}

		private IEnumerable<FileInfo> GetFiles()
		{
			var filePaths = Directory.GetFiles(_metricsFolderPath, FilePattern);
			foreach (var filePath in filePaths)
			{
				if (TryGetFileInfo(filePath, out var fileInfo))
					yield return fileInfo;
			}
		}

		private IEnumerable<FileInfo> GetMostRecentFiles()
		{
			return GetFiles()
				.OrderByDescending(file => file.LastWriteTimeUtc)
				.Take(MaxFileRead);
		}

		private Dictionary<string, long> GetFileSizes()
		{
			try
			{
				var fileSizes = new Dictionary<string, long>();

				var files = GetMostRecentFiles();
				foreach (var file in files)
				{
					fileSizes[file.FullName] = file.Length;
				}

				return fileSizes;
			}
			catch (Exception ex)
			{
				throw new GenIfException("Failed to initialize metric reader.", ex);
			}
		}

		public List<string> ReadLines()
		{
			try
			{
				var lines = new List<string>();

				var files = GetMostRecentFiles();

				foreach (var file in files)
				{
					var filePath = file.FullName;
					if (!_oldFileSizes.TryGetValue(filePath, out var oldFileSize))
						oldFileSize = 0;

					var newFileSize = file.Length;
					_newFileSizes[filePath] = newFileSize;

					if (newFileSize <= oldFileSize)
						continue;

					ReadLines(filePath, oldFileSize, lines);
				}

				(_oldFileSizes, _newFileSizes) = (_newFileSizes, _oldFileSizes);
				_newFileSizes.Clear();

				return lines;
			}
			catch (Exception ex)
			{
				throw new GenIfException($"Failed to read metrics", ex);
			}
		}

		private static void ReadLines(string filePath, long offset, List<string> lines)
		{
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
