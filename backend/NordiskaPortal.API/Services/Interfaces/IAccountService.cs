using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAccountService
{
    Task<AccountDto?> CreateAccountAsync(Guid userId, string accountType);
    Task<AccountsResponseDto> GetAccountsForUserAsync(Guid userId);

    Task<AccountDto?> GetBalanceAsync(Guid userId, Guid accountId);
    Task<AccountOperationResult> DepositAsync(Guid userId, Guid accountId, decimal amount);
    Task<AccountOperationResult> WithdrawAsync(Guid userId, Guid accountId, decimal amount);
}