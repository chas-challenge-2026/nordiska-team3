using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId); // Inloggad användares alla konton GET /api/accounts/
    Task<bool> ExistsByAccountNumberAsync(string accountNumber); // Används av AccountService för att generera unikt kontonummer
    Task<decimal> GetBalanceAsync(Guid accountId);
}