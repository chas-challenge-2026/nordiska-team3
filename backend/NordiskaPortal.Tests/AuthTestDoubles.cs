using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.Tests.Services;

public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public TestClock(DateTimeOffset now)
    {
        Now = now;
    }

    public void Advance(TimeSpan by) => Now = Now.Add(by);

    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();

    public Task<RefreshToken?> GetByIdAsync(Guid id) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<RefreshToken>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Tokens.ToList());

    public Task AddAsync(RefreshToken entity)
    {
        Tokens.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(RefreshToken entity)
    {
    }

    public void Remove(RefreshToken entity) => Tokens.Remove(entity);

    public void RemoveRange(IEnumerable<RefreshToken> tokens)
    {
        foreach (var token in tokens.ToList())
            Tokens.Remove(token);
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> GetByUserAsync(Guid userId) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Tokens.Where(t => t.UserId == userId).ToList());

    public Task<IReadOnlyList<RefreshToken>> GetStaleAsync(Guid userId, DateTime now, DateTime rotatedBefore) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Tokens
            .Where(t => t.UserId == userId && (t.ExpiresAt <= now || (t.RotatedAt != null && t.RotatedAt < rotatedBefore)))
            .ToList());
}