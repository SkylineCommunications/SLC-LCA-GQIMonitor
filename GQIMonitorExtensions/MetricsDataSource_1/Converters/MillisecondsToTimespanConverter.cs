using Newtonsoft.Json;
using System;

namespace MetricsDataSource_1.Converters
{
    internal sealed class MillisecondsToTimespanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(int);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                long durationInMs = (long)reader.Value;
                return TimeSpan.FromMilliseconds(durationInMs);
            }

            throw new JsonSerializationException("Unexpected token type.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
