using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class PdfSigningServiceTests : IDisposable
{
    private readonly string _workDirectory = Path.Combine(Path.GetTempPath(), "pdf-signing-tests-" + Guid.NewGuid());
    private readonly string _signerPath;
    private readonly string _pdfPath;
    private readonly Mock<INativeProcessRunner> _runnerMock = new();
    private readonly List<string[]> _calls = new();

    public PdfSigningServiceTests()
    {
        Directory.CreateDirectory(_workDirectory);
        _signerPath = Path.Combine(_workDirectory, "pdf_signer");
        File.WriteAllText(_signerPath, "stub");
        _pdfPath = Path.Combine(_workDirectory, "report.pdf");
        File.WriteAllBytes(_pdfPath, new byte[] { 1, 2, 3 });
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
    private string SignaturePath => Path.Combine(_workDirectory, "report.sig");

    [Fact]
    public async Task SignAsync_SignsFirstAndThenChecksTheSignatureWithThePublicKey()
    {
        SetupRunner();

        var result = await CreateService().SignAsync(_pdfPath);

        _calls.Should().HaveCount(2);
        _calls[0].Should().Equal("sign", _pdfPath, PrivateKeyPath, SignaturePath);
        _calls[1].Should().Equal("verify", _pdfPath, PublicKeyPath, SignaturePath);
        result.SignaturePath.Should().Be(SignaturePath);
    }

    [Fact]
    public async Task SignAsync_ReturnsTheSha256OfThePdf()
    {
        SetupRunner();

        var result = await CreateService().SignAsync(_pdfPath);

        result.Sha256.Should().Be(Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 })).ToLowerInvariant());
    }

    [Fact]
    public async Task SignAsync_RemovesALeftoverSignatureBeforeSigning()
    {
        File.WriteAllBytes(SignaturePath, new byte[] { 9 });
        var signatureExistedWhenSigning = true;
        SetupRunner(onSign: () => signatureExistedWhenSigning = File.Exists(SignaturePath));

        await CreateService().SignAsync(_pdfPath);

        signatureExistedWhenSigning.Should().BeFalse("the signer refuses to overwrite an existing signature");
    }

    [Fact]
    public async Task SignAsync_WhenSigningFails_ThrowsASafeMessageAndDoesNotCheckTheSignature()
    {
        SetupRunner(signExitCode: 4, signStandardError: "could not open private key file /secrets/key.pem");

        var act = () => CreateService().SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("PDF signing failed (code 4).");
        _calls.Should().ContainSingle();
    }

    [Fact]
    public async Task SignAsync_WhenTheCheckFails_ThrowsAndRemovesTheSignature()
    {
        SetupRunner(verifyExitCode: 4);

        var act = () => CreateService().SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("PDF signature check failed (code 4).");
        File.Exists(SignaturePath).Should().BeFalse();
    }

    [Fact]
    public async Task SignAsync_WhenTheSignerTimesOut_Throws()
    {
        _runnerMock
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NativeProcessResult(-1, "", "", TimedOut: true));

        var act = () => CreateService().SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("PDF signing timed out.");
    }

    [Fact]
    public async Task SignAsync_WhenTheSignerIsMissing_ThrowsWithoutRunningIt()
    {
        var act = () => CreateService(Path.Combine(_workDirectory, "does-not-exist")).SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("The PDF signer is not available.");
        _runnerMock.Verify(r => r.RunAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignAsync_WhenThereIsNoKey_CreatesAMatchingRsaKeyPairInTheFormatTheSignerReads()
    {
        SetupRunner();

        await CreateService().SignAsync(_pdfPath);

        var privatePem = await File.ReadAllTextAsync(PrivateKeyPath);
        var publicPem = await File.ReadAllTextAsync(PublicKeyPath);
        privatePem.Should().StartWith("-----BEGIN PRIVATE KEY-----");
        publicPem.Should().StartWith("-----BEGIN PUBLIC KEY-----");

        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(privatePem);
        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(publicPem);
        privateKey.KeySize.Should().Be(2048);

        var data = Encoding.UTF8.GetBytes("report");
        var signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();
    }

    [Fact]
    public async Task SignAsync_WhenTheKeyAlreadyExists_KeepsIt()
    {
        SetupRunner();
        var service = CreateService();
        await service.SignAsync(_pdfPath);
        var privateBefore = await File.ReadAllTextAsync(PrivateKeyPath);
        var publicBefore = await File.ReadAllTextAsync(PublicKeyPath);

        await service.SignAsync(_pdfPath);

        (await File.ReadAllTextAsync(PrivateKeyPath)).Should().Be(privateBefore);
        (await File.ReadAllTextAsync(PublicKeyPath)).Should().Be(publicBefore);
    }

    [Fact]
    public async Task SignAsync_WhenOnlyThePublicKeyIsMissing_DerivesItFromThePrivateKey()
    {
        SetupRunner();
        await CreateService().SignAsync(_pdfPath);
        var publicBefore = await File.ReadAllTextAsync(PublicKeyPath);
        File.Delete(PublicKeyPath);

        await CreateService().SignAsync(_pdfPath);

        (await File.ReadAllTextAsync(PublicKeyPath)).Should().Be(publicBefore);
    }

    [Fact]
    public async Task SignAsync_WhenThePublicKeyDoesNotMatchThePrivateKey_ThrowsWithoutRunningTheSigner()
    {
        SetupRunner();
        var service = CreateService();
        await service.SignAsync(_pdfPath);
        var callsBefore = _calls.Count;
        using var otherKey = RSA.Create(2048);
        await File.WriteAllTextAsync(PublicKeyPath, otherKey.ExportSubjectPublicKeyInfoPem());

        var act = () => service.SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("The PDF signer keys are not valid.");
        _calls.Should().HaveCount(callsBefore);
    }

    [Fact]
    public async Task SignAsync_WhenAKeyFileIsNotAValidKey_Throws()
    {
        SetupRunner();
        var service = CreateService();
        await service.SignAsync(_pdfPath);
        await File.WriteAllTextAsync(PrivateKeyPath, "not a key");

        var act = () => service.SignAsync(_pdfPath);

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("The PDF signer keys are not valid.");
    }

    [Fact]
    public async Task GetPublicKeyPemAsync_WhenThereIsNoKey_CreatesTheKeyPairAndReturnsThePublicKey()
    {
        var publicPem = await CreateService().GetPublicKeyPemAsync();

        publicPem.Should().StartWith("-----BEGIN PUBLIC KEY-----");
        File.Exists(PrivateKeyPath).Should().BeTrue();

        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(await File.ReadAllTextAsync(PrivateKeyPath));
        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(publicPem);
        publicKey.ExportSubjectPublicKeyInfo().Should().Equal(privateKey.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public async Task GetPublicKeyPemAsync_WhenTheKeyAlreadyExists_ReturnsItUnchanged()
    {
        var service = CreateService();
        var first = await service.GetPublicKeyPemAsync();

        var second = await service.GetPublicKeyPemAsync();

        second.Should().Be(first);
        (await File.ReadAllTextAsync(PublicKeyPath)).Should().Be(first);
    }

    [Fact]
    public async Task GetPublicKeyPemAsync_WhenThePublicKeyDoesNotMatchThePrivateKey_Throws()
    {
        var service = CreateService();
        await service.GetPublicKeyPemAsync();
        using var otherKey = RSA.Create(2048);
        await File.WriteAllTextAsync(PublicKeyPath, otherKey.ExportSubjectPublicKeyInfoPem());

        var act = () => service.GetPublicKeyPemAsync();

        var exception = await act.Should().ThrowAsync<TaxReportGenerationException>();
        exception.Which.Message.Should().Be("The PDF signer keys are not valid.");
    }

    private PdfSigningService CreateService(string? signerPath = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxReport:PdfSignerPath"] = signerPath ?? _signerPath,
                ["TaxReport:PrivateKeyPath"] = PrivateKeyPath,
                ["TaxReport:PublicKeyPath"] = PublicKeyPath,
                ["TaxReport:TimeoutSeconds"] = "7"
            })
            .Build();

        return new PdfSigningService(_runnerMock.Object, configuration, NullLogger<PdfSigningService>.Instance);
    }

    // Acts as the native signer: writes a signature file on "sign" and returns the chosen exit codes.
    private void SetupRunner(
        int signExitCode = 0,
        int verifyExitCode = 0,
        string signStandardError = "",
        Action? onSign = null)
    {
        _runnerMock
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns((string _, IReadOnlyList<string> args, TimeSpan _, CancellationToken _) =>
            {
                _calls.Add(args.ToArray());

                if (args[0] == "sign")
                {
                    onSign?.Invoke();
                    if (signExitCode == 0)
                    {
                        File.WriteAllBytes(args[3], new byte[256]);
                    }

                    return Task.FromResult(new NativeProcessResult(signExitCode, "", signStandardError, TimedOut: false));
                }

                return Task.FromResult(new NativeProcessResult(verifyExitCode, "", "", TimedOut: false));
            });
    }
}