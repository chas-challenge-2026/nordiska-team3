namespace NordiskaPortal.API.DTOs.Faq
{
    // Query string of GET /api/faq/search?q=...
    public sealed record FaqSearchRequestDto(string? Q);

    // One entry in GET /api/faq
    public sealed record FaqEntryDto(Guid Id, string Question, string Answer, string Category);

    // Response of GET /api/faq/search. Clients should branch on MatchFound, not on Score.
    // Score is 0-1; when MatchFound is false, Message holds the customer service fallback.
    public sealed record FaqSearchResultDto(bool MatchFound, double Score, string? Question, string? Answer, string? Category, string? Message);

}
