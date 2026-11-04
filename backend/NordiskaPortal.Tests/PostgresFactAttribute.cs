using Xunit;

namespace NordiskaPortal.Tests.Postgres;

// A test with this attribute only runs when NORDISKA_TEST_DB holds a connection string to a Postgres database.
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvVar = "NORDISKA_TEST_DB";

    public PostgresFactAttribute()
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvVar);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Skip = $"Set {EnvVar} to a Postgres connection string to run this test.";
        }
        else if (!connectionString.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"{EnvVar} must point to a database with \"test\" in its name.";
        }
    }
}