using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NordiskaPortal.API;
using Xunit;

namespace NordiskaPortal.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ReturnsTheCorrelationIdButNotTheExceptionDetails()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "req-42" };
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context, new InvalidOperationException("password is hunter2"), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("\"correlationId\":\"req-42\"");
        json.Should().NotContain("hunter2");
    }
}
