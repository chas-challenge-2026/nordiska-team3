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

    public async Task<bool> ExistsByAccountNumberAsync(string accountNumber) => // Kollar om ett visst kontonummer redan finns
        await _dbSet.AnyAsync(a => a.AccountNumber == accountNumber);

    public async Task<decimal> GetBalanceAsync(Guid accountId) => // Vi ville ha saldo som en beräkning inte ett sparat värde som kan bli inaktuellt
        await _context.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .SumAsync(l => l.Amount);
}