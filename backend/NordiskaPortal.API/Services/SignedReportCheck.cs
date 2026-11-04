using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Services;

public sealed class SignedReportCheck : ISignedReportCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SignedReportCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> AnySignedReportAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.TaxReports
            .AsNoTracking()
            .AnyAsync(r => r.SignaturePath != null, cancellationToken);
    }
}