namespace NordiskaPortal.API.Services.Interfaces;

public interface ISignedReportCheck
{
    Task<bool> AnySignedReportAsync(CancellationToken cancellationToken = default);
}