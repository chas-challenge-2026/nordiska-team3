namespace NordiskaPortal.API.DTOs.TaxReports
{
    public sealed record TaxReportInputDto(
        int schemaVersion,
        Guid reportId,
        int reportYear,
        DateTimeOffset GeneratedAt,
        string Currency,
        BankDto Bank,
        CustomerDto Customer,
        IReadOnlyList<TaxAccountDto> Accounts,
        TaxSummaryDto Summary
    );

    public sealed record BankDto(
        string Name,
        string OrganizationNumber,
        string Address,
        string Phone,
        string Email
    );

    public sealed record CustomerDto(Guid Id, string DisplayName);

    public sealed record TaxAccountDto(
        Guid Id,
        string DisplayNumber,
        string AccountType,
        string OpeningBalance,
        string ClosingBalance,
        IReadOnlyList<TaxTransactionDto> Transactions
    );

    public sealed record TaxTransactionDto(
        Guid Id,
        DateTimeOffset BookedAt,
        string Type,
        string Description,
        string Amount
    );

    public sealed record TaxSummaryDto(
        string TotalDeposits,
        string TotalWithdrawals,
        string InterestIncome,
        string CapitalTaxRate,
        string CapitalTax
    );
}

