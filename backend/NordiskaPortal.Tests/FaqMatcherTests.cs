using FluentAssertions;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class FaqMatcherTests
{
    private static readonly IReadOnlyList<FaqEntry> Entries = FaqSeedData.Entries;

    [Theory]
    [InlineData("hur tar jag ut pengar", "Hur tar jag ut pengar?")]
    [InlineData("ta ut pengar", "Hur tar jag ut pengar?")]
    [InlineData("hur kan jag ta ut mina pengar", "Hur tar jag ut pengar?")]
    [InlineData("hur sätter jag in pengar", "Hur sätter jag in pengar?")]
    [InlineData("hur avslutar jag mitt konto", "Hur avslutar jag mitt sparkonto?")]
    [InlineData("jag vill stänga mitt konto", "Hur avslutar jag mitt sparkonto?")]
    [InlineData("stänger kontot", "Hur avslutar jag mitt sparkonto?")]
    [InlineData("hur öppnar jag ett konto", "Hur öppnar jag ett nytt sparkonto?")]
    [InlineData("när betalas räntan ut", "När betalas räntan ut?")]
    [InlineData("räntorna", "När betalas räntan ut?")]
    [InlineData("insättningar", "Hur sätter jag in pengar?")]
    [InlineData("hur flyttar jag pengar mellan mina konton", "Hur flyttar jag pengar mellan mina konton?")]
    [InlineData("hur ändrar jag mitt namn", "Hur ändrar jag mina kontaktuppgifter?")]

    public void FindBestMatch_ShouldReturnExpectedEntry(string query, string expectedQuestion)
    {
        var result = FaqMatcher.FindBestMatch(query, Entries);

        result.Should().NotBeNull();
        result!.Entry.Question.Should().Be(expectedQuestion);
    }

    [Fact]
    public void FindBestMatch_WhenQueryIsAnEntryQuestion_ReturnsThatEntry()
    {
        foreach (var entry in Entries)
        {
            var result = FaqMatcher.FindBestMatch(entry.Question, Entries);

            result.Should().NotBeNull($"'{entry.Question}' should match itself");
            result!.Entry.Id.Should().Be(entry.Id, $"'{entry.Question}' should match itself");
        }
    }

    [Fact]
    public void FindBestMatch_WhenWholeQueryMatchesAPhrase_ScoresOne()
    {
        var result = FaqMatcher.FindBestMatch("hur tar jag ut pengar", Entries);

        result.Should().NotBeNull();
        result!.Score.Should().BeApproximately(1.0, 0.001);
    }

    [Theory]
    [InlineData("hur bokar jag en flygresa")]
    [InlineData("in a")]
    [InlineData("hur gör jag det")]
    [InlineData("pengar")]
    [InlineData("")]
    [InlineData("   ")]
    public void FindBestMatch_WhenNoEntryIsRelevant_ReturnsNull(string query)
    {
        FaqMatcher.FindBestMatch(query, Entries).Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_WhenQueryIsNull_ReturnsNull()
    {
        FaqMatcher.FindBestMatch(null, Entries).Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_WhenTwoEntriesTie_ReturnsNull()
    {
        FaqMatcher.FindBestMatch("ränta skatt", Entries).Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_WhenEnoughOfTheQueryMatches_ReturnsTheEntry()
    {
        var result = FaqMatcher.FindBestMatch("ränta idag", Entries);

        result.Should().NotBeNull();
        result!.Entry.Question.Should().Be("När betalas räntan ut?");
    }

    [Fact]
    public void FindBestMatch_WhenTooLittleOfTheQueryMatches_ReturnsNull()
    {
        FaqMatcher.FindBestMatch("ränta idag igen", Entries).Should().BeNull();
    }

    [Fact]
    public void Tokenize_IgnoresCaseAndPunctuation()
    {
        FaqMatcher.Tokenize("HUR TAR JAG UT PENGAR???").Should().Equal("tar", "ut", "peng");
    }

    [Fact]
    public void Tokenize_KeepsTheDirectionWords()
    {
        FaqMatcher.Tokenize("sätta in och ta ut").Should().Equal("sätt", "in", "ta", "ut");
    }

    [Theory]
    [InlineData("öppna", "öppnar", "öppnade")]
    [InlineData("ränta", "räntan", "räntor")]
    [InlineData("avsluta", "avslutar", "avslutade")]
    [InlineData("konto", "konton", "kontot")]
    [InlineData("insättning", "insättningar", "insättningen")]
    [InlineData("uttag", "uttaget", "uttagen")]
    public void Stem_MapsInflectionsToTheSameStem(string first, string second, string third)
    {
        FaqMatcher.Stem(second).Should().Be(FaqMatcher.Stem(first));
        FaqMatcher.Stem(third).Should().Be(FaqMatcher.Stem(first));
    }

    [Theory]
    [InlineData("ta")]
    [InlineData("tar")]
    [InlineData("in")]
    [InlineData("ut")]
    public void Stem_LeavesShortWordsAlone(string word)
    {
        FaqMatcher.Stem(word).Should().Be(word);
    }

    [Fact]
    public void Stem_KeepsContactAndAccountApart()
    {
        FaqMatcher.Stem("kontakt").Should().NotBe(FaqMatcher.Stem("konto"));
    }
}
