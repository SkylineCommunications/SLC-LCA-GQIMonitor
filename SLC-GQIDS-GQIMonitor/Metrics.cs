using System;
using GQI.Converters;
using Newtonsoft.Json;

namespace GQI
{
	public abstract class Metric
	{
		[JsonConverter(typeof(UnixTimeToDateTimeConverter))]
		public DateTime Time { get; set; }

		public string User { get; set; }
	}

	public sealed class RequestDurationMetric : Metric
	{
		public string Request { get; set; }

		[JsonConverter(typeof(MillisecondsToTimespanConverter))]
		public TimeSpan Duration { get; set; }
	}

	public abstract class QueryDurationMetric : Metric
	{
		public string Query
		{
			set
			{
				_app = AppNameConverter.GetAppId(value);
			}
		}

		[JsonConverter(typeof(MillisecondsToTimespanConverter))]
		public TimeSpan Duration { get; set; }

		public int Rows { get; set; }

		public string App => _app;

		private string _app;
	}

	public sealed class FirstPageDurationMetric : QueryDurationMetric
	{
	}

	public sealed class AllPagesDurationMetric : QueryDurationMetric
	{
		public int Pages { get; set; }
	}
}
