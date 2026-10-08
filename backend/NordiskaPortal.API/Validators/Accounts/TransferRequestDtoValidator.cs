using FluentValidation;
using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Validators.Accounts;

public class TransferRequestDtoValidator : AbstractValidator<TransferRequestDto>
{
    public TransferRequestDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Beloppet måste vara större än 0.")
            .HasAtMostTwoDecimals();

        RuleFor(x => x.ToAccountNumber)
            .NotEmpty()
            .Matches(@"^NKM-\d{5}\z")
            .WithMessage("Ange ett giltigt kontonummer (NKM-xxxxx).");
    }
}