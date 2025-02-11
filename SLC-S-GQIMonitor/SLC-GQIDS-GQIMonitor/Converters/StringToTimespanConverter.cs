using Newtonsoft.Json;
using System;
using System.Globalization;

namespace GQI.Converters
{
    internal sealed class StringToTimespanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string timeSpanString = (string)reader.Value;
                return TimeSpan.Parse(timeSpanString, CultureInfo.InvariantCulture);
            }

            throw new JsonSerializationException("Unexpected token type.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
