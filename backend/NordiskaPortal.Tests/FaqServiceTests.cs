using FluentAssertions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using Xunit;
using NordiskaPortal.API.Models;

namespace NordiskaPortal.Tests.Services;

public class FaqServiceTests
{
    private readonly Mock<IFaqRepository> _faqRepoMock = new();

    private FaqService CreateService()
    {
        _faqRepoMock.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(FaqSeedData.Entries);

        return new FaqService(_faqRepoMock.Object);
    }

    [Fact]
    public async Task SearchAsync_WhenAnEntryMatches_ReturnsTheEntryWithItsScore()
    {
        var result = await CreateService().SearchAsync("hur tar jag ut pengar");

        result.MatchFound.Should().BeTrue();
        result.Question.Should().Be("Hur tar jag ut pengar?");
        result.Answer.Should().Contain("Ta ut");
        result.Category.Should().Be("Uttag");
        result.Score.Should().BeApproximately(1.0, 0.001);
        result.Message.Should().BeNull();
        result.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenNothingMatches_ReturnsTheCustomerServiceFallback()
    {
        var result = await CreateService().SearchAsync("hur bokar jag en flygresa");

        result.MatchFound.Should().BeFalse();
        result.Score.Should().Be(0);
        result.Question.Should().BeNull();
        result.Answer.Should().BeNull();
        result.Category.Should().BeNull();
        result.Message.Should().Contain("kundtjänst").And.Contain("08-123 456 78");
        result.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WhenTwoEntriesTie_SuggestsBoth()
    {
        var result = await CreateService().SearchAsync("ränta skatt");

        result.MatchFound.Should().BeFalse();
        result.Message.Should().Contain("kundtjänst");
        result.Suggestions.Select(s => s.Question).Should().Equal("När betalas räntan ut?", "Var hittar jag mitt räntebesked eller årsbesked?");
    }

    [Fact]
    public async Task SearchAsync_WhenTheScoreIsTooLow_SuggestsTheClosestEntry()
    {
        var result = await CreateService().SearchAsync("ränta idag igen");

        result.MatchFound.Should().BeFalse();
        result.Suggestions.Select(s => s.Question).Should().Equal("När betalas räntan ut?");
    }

    [Fact]
    public async Task SearchAsync_SuggestsAtMostThreeEntries()
    {
        var entries = Enumerable.Range(1, 4)
            .Select(i => new FaqEntry { Question = $"Fråga {i}?", Answer = "Svar", Category = "Test", Keywords = "sparmål" })
            .ToList();
        _faqRepoMock.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(entries);

        var result = await new FaqService(_faqRepoMock.Object).SearchAsync("sparmål");

        result.MatchFound.Should().BeFalse();
        result.Suggestions.Select(s => s.Question).Should().Equal("Fråga 1?", "Fråga 2?", "Fråga 3?");
    }


    [Fact]
    public async Task SearchAsync_RoundsTheScoreToTwoDecimals()
    {
        var result = await CreateService().SearchAsync("när betalas räntan ut");

        result.MatchFound.Should().BeTrue();
        result.Score.Should().BeApproximately(0.83, 1e-9);
    }

    [Fact]
    public async Task SearchAsync_ComparesAgainstEveryEntryFromTheRepository()
    {
        var service = CreateService();

        await service.SearchAsync("hur tar jag ut pengar");

        _faqRepoMock.Verify(r => r.GetAllOrderedAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryEntryInRepositoryOrder()
    {
        var result = await CreateService().GetAllAsync();

        result.Should().HaveCount(FaqSeedData.Entries.Count);
        result.Select(e => e.Id).Should().Equal(FaqSeedData.Entries.Select(e => e.Id));
        result[0].Question.Should().Be("Hur öppnar jag ett nytt sparkonto?");
    }
}
