using FluentValidation;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Serilog;

namespace NordiskaPortal.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRepository<LedgerEntry>, Repository<LedgerEntry>>();
        services.AddScoped<IRepository<Notification>, Repository<Notification>>();
        services.AddScoped<IFaqRepository, FaqRepository>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITaxReportDataService, TaxReportDataService>();
        services.AddScoped<ITaxReportProcessingService, TaxReportProcessingService>();
        services.AddSingleton<TaxReportQueue>();
        services.AddHostedService<TaxReportBackgroundWorker>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddHostedService<NotificationBackgroundWorker>();
        services.AddScoped<IFaqService, FaqService>();
        services.AddSingleton<IPersonalNumberProtector, PersonalNumberProtector>();
        services.AddSingleton<INativeProcessRunner, NativeProcessRunner>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton(sp =>
        {
            // Same pattern as the JWT key. Configuration first, otherwise a key that is created once and kept in /secrets.
            var key = sp.GetRequiredService<IConfiguration>()["Audit:Key"] ?? GetOrCreateSigningKey("/secrets/audit.key");
            return new AuditKey(Convert.FromBase64String(key));
        });

        // Validation
        services.AddValidatorsFromAssemblyContaining<Program>(); // Letar alla klasser som ärver AbstractValidator<T>

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? GetOrCreateSigningKey("/secrets/jwt.key");
        configuration["Jwt:Key"] = jwtKey;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();

        return services;
    }

    // Läser en tidigare genererad nyckel från disk om den finns (stage/prod,
    // ingen Jwt:Key i config). Annars genereras en ny slumpmässig nyckel och
    // sparas, så samma nyckel återanvänds vid nästa omstart av containern
    private static string GetOrCreateSigningKey(string path)
    {
        if (File.Exists(path))
            return File.ReadAllText(path).Trim();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(path, key);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return key;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter your JWT token here (without 'Bearer' prefix)",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // I stage/prod innehåller connection string inget lösenord - secrets-init
        // genererar ett och delar det med db/api via en egen volym (docker-compose.yml).
        const string dbPasswordFile = "/run/secrets/db_password";
        if (File.Exists(dbPasswordFile))
        {
            connectionString = $"{connectionString};Password={File.ReadAllText(dbPasswordFile).Trim()}";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        return builder;
    }

    // Apply any pending EF Core migrations automatically on startup,
    // so no one needs to run `dotnet ef database update` manually.
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        return app;
    }

    // Recomputes the audit chain at startup and logs the result. A broken chain never stops the app from starting.
    public static async Task VerifyAuditChainAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuditService>>();

        var result = await audit.VerifyChainAsync();

        if (result.IsValid)
        {
            logger.LogInformation("Audit chain verified: {Count} entries.", result.CheckedCount);
        }
        else
        {
            logger.LogError("Audit chain is broken at entry {EntryId}, after {Count} valid entries.",
                result.FirstInvalidId, result.CheckedCount);
        }
    }

    // Lägger in en testanvändare om den saknas, efter att migrationerna körts.
    // Ersätter infra/user.sql, som försökte göra samma sak innan Users-tabellen
    public static async Task SeedTestDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IPersonalNumberProtector>();

        const string testPersonalNumber = "19900101-1234";
        var testHash = protector.ComputeHash(testPersonalNumber);
        if (!await db.Users.AnyAsync(u => u.PersonalNumberHash == testHash))
        {
            db.Users.Add(new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PersonalNumber = protector.Protect(testPersonalNumber),
                PersonalNumberHash = testHash,
                FirstName = "Test",
                LastName = "Testsson",
                Email = "test@example.com",
                PinHash = "$2b$12$Ma9ikA7xtUOXMH86.OZA7eYIEb.yDiazDkk5uu5M/4PpbuC3ORvSu"
            });

            await db.SaveChangesAsync();
        }
    }

    // Krypterar personnummer som fortfarande ligger i klartext (demoanvandaren från migrationen
    // och rader från innan krypteringen infördes). Körs efter migrationerna och är idempotent.
    public static async Task ProtectExistingPersonalNumbersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IPersonalNumberProtector>();

        var sample = await db.Users
            .Where(u => u.PersonalNumber.StartsWith(PersonalNumberProtector.Prefix))
            .Select(u => u.PersonalNumber)
            .FirstOrDefaultAsync();

        if (!protector.IsKeyAvailable)
        {
            // Finns det redan krypterade personnummer får vi ALDRIG skapa en ny nyckel,
            if (sample is not null)
            {
                throw new InvalidOperationException(
                    "Personal number key is missing but encrypted personal numbers exist. " +
                    "Restore /secrets/pn.key (or PersonalNumberProtection:Key). Refusing to generate a new key.");
            }

            // Allra första starten: inga krypterade rader finns än, skapa nyckeln.
            protector.GenerateNewKey();
        }
        else if (sample is not null)
        {
            // Nyckeln finns: kontrollera att den faktiskt kan öppna befintliga krypterade rader.
            try
            {
                protector.Unprotect(sample);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException(
                    "Personal number key does not match the encrypted data. Is the api_secrets volume missing?");
            }
        }

        var users = await db.Users
            .Where(u => !u.PersonalNumber.StartsWith(PersonalNumberProtector.Prefix)
                        || u.PersonalNumberHash == null)
            .ToListAsync();

        foreach (var user in users)
        {
            var plain = protector.Unprotect(user.PersonalNumber);

            if (!protector.IsProtected(user.PersonalNumber))
            {
                user.PersonalNumber = protector.Protect(plain);
            }

            user.PersonalNumberHash = protector.ComputeHash(plain);
        }

        if (users.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }
}