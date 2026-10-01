using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NordiskaPortal.API.Services
{
    public class FaqService : IFaqService
    {
        private readonly ApplicationDbContext _context;

         
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "hur", "vad", "var", "när", "varför", "vem", "vilken", "vilket", "vilka",
            "jag", "mig", "min", "mitt", "mina", "du", "dig", "din", "ditt", "dina",
            "han", "hon", "den", "det", "vi", "oss", "vår", "vårt", "våra", "ni", "er", "ert", "era", "de", "dem",
            "är", "var", "har", "hade", "gör", "gjorde", "kan", "kunde", "ska", "skulle", "vill", "ville",
            "en", "ett", "i", "på", "till", "från", "av", "med", "för", "om", "att", "och", "eller", "men", "som", "in", "a"
        };

        public FaqService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FaqEntry>> GetDefaultFaqsAsync(int count = 6)
        {
            return await _context.FaqEntries
                .AsNoTracking()
                .Take(count)
                .ToListAsync();
        }

        public async Task<FaqSearchResultDto> SearchFaqAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new FaqSearchResultDto
                {
                    MatchFound = false,
                    Score = 0,
                    Message = "Ingen sökfras angavs. Vänligen kontakta kundservice för hjälp."
                };
            }

            
            var cleanQuery = Regex.Replace(query, @"[^\w\s]", " ").ToLowerInvariant();

            
            var tokens = cleanQuery
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2 && !StopWords.Contains(t))
                .ToHashSet();

            
            if (tokens.Count == 0)
            {
                return new FaqSearchResultDto
                {
                    MatchFound = false,
                    Score = 0,
                    Message = "Inga matchande frågor hittades. Vänligen kontakta kundservice för mer hjälp."
                };
            }

            var faqs = await _context.FaqEntries.AsNoTracking().ToListAsync();

            var scoredList = faqs
                .Select(faq => new
                {
                    Faq = faq,
                    Score = CalculateScore(faq, tokens)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            var bestMatch = scoredList.FirstOrDefault();

            if (bestMatch == null)
            {
                return new FaqSearchResultDto
                {
                    MatchFound = false,
                    Score = 0,
                    Message = "Inga matchande frågor hittades. Vänligen kontakta kundservice för mer hjälp."
                };
            }

            return new FaqSearchResultDto
            {
                MatchFound = true,
                Question = bestMatch.Faq.Question,
                Answer = bestMatch.Faq.Answer,
                Category = bestMatch.Faq.Category,
                Score = bestMatch.Score
            };
        }

        private static int CalculateScore(FaqEntry faq, HashSet<string> tokens)
        {
            int score = 0;

            var cleanQuestion = Regex.Replace(faq.Question ?? "", @"[^\w\s]", " ").ToLowerInvariant();
            var questionWords = cleanQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var cleanKeywords = Regex.Replace(faq.Keywords ?? "", @"[^\w\s]", " ").ToLowerInvariant();
            var keywordWords = cleanKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (questionWords.Contains(token)) score += 3;
                if (keywordWords.Contains(token)) score += 2;
            }

            return score;
        }
    }
}