using System.Globalization;
using System.Net.Http.Json;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

// Reads interest rate series from the open SWEA API at Riksbanken (no API key needed).
// The base address comes from configuration (Riksbank:BaseUrl)
public class RiksbankRateSource : IInterestRateSource
{
    private readonly HttpClient _httpClient;

    public RiksbankRateSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RateObservation>> GetObservationsAsync(
        string seriesId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var url = $"Observations/{Uri.EscapeDataString(seriesId)}/"
                  + $"{from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/"
                  + $"{to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        var observations = await _httpClient.GetFromJsonAsync<List<ObservationDto>>(url, cancellationToken)
                           ?? new List<ObservationDto>();

        return observations.Select(o => new RateObservation(o.Date, o.Value)).ToList();
    }

    // The shape of one observation in the API response: { "date": "2026-10-08", "value": 1.75 }
    internal sealed record ObservationDto(DateOnly Date, decimal Value);
}