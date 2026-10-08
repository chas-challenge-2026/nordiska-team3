namespace NordiskaPortal.API.Models;

// What an account type pays compared with the policy rate. The rate of an account is the policy rate
// plus SpreadPercentagePoints, never below 0. For a given day, the row with the latest EffectiveFrom
// on or before that day applies.
public class AccountInterestPolicy
{
    public string AccountType { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public decimal SpreadPercentagePoints { get; set; }
}