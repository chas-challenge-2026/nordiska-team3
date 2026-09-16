namespace NordiskaPortal.API.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PersonalNumber { get; set; } // Används vid PIN-inlog, "ÅÅÅÅMMDD-XXXX" format.
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PinHash { get; set; } // Pin-kod som hashas i AuthService, ska aldrig lagras eller loggas
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}