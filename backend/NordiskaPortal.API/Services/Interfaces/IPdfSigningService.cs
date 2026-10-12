namespace NordiskaPortal.API.Services.Interfaces;

public sealed record PdfSignature(string SignaturePath, string Sha256);

public interface IPdfSigningService
{
    // Signs the PDF with the native signer and checks the signature with the public key before returning.
    // Throws TaxReportGenerationException (its message is safe to show the customer) if signing or the check fails.
    Task<PdfSignature> SignAsync(string pdfPath, CancellationToken cancellationToken = default);
}