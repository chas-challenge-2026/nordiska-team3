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

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetByAccountIdAsync(Guid accountId, int page, int pageSize)
    {
        var query = _dbSet.Where(t => t.AccountId == accountId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}