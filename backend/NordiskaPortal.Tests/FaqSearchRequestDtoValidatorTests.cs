using FluentAssertions;
using Moq;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Validators.Faq;
using Xunit;

namespace NordiskaPortal.Tests.Validators;

public class FaqSearchRequestDtoValidatorTests
{
    private readonly FaqSearchRequestDtoValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTheQueryIsEmpty_ReturnsAnError(string? query)
    {
        var result = _validator.Validate(new FaqSearchRequestDto(query));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Q" && e.ErrorMessage.Contains("tom"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void Validate_WhenTheQueryIsAtMost200Characters_IsValid(int length)
    {
        var result = _validator.Validate(new FaqSearchRequestDto(new string('a', length)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenTheQueryIsLongerThan200Characters_ReturnsAnError()
    {
        var result = _validator.Validate(new FaqSearchRequestDto(new string('a', 201)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Q" && e.ErrorMessage.Contains("200"));
    }
}