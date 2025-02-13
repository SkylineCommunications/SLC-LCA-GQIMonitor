using GQI.Caches;
using GQIMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GQI
{
    internal sealed class LiveMetricCollection : IDisposable
    {
        private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(1);

        public event Action Updated;
        public event Action<string> BucketRemoved;
        public event Action<Bucket> BucketAdded;

        public DateTime StartTime => new DateTime(_startTicks, DateTimeKind.Utc);
        public DateTime EndTime => new DateTime(_endTicks, DateTimeKind.Utc);
        public DateTime LastUpdated => new DateTime(_lastUpdatedTicks, DateTimeKind.Utc);

        private readonly object _lock = new object();
        private readonly int _bucketCount;
        private readonly long _bucketTickSize;
        private readonly Queue<Bucket> _buckets;
        private readonly LiveMetricReader _reader;
        private readonly Timer _timer;

        private Bucket _overflowBucket;
        private long _startTicks;
        private long _endTicks;
        private long _lastUpdatedTicks;

        public LiveMetricCollection(IGQIProvider gqiProvider, ConfigCache configCache)
        {
            var config = configCache.GetConfig();
            _bucketCount = Math.Max(0, config.LiveMetricsHistory);
            
            var refreshInterval = config.LiveMetricRefreshInterval;
            if (refreshInterval < MinRefreshInterval)
                refreshInterval = MinRefreshInterval;
            _bucketTickSize = refreshInterval.Ticks;

            _reader = new LiveMetricReader(gqiProvider.MetricsPath);
            _timer = new Timer(Update, null, refreshInterval, refreshInterval);
            _buckets = GetInitialBuckets();
        }

        public void Dispose()
        {
            _timer.Dispose();
            _reader.Dispose();
        }

        public Bucket[] GetBuckets()
        {
            lock (_lock)
            {
                return _buckets.ToArray();
            }
        }

        private Queue<Bucket> GetInitialBuckets()
        {
            var lines = _reader.ReadLines();

            UpdateTimeRange();

            var buckets = Enumerable.Range(0, _bucketCount)
                .Select(bucketIndex => new Bucket(GetBucketBounds(bucketIndex)))
                .ToArray();

            _overflowBucket = new Bucket(GetBucketBounds(_bucketCount));
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

                if (bucketIndex >= _bucketCount)
                {
                    // Too new
                    _overflowBucket.Metrics.Add(metric);
                    continue;
                }

                buckets[bucketIndex].Metrics.Add(metric);
            }

            return new Queue<Bucket>(buckets);
        }

        private int GetBucketIndex(long metricTicks)
        {
            var ticksFromStart = metricTicks - _startTicks;
            return (int)(ticksFromStart / _bucketTickSize);
        }

        private BucketBounds GetBucketBounds(int bucketIndex)
        {
            var bucketStartTicks = _startTicks + bucketIndex * _bucketTickSize;
            var bucketEndTicks = bucketStartTicks + _bucketTickSize;
            return new BucketBounds(bucketStartTicks, bucketEndTicks);
        }

        private void UpdateTimeRange()
        {
            _lastUpdatedTicks = DateTime.UtcNow.Ticks;
            _endTicks = (_lastUpdatedTicks / _bucketTickSize) * _bucketTickSize;
            _startTicks = _endTicks - (_bucketCount * _bucketTickSize);
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
            UpdateTimeRange();

            var currentBucket = _overflowBucket;
            _overflowBucket = new Bucket(GetBucketBounds(_bucketCount));
            foreach (var line in lines)
            {
                var metric = MetricCollection.ParseQueryDurationMetric(line);
                if (metric is null)
                    continue;

                var bucketIndex = GetBucketIndex(metric.Time.Ticks);
                if (bucketIndex < _bucketCount - 1)
                {
                    // Too old
                    continue;
                }

                if (bucketIndex >= _bucketCount)
                {
                    // Too new
                    _overflowBucket.Metrics.Add(metric);
                    continue;
                }

                currentBucket.Metrics.Add(metric);
            }

            Bucket bucketToRemove = null;
            lock (_lock)
            {
                bucketToRemove = _buckets.Dequeue();
                _buckets.Enqueue(currentBucket);
            }

            BucketRemoved?.Invoke(bucketToRemove.Key);
            BucketAdded?.Invoke(currentBucket);
            Updated?.Invoke();
        }
    }
}
