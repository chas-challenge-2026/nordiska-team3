using FluentValidation;
using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Validators.Accounts;

public class CreateAccountRequestDtoValidator : AbstractValidator<CreateAccountRequestDto>
{
    private static readonly string[] AllowedAccountTypes = { "SAVINGS", "CHECKING" };

    public CreateAccountRequestDtoValidator()
    {
        RuleFor(x => x.AccountType)
            .NotEmpty()
            .Must(t => AllowedAccountTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Kontotyp måste vara en av: {string.Join(", ", AllowedAccountTypes)}.");
    }
}