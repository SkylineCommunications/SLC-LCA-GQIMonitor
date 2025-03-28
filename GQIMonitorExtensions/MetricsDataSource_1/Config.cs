﻿using MetricsDataSource_1.Converters;
using Newtonsoft.Json;
using System;

namespace MetricsDataSource_1
{
    internal sealed class Config
    {
        public string Mode { get; set; } = MetricsDataSource_1.Mode.Live;

        public string Snapshot { get; set; } = string.Empty;

        public string AggregationTimeInterval { get; set; } = MetricsDataSource_1.AggregationTimeInterval.OneHour;

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
