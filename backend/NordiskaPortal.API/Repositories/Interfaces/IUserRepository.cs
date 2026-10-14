using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByPersonalNumberHashAsync(string personalNumberHash); // Hämtar en användare via personnummerets hash
    Task<bool> ExistsByEmailAsync(string email); // Kollar om email redan finns hos en ny användare
    Task<int> RegisterFailedPinAttemptAsync(Guid userId);
    Task LockPinLoginAsync(Guid userId, DateTime lockedUntil);
}