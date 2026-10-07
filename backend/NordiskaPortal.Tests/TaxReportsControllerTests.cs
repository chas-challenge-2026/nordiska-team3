using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NordiskaPortal.API.Controllers;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Controllers;

public class TaxReportsControllerTests : IDisposable
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _pdfPath = Path.Combine(Path.GetTempPath(), "tax-report-controller-" + Guid.NewGuid() + ".pdf");
    private readonly Mock<ITaxReportProcessingService> _serviceMock = new();

    public void Dispose()
    {
        if (File.Exists(_pdfPath))
        {
            File.Delete(_pdfPath);
        }
    }

    [Fact]
    public async Task DownloadTaxReport_WhenTheReportDoesNotBelongToTheCustomer_ReturnsNotFound()
    {
        var reportId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetStatusAsync(_userId, reportId)).ReturnsAsync((TaxReport?)null);

        var result = await CreateController().DownloadTaxReport(reportId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DownloadTaxReport_WhenTheReportIsNotReady_ReturnsConflict()
    {
        var report = SetupReport(status: "PROCESSING");

        var result = await CreateController().DownloadTaxReport(report.Id);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DownloadTaxReport_WhenTheFileIsGone_ReturnsNotFound()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath);

        var result = await CreateController().DownloadTaxReport(report.Id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadTaxReport_WhenTheReportIsReady_ReturnsThePdfAsADownload()
    {
        File.WriteAllBytes(_pdfPath, new byte[] { 1, 2, 3 });
        var report = SetupReport(status: "READY", pdfPath: _pdfPath, reportYear: 2026);

        var result = await CreateController().DownloadTaxReport(report.Id);

        var file = result.Should().BeOfType<PhysicalFileResult>().Subject;
        file.FileName.Should().Be(_pdfPath);
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("skatterapport-2026.pdf");
    }

    [Fact]
    public async Task GetTaxReportStatus_WhenThereIsNoSignature_ReportsTheReportAsUnsigned()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath);

        var result = await CreateController().GetTaxReportStatus(report.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { Status = "READY", Signed = false });
    }

    private TaxReport SetupReport(string status, string? pdfPath = null, int reportYear = 2026)
    {
        var report = new TaxReport { UserId = _userId, ReportYear = reportYear, Status = status, PdfPath = pdfPath };
        _serviceMock.Setup(s => s.GetStatusAsync(_userId, report.Id)).ReturnsAsync(report);
        return report;
    }

    private TaxReportsController CreateController() =>
        new(_serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()) }, "test"))
                }
            }
        };
}
