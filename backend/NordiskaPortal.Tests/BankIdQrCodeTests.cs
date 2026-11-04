using FluentAssertions;
using NordiskaPortal.API.Services.BankId;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class BankIdQrCodeTests
{
    private const string Token = "67df3917-fa0d-44e5-b327-edcc928297f8";
    private const string Secret = "d28db9a7-4cde-429e-a983-359be676944c";

    [Theory]
    [InlineData(0, "bankid.67df3917-fa0d-44e5-b327-edcc928297f8.0.dc69358e712458a66a7525beef148ae8526b1c71610eff2c16cdffb4cdac9bf8")]
    [InlineData(1, "bankid.67df3917-fa0d-44e5-b327-edcc928297f8.1.949d559bf23403952a94d103e67743126381eda00f0b3cbddbf7c96b1adcbce2")]
    [InlineData(2, "bankid.67df3917-fa0d-44e5-b327-edcc928297f8.2.a9e5ec59cb4eee4ef4117150abc58fad7a85439a6a96ccbecc3668b41795b3f3")]
    public void Create_GivesTheSameTextAsBankIdForTheSameInput(int seconds, string expected)
    {
        BankIdQrCode.Create(Token, Secret, seconds).Should().Be(expected);
    }

    [Fact]
    public void Create_NeverIncludesTheSecret()
    {
        BankIdQrCode.Create(Token, Secret, 5).Should().NotContain(Secret);
    }
}