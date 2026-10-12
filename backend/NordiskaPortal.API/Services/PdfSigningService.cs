using System.Security.Cryptography;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

// Signs generated tax report PDFs with the native pdf_signer (RSA PKCS#1, SHA-256) and checks the signature
// straight away, so a report is only READY when its signature is known to match the file.
public sealed class PdfSigningService : IPdfSigningService
{
    private const int KeySizeBits = 2048;
    private const int MaxLoggedErrorLength = 1000;

    private readonly INativeProcessRunner _processRunner;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfSigningService> _logger;
    private readonly object _keyLock = new();

    public PdfSigningService(INativeProcessRunner processRunner, IConfiguration configuration, ILogger<PdfSigningService> logger)
    {
        _processRunner = processRunner;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PdfSignature> SignAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var signerPath = Path.GetFullPath(_configuration["TaxReport:PdfSignerPath"]!);
        if (!File.Exists(signerPath))
        {
            _logger.LogError("PDF signer not found at {Path}", signerPath);
            throw new TaxReportGenerationException("The PDF signer is not available.");
        }

        var (privateKeyPath, publicKeyPath) = EnsureKeyPair();
        var pdfFullPath = Path.GetFullPath(pdfPath);
        var signaturePath = Path.ChangeExtension(pdfFullPath, ".sig");

        // The signer refuses to overwrite a signature, so clear any leftover from an earlier attempt.
        File.Delete(signaturePath);

        var timeout = TimeSpan.FromSeconds(_configuration.GetValue("TaxReport:TimeoutSeconds", 30));

        await RunSignerAsync(signerPath, "signing", new[] { "sign", pdfFullPath, privateKeyPath, signaturePath }, timeout, cancellationToken);

        try
        {
            await RunSignerAsync(signerPath, "signature check", new[] { "verify", pdfFullPath, publicKeyPath, signaturePath }, timeout, cancellationToken);
        }
        catch
        {
            File.Delete(signaturePath);
            throw;
        }

        return new PdfSignature(signaturePath, ComputeSha256(pdfFullPath));
    }

    private async Task RunSignerAsync(string signerPath, string step, string[] arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(signerPath, arguments, timeout, cancellationToken);

        if (result.TimedOut)
        {
            _logger.LogError("PDF signer {Step} timed out after {Seconds}s", step, timeout.TotalSeconds);
            throw new TaxReportGenerationException($"PDF {step} timed out.");
        }

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "PDF signer {Step} failed with exit code {ExitCode}: {StandardError}",
                step, result.ExitCode, Truncate(result.StandardError));
            throw new TaxReportGenerationException($"PDF {step} failed (code {result.ExitCode}).");
        }
    }

    // The private key is created once and kept outside git (by default in the api_secrets volume).
    private (string PrivateKeyPath, string PublicKeyPath) EnsureKeyPair()
    {
        var privateKeyPath = Path.GetFullPath(_configuration["TaxReport:PrivateKeyPath"]!);
        var publicKeyPath = Path.GetFullPath(_configuration["TaxReport:PublicKeyPath"]!);

        lock (_keyLock)
        {
            if (!File.Exists(privateKeyPath))
            {
                using var rsa = RSA.Create(KeySizeBits);
                WriteKeyFile(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem(), isSecret: true);
                WriteKeyFile(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem(), isSecret: false);

                _logger.LogWarning("Tax report signing key not found, a new key pair was generated at {Path}.", privateKeyPath);
            }
            else if (!File.Exists(publicKeyPath))
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
                WriteKeyFile(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem(), isSecret: false);
            }
        }

        return (privateKeyPath, publicKeyPath);
    }

    private static void WriteKeyFile(string path, string pem, bool isSecret)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, pem);

        if (isSecret && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Truncate(string text) =>
        text.Length <= MaxLoggedErrorLength ? text : text[..MaxLoggedErrorLength];
}
