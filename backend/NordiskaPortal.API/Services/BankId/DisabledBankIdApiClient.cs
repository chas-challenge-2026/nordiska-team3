namespace NordiskaPortal.API.Services.BankId;

// Used when BankId:Mode is "Off". Every login attempt is refused with the error code "disabled".
public sealed class DisabledBankIdApiClient : IBankIdApiClient
{
    public Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken cancellationToken) =>
        throw new BankIdApiException("disabled", "BankID is not enabled.");

    public Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken cancellationToken) =>
        throw new BankIdApiException("disabled", "BankID is not enabled.");

    public Task CancelAsync(string orderRef, CancellationToken cancellationToken) =>
        throw new BankIdApiException("disabled", "BankID is not enabled.");
}