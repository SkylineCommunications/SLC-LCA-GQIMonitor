using GQI.Converters;
using Newtonsoft.Json;
using System;

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
                _app = GetAppId(value);
            }
        }

        [JsonConverter(typeof(MillisecondsToTimespanConverter))]
        public TimeSpan Duration { get; set; }

        public int Rows { get; set; }

        public string App => _app;

        private string _app;

        private static string GetAppId(string queryTag)
        {
            if (queryTag.Length < 40)
                return null;

            if (!queryTag.StartsWith("app/"))
                return null;

            return queryTag.Substring(4, 36);
        }
    }

    public sealed class FirstPageDurationMetric : QueryDurationMetric
    {
    }

    public sealed class AllPagesDurationMetric : QueryDurationMetric
    {
        public int Pages { get; set; }
    }
}
