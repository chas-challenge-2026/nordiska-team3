namespace NordiskaPortal.API.DTOs.Accounts;

// Konto i listan från GET /api/accounts. string balance för att undvika flyttalsavrundning utifrån native-kontrakt.
public sealed record AccountDto(Guid Id, string AccountNumber, string AccountType, string Name, string Status, string Balance);

// Svar från Get /Api/Accounts
public sealed record AccountsResponseDto(IReadOnlyList<AccountDto> Accounts);

public sealed record CreateAccountRequestDto(string AccountType);

// PATCH /api/accounts/{id}/name - För att kunna namne konto efter skapandet
public sealed record RenameAccountRequestDto(string Name);

public sealed record TransactionRequestDto(decimal Amount);

public sealed record TransactionResultDto(Guid TransactionId, string Balance);

public sealed record TransactionDto(Guid Id, string TransactionType, string Amount, string Status, DateTime CreatedAt, DateTime? CompletedAt, Guid? TransferId, string? Counterparty);

// Svar från GET /api/accounts/{id}/transactions
public sealed record TransactionHistoryResponseDto(
    IReadOnlyList<TransactionDto> Transactions,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

// POST /api/accounts/{accountId}/transfer - skicka pengar till ett annat konto via kontonummer
public sealed record TransferRequestDto(string ToAccountNumber, decimal Amount);

// Svar fran transfer. ToBalance fylls bara i när mottagarkontot tillhör samma användare.
public sealed record TransferResultDto(Guid TransferId, string FromBalance, string? ToBalance);

// Svar fran GET /api/accounts/lookup?accountNumber=NKM-xxxxx - maskerat namn, t.ex. "Robin.M"
public sealed record RecipientLookupDto(string AccountNumber, string OwnerName);