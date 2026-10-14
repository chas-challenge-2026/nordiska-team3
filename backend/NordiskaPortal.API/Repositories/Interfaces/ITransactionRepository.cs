using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetByAccountIdAsync(Guid accountId, int page, int pageSize); // Nyast transaktionen först
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetByUserIdAsync(Guid userId, Guid? accountId, string? type, DateTime? fromUtc, DateTime? toUtcExclusive, int page, int pageSize);
}