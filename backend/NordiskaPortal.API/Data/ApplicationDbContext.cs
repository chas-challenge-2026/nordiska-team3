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
    public DbSet<TaxReport> TaxReports { get; set; }

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


        //seed data for frontend
        var demoUserId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
        var checkingAccountId = Guid.Parse("d0000000-0000-0000-0000-000000000002");
        var savingsAccountId = Guid.Parse("d0000000-0000-0000-0000-000000000003");
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = demoUserId,
                PersonalNumber = "19850615-5678",
                FirstName = "Demo",
                LastName = "Kund",
                Email = "demo@nordiskaportal.se",
                PinHash = "$2b$12$Ma9ikA7xtUOXMH86.OZA7eYIEb.yDiazDkk5uu5M/4PpbuC3ORvSu", // PIN: 1234
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        modelBuilder.Entity<Account>().HasData(
            new Account
            {
                Id = checkingAccountId,
                UserId = demoUserId,
                AccountNumber = "NKM-10001",
                AccountType = "CHECKING",
                Name = "Transaktionskonto",
                Status = "ACTIVE",
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Account
            {
                Id = savingsAccountId,
                UserId = demoUserId,
                AccountNumber = "NKM-10002",
                AccountType = "SAVINGS",
                Name = "Sparkonto",
                Status = "ACTIVE",
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        var tx1 = Guid.Parse("d0000000-0000-0000-0000-000000000004");
        var tx2 = Guid.Parse("d0000000-0000-0000-0000-000000000005");
        var tx3 = Guid.Parse("d0000000-0000-0000-0000-000000000006");
        var tx4 = Guid.Parse("d0000000-0000-0000-0000-000000000007");
        var tx5 = Guid.Parse("d0000000-0000-0000-0000-000000000008");

        modelBuilder.Entity<Transaction>().HasData(
            new Transaction { Id = tx1, AccountId = checkingAccountId, Amount = 5000m, TransactionType = "DEPOSIT", Status = "COMPLETED", CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = tx2, AccountId = checkingAccountId, Amount = 3000m, TransactionType = "DEPOSIT", Status = "COMPLETED", CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = tx3, AccountId = checkingAccountId, Amount = 750m, TransactionType = "WITHDRAWAL", Status = "COMPLETED", CreatedAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = tx4, AccountId = savingsAccountId, Amount = 20000m, TransactionType = "DEPOSIT", Status = "COMPLETED", CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = tx5, AccountId = savingsAccountId, Amount = 5000m, TransactionType = "DEPOSIT", Status = "COMPLETED", CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<LedgerEntry>().HasData(
            new LedgerEntry { Id = Guid.Parse("d0000000-0000-0000-0000-000000000009"), AccountId = checkingAccountId, TransactionId = tx1, Amount = 5000m, EntryType = "DEPOSIT", Description = "Deposit", CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new LedgerEntry { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000a"), AccountId = checkingAccountId, TransactionId = tx2, Amount = 3000m, EntryType = "DEPOSIT", Description = "Deposit", CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new LedgerEntry { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000b"), AccountId = checkingAccountId, TransactionId = tx3, Amount = -750m, EntryType = "WITHDRAWAL", Description = "Withdrawal", CreatedAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LedgerEntry { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000c"), AccountId = savingsAccountId, TransactionId = tx4, Amount = 20000m, EntryType = "DEPOSIT", Description = "Deposit", CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
            new LedgerEntry { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000d"), AccountId = savingsAccountId, TransactionId = tx5, Amount = 5000m, EntryType = "DEPOSIT", Description = "Deposit", CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) }
        );

        // TaxReport relationships
        modelBuilder.Entity<TaxReport>()
            .HasOne(t => t.User)
            .WithMany(u => u.TaxReports)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}