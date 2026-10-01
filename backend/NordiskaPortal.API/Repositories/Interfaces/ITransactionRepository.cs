using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetByAccountIdAsync(Guid accountId, int page, int pageSize); // Nyast transaktionen först
}