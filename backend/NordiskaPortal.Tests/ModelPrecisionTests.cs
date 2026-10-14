using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using Xunit;

namespace NordiskaPortal.Tests;

public class ModelPrecisionTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Every_decimal_property_has_an_explicit_precision()
    {
        using var context = CreateContext();

        var missing = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            .Where(p => p.GetPrecision() is null || p.GetScale() is null)
            .Select(p => $"{p.DeclaringType.DisplayName()}.{p.Name}")
            .ToList();

        missing.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(Transaction), nameof(Transaction.Amount))]
    [InlineData(typeof(LedgerEntry), nameof(LedgerEntry.Amount))]
    [InlineData(typeof(InterestPayout), nameof(InterestPayout.Amount))]
    public void Money_columns_are_numeric_18_2(Type entity, string property)
    {
        using var context = CreateContext();

        var prop = context.Model.FindEntityType(entity)!.FindProperty(property)!;

        prop.GetPrecision().Should().Be(18);
        prop.GetScale().Should().Be(2);
    }
}