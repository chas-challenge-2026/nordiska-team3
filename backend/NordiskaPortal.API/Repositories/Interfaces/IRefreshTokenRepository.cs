using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task<IReadOnlyList<RefreshToken>> GetByUserAsync(Guid userId);
    Task<IReadOnlyList<RefreshToken>> GetStaleAsync(Guid userId, DateTime now, DateTime rotatedBefore);
    void RemoveRange(IEnumerable<RefreshToken> tokens);
}