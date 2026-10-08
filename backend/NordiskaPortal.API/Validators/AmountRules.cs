using FluentValidation;

namespace NordiskaPortal.API.Validators;

public static class AmountRules
{
    // Money has at most two decimals. 10.005 would otherwise be rounded or rejected by the database.
    public static IRuleBuilderOptions<T, decimal> HasAtMostTwoDecimals<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Beloppet f\u00e5r ha h\u00f6gst tv\u00e5 decimaler.");
}