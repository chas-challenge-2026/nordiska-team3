using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class AuditService : IAuditService
{
    // Any fixed number works as long as evert writer uses the same one.
    private const long ChainLockKey = 7201903;

    private readonly ApplicationDbContext _context;
    private readonly AuditKey _key;

    public AuditService(ApplicationDbContext context, AuditKey key)
    {
        _context = context;
        _key = key;
    }

    public async Task EnsureKeyAsync()
    {
        if (_key.IsAvailable)
        {
            return;
        }

        if (await _context.AuditEntries.AnyAsync())
        {
            throw new InvalidOperationException(
                "The audit key is missing but audit entries exist. " +
                "Restore /secrets/audit.key (or Audit:Key). Refusing to generate a new key.");
        }

        _key.GenerateNewKey();
    }

    public async Task AppendAsync(Guid? userId, string action, string entityType, Guid entityId, object details)
    {
        if (_context.ChangeTracker.Entries<AuditEntry>().Any(e => e.State == EntityState.Added))
        {
            throw new InvalidOperationException("Save the previous audit entry before appending another one.");
        }

        // The in-memory database used by the unit tests has neither transactions nor advisory locks.
        if (_context.Database.IsRelational())
        {
            if (_context.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException("Audit entries must be appended inside a database transaction.");
            }

            // Held until the transaction ends, so two requests cannot extend the chain from the same last entry.
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({ChainLockKey})");
        }

        var previousHash = await _context.AuditEntries
            .AsNoTracking()
            .OrderByDescending(a => a.Id)
            .Select(a => a.Hash)
            .FirstOrDefaultAsync() ?? AuditChain.GenesisHash;

        var entry = new AuditEntry
        {
            CreatedAt = TruncateToMicroseconds(DateTime.UtcNow),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.Serialize(details),
            PreviousHash = previousHash
        };

        entry.Hash = AuditChain.ComputeHash(_key.Bytes, entry);

        _context.AuditEntries.Add(entry);
    }

    public async Task<AuditVerificationResult> VerifyChainAsync()
    {
        var expectedPreviousHash = AuditChain.GenesisHash;
        var checkedCount = 0;

        await foreach (var entry in _context.AuditEntries.AsNoTracking().OrderBy(a => a.Id).AsAsyncEnumerable())
        {
            var isValid = entry.PreviousHash == expectedPreviousHash && entry.Hash == AuditChain.ComputeHash(_key.Bytes, entry);

            if (!isValid)
            {
                return new AuditVerificationResult(false, checkedCount, entry.Id);
            }

            expectedPreviousHash = entry.Hash;
            checkedCount++;
        }

        return new AuditVerificationResult(true, checkedCount, null);
    }

    // PostgreSQL stores microseconds, so the hash has to be computed on a value that survives a round trip.
    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
}

