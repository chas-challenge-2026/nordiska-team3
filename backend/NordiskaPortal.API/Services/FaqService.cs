using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services.Interfaces;
using System.Text.RegularExpressions;

namespace NordiskaPortal.API.Services;

public class FaqService : IFaqService
{
    private const string NoMatchMessage = $"Vi hittade inget svar på din fråga. Kontakta kundtjänst på {CustomerServiceContact.Phone}, {CustomerServiceContact.OpeningHours}, eller mejla {CustomerServiceContact.Email}.";
    private const int MaxSuggestions = 3;

    private static readonly Regex EmailAddress = new(@"\S+@\S+");
    private static readonly Regex Digit = new(@"\d");

    private readonly IFaqRepository _faqRepository;
    private readonly ILogger<FaqService> _logger;

    public FaqService(IFaqRepository faqRepository, ILogger<FaqService> logger)
    {
        _faqRepository = faqRepository;
        _logger = logger;
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

            _logger.LogInformation("FAQ search found no answer for {Query}, {SuggestionCount} suggestions", MaskForLog(query), suggestions.Count);

            return new FaqSearchResultDto(false, 0, null, null, null, NoMatchMessage, suggestions);
        }

        return new FaqSearchResultDto(true, Math.Round(match.Score, 2), match.Entry.Question, match.Entry.Answer, match.Entry.Category, null, []);
    }

    // Digits and e-mail addresses are masked, so personal numbers, account numbers and phone numbers never reach the log
    public static string MaskForLog(string query) => Digit.Replace(EmailAddress.Replace(query, "[e-post]"), "#");

    public async Task<IReadOnlyList<FaqEntryDto>> GetAllAsync()
    {
        var entries = await _faqRepository.GetAllOrderedAsync();

        return entries.Select(e => new FaqEntryDto(e.Id, e.Question, e.Answer, e.Category)).ToList();
    }

}
