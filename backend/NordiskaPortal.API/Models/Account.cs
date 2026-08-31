namespace NordiskaPortal.API.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string AccountType { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}