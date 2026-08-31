namespace NordiskaPortal.API.Models;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string EntryType { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; }
    public Transaction Transaction { get; set; }
}