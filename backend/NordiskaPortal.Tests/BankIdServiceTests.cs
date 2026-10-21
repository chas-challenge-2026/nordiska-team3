using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.BankId;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class BankIdServiceTests
{
    private static readonly AuthResult Tokens = new(
        new LoginResponseDto("access", new UserDto(Guid.NewGuid(), "Test Testsson", "test@example.com", "19900101-XXXX")),
        "refresh-token",
        DateTime.UtcNow.AddMinutes(30));

    private readonly Mock<IBankIdApiClient> _api = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly BankIdOrderStore _store = new();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));

    public BankIdServiceTests()
    {
        _api.Setup(a => a.AuthAsync("1.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankIdAuthResponse("ref-1", "auto-1", "qr-token", "qr-secret"));
        SetCollect("pending", "outstandingTransaction", null);
        _authService.Setup(a => a.LoginWithPersonalNumberAsync("19900101-1234")).ReturnsAsync(Tokens);
    }

    [Fact]
    public async Task Start_GivesOurOwnOrderIdAndTheAutoStartToken()
    {
        var result = await CreateService().StartAsync("1.2.3.4", CancellationToken.None);

        result.AutoStartToken.Should().Be("auto-1");
        result.OrderId.Should().NotBe("ref-1");
        result.OrderId.Should().HaveLength(64);
    }

    [Fact]
    public async Task Status_WhilePending_GivesAQrCodeThatChangesEverySecond()
    {
        var orderId = await StartAsync();

        var first = await CreateService().GetStatusAsync(orderId, CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(1));
        var second = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        first.Status.Should().Be("pending");
        first.QrData.Should().Be(BankIdQrCode.Create("qr-token", "qr-secret", 0));
        second.QrData.Should().Be(BankIdQrCode.Create("qr-token", "qr-secret", 1));
    }

    [Fact]
    public async Task Status_AsksBankIdAtMostEveryTwoSeconds()
    {
        var orderId = await StartAsync();

        await CreateService().GetStatusAsync(orderId, CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await CreateService().GetStatusAsync(orderId, CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        _api.Verify(a => a.CollectAsync("ref-1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Status_WhenComplete_LogsInTheUserWithThePersonalNumberInOurFormat()
    {
        var orderId = await StartAsync();
        SetCollect("complete", null, "199001011234");

        var result = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        result.Status.Should().Be("complete");
        result.AuthResult.Should().BeSameAs(Tokens);
        _authService.Verify(a => a.LoginWithPersonalNumberAsync("19900101-1234"), Times.Once);
    }

    [Fact]
    public async Task Status_WhenCompleteForSomeoneWithoutAnAccount_Fails()
    {
        var orderId = await StartAsync();
        SetCollect("complete", null, "198001010000");
        _authService.Setup(a => a.LoginWithPersonalNumberAsync("19800101-0000")).ReturnsAsync((AuthResult?)null);

        var result = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        result.Status.Should().Be("failed");
        result.HintCode.Should().Be("userNotFound");
        result.AuthResult.Should().BeNull();
    }

    [Fact]
    public async Task Status_AfterTheOrderIsComplete_CannotBeUsedToLogInAgain()
    {
        var orderId = await StartAsync();
        SetCollect("complete", null, "199001011234");
        await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        var again = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        again.Status.Should().Be("failed");
        again.AuthResult.Should().BeNull();
        _authService.Verify(a => a.LoginWithPersonalNumberAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Status_WhenBankIdSaysFailed_PassesOnTheHintCode()
    {
        var orderId = await StartAsync();
        _api.Setup(a => a.CollectAsync("ref-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankIdCollectResponse("ref-1", "failed", "userCancel", null));

        var result = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        result.Status.Should().Be("failed");
        result.HintCode.Should().Be("userCancel");
    }

    [Fact]
    public async Task Status_WhenBankIdCannotBeReached_Fails()
    {
        var orderId = await StartAsync();
        _api.Setup(a => a.CollectAsync("ref-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BankIdApiException("unreachable", "down"));

        var result = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        result.Status.Should().Be("failed");
        result.HintCode.Should().Be("bankIdError");
    }

    [Fact]
    public async Task Status_ForAnOrderWeDoNotKnow_Fails()
    {
        var result = await CreateService().GetStatusAsync("not-an-order", CancellationToken.None);

        result.Status.Should().Be("failed");
        result.HintCode.Should().Be("unknownOrder");
    }

    [Fact]
    public async Task Cancel_TellsBankIdAndEndsTheOrder()
    {
        var orderId = await StartAsync();

        await CreateService().CancelAsync(orderId, CancellationToken.None);
        var result = await CreateService().GetStatusAsync(orderId, CancellationToken.None);

        _api.Verify(a => a.CancelAsync("ref-1", It.IsAny<CancellationToken>()), Times.Once);
        result.Status.Should().Be("failed");
    }

    [Fact]
    public async Task MockApprove_DoesNothingWhenBankIdIsReal()
    {
        var orderId = await StartAsync();

        CreateService().MockApprove(orderId, "19900101-1234").Should().BeFalse();
        CreateService().IsMock.Should().BeFalse();
    }

    [Fact]
    public async Task TheMock_StaysPendingUntilApproved_ThenLogsInTheApprovedPersonalNumber()
    {
        var mock = new MockBankIdApiClient();
        var service = new BankIdService(mock, _store, _authService.Object, _clock, NullLogger<BankIdService>.Instance);
        var orderId = (await service.StartAsync("1.2.3.4", CancellationToken.None)).OrderId;

        var before = await service.GetStatusAsync(orderId, CancellationToken.None);
        service.MockApprove(orderId, "19900101-1234").Should().BeTrue();
        _clock.Advance(TimeSpan.FromSeconds(2));
        var after = await service.GetStatusAsync(orderId, CancellationToken.None);

        service.IsMock.Should().BeTrue();
        before.Status.Should().Be("pending");
        after.Status.Should().Be("complete");
        _authService.Verify(a => a.LoginWithPersonalNumberAsync("19900101-1234"), Times.Once);
    }

    private async Task<string> StartAsync() =>
        (await CreateService().StartAsync("1.2.3.4", CancellationToken.None)).OrderId;

    private void SetCollect(string status, string? hintCode, string? personalNumber)
    {
        var completion = personalNumber is null
            ? null
            : new BankIdCompletionData(new BankIdUser(personalNumber, "Test Testsson", "Test", "Testsson"));

        _api.Setup(a => a.CollectAsync("ref-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankIdCollectResponse("ref-1", status, hintCode, completion));
    }

    private BankIdService CreateService() =>
        new(_api.Object, _store, _authService.Object, _clock, NullLogger<BankIdService>.Instance);
}