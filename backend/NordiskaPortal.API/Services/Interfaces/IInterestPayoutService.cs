namespace NordiskaPortal.API.Services.Interfaces;

public interface IInterestPayoutService
{
    Task<InterestPayoutResult> PayoutYearAsync(int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterestPayoutResult>> PayoutDueYearsAsync(CancellationToken cancellationToken = default);
}

public sealed record InterestPayoutResult(
    int Year,
    int Paid,
    int ZeroInterest,
    int AlreadyPaid,
    int Failed,
    decimal TotalAmount,
    string? SkippedReason);