using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NordiskaPortal.API.Controllers;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.BankId;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Controllers;

public class BankIdControllerTests
{
    private readonly Mock<IBankIdService> _bankId = new();

    [Fact]
    public async Task Status_WhenComplete_SetsTheRefreshCookieAsASessionCookie()
    {
        _bankId.Setup(s => s.GetStatusAsync("order-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BankIdStatusResult.Complete(new AuthResult(
                new LoginResponseDto("access", new UserDto(Guid.NewGuid(), "Test Testsson", "test@example.com", "19900101-XXXX")),
                "refresh-token",
                DateTime.UtcNow.AddMinutes(30))));
        var controller = CreateController();

        var result = await controller.Status("order-1", CancellationToken.None);

        var body = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<BankIdStatusResponseDto>().Subject;
        body.Status.Should().Be("complete");
        body.Login!.AccessToken.Should().Be("access");

        var cookie = controller.Response.Headers.SetCookie.ToString();
        cookie.Should().Contain("refreshToken=refresh-token");
        cookie.Should().NotContainEquivalentOf("expires=");
        cookie.Should().Contain("path=/api/auth");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=strict");
        cookie.Should().ContainEquivalentOf("secure");
    }

    [Fact]
    public async Task Status_WhilePending_GivesTheQrTextAndNoCookie()
    {
        _bankId.Setup(s => s.GetStatusAsync("order-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BankIdStatusResult.Pending("outstandingTransaction", "bankid.qr"));
        var controller = CreateController();

        var result = await controller.Status("order-1", CancellationToken.None);

        var body = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<BankIdStatusResponseDto>().Subject;
        body.Status.Should().Be("pending");
        body.QrData.Should().Be("bankid.qr");
        body.Login.Should().BeNull();
        controller.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WhenBankIdCannotBeReached_Returns502()
    {
        _bankId.Setup(s => s.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BankIdApiException("unreachable", "down"));
        var controller = CreateController();

        var result = await controller.Start(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public void MockApprove_WhenBankIdIsReal_Returns404()
    {
        _bankId.SetupGet(s => s.IsMock).Returns(false);
        var controller = CreateController();

        var result = controller.MockApprove(new BankIdMockApproveRequestDto("order-1", "19900101-1234"));

        result.Should().BeOfType<NotFoundResult>();
        _bankId.Verify(s => s.MockApprove(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private BankIdController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";

        return new BankIdController(_bankId.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}