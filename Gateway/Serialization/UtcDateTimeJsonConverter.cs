using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Serialization;

public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetDateTime(out var value))
        {
            return AsUtc(value);
        }

        var raw = reader.GetString();
        return string.IsNullOrWhiteSpace(raw)
            ? default
            : AsUtc(DateTime.Parse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(AsUtc(value).ToString("O"));
    }

    public static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
