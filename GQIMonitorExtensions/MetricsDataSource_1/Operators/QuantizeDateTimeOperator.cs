using Skyline.DataMiner.Analytics.GenericInterface;
using System;

namespace MetricsDataSource_1.Operators
{
    [GQIMetaData(Name = "GQI Monitor - Quantize date/time")]
    public sealed class QuantizeDateTimeOperator : IGQIRowOperator, IGQIInputArguments
    {
        private static readonly GQIArgument<GQIColumn> _columnArg = new GQIColumnDropdownArgument("Column to quantize")
        {
            IsRequired = true,
            Types = new[] { GQIColumnType.DateTime },
        };

        public static readonly string[] IntervalOptions = new[]
        {
            AggregationTimeInterval.OneMinute,
            AggregationTimeInterval.FiveMinutes ,
            AggregationTimeInterval.FifteenMinutes ,
            AggregationTimeInterval.OneHour,
            AggregationTimeInterval.ThreeHours,
            AggregationTimeInterval.SixHours,
            AggregationTimeInterval.TwelveHours,
            AggregationTimeInterval.TwentyFourHours,
    };

        private static readonly GQIArgument<string> _defaultIntervalArg = new GQIStringDropdownArgument("Default quantization interval", IntervalOptions)
        {
            IsRequired = true,
            DefaultValue = AggregationTimeInterval.OneHour,
        };

        private static readonly GQIArgument<string> _feedIntervalArg = new GQIStringArgument("Feed quantization interval")
        {
            IsRequired = false,
            DefaultValue = null,
        };

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[]
            {
                _columnArg,
                _defaultIntervalArg,
                _feedIntervalArg,
            };
        }

        private GQIColumn<DateTime> _column;
        private Func<DateTime, DateTime> _quantizer;

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            _column = (GQIColumn<DateTime>)args.GetArgumentValue(_columnArg);

            string interval = GetIntervalValue(args);
            _quantizer = GetQuantizer(interval);

            return default;
        }

        private string GetIntervalValue(OnArgumentsProcessedInputArgs args)
        {
            if (!args.HasArgumentValue(_feedIntervalArg))
                return args.GetArgumentValue(_defaultIntervalArg);

            var interval = args.GetArgumentValue(_feedIntervalArg);
            if (string.IsNullOrEmpty(interval))
                return args.GetArgumentValue(_defaultIntervalArg);

            return interval;
        }

        public void HandleRow(GQIEditableRow row)
        {
            if (!row.TryGetValue(_column, out DateTime t))
            {
                row.Delete();
                return;
            }

            var quantized = _quantizer(t);
            row.SetValue(_column, quantized);
        }

        private static Func<DateTime, DateTime> GetQuantizer(string interval)
        {
            switch (interval)
            {
                case AggregationTimeInterval.OneMinute:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.FiveMinutes:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, (t.Minute / 5) * 5, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.FifteenMinutes:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, (t.Minute / 15) * 15, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.OneHour:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, 0, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.ThreeHours:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 3) * 3, 0, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.SixHours:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 6) * 6, 0, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.TwelveHours:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 12) * 12, 0, 0, DateTimeKind.Utc);
                case AggregationTimeInterval.TwentyFourHours:
                    return t => new DateTime(t.Year, t.Month, t.Day, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new GenIfException($"Invalid quantization interval: {interval}");
            }
        }
    }
}
