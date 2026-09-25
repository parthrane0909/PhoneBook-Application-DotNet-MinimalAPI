using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phonebook.Middleware;

/// <summary>
/// Keeps the exact date format the frontend expects: a local ISO datetime
/// with no timezone suffix (the Java/Jackson backend serialized LocalDateTime
/// the same way, and the frontend parses these values as local time).
/// Fractional seconds are written with up to 6 digits (microseconds, matching
/// PostgreSQL timestamp precision) and omitted entirely when zero.
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return DateTime.Parse(text!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        var text = value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        var microseconds = (value.Ticks % TimeSpan.TicksPerSecond) / 10;

        if (microseconds != 0)
        {
            text += "." + microseconds.ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0');
        }

        writer.WriteStringValue(text);
    }
}
