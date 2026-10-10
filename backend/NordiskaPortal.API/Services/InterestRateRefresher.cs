using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

// Fetches the policy rate from Riksbanken and stores one row per day.
// An error never reaches the caller: the values already stored keep being used.
public class InterestRateRefresher
{
    public const string DefaultSeriesId = "SECBREPOEFF";

    private readonly ApplicationDbContext _context;
    private readonly IInterestRateSource _source;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InterestRateRefresher> _logger;

    public InterestRateRefresher(
        ApplicationDbContext context,
        IInterestRateSource source,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<InterestRateRefresher> logger)
    {
        _context = context;
        _source = source;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var seriesId = _configuration["Riksbank:PolicyRateSeriesId"] ?? DefaultSeriesId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        try
        {
            var latestStored = await _context.InterestRateObservations
                .Where(o => o.SeriesId == seriesId)
                .MaxAsync(o => (DateOnly?)o.Date, cancellationToken);

            // First run: start on 1 December last year, so that 1 January (a holiday without a value) still has a rate.
            // Later runs: start from the latest stored day, which is fetched again in case it was revised.
            var from = latestStored ?? new DateOnly(today.Year - 1, 12, 1);

            var fetched = await _source.GetObservationsAsync(seriesId, from, today, cancellationToken);

            if (fetched.Count == 0)
            {
                _logger.LogWarning("Riksbanken returned no values for {SeriesId} between {From} and {To}", seriesId, from, today);
                return;
            }

            var existing = await _context.InterestRateObservations
                .Where(o => o.SeriesId == seriesId && o.Date >= from)
                .ToDictionaryAsync(o => o.Date, cancellationToken);

            foreach (var observation in fetched.GroupBy(o => o.Date).Select(g => g.Last()))
            {
                if (existing.TryGetValue(observation.Date, out var row))
                {
                    if (row.Value != observation.Value)
                    {
                        row.Value = observation.Value;
                        row.FetchedAt = now;
                    }
                }
                else
                {
                    _context.InterestRateObservations.Add(new InterestRateObservation
                    {
                        SeriesId = seriesId,
                        Date = observation.Date,
                        Value = observation.Value,
                        FetchedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Interest rate {SeriesId} updated from {From} to {To}", seriesId, from, today);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch interest rate {SeriesId} from Riksbanken. The stored values are used.", seriesId);
        }
    }
}