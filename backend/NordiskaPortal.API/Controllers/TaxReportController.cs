using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NordiskaPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaxReportsController : ControllerBase
    {
        private readonly ITaxReportProcessingService _taxReportProcessingService;

        public TaxReportsController(ITaxReportProcessingService taxReportProcessingService)
        {
            _taxReportProcessingService = taxReportProcessingService;
        }

        public record CreateTaxReportRequest(int ReportYear);

        [HttpPost]
        public async Task<IActionResult> CreateTaxReport(CreateTaxReportRequest request)
        {
            var report = await _taxReportProcessingService.QueueReportAsync(CurrentUserId, request.ReportYear);
            return Ok(new { report.Id, report.Status });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaxReportStatus(Guid id)
        {
            var report = await _taxReportProcessingService.GetStatusAsync(CurrentUserId, id);
            if (report is null) return NotFound();

            return Ok(new { report.Id, report.Status, report.CreatedAt, report.CompletedAt, report.ErrorMessage });
        }

        // Shell for future download so task can be checked
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadTaxReport(Guid id)
        {
            var report = await _taxReportProcessingService.GetStatusAsync(CurrentUserId, id);
            if (report is null) return NotFound();

            if (report.Status != "READY")
                return Conflict(new { message = $"Report is not ready yet (status: {report.Status})" });

            return StatusCode(501, new { message = "PDF download not implemented yet, waiting on native PDF generation" }); // TODO: stream the signed PDF from report.PdfPath once native generation exists.
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);


    }
}
