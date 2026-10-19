using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Data;

// Demo data for the demo: four customers, ten accounts and history from January 2025 until today
// (deposits, withdrawals, transfers between own accounts and between customers). The savings accounts
// also get their interest for 2025 booked on 31 December 2025, so the tax report for 2025 shows it.
// Saved in one SaveChanges, so it is all or nothing. Running it again does nothing.
// Booked as plain rows (no notifications, no audit entries): the history is "from before" the demo.
public class DemoDataSeeder
{
    // PIN 1234, the same bcrypt hash as the demo user from the migration.
    private const string PinHash = "$2b$12$Ma9ikA7xtUOXMH86.OZA7eYIEb.yDiazDkk5uu5M/4PpbuC3ORvSu";

    private static readonly DateOnly FirstDay = new(2025, 1, 3);

    // Fictional customers. Anna is the main demo customer and also the marker for "already seeded".
    private static readonly Persona[] Personas =
    {
        new("anna", "Anna", "Lindqvist", "19880312-4421", "anna.lindqvist@demo.example.com", 32000m, 8000m, 18000m, 3500m, 40000m, 80000m),
        new("erik", "Erik", "Johansson", "19810927-1136", "erik.johansson@demo.example.com", 26000m, 3000m, 19000m, 3000m, 25000m, 40000m),
        new("sara", "Sara", "Nilsson", "19930504-7782", "sara.nilsson@demo.example.com", 38000m, 12000m, 20000m, 4000m, 50000m, 120000m),
        new("omar", "Omar", "Hassan", "19790218-3355", "omar.hassan@demo.example.com", 22000m, 2000m, 16500m, 3000m, 20000m, 10000m)
    };

    // The Riksbank policy rate in 2025 (percent), from the day each rate applied, and what a savings account
    // pays compared with it (the same spread as the app uses for 2026). The first rate also covers the days before it.
    private static readonly RatePoint[] Rates2025 =
    {
        new(new DateOnly(2024, 12, 25), 2.50m),
        new(new DateOnly(2025, 2, 5), 2.25m),
        new(new DateOnly(2025, 6, 25), 2.00m),
        new(new DateOnly(2025, 10, 1), 1.75m)
    };

    private static readonly SpreadPoint[] Spreads2025 = { new(new DateOnly(2025, 1, 1), -0.5m) };

    private static readonly AccountPlan[] AccountPlans =
    {
        new("anna_chk", "anna", "NKM-20001", "CHECKING", "Transaktionskonto"),
        new("anna_sav", "anna", "NKM-20002", "SAVINGS", "Sparkonto"),
        new("anna_trv", "anna", "NKM-20003", "SAVINGS", "Semesterkassan"),
        new("erik_chk", "erik", "NKM-30001", "CHECKING", "Transaktionskonto"),
        new("erik_sav", "erik", "NKM-30002", "SAVINGS", "Sparkonto"),
        new("sara_chk", "sara", "NKM-40001", "CHECKING", "Transaktionskonto"),
        new("sara_sav", "sara", "NKM-40002", "SAVINGS", "Sparkonto"),
        new("sara_buf", "sara", "NKM-40003", "SAVINGS", "Buffert"),
        new("omar_chk", "omar", "NKM-50001", "CHECKING", "Transaktionskonto"),
        new("omar_sav", "omar", "NKM-50002", "SAVINGS", "Sparkonto")
    };

    // Single events on top of the monthly pattern. Transfers between customers come after the salary and
    // the monthly saving of the month, so the sender always has the money.
    private static readonly Step[] OneOffs =
    {
        new(new DateOnly(2026, 1, 5), StepKind.Deposit, "anna_sav", null, 20000m),
        new(new DateOnly(2026, 2, 2), StepKind.Transfer, "sara_sav", "sara_buf", 15000m),
        new(new DateOnly(2026, 3, 27), StepKind.Transfer, "anna_chk", "erik_chk", 1200m),
        new(new DateOnly(2026, 4, 27), StepKind.Transfer, "erik_chk", "anna_chk", 450m),
        new(new DateOnly(2026, 5, 29), StepKind.Transfer, "anna_sav", "anna_trv", 10000m),
        new(new DateOnly(2026, 6, 27), StepKind.Transfer, "sara_chk", "anna_chk", 800m),
        new(new DateOnly(2026, 7, 15), StepKind.Transfer, "anna_trv", "anna_chk", 4000m),
        new(new DateOnly(2026, 8, 27), StepKind.Transfer, "anna_chk", "omar_chk", 2500m),
        new(new DateOnly(2026, 9, 28), StepKind.Transfer, "omar_chk", "sara_chk", 600m),
        new(new DateOnly(2026, 10, 8), StepKind.Transfer, "anna_chk", "sara_chk", 350m),
        new(new DateOnly(2026, 10, 9), StepKind.Deposit, "anna_chk", null, 1500m)
    };

