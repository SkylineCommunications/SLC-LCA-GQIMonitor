using Skyline.DataMiner.Analytics.GenericInterface;

namespace GQI.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Fix aggregated column")]
    public sealed class FixAggregationOperator : IGQIRowOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn> _columnArg = new GQIColumnDropdownArgument("Aggregation to fix")
        {
            Types = new[] { GQIColumnType.Double },
            IsRequired = true,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnArg,
            };
        }

        private GQIColumn<double> _column;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _column = (GQIColumn<double>)args.GetArgumentValue(_columnArg);
            return default;
        }

        public void HandleRow(GQIEditableRow row)
        {
            if (!row.TryGetValue(_column, out int integerValue))
                return;

            row.SetValue(_column, integerValue);
        }
    }
}
