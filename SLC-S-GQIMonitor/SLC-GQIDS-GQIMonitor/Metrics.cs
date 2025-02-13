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
        public string Query { get; set; }

        [JsonConverter(typeof(MillisecondsToTimespanConverter))]
        public TimeSpan Duration { get; set; }

        public int Rows { get; set; }
    }

    public sealed class FirstPageDurationMetric : QueryDurationMetric
    {
    }

    public sealed class AllPagesDurationMetric : QueryDurationMetric
    {
        public int Pages { get; set; }
    }
}
