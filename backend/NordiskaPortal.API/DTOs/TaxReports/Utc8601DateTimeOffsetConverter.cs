using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NordiskaPortal.API.DTOs.TaxReports;

// Native's JSON contract requires dates to end in "Z" — the default DateTimeOffset
// serialization produces "+00:00" instead, which native's parser won't recognize.
public class Utc8601DateTimeOffsetConverter : JsonConverter<DateTimeOffset> 
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}