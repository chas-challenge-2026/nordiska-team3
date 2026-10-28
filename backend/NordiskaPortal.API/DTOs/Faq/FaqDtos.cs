namespace NordiskaPortal.API.DTOs.Faq;

// Query string of GET /api/faq/search?q=...
public sealed record FaqSearchRequestDto(string? Q);

// One entry in GET /api/faq
public sealed record FaqEntryDto(Guid Id, string Question, string Answer, string Category);

// A question the customer may have meant, when the search has no clear answer
public sealed record FaqSuggestionDto(Guid Id, string Question);

// Response of GET /api/faq/search. Clients should branch on MatchFound, not on Score.
// Score is 0-1; when MatchFound is false, Message holds the customer service fallback
// and Suggestions holds up to three questions the customer may have meant (empty if none).
public sealed record FaqSearchResultDto(bool MatchFound, double Score, string? Question, string? Answer, string? Category, string? Message, IReadOnlyList<FaqSuggestionDto> Suggestions);