    private readonly ApplicationDbContext _context;
    private readonly IPersonalNumberProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApplicationDbContext context,
        IPersonalNumberProtector protector,
        TimeProvider timeProvider,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _protector = protector;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Returns true if data was added.
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        var marker = _protector.ComputeHash(Personas[0].PersonalNumber);
        if (await _context.Users.AnyAsync(u => u.PersonalNumberHash == marker, cancellationToken))
        {
            _logger.LogInformation("Demo data already exists. Nothing was added.");
            return false;
        }

        // Never crash on a unique index: if someone already uses one of our emails, personal numbers or
        // account numbers, add nothing at all.
        var emails = Personas.Select(p => p.Email).ToList();
        var hashes = Personas.Select(p => _protector.ComputeHash(p.PersonalNumber)).ToList();
        var numbers = AccountPlans.Select(a => a.Number).ToList();

        var taken =
            await _context.Users.AnyAsync(u => emails.Contains(u.Email) || hashes.Contains(u.PersonalNumberHash!), cancellationToken)
            || await _context.Accounts.AnyAsync(a => numbers.Contains(a.AccountNumber), cancellationToken);

        if (taken)
        {
            _logger.LogWarning("Demo data was not added: an email, personal number or account number is already in use.");
            return false;
        }

        var createdAt = At(FirstDay, 0);

