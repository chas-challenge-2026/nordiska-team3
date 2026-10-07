using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByPersonalNumberHashAsync(string personalNumberHash); // Hämtar en användare via personnummerets hash
    Task<User?> GetByRefreshTokenAsync(string refreshToken); // Hämtar en användare baserat på refresh-token
    Task<bool> ExistsByEmailAsync(string email); // Kollar om email redan finns hos en ny användare
}