using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
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
    private readonly string _signaturePath = Path.Combine(Path.GetTempPath(), "tax-report-controller-" + Guid.NewGuid() + ".sig");
    private readonly Mock<ITaxReportProcessingService> _serviceMock = new();
    private readonly Mock<IPdfSigningService> _signingMock = new();

    public void Dispose()
    {
        foreach (var path in new[] { _pdfPath, _signaturePath })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
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
    public async Task DownloadSignature_WhenTheReportDoesNotBelongToTheCustomer_ReturnsNotFound()
    {
        var reportId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetStatusAsync(_userId, reportId)).ReturnsAsync((TaxReport?)null);

        var result = await CreateController().DownloadSignature(reportId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DownloadSignature_WhenTheReportIsNotReady_ReturnsConflict()
    {
        var report = SetupReport(status: "PROCESSING");

        var result = await CreateController().DownloadSignature(report.Id);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DownloadSignature_WhenTheReportHasNoSignature_ReturnsNotFound()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath);

        var result = await CreateController().DownloadSignature(report.Id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadSignature_WhenTheSignatureFileIsGone_ReturnsNotFound()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath, signaturePath: _signaturePath);

        var result = await CreateController().DownloadSignature(report.Id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadSignature_WhenTheReportIsSigned_ReturnsTheSignatureAsADownload()
    {
        File.WriteAllBytes(_signaturePath, new byte[] { 4, 5, 6 });
        var report = SetupReport(status: "READY", pdfPath: _pdfPath, reportYear: 2026, signaturePath: _signaturePath);

        var result = await CreateController().DownloadSignature(report.Id);

        var file = result.Should().BeOfType<PhysicalFileResult>().Subject;
        file.FileName.Should().Be(_signaturePath);
        file.ContentType.Should().Be("application/octet-stream");
        file.FileDownloadName.Should().Be("skatterapport-2026.pdf.sig");
    }

    [Fact]
    public async Task DownloadPublicKey_ReturnsThePublicKeyAsAPemFile()
    {
        const string pem = "-----BEGIN PUBLIC KEY-----\nabc\n-----END PUBLIC KEY-----\n";
        _signingMock.Setup(s => s.GetPublicKeyPemAsync(It.IsAny<CancellationToken>())).ReturnsAsync(pem);

        var result = await CreateController().DownloadPublicKey(CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        Encoding.ASCII.GetString(file.FileContents).Should().Be(pem);
        file.ContentType.Should().Be("application/x-pem-file");
        file.FileDownloadName.Should().Be("skatterapport-publik-nyckel.pem");
    }

    [Fact]
    public void TheController_RequiresAuthenticationAndHasNoAnonymousEndpoints()
    {
        typeof(TaxReportsController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(TaxReportsController).GetMethods()
            .Should().NotContain(method => method.IsDefined(typeof(AllowAnonymousAttribute), true));
    }

    [Fact]
    public async Task GetTaxReportStatus_WhenThereIsNoSignature_ReportsTheReportAsUnsigned()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath);

        var result = await CreateController().GetTaxReportStatus(report.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { Status = "READY", Signed = false });
    }

    [Fact]
    public async Task GetTaxReportStatus_WhenThePdfIsSigned_ReportsTheReportAsSigned()
    {
        var report = SetupReport(status: "READY", pdfPath: _pdfPath, signaturePath: _signaturePath);

        var result = await CreateController().GetTaxReportStatus(report.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { Status = "READY", Signed = true });
    }

    [Fact]
    public async Task GetTaxReports_ReturnsTheReportsAndWhetherEachPdfIsStillAvailable()
    {
        File.WriteAllBytes(_pdfPath, new byte[] { 1 });
        var withFile = new TaxReport { UserId = _userId, ReportYear = 2026, Status = "READY", PdfPath = _pdfPath };
        var withoutFile = new TaxReport
        {
            UserId = _userId,
            ReportYear = 2025,
            Status = "READY",
            PdfPath = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".pdf")
        };
        var queued = new TaxReport { UserId = _userId, ReportYear = 2024, Status = "QUEUED" };

        _serviceMock
            .Setup(s => s.GetReportsForUserAsync(_userId))
            .ReturnsAsync((IReadOnlyList<TaxReport>)new List<TaxReport> { withFile, withoutFile, queued });

        var result = await CreateController().GetTaxReports();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            Reports = new[]
            {
                new { ReportYear = 2026, Status = "READY", PdfAvailable = true },
                new { ReportYear = 2025, Status = "READY", PdfAvailable = false },
                new { ReportYear = 2024, Status = "QUEUED", PdfAvailable = false }
            }
        }, options => options.WithStrictOrdering());
    }

    private TaxReport SetupReport(string status, string? pdfPath = null, int reportYear = 2026, string? signaturePath = null)
    {
        var report = new TaxReport
        {
            UserId = _userId,
            ReportYear = reportYear,
            Status = status,
            PdfPath = pdfPath,
            SignaturePath = signaturePath
        };
        _serviceMock.Setup(s => s.GetStatusAsync(_userId, report.Id)).ReturnsAsync(report);
        return report;
    }

    private TaxReportsController CreateController() =>
        new(_serviceMock.Object, _signingMock.Object)
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