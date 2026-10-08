namespace NordiskaPortal.API.Models;

// One row per audited action. Rows are only ever added: the database rejects UPDATE, DELETE and TRUNCATE.
// Hash is a keyed hash over the row's fields and PreviousHash, so a changed or removed row breaks the chain.
public class AuditEntry
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Details { get; set; } = "{}";
    public string PreviousHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}