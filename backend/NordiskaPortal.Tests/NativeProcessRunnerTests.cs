using FluentAssertions;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class NativeProcessRunnerTests
{
    private static (string FileName, string[] Arguments) LongRunningProcess() =>
        OperatingSystem.IsWindows()
            ? ("powershell", new[] { "-NoProfile", "-Command", "Start-Sleep -Seconds 30" })
            : ("sleep", new[] { "30" });

    [Fact]
    public async Task RunAsync_ReturnsTheExitCodeAndTheOutputOfAFinishedProcess()
    {
        var result = await new NativeProcessRunner().RunAsync("dotnet", new[] { "--version" }, TimeSpan.FromSeconds(60));

        result.ExitCode.Should().Be(0);
        result.TimedOut.Should().BeFalse();
        result.StandardOutput.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RunAsync_WhenTheProcessFails_ReturnsTheNonZeroExitCode()
    {
        var result = await new NativeProcessRunner().RunAsync("dotnet", new[] { "this-command-does-not-exist" }, TimeSpan.FromSeconds(60));

        result.ExitCode.Should().NotBe(0);
        result.TimedOut.Should().BeFalse();
        (result.StandardOutput + result.StandardError).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RunAsync_WhenTheProcessRunsTooLong_KillsItAndReportsATimeout()
    {
        var (fileName, arguments) = LongRunningProcess();

        var result = await new NativeProcessRunner().RunAsync(fileName, arguments, TimeSpan.FromSeconds(1));

        result.TimedOut.Should().BeTrue();
        result.ExitCode.Should().Be(-1);
    }

    [Fact]
    public async Task RunAsync_WhenTheCallerCancels_KillsTheProcessAndThrows()
    {
        var (fileName, arguments) = LongRunningProcess();
        using var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromMilliseconds(500));

        var act = () => new NativeProcessRunner().RunAsync(fileName, arguments, TimeSpan.FromSeconds(30), source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}