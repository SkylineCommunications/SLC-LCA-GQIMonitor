namespace GQI.Operators
{
	using System;
	using System.Collections.Generic;
	using GQI.Caches;
	using GQIMonitor;
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

        private Lazy<Dictionary<string, Application>> _applications;

        private GQIDMS _dms;
        private IGQILogger _logger;

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;

			_applications = new Lazy<Dictionary<string, Application>>(GetApplications);

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
			var queryName = row.GetValue<string>(_queryNameColumn);
			var queryData = queryName.Split('/');

			if (queryData[0] == "app")
			{
				var applications = _applications.Value;
				if (applications.TryGetValue(queryData[1], out Application app))
				{
					row.SetValue(_appColumn, app.Name);
				}
			}
			else
			{
				row.SetValue(_appColumn, "<Other>");
			}
		}

        private Dictionary<string, Application> GetApplications()
		{
			return Cache.Instance.Applications
				.GetApplications(_dms, _logger);
		}
	}
}
