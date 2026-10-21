using System.Collections.Concurrent;

namespace NordiskaPortal.API.Services.BankId;

// A fake BankID for local development. An order stays "pending" until Approve is called, which stands in for
// the person confirming in the BankID app. The rest of the login (QR code, polling, tokens) is the real code.
public sealed class MockBankIdApiClient : IBankIdApiClient
{
    private sealed class MockOrder
    {
        public string? PersonalNumber { get; set; }
        public bool Cancelled { get; set; }
    }

    private readonly ConcurrentDictionary<string, MockOrder> _orders = new();

    public Task<BankIdAuthResponse> AuthAsync(string endUserIp, CancellationToken cancellationToken)
    {
        var orderRef = Guid.NewGuid().ToString();
        _orders[orderRef] = new MockOrder();

        return Task.FromResult(new BankIdAuthResponse(
            orderRef, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
    }

    public void Approve(string orderRef, string personalNumber)
    {
        if (_orders.TryGetValue(orderRef, out var order))
        {
            order.PersonalNumber = new string(personalNumber.Where(char.IsDigit).ToArray());
        }
    }

    public Task<BankIdCollectResponse> CollectAsync(string orderRef, CancellationToken cancellationToken)
    {
        if (!_orders.TryGetValue(orderRef, out var order))
        {
            throw new BankIdApiException("invalidParameters", "Unknown order.");
        }

        if (order.Cancelled)
        {
            return Task.FromResult(new BankIdCollectResponse(orderRef, "failed", "userCancel", null));
        }

        if (order.PersonalNumber is null)
        {
            return Task.FromResult(new BankIdCollectResponse(orderRef, "pending", "outstandingTransaction", null));
        }

        _orders.TryRemove(orderRef, out _);

        var user = new BankIdUser(order.PersonalNumber, "Mock User", "Mock", "User");
        return Task.FromResult(new BankIdCollectResponse(orderRef, "complete", null, new BankIdCompletionData(user)));
    }

    public Task CancelAsync(string orderRef, CancellationToken cancellationToken)
    {
        if (_orders.TryGetValue(orderRef, out var order))
        {
            order.Cancelled = true;
        }

        return Task.CompletedTask;
    }
}