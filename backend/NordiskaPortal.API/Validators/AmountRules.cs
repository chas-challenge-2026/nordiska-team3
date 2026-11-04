using FluentValidation;

namespace NordiskaPortal.API.Validators;

public static class AmountRules
{
    // The highest amount for one deposit, withdrawal or transfer. Without a limit, a huge amount could make
    // the balance of an account too large to read.
    public const decimal MaxAmount = 10_000_000m;

    // Money has at most two decimals. 10.005 would otherwise be rounded or rejected by the database.
    public static IRuleBuilderOptions<T, decimal> HasAtMostTwoDecimals<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Beloppet f\u00e5r ha h\u00f6gst tv\u00e5 decimaler.");

    public static IRuleBuilderOptions<T, decimal> IsWithinMaxAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.LessThanOrEqualTo(MaxAmount)
            .WithMessage("Beloppet f\u00e5r vara h\u00f6gst 10 000 000 kr.");
}