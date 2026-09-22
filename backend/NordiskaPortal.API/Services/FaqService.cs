using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public class FaqService : IFaqService
{
    private readonly ApplicationDbContext _context;
    private const double MatchThreshold = 0.3; // Minsta acceptabla score för en godkänd träff

    public FaqService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FaqSearchResultDto> SearchFaqAsync(string userQuery)
    {
        // 1. The question is normalized
        var normalizedQuery = NormalizeText(userQuery);
        var queryTokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (queryTokens.Length == 0)
        {
            return FallbackToCustomerService();
        }

        // 2. All relevant FAQ entries are compared
        var allFaqs = await _context.FaqEntries.AsNoTracking().ToListAsync();

        if (!allFaqs.Any())
        {
            return FallbackToCustomerService();
        }

        // 3. Results get a match score
        var rankedResults = allFaqs.Select(faq =>
        {
            var normalizedQuestion = NormalizeText(faq.Question);
            var normalizedKeywords = NormalizeText(faq.Keywords);
            var normalizedCategory = NormalizeText(faq.Category);

            double score = CalculateScore(queryTokens, normalizedQuestion, normalizedKeywords, normalizedCategory);

            return new
            {
                Faq = faq,
                Score = score
            };
        })
        .OrderByDescending(r => r.Score)
        .ToList();

        // 4. The best match is returned
        var bestMatch = rankedResults.FirstOrDefault();

        if (bestMatch != null && bestMatch.Score >= MatchThreshold)
        {
            return new FaqSearchResultDto
            {
                MatchFound = true,
                Score = Math.Round(bestMatch.Score, 2),
                Question = bestMatch.Faq.Question,
                Answer = bestMatch.Faq.Answer,
                Category = bestMatch.Faq.Category,
                Message = "Svar hittades."
            };
        }

        // 5. A poor match falls back to customer service
        return FallbackToCustomerService();
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var lower = text.ToLowerInvariant();
        return Regex.Replace(lower, @"[^\w\s]", " ").Trim();
    }

    private static double CalculateScore(string[] queryTokens, string targetQuestion, string targetKeywords, string targetCategory)
    {
        double score = 0;
        var questionWords = targetQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keywordsList = targetKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in queryTokens)
        {
            if (keywordsList.Any(k => k.Contains(token) || token.Contains(k)))
            {
                score += 0.5;
            }
            if (questionWords.Any(q => q.Contains(token) || token.Contains(q)))
            {
                score += 0.3;
            }
            if (targetCategory.Contains(token))
            {
                score += 0.2;
            }
        }

        return score / queryTokens.Length;
    }

    private static FaqSearchResultDto FallbackToCustomerService()
    {
        return new FaqSearchResultDto
        {
            MatchFound = false,
            Score = 0.0,
            Message = "Vi hittade inget svar som matchade din fråga. Vänligen kontakta kundservice för personlig hjälp på support@nordiskaportal.se eller ring 08-123 456."
        };
    }
}