using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.Services.Interfaces;
using NordiskaPortal.API.Extensions;
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

        [EnableRateLimiting(RateLimitPolicies.TaxReport)]
        [HttpPost]
        public async Task<IActionResult> CreateTaxReport(CreateTaxReportRequest request)
        {
            var report = await _taxReportProcessingService.QueueReportAsync(CurrentUserId, request.ReportYear);
            return Ok(new { report.Id, report.Status });
        }

        [HttpGet]
        public async Task<IActionResult> GetTaxReports()
        {
            var reports = await _taxReportProcessingService.GetReportsForUserAsync(CurrentUserId);

            return Ok(new
            {
                Reports = reports.Select(r => new
                {
                    r.Id,
                    r.ReportYear,
                    r.Status,
                    r.CreatedAt,
                    r.CompletedAt,
                    r.ErrorMessage,
                    Signed = r.SignaturePath is not null,
                    // The PDF lives on the container disk, so a READY report can still lack its file.
                    PdfAvailable = r.Status == "READY" && r.PdfPath is not null && System.IO.File.Exists(r.PdfPath)
                })
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaxReportStatus(Guid id)
        {
            var report = await _taxReportProcessingService.GetStatusAsync(CurrentUserId, id);
            if (report is null) return NotFound();

            // Signed stays false until the native signer is wired in, so the client can label the PDF as unsigned.
            return Ok(new
            {
                report.Id,
                report.Status,
                report.CreatedAt,
                report.CompletedAt,
                report.ErrorMessage,
                Signed = report.SignaturePath is not null
            });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadTaxReport(Guid id)
        {
            // GetStatusAsync only returns the report if it belongs to the signed-in customer.
            var report = await _taxReportProcessingService.GetStatusAsync(CurrentUserId, id);
            if (report is null) return NotFound();

            if (report.Status != "READY")
                return Conflict(new { message = $"Report is not ready yet (status: {report.Status})" });

            if (report.PdfPath is null || !System.IO.File.Exists(report.PdfPath))
                return NotFound(new { message = "The report file is no longer available." });

            return PhysicalFile(report.PdfPath, "application/pdf", $"skatterapport-{report.ReportYear}.pdf");
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }
}