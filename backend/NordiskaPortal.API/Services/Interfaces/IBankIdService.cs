using NordiskaPortal.API.Services.BankId;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IBankIdService
{
    // True when BankID is faked (BankId:Mode is "Mock").
    bool IsMock { get; }

    Task<BankIdStartResult> StartAsync(string endUserIp, CancellationToken cancellationToken);
    Task<BankIdStatusResult> GetStatusAsync(string orderId, CancellationToken cancellationToken);
    Task CancelAsync(string orderId, CancellationToken cancellationToken);

    // Only works in mock mode: stands in for the person confirming in the BankID app.
    bool MockApprove(string orderId, string personalNumber);
}