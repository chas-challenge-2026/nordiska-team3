using FluentValidation;
using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Validators.Accounts;

public class RenameAccountRequestDtoValidator : AbstractValidator<RenameAccountRequestDto>
{
    public RenameAccountRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Kontonamn får inte vara tomt.")
            .MaximumLength(50)
            .WithMessage("Kontonamn får max vara 50 tecken.");
    }
}