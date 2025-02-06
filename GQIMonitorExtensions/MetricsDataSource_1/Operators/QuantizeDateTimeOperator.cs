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

        private const string INTERVAL_1_MINUTE = "1 minute";
        private const string INTERVAL_5_MINUTES = "5 minutes";
        private const string INTERVAL_15_MINUTES = "15 minutes";
        private const string INTERVAL_1_HOUR = "1 hour";
        private const string INTERVAL_3_HOURS = "3 hours";
        private const string INTERVAL_6_HOURS = "6 hours";
        private const string INTERVAL_12_HOURS = "12 hours";
        private const string INTERVAL_24_HOURS = "24 hours";

        public static readonly string[] IntervalOptions = new[]
        {
            INTERVAL_1_MINUTE,
            INTERVAL_5_MINUTES,
            INTERVAL_15_MINUTES,
            INTERVAL_1_HOUR,
            INTERVAL_3_HOURS,
            INTERVAL_6_HOURS,
            INTERVAL_12_HOURS,
            INTERVAL_24_HOURS,
        };

        private static readonly GQIArgument<string> _defaultIntervalArg = new GQIStringDropdownArgument("Default quantization interval", IntervalOptions)
        {
            IsRequired = true,
            DefaultValue = INTERVAL_5_MINUTES,
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
                case INTERVAL_1_MINUTE:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
                case INTERVAL_5_MINUTES:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, (t.Minute / 5) * 5, 0, DateTimeKind.Utc);
                case INTERVAL_15_MINUTES:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, (t.Minute / 15) * 15, 0, DateTimeKind.Utc);
                case INTERVAL_1_HOUR:
                    return t => new DateTime(t.Year, t.Month, t.Day, t.Hour, 0, 0, DateTimeKind.Utc);
                case INTERVAL_3_HOURS:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 3) * 3, 0, 0, DateTimeKind.Utc);
                case INTERVAL_6_HOURS:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 6) * 6, 0, 0, DateTimeKind.Utc);
                case INTERVAL_12_HOURS:
                    return t => new DateTime(t.Year, t.Month, t.Day, (t.Hour / 12) * 12, 0, 0, DateTimeKind.Utc);
                case INTERVAL_24_HOURS:
                    return t => new DateTime(t.Year, t.Month, t.Day, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new GenIfException($"Invalid quantization interval: {interval}");
            }
        }
    }
}
