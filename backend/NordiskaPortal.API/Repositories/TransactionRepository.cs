using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository // Ärver CRUD från basen + det som är unikt för "Transaction"
{
    public TransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(Guid accountId) =>
        await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
}