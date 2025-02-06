using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MetricsDataSource_1
{
    internal sealed class LogCollection
    {
        public static LogCollection Parse(string directoryPath, IGQILogger logger)
        {
            logger.Information($"Parsing logs in directory \"{directoryPath}\"");
            var collection = new LogCollection();

            if (!Directory.Exists(directoryPath))
            {
                logger.Warning($"Log directory \"{directoryPath}\" does not exist");
                return collection;
            }

            var filePaths = Directory.GetFiles(directoryPath, "log*.txt");
            logger.Information($"Found {filePaths.Length} log files");

            foreach (var filePath in filePaths)
            {
                collection.AddLogFile(filePath, logger);
            }

            return collection;
        }

        private const string _dateFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
        private const DateTimeStyles _dateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        private static readonly Regex _logRegex = new Regex(@"^\[(?<time>.*) (?<level>\S{3})\] (?<message>.*) {""RequestID"":(?<requestId>\d*)}$", RegexOptions.Compiled);

        public IReadOnlyList<Log> Logs => _logs;

        private readonly List<Log> _logs;

        private LogCollection()
        {
            _logs = new List<Log>();
        }

        private void AddLogFile(string filePath, IGQILogger logger)
        {
            var fileName = Path.GetFileName(filePath);
            logger.Information($"Parsing logs from \"{fileName}\"");

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line is null)
                            break;
                        var log = GetLog(line, logger);
                        if (log is null)
                            continue;

                        _logs.Add(log);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new GenIfException($"Failed parsing logs from \"{fileName}\": {ex.Message}");
            }
        }

        private static Log GetLog(string line, IGQILogger logger)
        {
            try
            {
                var match = _logRegex.Match(line);
                if (!match.Success)
                    return null;

                var dateString = match.Groups["time"].Value;
                if (!DateTime.TryParseExact(dateString, _dateFormat, _culture, _dateTimeStyles, out var time))
                    return null;

                var requestIdString = match.Groups["requestId"].Value;
                if (!int.TryParse(requestIdString, out int requestId))
                    return null;

                return new Log
                {
                    Time = time,
                    Level = match.Groups["level"].Value,
                    Message = match.Groups["message"].Value,
                    RequestId = requestId,
                };
            }
            catch (Exception ex)
            {
                logger.Warning($"Error parsing log line: {ex.Message}");
                return null;
            }
        }

        public sealed class Log
        {
            public DateTime Time { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
            public int RequestId { get; set; }
        }
    }
}
