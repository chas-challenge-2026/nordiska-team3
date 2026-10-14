namespace NordiskaPortal.API.Services;

public sealed class TransferOperationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Guid? TransferId { get; }
    public decimal? FromBalance { get; }
    public decimal? ToBalance { get; } // Bara satt när en användare skickat pengar mellan egna konton

    private TransferOperationResult(bool isSuccess, string? errorMessage,
        Guid? transferId, decimal? fromBalance, decimal? toBalance)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        TransferId = transferId;
        FromBalance = fromBalance;
        ToBalance = toBalance;
    }

    public static TransferOperationResult Success(Guid transferId, decimal fromBalance, decimal? toBalance) =>
        new(true, null, transferId, fromBalance, toBalance);

    public static TransferOperationResult Failure(string errorMessage) =>
        new(false, errorMessage, null, null, null);
}