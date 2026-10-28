namespace NordiskaPortal.API.Services.BankId;

public sealed class BankIdOptions
{
    public const string SectionName = "BankId";

    // "Off" (default): BankID is not available. "Mock": BankID is faked in memory (Development only).
    // "Test": the real API on BankID's test server.

    public string Mode { get; set; } = "Off";

    public string BaseUrl { get; set; } = "https://appapi2.test.bankid.com/rp/v6.0/";

    // Paths are relative to the content root (the folder of the application).
    public string ClientCertificatePath { get; set; } = "Certs/FPTestcert5_20240610.p12";
    public string? ClientCertificatePassword { get; set; }
    public string RootCertificatePath { get; set; } = "Certs/TestBankIDSSLRootCAv1Test.crt";

    public bool IsTest => string.Equals(Mode, "Test", StringComparison.OrdinalIgnoreCase);
    public bool IsMock => string.Equals(Mode, "Mock", StringComparison.OrdinalIgnoreCase);
    public bool IsOff => string.Equals(Mode, "Off", StringComparison.OrdinalIgnoreCase);
}