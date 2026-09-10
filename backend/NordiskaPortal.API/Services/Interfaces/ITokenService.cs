using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user); // 15min JWT token som skickas till frontend och används vid varje inloggad request
    string GenerateRefreshToken(); // 7 dagars slumpad stängtoken som sparas som http cookie
}