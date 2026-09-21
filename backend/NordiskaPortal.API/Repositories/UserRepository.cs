using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;   

namespace NordiskaPortal.API.Repositories;

public class UserRepository : Repository<User>, IUserRepository // Ärver CRUD från basen + det som är unikt från "User"
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByPersonalNumberAsync(string personalNumber) => // Söks via PIN-login, POST /api/auth/login-pin
        await _dbSet.SingleOrDefaultAsync(u => u.PersonalNumber == personalNumber); // Null om det inte matchar (personalNumber är unikt, där av inte FirstOrDefaultAsync! )
}