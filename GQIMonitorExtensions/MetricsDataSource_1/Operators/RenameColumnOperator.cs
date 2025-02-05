using Skyline.DataMiner.Analytics.GenericInterface;

namespace MetricsDataSource_1.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Rename column")]
    public sealed class RenameColumnOperator : IGQIColumnOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn> _columnArg = new GQIColumnDropdownArgument("Column to rename")
        {
            IsRequired = true,
        };

        private static readonly GQIArgument<string> _nameArg = new GQIStringArgument("New name")
        {
            IsRequired = true,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnArg,
                _nameArg,
            };
        }

        private GQIColumn _column;
        private string _name;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _column = args.GetArgumentValue(_columnArg);
            _name = args.GetArgumentValue(_nameArg);

            return default;
        }

        public void HandleColumns(GQIEditableHeader header)
        {
            header.RenameColumn(_column, _name);
        }
    }
}
