using FluentValidation;
using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Validators.Auth;

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.PersonalNumber)
            .NotEmpty()
            .Matches(@"^\d{8}-\d{4}$")
            .WithMessage("Personnummer måste vara i formatet ÅÅÅÅMMDD-XXXX.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Ange en giltig e-postadress.");

        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("PIN-koden måste vara exakt 4 siffror.");
    }
}