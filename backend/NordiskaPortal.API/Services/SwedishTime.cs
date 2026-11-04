namespace NordiskaPortal.API.Services;

// Interest days and tax years follow the Swedish calendar (Europe/Stockholm), not UTC.
// Timestamps are still stored in UTC; this class converts them to Swedish dates and back.
public static class SwedishTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    // Today's date in Sweden.
    public static DateOnly TodayIn(TimeProvider timeProvider) => DateOf(timeProvider.GetUtcNow().UtcDateTime);

    // The Swedish calendar day of a moment that is stored as UTC (a value without a kind counts as UTC).
    public static DateOnly DateOf(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    // The UTC moment when the given Swedish date begins, which is 00:00 in Stockholm.
    public static DateTime StartOfDayUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Zone);

    // The same moment with Sweden's offset from UTC (+01:00 in winter, +02:00 in summer).
    public static DateTimeOffset ToLocal(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone);
}