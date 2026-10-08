using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services;

// Hashing for the audit chain. Each entry's hash covers its own fields plus the previous entry's hash.

public static class AuditChain
{
    // PreviousHash of the very first entry.
    public static readonly string GenesisHash = new('0', 64);

    public static string ComputeHash(byte[] key, AuditEntry entry)
    {
        // Length-prefixed fields, so one field can never run into the next one.
        var text = new StringBuilder();
        Append(text, entry.PreviousHash);
        Append(text, entry.CreatedAt.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture));
        Append(text, entry.UserId?.ToString("D"));
        Append(text, entry.Action);
        Append(text, entry.EntityType);
        Append(text, entry.EntityId.ToString("D"));
        Append(text, entry.Details);

        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(text.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void Append(StringBuilder text, string? value) =>
        text.Append(value is null ? "-" : $"{value.Length}:{value}").Append('\n');
}