namespace NordiskaPortal.API.Services;

public sealed class RegisterResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public AuthResult? AuthResult { get; }

    private RegisterResult(bool isSuccess, string? errorMessage, AuthResult? authResult)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        AuthResult = authResult;
    }

    public static RegisterResult Success(AuthResult authResult) => new(true, null, authResult);
    public static RegisterResult Failure(string errorMessage) => new(false, errorMessage, null);
}