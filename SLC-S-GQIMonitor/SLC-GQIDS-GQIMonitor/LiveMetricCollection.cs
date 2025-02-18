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
        public bool IsLive => _isLive;

        private readonly IGQIProvider _gqiProvider;
        private readonly object _lock = new object();
        private readonly int _bucketCount;
        private readonly long _bucketTickSize;
        private readonly Queue<Bucket> _buckets;

        private LiveMetricReader _reader;
        private Timer _timer;

        private Bucket _overflowBucket;
        private long _startTicks;
        private long _endTicks;
        private long _lastUpdatedTicks;
        private bool _isLive = false;

        public LiveMetricCollection(IGQIProvider gqiProvider, ConfigCache configCache)
        {
            _gqiProvider = gqiProvider;

            var config = configCache.GetConfig();
            _bucketCount = Math.Max(0, config.LiveMetricsHistory);

            _buckets = new Queue<Bucket>(_bucketCount);

            var refreshInterval = config.LiveMetricRefreshInterval;
            if (refreshInterval < MinRefreshInterval)
                refreshInterval = MinRefreshInterval;
            _bucketTickSize = refreshInterval.Ticks;
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

        public void Toggle()
        {
            Bucket[] bucketsToRemove = null;
            Bucket[] bucketsToAdd = null;

            lock (_lock)
            {
                if (_isLive)
                {
                    Stop();
                    _isLive = false;
                }
                else
                {
                    bucketsToRemove = Reset();
                    bucketsToAdd = Start();
                    _isLive = true;
                }
            }

            if (bucketsToRemove != null)
            {
                foreach (var bucket in bucketsToRemove)
                {
                    BucketRemoved?.Invoke(bucket.Key);
                }
            }

            if (bucketsToAdd != null)
            {
                foreach (var bucket in bucketsToAdd)
                {
                    BucketAdded?.Invoke(bucket);
                }
            }

            Updated?.Invoke();
        }

        private Bucket[] Reset()
        {
            var bucketsToRemove = _buckets.ToArray();
            _buckets.Clear();
            return bucketsToRemove;
        }

        private Bucket[] Start()
        {
            UpdateTimeRange();
            _reader = new LiveMetricReader(_gqiProvider.MetricsPath);

            _overflowBucket = new Bucket(GetBucketBounds(_bucketCount));
            var bucketsToAdd = Enumerable.Range(0, _bucketCount)
                .Select(bucketIndex => new Bucket(GetBucketBounds(bucketIndex)))
                .ToArray();
            foreach (var bucket in bucketsToAdd)
            {
                _buckets.Enqueue(bucket);
            }

            var refreshInterval = new TimeSpan(_bucketTickSize);
            _timer = new Timer(Update, null, refreshInterval, refreshInterval);

            return bucketsToAdd;
        }

        private void Stop()
        {
            _reader?.Dispose();
            _reader = null;

            _timer?.Dispose();
            _timer = null;
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
            catch
            {
                // Prevent crashing
            }
        }

        private void Update()
        {
            var reader = _reader;
            if (reader is null)
                return;

            var lines = reader.ReadLines();
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
                if (!_isLive)
                    return;

                bucketToRemove = _buckets.Dequeue();
                _buckets.Enqueue(currentBucket);
            }

            BucketRemoved?.Invoke(bucketToRemove.Key);
            BucketAdded?.Invoke(currentBucket);
            Updated?.Invoke();
        }
    }
}
