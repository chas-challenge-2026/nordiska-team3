using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByPersonalNumberAsync(string personalNumber); // Hämtar en användare baserat på personnummer
}