using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.DTOs.TaxReports;
using NordiskaPortal.API.Services.Interfaces;
using System.Globalization;

namespace NordiskaPortal.API.Services
{
    public class TaxReportDataService : ITaxReportDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public TaxReportDataService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<TaxReportInputDto> GetTaxReportDataAsync(Guid userId, int reportYear, Guid reportId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("User not found.");

            var yearStartUtc = SwedishTime.StartOfDayUtc(new DateOnly(reportYear, 1, 1));
            var yearEndUtc = SwedishTime.StartOfDayUtc(new DateOnly(reportYear + 1, 1, 1));

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();

            var accountDtos = new List<TaxAccountDto>();
            decimal totalDeposits = 0, totalWithdrawals = 0, interestIncome = 0;

            foreach (var account in accounts)
            {
                var entries = await _context.LedgerEntries
                    .Include(l => l.Transaction)
                    .Where(l => l.AccountId == account.Id && l.Transaction.Status == "COMPLETED")
                    .OrderBy(l => l.CreatedAt)
                    .ThenBy(l => l.Id)
                    .ToListAsync();

                var openingBalance = entries.Where(e => e.CreatedAt < yearStartUtc).Sum(e => e.Amount);
                var yearEntries = entries.Where(e => e.CreatedAt >= yearStartUtc && e.CreatedAt < yearEndUtc).ToList();
                var closingBalance = openingBalance + yearEntries.Sum(e => e.Amount);

                foreach (var entry in yearEntries)
                {
                    switch (entry.EntryType)
                    {
                        case "DEPOSIT":
                            totalDeposits += entry.Amount;
                            break;
                        case "WITHDRAWAL":
                            totalWithdrawals += Math.Abs(entry.Amount);
                            break;
                        case "INTEREST":
                            interestIncome += entry.Amount;
                            break;
                    }

                }

                accountDtos.Add(new TaxAccountDto(
                    account.Id,
                    account.AccountNumber,
                    account.AccountType,
                    openingBalance.ToString("F2", CultureInfo.InvariantCulture),
                    closingBalance.ToString("F2", CultureInfo.InvariantCulture),
                    yearEntries.Select(entry => new TaxTransactionDto(
                        entry.Id,
                        new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero),
                        entry.EntryType,
                        DescribeEntry(entry),
                        entry.Amount.ToString("F2", CultureInfo.InvariantCulture)
                    )).ToList()
                ));
            }

            var capitalTaxRate = Math.Round(interestIncome * 0.30m, 2, MidpointRounding.AwayFromZero);

            var bank = new BankDto(
            _configuration["Bank:Name"]!,
            _configuration["Bank:OrganizationNumber"]!,
            _configuration["Bank:Address"]!,
            _configuration["Bank:Phone"]!,
            _configuration["Bank:Email"]!);

            return new TaxReportInputDto(
                1,
                reportId,
                reportYear,
                DateTimeOffset.UtcNow,
                "SEK",
                bank,
                new CustomerDto(user.Id, $"{user.FirstName} {user.LastName}"),
                accountDtos,
                new TaxSummaryDto(
                    totalDeposits.ToString("F2", CultureInfo.InvariantCulture),
                    totalWithdrawals.ToString("F2", CultureInfo.InvariantCulture),
                    interestIncome.ToString("F2", CultureInfo.InvariantCulture),
                    "0.30", // Capital tax rate is 30% in Sweden
                    capitalTaxRate.ToString("F2", CultureInfo.InvariantCulture)
                )
            );

        }

        private static string DescribeEntry(LedgerEntry entry)
        {
            var label = entry.Transaction?.CounterpartyLabel;

            if ((entry.EntryType == "TRANSFER_OUT" || entry.EntryType == "TRANSFER_IN")
                && !string.IsNullOrWhiteSpace(label))
            {
                return $"{entry.Description} ({label})";
            }

            return entry.Description;
        }
    }
}
