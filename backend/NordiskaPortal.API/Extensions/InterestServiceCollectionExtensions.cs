using Microsoft.Extensions.DependencyInjection.Extensions;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Extensions;

public static class InterestServiceCollectionExtensions
{
    public static IServiceCollection AddInterestServices(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Riksbank:BaseUrl"] ?? "https://api.riksbank.se/swea/v1/";
        if (!baseUrl.EndsWith('/')) baseUrl += "/";

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IInterestRateSource, RiksbankRateSource>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<InterestRateRefresher>();
        services.AddScoped<IInterestService, InterestService>();
        services.AddHostedService<InterestRateRefreshWorker>();

        return services;
    }
}