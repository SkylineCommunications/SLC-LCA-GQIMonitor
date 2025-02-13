using System;
using System.Collections.Generic;

namespace GQI
{
    public sealed class Bucket
    {
        public string Key => $"{Bounds.Start.Ticks}";
        public BucketBounds Bounds { get; set; }
        public List<QueryDurationMetric> Metrics { get; }

        public Bucket(BucketBounds bounds)
        {
            Bounds = bounds;
            Metrics = new List<QueryDurationMetric>();
        }
    }

    public readonly struct BucketBounds
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public BucketBounds(long start, long end)
        {
            Start = new DateTime(start, DateTimeKind.Utc);
            End = new DateTime(end, DateTimeKind.Utc);
        }

        private BucketBounds(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public BucketBounds StartAt(DateTime start)
        {
            return new BucketBounds(start, End);
        }

        public BucketBounds EndAt(DateTime end)
        {
            return new BucketBounds(Start, end);
        }
    }
}
