using System.Security.Cryptography;
using System.Text;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public sealed class PersonalNumberProtector : IPersonalNumberProtector
{
    public const string Prefix = "enc:v1:";
    private const string KeyFilePath = "/secrets/pn.key";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _encryptionKey;
    private readonly byte[] _hashKey;

    public PersonalNumberProtector(IConfiguration configuration, ILogger<PersonalNumberProtector> logger)
    {
        var masterKey = LoadMasterKey(configuration, logger);
        _encryptionKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32,
            info: Encoding.UTF8.GetBytes("personal-number-encryption"));
        _hashKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32,
            info: Encoding.UTF8.GetBytes("personal-number-hash"));
    }

    public bool IsProtected(string stored) =>
        stored.StartsWith(Prefix, StringComparison.Ordinal);

    public string Protect(string personalNumber)
    {
        var plaintext = Encoding.UTF8.GetBytes(personalNumber.Trim());
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string stored)
    {
        if (!IsProtected(stored))
        {
            return stored;
        }

        var payload = Convert.FromBase64String(stored[Prefix.Length..]);
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string ComputeHash(string personalNumber) =>
        Convert.ToBase64String(
            HMACSHA256.HashData(_hashKey, Encoding.UTF8.GetBytes(personalNumber.Trim())));

    private static byte[] LoadMasterKey(IConfiguration configuration, ILogger logger)
    {
        var configured = configuration["PersonalNumberProtection:Key"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            logger.LogInformation("Personal number key loaded from configuration.");
            return Convert.FromBase64String(configured);
        }

        if (File.Exists(KeyFilePath))
        {
            logger.LogInformation("Personal number key loaded from {Path}.", KeyFilePath);
            return Convert.FromBase64String(File.ReadAllText(KeyFilePath).Trim());
        }

        var key = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(Path.GetDirectoryName(KeyFilePath)!);
        File.WriteAllText(KeyFilePath, Convert.ToBase64String(key));

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(KeyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        logger.LogWarning("Personal number key not found, a new key was generated at {Path}.", KeyFilePath);
        return key;
    }
}