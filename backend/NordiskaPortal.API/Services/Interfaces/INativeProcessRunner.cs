namespace NordiskaPortal.API.Services.Interfaces
{
    public sealed record NativeProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

    public interface INativeProcessRunner
    {
        // Starts an executable (no shell) and waits for it to exit. The process is killed if it runs longer than the timeout.
        Task<NativeProcessResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);
    }

}
