using NordiskaPortal.API.DTOs.TaxReports;

namespace NordiskaPortal.API.Services.Interfaces
{
    public interface ITaxReportDataService
    {
        Task<TaxReportInputDto> GetTaxReportDataAsync(Guid userId, int reportYear, Guid reportId);
    }
}
