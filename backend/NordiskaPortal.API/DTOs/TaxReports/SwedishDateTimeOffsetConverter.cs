using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NordiskaPortal.API.Services;

namespace NordiskaPortal.API.DTOs.TaxReports;

public class SwedishDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(SwedishTime.ToLocal(value).ToString(Format, CultureInfo.InvariantCulture));
}