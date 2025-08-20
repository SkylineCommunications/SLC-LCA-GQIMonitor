using Skyline.DataMiner.Analytics.GenericInterface;

namespace GQI.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Default if empty")]
    public sealed class DefaultIfEmptyOperator : IGQIRowOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn> _columnArg = new GQIColumnDropdownArgument("Column")
        {
            Types = new[] { GQIColumnType.String },
            IsRequired = true,
        };

        private static readonly GQIArgument<string> _defaultValueArg = new GQIStringArgument("Default value")
        {
            IsRequired = true,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnArg,
                _defaultValueArg,
            };
        }

        private GQIColumn<string> _column;
        private string _defaultValue;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _column = (GQIColumn<string>)args.GetArgumentValue(_columnArg);
            _defaultValue = args.GetArgumentValue(_defaultValueArg);

            return default;
        }

        public void HandleRow(GQIEditableRow row)
        {
            if (!IsEmpty(row))
                return;

            row.SetValue(_column, _defaultValue);
        }

        private bool IsEmpty(GQIEditableRow row)
        {
            return !row.TryGetValue(_column, out string value) || string.IsNullOrEmpty(value);
        }
    }
}
