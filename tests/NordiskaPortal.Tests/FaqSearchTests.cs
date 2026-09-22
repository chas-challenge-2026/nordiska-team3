using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class FaqSearchTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.FaqEntries.AddRange(
            new FaqEntry
            {
                Id = Guid.NewGuid(),
                Question = "Hur gör jag en insättning?",
                Answer = "Du kan sätta in pengar via Swish eller banköverföring.",
                Keywords = "insättning sätta in pengar",
                Category = "Konton"
            },
            new FaqEntry
            {
                Id = Guid.NewGuid(),
                Question = "Hur ändrar jag min PIN-kod?",
                Answer = "Gå till inställningar för att byta PIN.",
                Keywords = "pinkod pin säkerhet",
                Category = "Säkerhet"
            }
        );
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task SearchFaq_NormalizesPunctuationAndCase_ReturnsMatch()
    {
        var context = CreateInMemoryDbContext();
        var service = new FaqService(context);

        
        var result = await service.SearchFaqAsync("HUR GÖR JAG EN INSÄTTNING???");

        Assert.True(result.MatchFound);
        Assert.Equal("Hur gör jag en insättning?", result.Question);
    }

    [Fact]
    public async Task SearchFaq_UnrelatedQuery_FallsBackToCustomerService()
    {
        var context = CreateInMemoryDbContext();
        var service = new FaqService(context);

        
        var result = await service.SearchFaqAsync("Hur bokar jag en flygresa?");

        Assert.False(result.MatchFound);
        Assert.Contains("kundservice", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchFaq_ShortIrrelevantQuery_DoesNotFalseMatchAndFallsBack()
    {
        var context = CreateInMemoryDbContext();
        var service = new FaqService(context);

        
        var result = await service.SearchFaqAsync("in a");

        Assert.False(result.MatchFound);
        Assert.Equal(0, result.Score);
    }
}