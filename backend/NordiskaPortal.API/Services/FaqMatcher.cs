using System.Text.RegularExpressions;
using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services;

public sealed record FaqMatch(FaqEntry Entry, double Score);

public static class FaqMatcher
{
    public const double MinScore = 0.35;

    private const int PhraseWeight = 4;
    private const int KeywordWeight = 3;
    private const int QuestionWeight = 2;
    private const int MinStemLength = 3;

    private static readonly Regex NonAlphanumeric = new(@"[^\p{L}\p{N}]+");

    private static readonly HashSet<string> StopWords = new()
    {
        "hur", "vad", "var", "vart", "när", "varför", "vem", "vilken", "vilket", "vilka",
        "jag", "mig", "min", "mitt", "mina", "du", "dig", "din", "ditt", "dina", "han", "hon",
        "den", "det", "vi", "oss", "vår", "vårt", "våra", "ni", "er", "ert", "era", "de", "dem", "man",
        "är", "har", "hade", "ha", "kan", "kunde", "ska", "skulle", "vill", "ville", "måste",
        "får", "få", "fick", "gör", "göra", "gjorde", "gjort", "finns", "hitta", "hittar",
        "en", "ett", "i", "på", "till", "från", "av", "med", "för", "om", "att", "och", "eller",
        "men", "som", "så", "då", "där", "här", "inte", "ju", "bara", "också", "mycket", "lite",
        "någon", "något", "några", "alla"
    };

    private static readonly string[] Suffixes = new[]
    {
        "heterna", "arnas", "ernas", "ornas", "heter", "arna", "erna", "orna", "ande", "ades", "aste",
        "ade", "het", "ens", "ets", "ar", "er", "or", "en", "et", "na", "as", "es", "at", "ad",
        "a", "e", "n"
    }.OrderByDescending(suffix => suffix.Length).ToArray();

    public static FaqMatch? FindBestMatch(string? query, IEnumerable<FaqEntry> entries)
    {
        var queryTokens = Tokenize(query).Distinct().ToList();
        if (queryTokens.Count == 0) return null;

        FaqMatch? best = null;
        var tied = false;

        foreach (var entry in entries)
        {
            var score = Score(entry, queryTokens);
            if (score <= 0) continue;

            if (best is null || score > best.Score)
            {
                best = new FaqMatch(entry, score);
                tied = false;
            }
            else if (Math.Abs(score - best.Score) < 1e-9)
            {
                tied = true;
            }
        }

        return best is { Score: >= MinScore } && !tied ? best : null;
    }

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        return NonAlphanumeric.Split(text.ToLowerInvariant())
            .Where(word => word.Length >= 2 && !StopWords.Contains(word))
            .Select(Stem)
            .ToList();
    }

    public static string Stem(string word)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            var suffix = Suffixes.FirstOrDefault(s => word.Length - s.Length >= MinStemLength && word.EndsWith(s, StringComparison.Ordinal));

            if (suffix is null) break;

            word = word[..^suffix.Length];
        }

        return word.Length >= 5 && word.EndsWith("ot", StringComparison.Ordinal) ? word[..^1] : word;
    }

    private static double Score(FaqEntry entry, IReadOnlyList<string> queryTokens)
    {
        var weights = new int[queryTokens.Count];

        void Credit(string token, int weight)
        {
            for (var i = 0; i < queryTokens.Count; i++)
            {
                if (queryTokens[i] == token && weights[i] < weight) weights[i] = weight;
            }
        }

        foreach (var term in entry.Keywords.Split(',', ';'))
        {
            var words = Tokenize(term);

            if (words.Count == 1)
            {
                Credit(words[0], KeywordWeight);
            }
            else if (words.Count > 1 && words.All(word => queryTokens.Contains(word)))
            {
                foreach (var word in words) Credit(word, PhraseWeight);
            }
        }

        foreach (var word in Tokenize(entry.Question)) Credit(word, QuestionWeight);

        if (!weights.Any(weight => weight >= KeywordWeight)) return 0;

        return weights.Sum() / (double)(PhraseWeight * queryTokens.Count);
    }
}
