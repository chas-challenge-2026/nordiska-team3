using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class FaqService : IFaqService
{
    private const string NoMatchMessage = "Vi hittade inget svar på din fråga. Kontakta kundtjänst på 08-123 456 78, vardagar 8-18, eller mejla hej@nordiska.se.";
    private const int MaxSuggestions = 3;
    private readonly IFaqRepository _faqRepository;

    public FaqService(IFaqRepository faqRepository)
    {
        _faqRepository = faqRepository;
    }

    public async Task<FaqSearchResultDto> SearchAsync(string query)
    {
        var entries = await _faqRepository.GetAllOrderedAsync();
        var match = FaqMatcher.FindBestMatch(query, entries);

        if (match is null)
        {
            var suggestions = FaqMatcher.Rank(query, entries)
                .Take(MaxSuggestions)
                .Select(m => new FaqSuggestionDto(m.Entry.Id, m.Entry.Question))
                .ToList();

            return new FaqSearchResultDto(false, 0, null, null, null, NoMatchMessage, suggestions);
        }

        return new FaqSearchResultDto(true, Math.Round(match.Score, 2), match.Entry.Question, match.Entry.Answer, match.Entry.Category, null, []);
    }

    public async Task<IReadOnlyList<FaqEntryDto>> GetAllAsync()
    {
        var entries = await _faqRepository.GetAllOrderedAsync();

        return entries.Select(e => new FaqEntryDto(e.Id, e.Question, e.Answer, e.Category)).ToList();
    }

}
