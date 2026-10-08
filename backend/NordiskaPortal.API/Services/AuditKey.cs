using System.Security.Cryptography;

namespace NordiskaPortal.API.Services;

// The secret that signs the audit chain. It is kept outside the database (configuration or a key file),
// so someone who can only edit the database cannot compute valid hashes.
// Like the personal number key, a new key is only created while the audit log is empty: with entries
// in the database a missing key is an error, because a new key would make the existing chain fail verification.
public sealed class AuditKey
{
    public const string DefaultKeyFilePath = "/secrets/audit.key";
    private const int MinimumLength = 32;

    private readonly string _keyFilePath;
    private byte[]? _bytes;

    // For tests and for code that already has the key.
    public AuditKey(byte[] bytes)
    {
        _keyFilePath = DefaultKeyFilePath;
        SetBytes(bytes);
    }

    private AuditKey(string keyFilePath)
    {
        _keyFilePath = keyFilePath;
    }

    public bool IsAvailable => _bytes is not null;

    public byte[] Bytes => _bytes ?? throw new InvalidOperationException("The audit key is not available.");

    // Reads the key from configuration (Audit:Key, base64) or from the key file. It stays unavailable if neither exists.
    public static AuditKey Load(IConfiguration configuration, string keyFilePath = DefaultKeyFilePath)
    {
        var key = new AuditKey(keyFilePath);

        var configured = configuration["Audit:Key"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            key.SetBytes(Convert.FromBase64String(configured));
        }
        else if (File.Exists(keyFilePath))
        {
            key.SetBytes(Convert.FromBase64String(File.ReadAllText(keyFilePath).Trim()));
        }

        return key;
    }

    // Creates and stores a new key. Only call this while no audit entries exist.
    public void GenerateNewKey()
    {
        if (IsAvailable)
        {
            throw new InvalidOperationException("The audit key already exists.");
        }

        var bytes = RandomNumberGenerator.GetBytes(MinimumLength);

        Directory.CreateDirectory(Path.GetDirectoryName(_keyFilePath)!);
        File.WriteAllText(_keyFilePath, Convert.ToBase64String(bytes));

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        SetBytes(bytes);
    }

    private void SetBytes(byte[] bytes)
    {
        if (bytes.Length < MinimumLength)
        {
            throw new InvalidOperationException("The audit key must be at least 32 bytes (base64).");
        }

        _bytes = bytes;
    }
}
