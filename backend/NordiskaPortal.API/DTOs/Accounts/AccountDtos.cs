namespace NordiskaPortal.API.DTOs.Accounts;

// Konto i listan från GET /api/accounts. string balance för att undvika flyttalsavrundning utifrån native-kontrakt.
public sealed record AccountDto(Guid Id, string AccountNumber, string AccountType, string Status, string Balance);

// Svar från Get /Api/Accounts
public sealed record AccountsResponseDto(IReadOnlyList<AccountDto> Accounts);

public sealed record CreateAccountRequestDto(string AccountType);

public sealed record TransactionRequestDto(decimal Amount);

public sealed record TransactionResultDto(Guid TransactionId, string Balance);
   