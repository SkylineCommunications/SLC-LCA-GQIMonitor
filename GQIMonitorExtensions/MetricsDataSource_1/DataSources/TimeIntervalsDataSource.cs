namespace MetricsDataSource_1.DataSources
{
    using MetricsDataSource_1.Operators;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using System.Linq;

    [GQIMetaData(Name = "GQI Monitor - Time intervals")]
    public sealed class TimeIntervalsDataSource : GQIMonitorLoader, IGQIDataSource
    {
        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Interval"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = QuantizeDateTimeOperator.IntervalOptions
                .Select(ToRow)
                .ToArray();

            return new GQIPage(rows)
            {
                HasNextPage = false,
            };
        }

        private GQIRow ToRow(string interval)
        {
            var cells = new GQICell[]
            {
                new GQICell { Value = interval },
            };
            return new GQIRow(cells);
        }
    }
}
