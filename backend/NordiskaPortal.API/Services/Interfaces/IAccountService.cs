using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IAccountService
{
    Task<AccountDto?> CreateAccountAsync(Guid userId, string accountType);
    Task<AccountsResponseDto> GetAccountsForUserAsync(Guid userId);

    Task<AccountDto?> GetBalanceAsync(Guid userId, Guid accountId);
    Task<AccountOperationResult> DepositAsync(Guid userId, Guid accountId, decimal amount);
    Task<AccountOperationResult> WithdrawAsync(Guid userId, Guid accountId, decimal amount);

    Task<AccountDto?> RenameAccountAsync(Guid userId, Guid accountId, string name);

    Task<TransactionHistoryResponseDto?> GetTransactionHistoryAsync(Guid userId, Guid accountId, int page, int pageSize);

    Task<RecipientLookupDto?> LookupRecipientAsync(string accountNumber);

    Task<TransferOperationResult> TransferAsync(Guid userId, Guid fromAccountId, string toAccountNumber, decimal amount);
}