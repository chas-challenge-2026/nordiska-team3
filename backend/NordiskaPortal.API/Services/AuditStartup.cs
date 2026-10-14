using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public static class AuditStartup
{
    // Makes sure the audit key exists and the chain is intact before the app takes any requests.
    // A broken chain stops the app, like a missing personal number key does. Audit:AllowBrokenChain
    // lets it start anyway (the error is still logged), for emergencies.
    public static async Task RunAsync(IAuditService audit, bool allowBrokenChain, ILogger logger)
    {
        await audit.EnsureKeyAsync();

        var result = await audit.VerifyChainAsync();

        if (result.IsValid)
        {
            logger.LogInformation("Audit chain verified: {Count} entries.", result.CheckedCount);
            return;
        }

        var message = $"Audit chain is broken at entry {result.FirstInvalidId}, after {result.CheckedCount} valid entries. " +
                      "The audit log was changed, or the audit key does not match.";

        if (!allowBrokenChain)
        {
            throw new InvalidOperationException(message);
        }

        logger.LogError("{Message} Starting anyway because Audit:AllowBrokenChain is set.", message);
    }
}