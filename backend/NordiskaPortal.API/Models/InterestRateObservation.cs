namespace NordiskaPortal.API.Models;

// One published value of an interest rate series from Riksbanken, for one day.
// The key is (SeriesId, Date). Value is percent: 1.75 means 1.75 %.
public class InterestRateObservation
{
    public string SeriesId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Value { get; set; }
    public DateTime FetchedAt { get; set; }
}