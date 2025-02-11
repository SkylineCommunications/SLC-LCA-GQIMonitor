using GQI.Converters;
using Newtonsoft.Json;
using System;

namespace GQI
{
    internal sealed class Config
    {
        public string Mode { get; set; } = GQI.Mode.Live;

        public string Snapshot { get; set; } = string.Empty;

        public string AggregationTimeInterval { get; set; } = GQI.AggregationTimeInterval.OneHour;

        [JsonConverter(typeof(StringToTimespanConverter))]
        public TimeSpan MetricsCacheTTL { get; set; } = TimeSpan.FromMinutes(5);

        [JsonConverter(typeof(StringToTimespanConverter))]
        public TimeSpan LogsCacheTTL { get; set; } = TimeSpan.FromMinutes(5);

        [JsonConverter(typeof(StringToTimespanConverter))]
        public TimeSpan ApplicationsCacheTTL { get; set; } = TimeSpan.FromMinutes(15);

        [JsonConverter(typeof(StringToTimespanConverter))]
        public TimeSpan LiveMetricRefreshInterval { get; set; } = TimeSpan.FromSeconds(10);

        public int LiveMetricsHistory { get; set; } = 30;
    }
}
