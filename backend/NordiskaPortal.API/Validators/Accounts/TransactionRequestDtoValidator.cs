using FluentValidation;
using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Validators.Accounts;

public class TransactionRequestDtoValidator : AbstractValidator<TransactionRequestDto>
{
    public TransactionRequestDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Beloppet måste vara större än 0.");
    }
}