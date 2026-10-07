using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Repositories;

public class FaqRepository : Repository<FaqEntry>, IFaqRepository
{
    public FaqRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<FaqEntry>> GetAllOrderedAsync() => await _dbSet.AsNoTracking().OrderBy(f => f.CreatedAt).ThenBy(f => f.Id).ToListAsync();
}