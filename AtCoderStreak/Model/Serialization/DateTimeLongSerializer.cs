using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtCoderStreak.Model.Serialization
{
    public class DateTimeLongSerializer : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()).DateTime;
            return time + TimeSpan.FromHours(9);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(new DateTimeOffset(value, TimeSpan.FromHours(9)).ToUnixTimeSeconds());
        }
    }
}
