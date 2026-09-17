using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services.Interfaces
{
    public interface ITaxReportProcessingService // This service is responsible for generating tax reports asynchronously
    {
        Task<TaxReport>  GenerateReportAsync(Guid userId, int reportYear);
        Task<TaxReport?> GetStatusAsync(Guid userId, Guid reportId);
    }
}
