using System.Globalization;
using FluentAssertions;
using NordiskaPortal.API.DTOs.Accounts;
using NordiskaPortal.API.Validators.Accounts;
using Xunit;

namespace NordiskaPortal.Tests.Validators;

public class AmountValidationTests
{
    private const string ValidAccountNumber = "NKM-12345";

    private static decimal Parse(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("1")]
    [InlineData("10.5")]
    [InlineData("10.50")]
    [InlineData("0.01")]
    [InlineData("1000000.99")]
    public void Amounts_with_at_most_two_decimals_are_accepted(string amount)
    {
        var value = Parse(amount);

        new TransactionRequestDtoValidator().Validate(new TransactionRequestDto(value)).IsValid.Should().BeTrue();
        new TransferRequestDtoValidator().Validate(new TransferRequestDto(ValidAccountNumber, value)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("10.005")]
    [InlineData("0.001")]
    [InlineData("1.999")]
    [InlineData("100.123")]
    public void Amounts_with_more_than_two_decimals_are_rejected(string amount)
    {
        var value = Parse(amount);

        var deposit = new TransactionRequestDtoValidator().Validate(new TransactionRequestDto(value));
        var transfer = new TransferRequestDtoValidator().Validate(new TransferRequestDto(ValidAccountNumber, value));

        deposit.IsValid.Should().BeFalse();
        deposit.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("decimaler"));
        transfer.IsValid.Should().BeFalse();
        transfer.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("decimaler"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.01")]
    public void Zero_and_negative_amounts_are_still_rejected(string amount)
    {
        var value = Parse(amount);

        new TransactionRequestDtoValidator().Validate(new TransactionRequestDto(value)).IsValid.Should().BeFalse();
        new TransferRequestDtoValidator().Validate(new TransferRequestDto(ValidAccountNumber, value)).IsValid.Should().BeFalse();
    }
}