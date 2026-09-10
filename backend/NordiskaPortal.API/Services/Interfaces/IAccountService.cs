using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAccountService
{
    Task<AccountDto?> CreateAccountAsync(Guid userId, string accountType);
    Task<AccountsResponseDto> GetAccountsForUserAsync(Guid userId);

    Task<AccountDto?> GetBalanceAsync(Guid accountId);
    Task<AccountOperationResult> DepositAsync(Guid accountId, decimal amount);
    Task<AccountOperationResult> WithdrawAsync(Guid accountId, decimal amount);
}