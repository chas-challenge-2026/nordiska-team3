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
            var report = await _taxReportProcessingService.GenerateReportAsync(CurrentUserId, request.ReportYear);
            return Ok(new { report.Id, report.Status });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaxReportStatus(Guid id)
        {
            var report = await _taxReportProcessingService.GetStatusAsync(CurrentUserId, id);
            if (report is null) return NotFound();

            return Ok(new { report.Id, report.Status, report.CreatedAt, report.CompletedAt, report.ErrorMessage });
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);


    }
}
