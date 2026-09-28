using FluentValidation;
using NordiskaPortal.API.DTOs.Auth;

namespace NordiskaPortal.API.Validators.Auth;

public class LoginPinRequestDtoValidator : AbstractValidator<LoginPinRequestDto>
{
    public LoginPinRequestDtoValidator()
    {
        RuleFor(x => x.PersonalNumber)
            .NotEmpty()
            .Matches(@"^\d{8}-\d{4}$")
            .WithMessage("Personnummer måste vara i formatet ÅÅÅÅMMDD-XXXX.");

        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("PIN-koden måste vara exakt 4 siffror.");
    }
}