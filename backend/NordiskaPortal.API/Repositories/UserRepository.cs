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

    public async Task<User?> GetByPersonalNumberHashAsync(string personalNumberHash) => // Söks via PIN-login, POST /api/auth/login-pin
        await _dbSet.SingleOrDefaultAsync(u => u.PersonalNumberHash == personalNumberHash); // Null om det inte matchar (hashen är unik)

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) => // Söks via POST /api/auth/refresh
        await _dbSet.SingleOrDefaultAsync(u => u.RefreshToken == refreshToken); // Null om ingen matchar (t.ex. redan roterad/utloggad)

    public async Task<bool> ExistsByEmailAsync(string email) => // Används av RegisterAsync för att undvika dubbletter
        await _dbSet.AnyAsync(u => u.Email == email);
}