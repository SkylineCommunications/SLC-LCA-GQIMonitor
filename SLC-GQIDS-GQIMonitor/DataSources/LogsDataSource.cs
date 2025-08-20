using GQI.Caches;
using Skyline.DataMiner.Analytics.GenericInterface;
using System.Linq;

namespace GQI.DataSources
{
    [GQIMetaData(Name = "GQI Monitor - Logs")]
    public sealed class LogsDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit
    {
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger;

            return default;
        }

        private static readonly GQIDateTimeColumn _timeColumn = new GQIDateTimeColumn("Time");
        private static readonly GQIStringColumn _levelColumn = new GQIStringColumn("Level");
        private static readonly GQIStringColumn _messageColumn = new GQIStringColumn("Message");
        private static readonly GQIIntColumn _requestIdColumn = new GQIIntColumn("Request ID");

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                _timeColumn,
                _levelColumn,
                _messageColumn,
                _requestIdColumn,
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var logs = Cache.Instance.Logs.GetLogs(_logger);
            var rows = logs.Logs
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow ToRow(LogCollection.Log log)
        {
            var cells = new[]
            {
                new GQICell { Value = log.Time },
                new GQICell { Value = log.Level },
                new GQICell { Value = log.Message },
                new GQICell { Value = log.RequestId },
            };
            return new GQIRow(cells);
        }

    }
}
