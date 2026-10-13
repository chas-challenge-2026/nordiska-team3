namespace NordiskaPortal.API.Models;

public class InterestPayout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public Guid? TransactionId { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}