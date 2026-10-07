using FluentValidation;
using static NordiskaPortal.API.Controllers.TaxReportsController;

namespace NordiskaPortal.API.Validators.TaxReports;

public class CreateTaxReportRequestValidator : AbstractValidator<CreateTaxReportRequest>
{
    public CreateTaxReportRequestValidator()
    {
        RuleFor(x => x.ReportYear)
            .InclusiveBetween(2025, DateTime.UtcNow.Year)
            .WithMessage($"Skatteår måste vara mellan 2025 och {DateTime.UtcNow.Year}.");
    }
}