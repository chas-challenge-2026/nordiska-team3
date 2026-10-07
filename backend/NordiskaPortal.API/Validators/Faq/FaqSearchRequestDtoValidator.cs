using FluentValidation;
using NordiskaPortal.API.DTOs.Faq;

namespace NordiskaPortal.API.Validators.Faq;

public class FaqSearchRequestDtoValidator : AbstractValidator<FaqSearchRequestDto>
{
    public FaqSearchRequestDtoValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty()
            .WithMessage("Sökfrasen får inte vara tom.")
            .MaximumLength(200)
            .WithMessage("Sökfrasen får max vara 200 tecken lång.");
    }
}
