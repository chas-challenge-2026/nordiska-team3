using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services.BankId;

public sealed class BankIdService : IBankIdService
{
    private static readonly TimeSpan MinCollectInterval = TimeSpan.FromSeconds(2);

    private readonly IBankIdApiClient _api;
    private readonly BankIdOrderStore _store;
    private readonly IAuthService _authService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BankIdService> _logger;

    public BankIdService(IBankIdApiClient api, BankIdOrderStore store, IAuthService authService,
        TimeProvider timeProvider, ILogger<BankIdService> logger)
    {
        _api = api;
        _store = store;
        _authService = authService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public bool IsMock => _api is MockBankIdApiClient;

    public async Task<BankIdStartResult> StartAsync(string endUserIp, CancellationToken cancellationToken)
    {
        var auth = await _api.AuthAsync(endUserIp, cancellationToken);
        var order = _store.Add(auth, _timeProvider.GetUtcNow());

        _logger.LogInformation("BankID order started.");

        return new BankIdStartResult(order.OrderId, auth.AutoStartToken);
    }

    public async Task<BankIdStatusResult> GetStatusAsync(string orderId, CancellationToken cancellationToken)
    {
        var order = _store.Get(orderId);

        if (order is null)
        {
            return BankIdStatusResult.Failed("unknownOrder");
        }

        await order.Gate.WaitAsync(cancellationToken);

        try
        {
            if (order.Finished)
            {
                return BankIdStatusResult.Failed("unknownOrder");
            }

            var now = _timeProvider.GetUtcNow();

            if (order.LastCollect is null || now - order.LastCollectAt >= MinCollectInterval)
            {
                try
                {
                    order.LastCollect = await _api.CollectAsync(order.BankIdOrderRef, cancellationToken);
                    order.LastCollectAt = now;
                }
                catch (BankIdApiException ex)
                {
                    _logger.LogWarning("BankID collect failed with {ErrorCode}.", ex.ErrorCode);
                    Finish(order);
                    return BankIdStatusResult.Failed("bankIdError");
                }
            }

            var collect = order.LastCollect!;

            if (collect.Status == BankIdStatuses.Pending)
            {
                var seconds = (int)(now - order.StartedAt).TotalSeconds;
                var qrData = BankIdQrCode.Create(order.QrStartToken, order.QrStartSecret, seconds);

                return BankIdStatusResult.Pending(collect.HintCode, qrData);
            }

            Finish(order);

            if (collect.Status != BankIdStatuses.Complete)
            {
                return BankIdStatusResult.Failed(collect.HintCode ?? "unknown");
            }

            var personalNumber = collect.CompletionData?.User.PersonalNumber;

            if (string.IsNullOrEmpty(personalNumber))
            {
                _logger.LogWarning("BankID completed without a personal number.");
                return BankIdStatusResult.Failed("noPersonalNumber");
            }

            var authResult = await _authService.LoginWithPersonalNumberAsync(ToStoredFormat(personalNumber));

            return authResult is null
                ? BankIdStatusResult.Failed("userNotFound")
                : BankIdStatusResult.Complete(authResult);
        }
        finally
        {
            order.Gate.Release();
        }
    }

    public async Task CancelAsync(string orderId, CancellationToken cancellationToken)
    {
        var order = _store.Get(orderId);

        if (order is null)
        {
            return;
        }

        Finish(order);

        try
        {
            await _api.CancelAsync(order.BankIdOrderRef, cancellationToken);
        }
        catch (BankIdApiException ex)
        {
            _logger.LogWarning("BankID cancel failed with {ErrorCode}.", ex.ErrorCode);
        }
    }

    public bool MockApprove(string orderId, string personalNumber)
    {
        if (_api is not MockBankIdApiClient mock)
        {
            return false;
        }

        var order = _store.Get(orderId);

        if (order is null)
        {
            return false;
        }

        mock.Approve(order.BankIdOrderRef, personalNumber);

        return true;
    }

    private void Finish(BankIdOrder order)
    {
        order.Finished = true;
        _store.Remove(order.OrderId);
    }

    // BankID gives 12 digits (199001011234). We store the personal number as 19900101-1234.
    internal static string ToStoredFormat(string personalNumber)
    {
        var digits = new string(personalNumber.Where(char.IsDigit).ToArray());

        return digits.Length == 12 ? $"{digits[..8]}-{digits[8..]}" : personalNumber;
    }
}