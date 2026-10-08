using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.TaxReports;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NordiskaPortal.Tests.Services;

public class TaxReportProcessingServiceTests : IDisposable
{
    private readonly string _workDirectory = Path.Combine(Path.GetTempPath(), "tax-report-tests-" + Guid.NewGuid());
    private readonly string _generatorPath;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ITaxReportDataService> _dataServiceMock = new();
    private readonly Mock<INativeProcessRunner> _runnerMock = new();

    public TaxReportProcessingServiceTests()
    {
        Directory.CreateDirectory(_workDirectory);
        _generatorPath = Path.Combine(_workDirectory, "pdf_generator");
        File.WriteAllText(_generatorPath, "stub");

        // The service saves the report and its audit entry in a transaction, which the in-memory database ignores.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);

        _dataServiceMock
            .Setup(s => s.GetTaxReportDataAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId, int year, Guid reportId) => CreateInput(userId, year, reportId));
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorSucceeds_MarksTheReportReadyAndUnsigned()
    {
        var report = await AddReportAsync();
        SetupRunner(args => File.WriteAllBytes(args[1], new byte[] { 1, 2, 3 }), exitCode: 0);

        await CreateService().ProcessReportAsync(report.Id);

        report.Status.Should().Be("READY");
        report.NativeExitCode.Should().Be(0);
        report.PdfPath.Should().EndWith($"{report.Id}.pdf");
        report.CompletedAt.Should().NotBeNull();
        report.SignaturePath.Should().BeNull();
        report.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task QueueReportAsync_SavesTheReportAndAnAuditEntryForTheCustomer()
    {
        var customerId = Guid.NewGuid();

        var report = await CreateService().QueueReportAsync(customerId, 2026);

        (await _context.TaxReports.FindAsync(report.Id)).Should().NotBeNull();
        var entry = await _context.AuditEntries.SingleAsync();
        entry.Action.Should().Be("TAX_REPORT_REQUESTED");
        entry.UserId.Should().Be(customerId);
        entry.EntityType.Should().Be("TaxReport");
        entry.EntityId.Should().Be(report.Id);
        entry.Details.Should().Be("{\"reportYear\":2026}");
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorSucceeds_WritesAGeneratedAuditEntry()
    {
        var report = await AddReportAsync();
        SetupRunner(args => File.WriteAllBytes(args[1], new byte[] { 1 }), exitCode: 0);

        await CreateService().ProcessReportAsync(report.Id);

        var entry = await _context.AuditEntries.SingleAsync();
        entry.Action.Should().Be("TAX_REPORT_GENERATED");
        entry.UserId.Should().Be(report.UserId);
        entry.EntityId.Should().Be(report.Id);
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorFails_WritesNoAuditEntry()
    {
        var report = await AddReportAsync();
        SetupRunner(_ => { }, exitCode: 3);

        var act = () => CreateService().ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        _context.AuditEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReportAsync_PassesTheJsonPathPdfPathAndTimeoutToTheGenerator()
    {
        var report = await AddReportAsync();
        SetupRunner(args => File.WriteAllBytes(args[1], new byte[] { 1 }), exitCode: 0);

        await CreateService().ProcessReportAsync(report.Id);

        _runnerMock.Verify(r => r.RunAsync(
            Path.GetFullPath(_generatorPath),
            It.Is<IReadOnlyList<string>>(args =>
                args.Count == 2 && args[0].EndsWith($"{report.Id}.json") && args[1].EndsWith($"{report.Id}.pdf")),
            TimeSpan.FromSeconds(7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorExitsWithAnError_MarksTheReportFailed()
    {
        var report = await AddReportAsync();
        SetupRunner(_ => { }, exitCode: 3, standardError: "could not save the PDF");

        var act = () => CreateService().ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        report.Status.Should().Be("FAILED");
        report.NativeExitCode.Should().Be(3);
        report.ErrorMessage.Should().Be("PDF generation failed (code 3).");
        report.PdfPath.Should().BeNull();
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorTimesOut_MarksTheReportFailed()
    {
        var report = await AddReportAsync();
        _runnerMock
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NativeProcessResult(-1, "", "", TimedOut: true));

        var act = () => CreateService().ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        report.Status.Should().Be("FAILED");
        report.NativeExitCode.Should().BeNull();
        report.ErrorMessage.Should().Be("PDF generation timed out.");
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorIsMissing_MarksTheReportFailedWithoutRunningIt()
    {
        var report = await AddReportAsync();

        var act = () => CreateService(Path.Combine(_workDirectory, "does-not-exist")).ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        report.Status.Should().Be("FAILED");
        report.ErrorMessage.Should().Be("The PDF generator is not available.");
        _runnerMock.Verify(r => r.RunAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReportAsync_WhenTheGeneratorSucceedsButNoPdfExists_MarksTheReportFailed()
    {
        var report = await AddReportAsync();
        SetupRunner(_ => { }, exitCode: 0);

        var act = () => CreateService().ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        report.Status.Should().Be("FAILED");
        report.ErrorMessage.Should().Be("The PDF generator did not produce a file.");
        report.PdfPath.Should().BeNull();
    }

    [Fact]
    public async Task ProcessReportAsync_WhenSomethingUnexpectedFails_HidesTheDetailsFromTheCustomer()
    {
        var report = await AddReportAsync();
        _dataServiceMock
            .Setup(s => s.GetTaxReportDataAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("connection string is Host=secret-db"));

        var act = () => CreateService().ProcessReportAsync(report.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        report.Status.Should().Be("FAILED");
        report.ErrorMessage.Should().Be("The report could not be generated.");
    }

    [Fact]
    public async Task GetReportsForUserAsync_ReturnsOnlyTheCustomersReportsNewestYearFirst()
    {
        var customerId = Guid.NewGuid();
        _context.TaxReports.AddRange(
            new TaxReport { UserId = customerId, ReportYear = 2024 },
            new TaxReport { UserId = Guid.NewGuid(), ReportYear = 2025 },
            new TaxReport { UserId = customerId, ReportYear = 2026 });
        await _context.SaveChangesAsync();

        var reports = await CreateService().GetReportsForUserAsync(customerId);

        reports.Select(r => r.ReportYear).Should().Equal(2026, 2024);
    }

    private TaxReportProcessingService CreateService(string? generatorPath = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxReport:OutputDirectory"] = Path.Combine(_workDirectory, "out"),
                ["TaxReport:PdfGeneratorPath"] = generatorPath ?? _generatorPath,
                ["TaxReport:TimeoutSeconds"] = "7"
            })
            .Build();

        return new TaxReportProcessingService(
            _context,
            _dataServiceMock.Object,
            _runnerMock.Object,
            configuration,
             NullLogger<TaxReportProcessingService>.Instance,
            new TaxReportQueue(),
            new AuditService(_context, new AuditKey(new byte[32])));
    }

    private async Task<TaxReport> AddReportAsync()
    {
        var report = new TaxReport { UserId = Guid.NewGuid(), ReportYear = 2026 };
        _context.TaxReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    // Lets a test act as the generator (for example by writing the PDF) and choose what the runner returns.
    private void SetupRunner(Action<IReadOnlyList<string>> onRun, int exitCode, string standardError = "")
    {
        _runnerMock
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns((string _, IReadOnlyList<string> args, TimeSpan _, CancellationToken _) =>
            {
                onRun(args);
                return Task.FromResult(new NativeProcessResult(exitCode, "", standardError, TimedOut: false));
            });
    }

    private static TaxReportInputDto CreateInput(Guid userId, int year, Guid reportId) =>
        new(
            1,
            reportId,
            year,
            DateTimeOffset.UtcNow,
            "SEK",
            new BankDto("Nordiska Sparbanken", "556677-8899", "Sveavägen 44", "08-123 456 00", "kontakt@example.com"),
            new CustomerDto(userId, "Test Testsson"),
            new List<TaxAccountDto>(),
            new TaxSummaryDto("0.00", "0.00", "0.00", "0.30", "0.00"));
}