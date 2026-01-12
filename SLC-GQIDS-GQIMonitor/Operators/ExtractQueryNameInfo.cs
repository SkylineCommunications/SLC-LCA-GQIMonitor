namespace GQI.Operators
{
	using GQI.Converters;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "GQI Monitor - ExtractQueryNameInfo")]
	public sealed class ExtractQueryNameInfoOperator : IGQIColumnOperator, IGQIRowOperator, IGQIInputArguments, IGQIOnInit
	{
		private static readonly GQIArgument<GQIColumn> _columnArg = new GQIColumnDropdownArgument("Query name column")
		{
			IsRequired = true,
		};

		private GQIColumn _queryNameColumn;
		private GQIStringColumn _appColumn = new GQIStringColumn("App");

		private AppNameConverter _appNameConverter;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_appNameConverter = new AppNameConverter(args.DMS, args.Logger);

			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[]
			{
				_columnArg,
			};
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			_queryNameColumn = args.GetArgumentValue(_columnArg);

			return default;
		}

		public void HandleColumns(GQIEditableHeader header)
		{
			header.AddColumns(_appColumn);
		}

		public void HandleRow(GQIEditableRow row)
		{
			var queryTag = row.GetValue<string>(_queryNameColumn);
			row.SetValue(_appColumn, _appNameConverter.GetAppNameByTag(queryTag));
		}
	}
}
