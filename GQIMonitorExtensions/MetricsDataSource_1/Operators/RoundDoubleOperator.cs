using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Linq;

namespace MetricsDataSource_1.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Round double columns")]
    public sealed class RoundDoubleOperator : IGQIRowOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn[]> _columnsArg = new GQIColumnListArgument("Columns")
        {
            Types = new[] { GQIColumnType.Double },
            IsRequired = true,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnsArg,
            };
        }

        private GQIColumn<double>[] _columns;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            var columns = args.GetArgumentValue(_columnsArg);
            _columns = columns
                .Cast<GQIColumn<double>>()
                .ToArray();

            return default;
        }

        public void HandleRow(GQIEditableRow row)
        {
            foreach (var column in _columns)
            {
                if (!row.TryGetValue(column, out double value))
                    continue;

                var rounded = Math.Round(value);
                row.SetValue(column, rounded);
            }
        }
    }
}