        var users = Personas.ToDictionary(
            p => p.Key,
            p => new User
            {
                PersonalNumber = _protector.Protect(p.PersonalNumber),
                PersonalNumberHash = _protector.ComputeHash(p.PersonalNumber),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                PinHash = PinHash,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

        var accounts = AccountPlans.ToDictionary(
            a => a.Key,
            a => new Account
            {
                UserId = users[a.OwnerKey].Id,
                AccountNumber = a.Number,
                AccountType = a.Type,
                Name = a.Name,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

        var ownerOf = AccountPlans.ToDictionary(a => a.Key, a => users[a.OwnerKey]);
        var book = new Book(accounts.Keys);

        _context.Users.AddRange(users.Values);
        _context.Accounts.AddRange(accounts.Values);

        var steps = BuildSteps(SwedishTime.TodayIn(_timeProvider));

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var at = At(step.Date, i);
            var fromAccount = accounts[step.Account];

            switch (step.Kind)
            {
                case StepKind.Deposit:
                    Post(fromAccount, step.Account, "DEPOSIT", step.Amount, "Deposit", at, null, null, book);
                    break;

                case StepKind.Withdrawal:
                    Post(fromAccount, step.Account, "WITHDRAWAL", -step.Amount, "Withdrawal", at, null, null, book);
                    break;

                case StepKind.Transfer:
                    var toAccount = accounts[step.ToAccount!];
                    var sender = ownerOf[step.Account];
                    var recipient = ownerOf[step.ToAccount!];
                    var own = sender.Id == recipient.Id;
                    var transferId = Guid.NewGuid();

                    // The same labels as AccountService.TransferAsync: own account shows the account name,
                    // another customer shows a masked name.
                    var outLabel = own ? toAccount.Name : MaskName(recipient.FirstName, recipient.LastName);
                    var inLabel = own ? fromAccount.Name : MaskName(sender.FirstName, sender.LastName);

                    Post(fromAccount, step.Account, "TRANSFER_OUT", -step.Amount, $"Transfer to {toAccount.AccountNumber}", at, transferId, outLabel, book);
                    Post(toAccount, step.ToAccount!, "TRANSFER_IN", step.Amount, $"Transfer from {fromAccount.AccountNumber}", at, transferId, inLabel, book);
                    break;

                case StepKind.Interest:
                    // Booked like InterestPayoutService does it: calculated from the day-by-day balance,
                    // dated 31 December 12:00 UTC, with a row in InterestPayouts.
                    var interest = InterestCalculator.AccruedInterest(
                        Rates2025, Spreads2025, 0m, book.Changes[step.Account], new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1));

                    if (interest > 0m)
                    {
                        var bookedAt = new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc);
                        var interestTransaction = Post(fromAccount, step.Account, "INTEREST", interest, "R\u00e4nta 2025", bookedAt, null, null, book);

                        _context.InterestPayouts.Add(new InterestPayout
                        {
                            AccountId = fromAccount.Id,
                            Year = 2025,
                            Amount = interest,
                            TransactionId = interestTransaction.Id,
                            PaidAt = bookedAt
                        });
                    }

                    break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Demo data added: {Users} customers, {Accounts} accounts, {Steps} events.",
            users.Count, accounts.Count, steps.Count);

        return true;
    }

    // The monthly pattern of every customer plus the single events, only for days before today,
    // in date order (events on the same day keep the order they are listed in).
    private static List<Step> BuildSteps(DateOnly today)
    {
        var steps = new List<Step>();

        foreach (var p in Personas)
        {
            var checking = p.Key + "_chk";
            var savings = p.Key + "_sav";

            steps.Add(new Step(FirstDay, StepKind.Deposit, checking, null, p.OpeningChecking));
            steps.Add(new Step(FirstDay.AddDays(1), StepKind.Deposit, savings, null, p.OpeningSavings));

            for (var month = new DateOnly(2025, 2, 1); month <= today; month = month.AddMonths(1))
            {
                steps.Add(new Step(month, StepKind.Withdrawal, checking, null, p.Rent));                         // the 1st
                steps.Add(new Step(month.AddDays(11), StepKind.Withdrawal, checking, null, p.Cash));             // the 12th
                steps.Add(new Step(month.AddDays(24), StepKind.Deposit, checking, null, p.Salary));              // the 25th
                steps.Add(new Step(month.AddDays(25), StepKind.Transfer, checking, savings, p.Saving));          // the 26th
            }
        }

        // The interest for 2025 is booked once, on the last day of the year, on every savings account.
        foreach (var plan in AccountPlans.Where(a => a.Type == "SAVINGS"))
        {
            steps.Add(new Step(new DateOnly(2025, 12, 31), StepKind.Interest, plan.Key, null, 0m));
        }

        steps.AddRange(OneOffs);

        return steps.Where(s => s.Date < today).OrderBy(s => s.Date).ToList();
    }

    // One transaction and its ledger entry. Throws if the balance would go below zero, so a
    // careless change of the numbers above is found at once (and by the tests).
    private Transaction Post(
        Account account,
        string key,
        string type,
        decimal signedAmount,
        string description,
        DateTime at,
        Guid? transferId,
        string? label,
        Book book)
    {
        book.Balances[key] += signedAmount;
        book.Changes[key].Add(new BalanceChange(SwedishTime.DateOf(at), signedAmount));

        if (book.Balances[key] < 0m)
        {
            throw new InvalidOperationException($"Demo data would give {account.AccountNumber} a negative balance on {at:yyyy-MM-dd}.");
        }

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Amount = Math.Abs(signedAmount),
            TransactionType = type,
            Status = "COMPLETED",
            CreatedAt = at,
            CompletedAt = at,
            TransferId = transferId,
            CounterpartyLabel = label
        };

        _context.Transactions.Add(transaction);
        _context.LedgerEntries.Add(new LedgerEntry
        {
            AccountId = account.Id,
            TransactionId = transaction.Id,
            Amount = signedAmount,
            EntryType = type,
            Description = description,
            CreatedAt = at
        });

        return transaction;
    }

    // 10:00 UTC is the same Swedish day all year. The seconds keep events on the same day in order.
    private static DateTime At(DateOnly date, int sequence) =>
        new DateTime(date.Year, date.Month, date.Day, 10, 0, 0, DateTimeKind.Utc).AddSeconds(sequence);

    private static string MaskName(string firstName, string lastName)
    {
        var first = firstName.Trim();
        var last = lastName.Trim();
        return last.Length == 0 ? first : $"{first} {char.ToUpperInvariant(last[0])}.";
    }

    private sealed record Persona(
        string Key, string FirstName, string LastName, string PersonalNumber, string Email,
        decimal Salary, decimal Saving, decimal Rent, decimal Cash, decimal OpeningChecking, decimal OpeningSavings);

    private sealed record AccountPlan(string Key, string OwnerKey, string Number, string Type, string Name);

    private enum StepKind { Deposit, Withdrawal, Transfer, Interest }

    // Running balance and day-by-day changes of every account while the history is built.
    private sealed class Book
    {
        public Book(IEnumerable<string> accountKeys)
        {
            Balances = accountKeys.ToDictionary(key => key, _ => 0m);
            Changes = accountKeys.ToDictionary(key => key, _ => new List<BalanceChange>());
        }

        public Dictionary<string, decimal> Balances { get; }

        public Dictionary<string, List<BalanceChange>> Changes { get; }
    }

    private sealed record Step(DateOnly Date, StepKind Kind, string Account, string? ToAccount, decimal Amount);
}