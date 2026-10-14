namespace NordiskaPortal.API.Services.Interfaces;

public interface IInterestRateSource
{
    // Returns the observations from 'from' to 'to', both included. Throws if the source cannot be reached.
    Task<IReadOnlyList<RateObservation>> GetObservationsAsync(
        string seriesId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

public sealed record RateObservation(DateOnly Date, decimal Value);