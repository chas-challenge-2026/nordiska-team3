using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(Guid accountId); // Nyast transaktionen först
}