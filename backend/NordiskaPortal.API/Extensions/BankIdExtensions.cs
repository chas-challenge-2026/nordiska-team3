using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NordiskaPortal.API.Services.BankId;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Extensions;

public static class BankIdExtensions
{
    public static IServiceCollection AddBankId(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(BankIdOptions.SectionName).Get<BankIdOptions>() ?? new BankIdOptions();

        services.AddSingleton(options);
        services.AddSingleton<BankIdOrderStore>();
        services.AddScoped<IBankIdService, BankIdService>();

        if (options.IsTest)
        {
            AddTestApiClient(services, options, environment.ContentRootPath);
        }
        else if (options.IsMock)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("BankId:Mode \"Mock\" is only allowed in the Development environment.");
            }

            services.AddSingleton<MockBankIdApiClient>();
            services.AddSingleton<IBankIdApiClient>(sp => sp.GetRequiredService<MockBankIdApiClient>());
        }
        else if (options.IsOff)
        {
            services.AddSingleton<IBankIdApiClient, DisabledBankIdApiClient>();
        }
        else
        {
            throw new InvalidOperationException($"Unknown BankId:Mode \"{options.Mode}\". Use Off, Mock or Test.");
        }

        return services;
    }

    private static void AddTestApiClient(IServiceCollection services, BankIdOptions options, string contentRoot)
    {
        var clientCertificate = LoadClientCertificate(options, contentRoot);
        var rootCertificate = LoadRootCertificate(options, contentRoot);

        services.AddHttpClient<IBankIdApiClient, BankIdApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);
                handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                    IsIssuedByBankId(certificate, errors, rootCertificate);
                return handler;
            });
    }

    private static bool IsIssuedByBankId(X509Certificate2? certificate, SslPolicyErrors errors, X509Certificate2 root)
    {
        if (certificate is null || (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        return chain.Build(certificate);
    }

    private static X509Certificate2 LoadClientCertificate(BankIdOptions options, string contentRoot)
    {
        var path = Path.Combine(contentRoot, options.ClientCertificatePath);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"BankID client certificate not found at '{path}'.");
        }

        var certificate = new X509Certificate2(path, options.ClientCertificatePassword);

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("The BankID client certificate has no private key.");
        }

        return certificate;
    }

    private static X509Certificate2 LoadRootCertificate(BankIdOptions options, string contentRoot)
    {
        var path = Path.Combine(contentRoot, options.RootCertificatePath);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"BankID root certificate not found at '{path}'.");
        }

        var bytes = File.ReadAllBytes(path);
        var text = System.Text.Encoding.ASCII.GetString(bytes);

        return text.Contains("-----BEGIN CERTIFICATE-----")
            ? X509Certificate2.CreateFromPem(text)
            : new X509Certificate2(bytes);
    }
}