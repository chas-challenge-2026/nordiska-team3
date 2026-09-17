namespace NordiskaPortal.API.Data;

using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<FaqEntry> FaqEntries { get; set; } = null!;
    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PersonalNumber)
            .IsUnique();

        // Account constraints
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        // Account relationships
        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // LedgerEntry relationships
        modelBuilder.Entity<LedgerEntry>()
            .HasOne(l => l.Account)
            .WithMany(a => a.LedgerEntries)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LedgerEntry>()
            .HasOne(l => l.Transaction)
            .WithMany(t => t.LedgerEntries)
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Transaction relationships
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data for FAQ
        modelBuilder.Entity<FaqEntry>().HasData(
            new FaqEntry
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Question = "How do I open a new account?",
                Answer = "You can apply for a new account directly through our portal under the Accounts tab.",
                Category = "Accounts",
                Keywords = "account, open, apply, create",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new FaqEntry
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Question = "What is the interest rate on the savings account?",
                Answer = "Our current savings account interest rate is 3.5% annually.",
                Category = "Savings",
                Keywords = "interest, rate, savings, deposit",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}