using System.Net;
using System.Text;
using FluentAssertions;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class RiksbankRateSourceTests
{
    [Fact]
    public async Task GetObservationsAsync_CallsTheIntervalUrl_AndReadsDatesAndValues()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            "[{\"date\":\"2026-10-07\",\"value\":1.75},{\"date\":\"2026-10-08\",\"value\":2.00}]");
        var source = CreateSource(handler);

        var result = await source.GetObservationsAsync("SECBREPOEFF", new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8));

        handler.LastRequestUri!.ToString().Should().Be(
            "https://api.example.test/swea/v1/Observations/SECBREPOEFF/2026-10-07/2026-10-08");
        result.Should().HaveCount(2);
        result[0].Should().Be(new RateObservation(new DateOnly(2026, 10, 7), 1.75m));
        result[1].Should().Be(new RateObservation(new DateOnly(2026, 10, 8), 2.00m));
    }

    [Fact]
    public async Task GetObservationsAsync_WhenTheListIsEmpty_ReturnsNoObservations()
    {
        var source = CreateSource(new StubHandler(HttpStatusCode.OK, "[]"));

        var result = await source.GetObservationsAsync("SECBREPOEFF", new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObservationsAsync_WhenTheServerFails_Throws()
    {
        var source = CreateSource(new StubHandler(HttpStatusCode.InternalServerError, ""));

        var act = () => source.GetObservationsAsync("SECBREPOEFF", new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static RiksbankRateSource CreateSource(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/swea/v1/") });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}