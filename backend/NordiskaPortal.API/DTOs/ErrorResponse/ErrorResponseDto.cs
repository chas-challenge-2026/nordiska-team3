namespace Nordiska.API.DTos.ErrorResponse;

// Används som Auto-fel vid både fel persomnummer eller Pin-kod.
public sealed record ErrorResponseDto(string Message);