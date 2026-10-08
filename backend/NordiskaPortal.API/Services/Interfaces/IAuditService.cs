namespace NordiskaPortal.API.Services.Interfaces;

public interface IAuditService
{
    // Adds one audit entry to the current unit of work. Call it inside a database transaction and save it together with the audited change, so both are stored or neither is. Save before appending another entry.
    // This takes the lock on the audit chain, which must always be taken last: take any row locks (SELECT ... FOR UPDATE) before calling it, or two requests can end up waiting for each other.
    Task AppendAsync(Guid? userId, string action, string entityType, Guid entityId, object details);

    // Recomputes the whole chain and reports the first entry that does not match.
    Task<AuditVerificationResult> VerifyChainAsync();
}

public sealed record AuditVerificationResult(bool IsValid, int CheckedCount, long? FirstInvalidId);
