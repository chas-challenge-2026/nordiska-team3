using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository // Ärver CRUD från basen + det som är unikt från "Account"
{
    public AccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId) => // Hämtar alla konton som tillhör en användare.
        await _dbSet.Where(a => a.UserId == userId).ToListAsync(); // Filtrerar på UserId.
}