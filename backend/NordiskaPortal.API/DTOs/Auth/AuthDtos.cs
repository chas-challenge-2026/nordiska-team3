namespace NordiskaPortal.API.DTOs.Auth;

//
// Använder sealed för att det inte ska kunna gå att ärva från, då detta inte ska användas som någon subklass.
//

public sealed record LoginPinRequestDto(string PersonalNumber, string Pin);

// Användarinfo som skickas till frontend. "Name" byggs ihop av "FirstName" och "LastName" i AuthService
public sealed record UserDto(Guid Id, string Name, string Email, string PersonalNumber);

// Svar från Post /api/auth/login-pin vid lyckad inloggning
public sealed record LoginResponseDto(string AccessToken, UserDto USer);

// Svar från GET /api/auth/me
public sealed record MeResponseDto(UserDto User);