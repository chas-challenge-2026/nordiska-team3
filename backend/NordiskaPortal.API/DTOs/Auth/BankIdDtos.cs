namespace NordiskaPortal.API.DTOs.Auth;

public sealed record BankIdStartResponseDto(string OrderId, string AutoStartToken, bool Mock);

public sealed record BankIdStatusResponseDto(string Status, string? HintCode, string? QrData, LoginResponseDto? Login);

public sealed record BankIdMockApproveRequestDto(string OrderId, string PersonalNumber);