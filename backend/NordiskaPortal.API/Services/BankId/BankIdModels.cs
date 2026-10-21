namespace NordiskaPortal.API.Services.BankId;

public sealed record BankIdAuthResponse(string OrderRef, string AutoStartToken, string QrStartToken, string QrStartSecret);

public sealed record BankIdUser(string PersonalNumber, string Name, string GivenName, string Surname);

public sealed record BankIdCompletionData(BankIdUser User);

public sealed record BankIdCollectResponse(string OrderRef, string Status, string? HintCode, BankIdCompletionData? CompletionData);

public sealed record BankIdStartResult(string OrderId, string AutoStartToken);

public sealed record BankIdStatusResult(string Status, string? HintCode, string? QrData, AuthResult? AuthResult)
{
    public static BankIdStatusResult Pending(string? hintCode, string qrData) =>
        new(BankIdStatuses.Pending, hintCode, qrData, null);

    public static BankIdStatusResult Failed(string hintCode) =>
        new(BankIdStatuses.Failed, hintCode, null, null);

    public static BankIdStatusResult Complete(AuthResult authResult) =>
        new(BankIdStatuses.Complete, null, null, authResult);
}

public static class BankIdStatuses
{
    public const string Pending = "pending";
    public const string Failed = "failed";
    public const string Complete = "complete";
}

public sealed class BankIdApiException : Exception
{
    public string ErrorCode { get; }

    public BankIdApiException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}