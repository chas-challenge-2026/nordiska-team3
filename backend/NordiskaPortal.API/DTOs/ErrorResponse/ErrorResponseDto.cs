namespace NordiskaPortal.API.DTOs.ErrorResponse;

// Används som Auto-fel vid både fel persomnummer eller Pin-kod.
public sealed record ErrorResponseDto(string Message);