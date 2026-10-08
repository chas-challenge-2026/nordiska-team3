using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NordiskaPortal.API.Middleware;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace NordiskaPortal.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public void HeaderName_IsTheStandardCorrelationIdHeader()
    {
        // Clients and proxies look for this exact name, so a typo here must not pass unnoticed.
        CorrelationIdMiddleware.HeaderName.Should().Be("X-Correlation-ID");
    }

    [Fact]
    public async Task InvokeAsync_WhenTheRequestHasNoId_CreatesOne()
    {
        var context = new DefaultHttpContext();

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        var id = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        id.Should().HaveLength(32);
        context.TraceIdentifier.Should().Be(id);
    }

    [Fact]
    public async Task InvokeAsync_WhenTheRequestHasAValidId_ReusesIt()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "frontend-123_a.b";

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("frontend-123_a.b");
        context.TraceIdentifier.Should().Be("frontend-123_a.b");
    }

    [Theory]
    [InlineData("has spaces")]
    [InlineData("new\nline")]
    [InlineData("<script>")]
    public async Task InvokeAsync_WhenTheRequestIdHasOddCharacters_ReplacesIt(string incoming)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = incoming;

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        var id = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        id.Should().NotBe(incoming);
        id.Should().HaveLength(32);
    }

    [Fact]
    public async Task InvokeAsync_WhenTheRequestIdIsTooLong_ReplacesIt()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = new string('a', 65);

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().HaveLength(32);
    }

    [Fact]
    public async Task InvokeAsync_PutsTheIdOnEveryLogEventWrittenWhileTheRequestRuns()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "req-42";

        await new CorrelationIdMiddleware(_ =>
        {
            logger.Information("inside the request");
            return Task.CompletedTask;
        }).InvokeAsync(context);
        logger.Information("after the request");

        sink.Events[0].Properties["CorrelationId"].ToString().Should().Be("\"req-42\"");
        sink.Events[1].Properties.Should().NotContainKey("CorrelationId");
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
