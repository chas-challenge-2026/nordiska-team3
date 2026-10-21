using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NordiskaPortal.API.Services.BankId;

public sealed class BankIdOrder
{
    public required string OrderId { get; init; }
    public required string BankIdOrderRef { get; init; }
    public required string QrStartToken { get; init; }
    public required string QrStartSecret { get; init; }
    public required DateTimeOffset StartedAt { get; init; }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public BankIdCollectResponse? LastCollect { get; set; }
    public DateTimeOffset LastCollectAt { get; set; }
    public bool Finished { get; set; }
}

public sealed class BankIdOrderStore
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, BankIdOrder> _orders = new();

    public BankIdOrder Add(BankIdAuthResponse auth, DateTimeOffset now)
    {
        RemoveOlderThan(now - MaxAge);

        var order = new BankIdOrder
        {
            OrderId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            BankIdOrderRef = auth.OrderRef,
            QrStartToken = auth.QrStartToken,
            QrStartSecret = auth.QrStartSecret,
            StartedAt = now
        };

        _orders[order.OrderId] = order;

        return order;
    }

    public BankIdOrder? Get(string orderId) =>
        _orders.TryGetValue(orderId, out var order) ? order : null;

    public void Remove(string orderId) => _orders.TryRemove(orderId, out _);

    private void RemoveOlderThan(DateTimeOffset limit)
    {
        foreach (var pair in _orders)
        {
            if (pair.Value.StartedAt < limit)
            {
                _orders.TryRemove(pair.Key, out _);
            }
        }
    }
}