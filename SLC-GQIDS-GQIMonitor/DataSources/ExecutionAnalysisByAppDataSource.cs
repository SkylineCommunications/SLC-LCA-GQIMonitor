namespace GQI.DataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using GQI.Caches;
	using GQI.Converters;
	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "GQI Monitor - Execution analysis by app")]
	public sealed class ExecutionsAnalysisByAppDataSource : GQIMonitorLoader, IGQIDataSource, IGQIOnInit, IGQIInputArguments
	{
		private GQIDMS _dms;
		private IGQILogger _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;

			return default;
		}

		private const string Metric_QueryCount = "QueryCount";
		private const string Metric_UserCount = "UserCount";
		private const string Metric_AvgDuration = "AvgDuration";
		private const string Metric_MaxDuration = "MaxDuration";

		private static readonly GQIArgument<string> MetricArg = new GQIStringDropdownArgument("Metric", new[]
		{
			Metric_QueryCount,
			Metric_UserCount,
			Metric_AvgDuration,
			Metric_MaxDuration,
		})
		{
			IsRequired = true,
		};
		private static readonly GQIArgument<string> GroupByArg = new GQIStringDropdownArgument("Group by", new[]
		{
			MetricsAnalysisCache.MetricProperty_App,
			MetricsAnalysisCache.MetricProperty_User,
		})
		{
			IsRequired = true,
		};
		private static readonly GQIIntArgument LimitArg = new GQIIntArgument("Limit")
		{
			IsRequired = true,
			DefaultValue = 10,
		};
		private static readonly GQIDateTimeArgument StartTimeArg = new GQIDateTimeArgument("Start time")
		{
			IsRequired = false,
		};
		private static readonly GQIDateTimeArgument EndTimeArg = new GQIDateTimeArgument("End time")
		{
			IsRequired = false
		};
		private static readonly GQIStringArgument FilterArg = new GQIStringArgument("Filter")
		{
			IsRequired = false,
		};

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[]
			{
				MetricArg,
				GroupByArg,
				LimitArg,
				StartTimeArg,
				EndTimeArg,
				FilterArg,
			};
		}

		private string _metric = string.Empty;
		private string _groupBy = string.Empty;
		private int _limit = 0;
		private DateTime _startTime;
		private DateTime _endTime;
		private string _filter = string.Empty;


		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			_metric = args.GetArgumentValue(MetricArg);
			_groupBy = args.GetArgumentValue(GroupByArg);
			_limit = args.GetArgumentValue(LimitArg);

			if (!args.TryGetArgumentValue(StartTimeArg, out _startTime))
			{
				_startTime = DateTime.MinValue;
			}

			if (!args.TryGetArgumentValue(EndTimeArg, out _endTime))
			{
				_endTime = DateTime.MaxValue;
			}

			if (!args.TryGetArgumentValue(FilterArg, out _filter))
			{
				_filter = string.Empty;
			}

			return default;
		}

		private static readonly GQIStringColumn _appIdColumn = new GQIStringColumn("App ID");
		private static readonly GQIStringColumn _appNameColumn = new GQIStringColumn("App name");
		private static readonly GQIIntColumn _queryCountColumn = new GQIIntColumn("Query count");
		private static readonly GQIIntColumn _userCountColumn = new GQIIntColumn("User count");
		private static readonly GQIIntColumn _maxDurationColumn = new GQIIntColumn("Maximum duration (ms)");
		private static readonly GQIIntColumn _avgDurationColumn = new GQIIntColumn("Average duration (ms)");

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				_appIdColumn,
				_appNameColumn,
				_queryCountColumn,
				_userCountColumn,
				_maxDurationColumn,
				_avgDurationColumn,
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			AppInfoConverter.RefreshApplicationsCache(_dms, _logger);

			var filter = new MetricsAnalysisCache.Options
			{
				StartTime = _startTime,
				EndTime = _endTime,
				GroupBy = _groupBy,
				Filter = _filter,
			};

			var analysis = Cache.Instance.MetricsAnalysis.GetAnalysis(filter, _logger);

			var rows = Sort(analysis)
				.Take(_limit)
				.Select(ToRow)
				.ToArray();

			return new GQIPage(rows)
			{
				HasNextPage = false,
			};
		}

		private IEnumerable<KeyValuePair<string, MetricsAnalysisCache.Result>> Sort(Dictionary<string, MetricsAnalysisCache.Result> analysis)
		{
			switch (_metric)
			{
				case Metric_QueryCount:
					return analysis.OrderByDescending(x => x.Value.QueryCount);
				case Metric_UserCount:
					return analysis.OrderByDescending(x => x.Value.UserCount);
				case Metric_MaxDuration:
					return analysis.OrderByDescending(x => x.Value.MaxDuration);
				case Metric_AvgDuration:
					return analysis.OrderByDescending(x => x.Value.AvgDuration);
				default:
					throw new GenIfException($"Invalid sort metric '{_metric}'");
			}
		}

		private GQIRow ToRow(KeyValuePair<string, MetricsAnalysisCache.Result> result)
		{
			var appName = AppInfoConverter.GetAppName(result.Key);

			var cells = new[]
			{
				new GQICell { Value = result.Key },
				new GQICell { Value = appName },
				new GQICell { Value = result.Value.QueryCount },
				new GQICell { Value = result.Value.UserCount },
				new GQICell { Value = (int)Math.Round(result.Value.MaxDuration) },
				new GQICell { Value = (int)Math.Round(result.Value.AvgDuration) },
			};
			return new GQIRow(cells);
		}
	}
}
