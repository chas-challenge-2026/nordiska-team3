using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services.Interfaces
{
    public interface ITaxReportProcessingService // This service is responsible for generating tax reports asynchronously
    {
        Task<TaxReport> QueueReportAsync(Guid userId, int reportYear); //Split in two so to never block the request when creating report.
        Task ProcessReportAsync(Guid reportId);
        Task<TaxReport?> GetStatusAsync(Guid userId, Guid reportId);
    }
}
