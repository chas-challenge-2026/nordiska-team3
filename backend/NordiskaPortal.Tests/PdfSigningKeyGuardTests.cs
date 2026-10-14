using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class PdfSigningKeyGuardTests : IDisposable
{
    private readonly string _workDirectory = Path.Combine(Path.GetTempPath(), "pdf-key-guard-tests-" + Guid.NewGuid());
    private readonly string _signerPath;
    private readonly string _pdfPath;
    private readonly Mock<INativeProcessRunner> _runnerMock = new();
    private readonly Mock<ISignedReportCheck> _signedReportsMock = new();

    public PdfSigningKeyGuardTests()
    {
        Directory.CreateDirectory(_workDirectory);
        _signerPath = Path.Combine(_workDirectory, "pdf_signer");
        File.WriteAllText(_signerPath, "stub");
        _pdfPath = Path.Combine(_workDirectory, "report.pdf");
        File.WriteAllBytes(_pdfPath, new byte[] { 1, 2, 3 });

        _runnerMock
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns((string _, IReadOnlyList<string> args, TimeSpan _, CancellationToken _) =>
            {
                if (args[0] == "sign")
                {
                    File.WriteAllBytes(args[3], new byte[256]);
                }

                return Task.FromResult(new NativeProcessResult(0, "", "", TimedOut: false));
            });
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }

    private string PrivateKeyPath => Path.Combine(_workDirectory, "keys", "signing.pem");
    private string PublicKeyPath => Path.Combine(_workDirectory, "keys", "signing.pub.pem");

    [Fact]
    public async Task SignAsync_WhenTheKeyIsMissingAndSignedReportsExist_RefusesToCreateANewKey()
    {
        _signedReportsMock.Setup(s => s.AnySignedReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateService().SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("The PDF signer keys are not available.");
        File.Exists(PrivateKeyPath).Should().BeFalse();
        File.Exists(PublicKeyPath).Should().BeFalse();
        _runnerMock.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPublicKeyPemAsync_WhenTheKeyIsMissingAndSignedReportsExist_RefusesToCreateANewKey()
    {
        _signedReportsMock.Setup(s => s.AnySignedReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateService().GetPublicKeyPemAsync();

        await act.Should().ThrowAsync<TaxReportGenerationException>();
        File.Exists(PrivateKeyPath).Should().BeFalse();
    }

    [Fact]
    public async Task SignAsync_WhenTheKeyIsMissingAndNoReportHasBeenSigned_CreatesTheKeyPair()
    {
        _signedReportsMock.Setup(s => s.AnySignedReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().SignAsync(_pdfPath);

        result.SignaturePath.Should().NotBeNullOrEmpty();
        File.Exists(PrivateKeyPath).Should().BeTrue();
        File.Exists(PublicKeyPath).Should().BeTrue();
    }

    [Fact]
    public async Task SignAsync_WhenTheKeyExists_DoesNotAskTheDatabase()
    {
        _signedReportsMock.Setup(s => s.AnySignedReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService();
        await service.GetPublicKeyPemAsync();
        _signedReportsMock.Invocations.Clear();

        await service.SignAsync(_pdfPath);

        _signedReportsMock.Verify(s => s.AnySignedReportAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private PdfSigningService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxReport:PdfSignerPath"] = _signerPath,
                ["TaxReport:PrivateKeyPath"] = PrivateKeyPath,
                ["TaxReport:PublicKeyPath"] = PublicKeyPath,
                ["TaxReport:TimeoutSeconds"] = "7"
            })
            .Build();

        return new PdfSigningService(_runnerMock.Object, configuration, NullLogger<PdfSigningService>.Instance, _signedReportsMock.Object);
    }
}

public class SignedReportCheckTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public SignedReportCheckTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task AnySignedReportAsync_WithoutReports_ReturnsFalse()
    {
        var result = await CreateCheck().AnySignedReportAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnySignedReportAsync_WhenNoReportHasASignature_ReturnsFalse()
    {
        await AddReportAsync(signaturePath: null);

        var result = await CreateCheck().AnySignedReportAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnySignedReportAsync_WhenAReportHasASignature_ReturnsTrue()
    {
        await AddReportAsync(signaturePath: "/app/tax-reports/report.sig");

        var result = await CreateCheck().AnySignedReportAsync();

        result.Should().BeTrue();
    }

    private SignedReportCheck CreateCheck() => new(_provider.GetRequiredService<IServiceScopeFactory>());

    private async Task AddReportAsync(string? signaturePath)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.TaxReports.Add(new TaxReport
        {
            UserId = Guid.NewGuid(),
            ReportYear = 2025,
            Status = "READY",
            SignaturePath = signaturePath
        });
        await context.SaveChangesAsync();
    }
}