using Skyline.DataMiner.Analytics.GenericInterface;
using System.Linq;

namespace MetricsDataSource_1.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Format double columns")]
    public sealed class FormatDoubleOperator : IGQIRowOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn[]> _columnsArg = new GQIColumnListArgument("Columns")
        {
            Types = new[] { GQIColumnType.Double },
            IsRequired = true,
        };

        private static readonly GQIArgument<string> _formatArg = new GQIStringArgument("Format")
        {
            IsRequired = true,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnsArg,
                _formatArg,
            };
        }

        private GQIColumn<double>[] _columns;
        private string _format;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            var columns = args.GetArgumentValue(_columnsArg);
            _columns = columns
                .Cast<GQIColumn<double>>()
                .ToArray();

            _format = args.GetArgumentValue(_formatArg);

            return default;
        }

        public void HandleRow(GQIEditableRow row)
        {
            foreach (var column in _columns)
            {
                if (!row.TryGetValue(column, out double value))
                    continue;

                var formatted = value.ToString(_format);
                row.SetDisplayValue(column, formatted);
            }
        }
    }
}
