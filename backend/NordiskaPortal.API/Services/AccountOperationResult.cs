namespace NordiskaPortal.API.Services;

public sealed class AccountOperationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public decimal? Balance { get; }
    public Guid? TransactionId { get; }

    private AccountOperationResult(bool isSuccess, string? errorMessage, Guid? transactionId, decimal? balance)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        TransactionId = transactionId;
        Balance = balance;
    }

    public static AccountOperationResult Success(Guid transactionId, decimal balance) =>
        new(true, null, transactionId, balance);

    public static AccountOperationResult Failure(string errorMessage) =>
        new(false, errorMessage, null, null);
}