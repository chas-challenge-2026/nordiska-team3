using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Repositories.Interfaces;

public interface IFaqRepository : IRepository<FaqEntry>
{
    Task<IReadOnlyList<FaqEntry>> GetAllOrderedAsync(); // Oldest first, then by Id, so the order is stable
}
