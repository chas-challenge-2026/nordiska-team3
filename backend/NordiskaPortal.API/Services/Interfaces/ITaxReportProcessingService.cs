using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services.Interfaces
{
    public interface ITaxReportProcessingService
    {
        Task<TaxReport> QueueReportAsync(Guid userId, int reportYear);
        Task ProcessReportAsync(Guid reportId);
        Task<int> RecoverUnfinishedReportsAsync();
        Task<TaxReport?> GetStatusAsync(Guid userId, Guid reportId);
        Task<IReadOnlyList<TaxReport>> GetReportsForUserAsync(Guid userId);
    }
}