using System.Text.Json;
using FluentAssertions;
using NordiskaPortal.API.DTOs.TaxReports;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class SwedishDateTimeOffsetConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new Utc8601DateTimeOffsetConverter() }
    };

    [Theory]
    [InlineData(2025, 12, 31, 23, 30, "2026-01-01T00:30:00.0000000+01:00")]
    [InlineData(2026, 6, 30, 22, 30, "2026-07-01T00:30:00.0000000+02:00")]
    [InlineData(2026, 12, 31, 12, 0, "2026-12-31T13:00:00.0000000+01:00")]
    public void BookedAt_IsWrittenWithTheSwedishOffsetSoTheDateIsTheSwedishDay(
        int year, int month, int day, int hour, int minute, string expected)
    {
        var transaction = new TaxTransactionDto(
            Guid.NewGuid(),
            new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero),
            "DEPOSIT",
            "Deposit",
            "100.00");

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(transaction, Options));

        json.RootElement.GetProperty("bookedAt").GetString().Should().Be(expected);
    }

    [Fact]
    public void GeneratedAt_IsStillWrittenInUtcWithZ()
    {
        var input = new TaxReportInputDto(
            1,
            Guid.NewGuid(),
            2026,
            new DateTimeOffset(2026, 10, 13, 23, 46, 0, TimeSpan.Zero),
            "SEK",
            new BankDto("Nordiska Sparbanken", "556677-8899", "Storgatan 1", "08-000 00 00", "bank@example.com"),
            new CustomerDto(Guid.NewGuid(), "Demo Kund"),
            new List<TaxAccountDto>(),
            new TaxSummaryDto("0.00", "0.00", "0.00", "0.30", "0.00"));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(input, Options));

        json.RootElement.GetProperty("generatedAt").GetString().Should().Be("2026-10-13T23:46:00.0000000Z");
    }
}