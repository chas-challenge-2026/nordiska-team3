using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class PersonalNumberProtectorTests
{
    private static readonly string KeyA = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
    private static readonly string KeyB = Convert.ToBase64String(Enumerable.Range(101, 32).Select(i => (byte)i).ToArray());

    private static PersonalNumberProtector CreateProtector(string key) =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PersonalNumberProtection:Key"] = key })
                .Build(),
            NullLogger<PersonalNumberProtector>.Instance);

    [Fact]
    public void IsKeyAvailable_WhenTheKeyIsConfigured_IsTrue()
    {
        CreateProtector(KeyA).IsKeyAvailable.Should().BeTrue();
    }

    [Fact]
    public void Protect_ThenUnprotect_ReturnsTheTrimmedPersonalNumber()
    {
        var protector = CreateProtector(KeyA);

        var stored = protector.Protect(" 19900101-1234 ");

        protector.Unprotect(stored).Should().Be("19900101-1234");
    }

    [Fact]
    public void Protect_UsesThePrefixHidesThePersonalNumberAndGivesADifferentResultEachTime()
    {
        var protector = CreateProtector(KeyA);

        var first = protector.Protect("19900101-1234");
        var second = protector.Protect("19900101-1234");

        first.Should().StartWith(PersonalNumberProtector.Prefix);
        first.Should().NotContain("19900101");
        second.Should().NotBe(first);
    }

    [Fact]
    public void IsProtected_IsTrueOnlyForValuesWithThePrefix()
    {
        var protector = CreateProtector(KeyA);

        protector.IsProtected(protector.Protect("19900101-1234")).Should().BeTrue();
        protector.IsProtected("19900101-1234").Should().BeFalse();
    }

    [Fact]
    public void Unprotect_OfAValueThatIsNotEncryptedYet_ReturnsItUnchanged()
    {
        CreateProtector(KeyA).Unprotect("19900101-1234").Should().Be("19900101-1234");
    }

    [Fact]
    public void Unprotect_OfATamperedValue_Throws()
    {
        var protector = CreateProtector(KeyA);
        var payload = Convert.FromBase64String(protector.Protect("19900101-1234")[PersonalNumberProtector.Prefix.Length..]);
        payload[^1] ^= 0xFF;
        var tampered = PersonalNumberProtector.Prefix + Convert.ToBase64String(payload);

        var act = () => protector.Unprotect(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_WithAnotherKey_Throws()
    {
        var stored = CreateProtector(KeyA).Protect("19900101-1234");

        var act = () => CreateProtector(KeyB).Unprotect(stored);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ComputeHash_IsStableIgnoresSurroundingSpacesAndDiffersPerPersonalNumber()
    {
        var protector = CreateProtector(KeyA);

        var hash = protector.ComputeHash("19900101-1234");

        protector.ComputeHash(" 19900101-1234 ").Should().Be(hash);
        protector.ComputeHash("19850615-5678").Should().NotBe(hash);
        Convert.FromBase64String(hash).Should().HaveCount(32);
    }

    [Fact]
    public void ComputeHash_DependsOnTheKey()
    {
        CreateProtector(KeyA).ComputeHash("19900101-1234")
            .Should().NotBe(CreateProtector(KeyB).ComputeHash("19900101-1234"));
    }

    [Fact]
    public void Constructor_WithAKeyShorterThan32Bytes_Throws()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);

        var act = () => CreateProtector(shortKey);

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 32 bytes*");
    }

    [Fact]
    public void GenerateNewKey_WhenAKeyAlreadyExists_Throws()
    {
        var act = () => CreateProtector(KeyA).GenerateNewKey();

        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }
}