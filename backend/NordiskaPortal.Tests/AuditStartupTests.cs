using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class AuditStartupTests
{
    private readonly Mock<IAuditService> _auditMock = new();

    [Fact]
    public async Task RunAsync_WhenTheChainIsIntact_EnsuresTheKeyAndLetsTheAppStart()
    {
        _auditMock.Setup(a => a.VerifyChainAsync()).ReturnsAsync(new AuditVerificationResult(true, 5, null));

        await AuditStartup.RunAsync(_auditMock.Object, allowBrokenChain: false, NullLogger.Instance);

        _auditMock.Verify(a => a.EnsureKeyAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenTheChainIsBroken_StopsTheApp()
    {
        _auditMock.Setup(a => a.VerifyChainAsync()).ReturnsAsync(new AuditVerificationResult(false, 4, 17));

        var act = () => AuditStartup.RunAsync(_auditMock.Object, allowBrokenChain: false, NullLogger.Instance);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*entry 17*after 4 valid entries*");
    }

    [Fact]
    public async Task RunAsync_WhenTheChainIsBrokenButAllowed_LetsTheAppStart()
    {
        _auditMock.Setup(a => a.VerifyChainAsync()).ReturnsAsync(new AuditVerificationResult(false, 4, 17));

        var act = () => AuditStartup.RunAsync(_auditMock.Object, allowBrokenChain: true, NullLogger.Instance);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_WhenTheKeyIsMissingWhileEntriesExist_StopsTheAppEvenIfBrokenChainsAreAllowed()
    {
        _auditMock.Setup(a => a.EnsureKeyAsync()).ThrowsAsync(new InvalidOperationException("The audit key is missing"));

        var act = () => AuditStartup.RunAsync(_auditMock.Object, allowBrokenChain: true, NullLogger.Instance);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _auditMock.Verify(a => a.VerifyChainAsync(), Times.Never);
    }
}
