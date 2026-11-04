namespace NordiskaPortal.API.Services.BankId;

public interface IBankIdApiClient
{
    Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken cancellationToken);
    Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken cancellationToken);
    Task CancelAsync(string orderRef, CancellationToken cancellationToken);
}