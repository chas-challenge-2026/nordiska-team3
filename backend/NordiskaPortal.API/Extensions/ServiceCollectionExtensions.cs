using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Repositories.Interfaces;

namespace NordiskaPortal.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) // "this" gör metoden till en extension method som du kan anropa
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }
}