using Newtonsoft.Json;
using System;

namespace MetricsDataSource_1.Converters
{
    internal sealed class UnixTimeToDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(long);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                long timestamp = (long)reader.Value;
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            }

            throw new JsonSerializationException("Unexpected token type.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
