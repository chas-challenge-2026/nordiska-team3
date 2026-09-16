using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Repositories;

public class Repository<T> : IRepository<T> where T : class // "T" är platshållare för den entiteten
{
    protected readonly ApplicationDbContext _context; // Referens till databasen
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context) // Kontruktor för att hämta rätt tabell till typen "T"
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public void Update(T entity) => _dbSet.Update(entity); // Markerar som ändrad

    public void Remove(T entity) => _dbSet.Remove(entity); // Markerar för borttagning

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id); // Hämtar en rad på "Id"

    public async Task<IReadOnlyList<T>> GetAllAsync() => await _dbSet.ToListAsync(); // Hämtar alla rader

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity); // Markerar som ny

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync(); // Sparar till databasen
}