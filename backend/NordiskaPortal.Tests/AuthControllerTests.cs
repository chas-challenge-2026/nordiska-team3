using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NordiskaPortal.API.Controllers;
using NordiskaPortal.API.DTOs.Auth;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Controllers;

// Browsers compare a cookie's path with the URL path case-sensitively. The frontend calls the lowercase
// /api/auth/... URLs, so the refresh cookie must be set (and cleared) for exactly that path.
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();

    [Fact]
    public async Task LoginPin_SetsTheRefreshCookieForTheAuthRoutes()
    {
        _authServiceMock
            .Setup(s => s.LoginWithPinAsync(It.IsAny<LoginPinRequestDto>()))
            .ReturnsAsync(LoginResult.Success(new AuthResult(
                new LoginResponseDto("access", new UserDto(Guid.NewGuid(), "Test Testsson", "test@example.com", "19900101-1234")),
                "refresh-token",
                DateTime.UtcNow.AddDays(7))));
        var controller = CreateController();

        await controller.LoginPin(new LoginPinRequestDto("19900101-1234", "1234"));

        var cookie = SetCookieHeader(controller);
        cookie.Should().NotContainEquivalentOf("expires=");
        cookie.Should().Contain("path=/api/auth");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=strict");
        cookie.Should().ContainEquivalentOf("secure");
    }

    [Fact]
    public async Task LoginPin_SetsTheCookiePathToTheControllerRoute()
    {
        _authServiceMock
            .Setup(s => s.LoginWithPinAsync(It.IsAny<LoginPinRequestDto>()))
            .ReturnsAsync(LoginResult.Success(new AuthResult(
                new LoginResponseDto("access", new UserDto(Guid.NewGuid(), "Test Testsson", "test@example.com", "19900101-1234")),
                "refresh-token",
                DateTime.UtcNow.AddDays(7))));
        var controller = CreateController();
        var routeTemplate = typeof(AuthController).GetCustomAttribute<RouteAttribute>()!.Template;

        await controller.LoginPin(new LoginPinRequestDto("19900101-1234", "1234"));

        SetCookieHeader(controller).Should().Contain($"path=/{routeTemplate}");
    }

    [Fact]
    public async Task Refresh_WhenTheTokenIsInvalid_ClearsTheCookieOnTheSamePath()
    {
        _authServiceMock.Setup(s => s.RefreshAsync("old-token")).ReturnsAsync((AuthResult?)null);
        var controller = CreateController();
        controller.Request.Headers.Cookie = "refreshToken=old-token";

        var result = await controller.Refresh();

        result.Should().BeOfType<UnauthorizedObjectResult>();
        SetCookieHeader(controller).Should().Contain("path=/api/auth");
    }

    [Fact]
    public async Task Logout_ClearsTheCookieOnTheSamePath()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(userId);

        var result = await controller.Logout();

        result.Should().BeOfType<NoContentResult>();
        _authServiceMock.Verify(s => s.LogoutAsync(userId, null), Times.Once);
        SetCookieHeader(controller).Should().Contain("path=/api/auth");
    }

    [Fact]
    public async Task Logout_PassesTheRefreshCookieOnSoOnlyThatDeviceIsLoggedOut()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(userId);
        controller.Request.Headers.Cookie = "refreshToken=device-token";

        await controller.Logout();

        _authServiceMock.Verify(s => s.LogoutAsync(userId, "device-token"), Times.Once);
    }

    [Fact]
    public async Task LoginPin_WhenTheAccountIsLocked_Returns423WithRetryAfter()
    {
        _authServiceMock
            .Setup(s => s.LoginWithPinAsync(It.IsAny<LoginPinRequestDto>()))
            .ReturnsAsync(LoginResult.LockedOut(TimeSpan.FromSeconds(299.2)));
        var controller = CreateController();

        var result = await controller.LoginPin(new LoginPinRequestDto("19900101-1234", "1234"));

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status423Locked);
        controller.Response.Headers.RetryAfter.ToString().Should().Be("300");
        controller.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task LoginPin_WhenTheCredentialsAreWrong_Returns401()
    {
        _authServiceMock
            .Setup(s => s.LoginWithPinAsync(It.IsAny<LoginPinRequestDto>()))
            .ReturnsAsync(LoginResult.InvalidCredentials());
        var controller = CreateController();

        var result = await controller.LoginPin(new LoginPinRequestDto("19900101-1234", "0000"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
        controller.Response.Headers.RetryAfter.ToString().Should().BeEmpty();
    }

    private AuthController CreateController(Guid? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";

        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()) }, "test"));
        }

        return new AuthController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static string SetCookieHeader(AuthController controller) =>
        controller.Response.Headers.SetCookie.ToString();
}