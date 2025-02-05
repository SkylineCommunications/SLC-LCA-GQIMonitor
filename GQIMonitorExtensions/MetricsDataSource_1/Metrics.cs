using MetricsDataSource_1.Converters;
using Newtonsoft.Json;
using System;

namespace MetricsDataSource_1
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

    public sealed class FirstPageDurationMetric : Metric
    {
        public int Rows { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(MillisecondsToTimespanConverter))]
        public TimeSpan Duration { get; set; }
    }

    public sealed class AllPagesDurationMetric : Metric
    {
        public int Rows { get; set; }

        public int Pages { get; set; }

        public string Query { get; set; }

        [JsonConverter(typeof(MillisecondsToTimespanConverter))]
        public TimeSpan Duration { get; set; }
    }
}
