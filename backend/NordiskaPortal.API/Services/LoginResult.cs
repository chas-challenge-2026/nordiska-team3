namespace NordiskaPortal.API.Services;

public sealed class LoginResult
{
    public bool IsSuccess { get; }
    public AuthResult? AuthResult { get; }
    public TimeSpan? RetryAfter { get; }
    public bool IsLockedOut => RetryAfter is not null;

    private LoginResult(bool isSuccess, AuthResult? authResult, TimeSpan? retryAfter)
    {
        IsSuccess = isSuccess;
        AuthResult = authResult;
        RetryAfter = retryAfter;
    }

    public static LoginResult Success(AuthResult authResult) => new(true, authResult, null);
    public static LoginResult InvalidCredentials() => new(false, null, null);
    public static LoginResult LockedOut(TimeSpan retryAfter) => new(false, null, retryAfter);
}