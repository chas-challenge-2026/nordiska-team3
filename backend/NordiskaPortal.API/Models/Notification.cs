namespace NordiskaPortal.API.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public User User { get; set; }
}