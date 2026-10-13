using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        await _dbSet.SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task<IReadOnlyList<RefreshToken>> GetByUserAsync(Guid userId) =>
        await _dbSet.Where(t => t.UserId == userId).ToListAsync();

    public async Task<IReadOnlyList<RefreshToken>> GetStaleAsync(Guid userId, DateTime now, DateTime rotatedBefore) =>
        await _dbSet
            .Where(t => t.UserId == userId && (t.ExpiresAt <= now || (t.RotatedAt != null && t.RotatedAt < rotatedBefore)))
            .ToListAsync();

    public void RemoveRange(IEnumerable<RefreshToken> tokens) => _dbSet.RemoveRange(tokens);
}