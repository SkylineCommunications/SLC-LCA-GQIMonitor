using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MetricsDataSource_1
{
    internal sealed class LiveMetricCollection : IDisposable
    {
        private const int BucketCount = 15;

        public static readonly GQIColumn[] Columns = new GQIColumn[]
        {
            new GQIDateTimeColumn("Start time"),
            new GQIDateTimeColumn("End time"),
            new GQIIntColumn("Query count"),
            new GQIDoubleColumn("Average duration"),
            new GQIDoubleColumn("Maximum duration"),
        };

        private static readonly TimeSpan RefreshRate = TimeSpan.FromSeconds(10);
        private static readonly long BucketTickSize = RefreshRate.Ticks;

        public event Action Updated;
        public event Action<string> RowRemoved;
        public event Action<GQIRow> RowAdded;

        public GQIRow[] Metrics => _metrics.ToArray();

        public DateTime StartTime => new DateTime(_startTicks, DateTimeKind.Utc);
        public DateTime EndTime => new DateTime(_endTicks, DateTimeKind.Utc);
        public DateTime LastUpdated => new DateTime(_lastUpdatedTicks, DateTimeKind.Utc); 

        private readonly Queue<GQIRow> _metrics;
        private readonly LiveMetricReader _reader;
        private readonly Timer _timer;

        private List<QueryDurationMetric> _overflowBucket;
        private long _startTicks;
        private long _endTicks;
        private long _lastUpdatedTicks;

        public LiveMetricCollection()
        {
            _reader = new LiveMetricReader();
            _timer = new Timer(Update, null, RefreshRate, RefreshRate);
            _metrics = GetInitialMetrics();
        }

        public void Dispose()
        {
            _timer.Dispose();
            _reader.Dispose();
        }

        private Queue<GQIRow> GetInitialMetrics()
        {
            var buckets = Enumerable.Range(0, BucketCount)
                .Select(bucketIndex => new List<QueryDurationMetric>())
                .ToArray();

            var lines = _reader.ReadLines();
            UpdateTimeRange();
            _overflowBucket = new List<QueryDurationMetric>();
            foreach (var line in lines)
            {
                var metric = MetricCollection.ParseQueryDurationMetric(line);
                if (metric is null)
                    continue;

                var bucketIndex = GetBucketIndex(metric.Time.Ticks);
                if (bucketIndex < 0)
                {
                    // Too old
                    continue;
                }

                if (bucketIndex >= BucketCount)
                {
                    // Too new
                    _overflowBucket.Add(metric);
                    continue;
                }

                buckets[bucketIndex].Add(metric);
            }

            return new Queue<GQIRow>(buckets.Select(ToRow));
        }

        private int GetBucketIndex(long metricTicks)
        {
            var ticksFromStart = metricTicks - _startTicks;
            return (int)(ticksFromStart / BucketTickSize);
        }

        private (long Start, long End) GetBucketBounds(int bucketIndex)
        {
            var bucketStartTicks = _startTicks + bucketIndex * BucketTickSize;
            var bucketEndTicks = bucketStartTicks + BucketTickSize;
            return (bucketStartTicks, bucketEndTicks);
        }

        private GQIRow ToRow(List<QueryDurationMetric> metrics, int bucketIndex)
        {
            var count = metrics.Count;
            var bounds = GetBucketBounds(bucketIndex);

            var avgDuration = GetAvgDuration(metrics);
            var maxDuration = GetMaxDuration(metrics);

            var cells = new GQICell[]
            {
                new GQICell { Value = new DateTime(bounds.Start, DateTimeKind.Utc) },
                new GQICell { Value = new DateTime(bounds.End, DateTimeKind.Utc) },
                new GQICell { Value = count },
                new GQICell { Value = avgDuration },
                new GQICell { Value = maxDuration },
            };
            return new GQIRow($"{bounds.Start}", cells);
        }

        private static double GetAvgDuration(List<QueryDurationMetric> metrics)
        {
            if (metrics.Count == 0)
                return 0;
            return metrics.Average(metric => metric.Duration.TotalMilliseconds);
        } 

        private static double GetMaxDuration(List<QueryDurationMetric> metrics)
        {
            if (metrics.Count == 0)
                return 0;
            return metrics.Max(metric => metric.Duration.TotalMilliseconds);
        }

        private void UpdateTimeRange()
        {
            _lastUpdatedTicks = DateTime.UtcNow.Ticks;
            _endTicks = (_lastUpdatedTicks / BucketTickSize) * BucketTickSize;
            _startTicks = _endTicks - (BucketCount * BucketTickSize);
        }

        private void Update(object state)
        {
            try
            {
                Update();
            }
            catch (Exception ex)
            {
                // Prevent crashing
            }
        }

        private void Update()
        {
            var lines = _reader.ReadLines();

            var currentBucket = _overflowBucket;
            _overflowBucket = new List<QueryDurationMetric>();
            foreach (var line in lines)
            {
                var metric = MetricCollection.ParseQueryDurationMetric(line);
                if (metric is null)
                    continue;

                var bucketIndex = GetBucketIndex(metric.Time.Ticks);
                if (bucketIndex < BucketCount)
                {
                    // Too old
                    continue;
                }

                if (bucketIndex > BucketCount)
                {
                    // Too new
                    _overflowBucket.Add(metric);
                    continue;
                }

                currentBucket.Add(metric);
            }

            var rowToRemove = _metrics.Dequeue();
            var rowToAdd = ToRow(currentBucket, BucketCount);
            _metrics.Enqueue(rowToAdd);

            UpdateTimeRange();

            RowRemoved?.Invoke(rowToRemove.Key);
            RowAdded?.Invoke(rowToAdd);
            Updated?.Invoke();
        }
    }
}
