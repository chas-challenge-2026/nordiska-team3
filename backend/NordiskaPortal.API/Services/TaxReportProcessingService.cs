using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.TaxReports;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.Json;

namespace NordiskaPortal.API.Services
{
    public class TaxReportProcessingService : ITaxReportProcessingService
    {
        private const string GenericErrorMessage = "The report could not be generated.";
        private const int MaxLoggedErrorLength = 1000;

        private readonly ApplicationDbContext _context;
        private readonly ITaxReportDataService _taxReportDataService;
        private readonly INativeProcessRunner _processRunner;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaxReportProcessingService> _logger;
        private readonly TaxReportQueue _queue;

        public TaxReportProcessingService(
            ApplicationDbContext context,
            ITaxReportDataService taxReportDataService,
            INativeProcessRunner processRunner,
            IConfiguration configuration,
            ILogger<TaxReportProcessingService> logger, TaxReportQueue queue)
        {
            _context = context;
            _taxReportDataService = taxReportDataService;
            _processRunner = processRunner;
            _configuration = configuration;
            _logger = logger;
            _queue = queue;
        }

        public async Task<TaxReport> QueueReportAsync(Guid userId, int reportYear)
        {
            var report = new TaxReport
            {
                UserId = userId,
                ReportYear = reportYear,
                Status = "QUEUED",
            };

            _context.TaxReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("TaxReport {TaxreportId} queued for user {UserId}, year {ReportYear}", report.Id, userId, reportYear);

            _queue.Enqueue(report.Id);

            return report;
        }

        public async Task ProcessReportAsync(Guid reportId)
        {
            var report = await _context.TaxReports.FindAsync(reportId);
            if (report is null) return;

            report.Status = "PROCESSING";
            await _context.SaveChangesAsync();

            try
            {
                var input = await _taxReportDataService.GetTaxReportDataAsync(report.UserId, report.ReportYear, report.Id);

                var json = JsonSerializer.Serialize(input, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new Utc8601DateTimeOffsetConverter() }
                });

                var outputDirectory = _configuration["TaxReport:OutputDirectory"]!;
                Directory.CreateDirectory(outputDirectory);

                var tempPath = Path.Combine(outputDirectory, $"{report.Id}.json.tmp");
                var finalPath = Path.Combine(outputDirectory, $"{report.Id}.json");

                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                await System.IO.File.WriteAllTextAsync(tempPath, json, utf8NoBom);
                System.IO.File.Move(tempPath, finalPath, overwrite: true);

                _logger.LogInformation("TaxReport {TaxReportId} input written to {Path}", report.Id, finalPath);

                await GeneratePdfAsync(report, finalPath, Path.Combine(outputDirectory, $"{report.Id}.pdf"));
                await _context.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                // ErrorMessage is returned to the customer, so unexpected errors get a generic text
                // and the details only go to the log.
                report.Status = "FAILED";
                report.ErrorMessage = ex is TaxReportGenerationException ? ex.Message : GenericErrorMessage;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, "TaxReport {TaxReportId} failed to process", report.Id);
                throw;
            }
        }

        public async Task<TaxReport?> GetStatusAsync(Guid userId, Guid reportId)
        {
            return await _context.TaxReports
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);
        }

        public async Task<IReadOnlyList<TaxReport>> GetReportsForUserAsync(Guid userId)
        {
            return await _context.TaxReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReportYear)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Runs the native PDF generator on the input JSON. The signer is not wired in yet,
        // so a successful report is READY but unsigned (SignaturePath stays null).
        private async Task GeneratePdfAsync(TaxReport report, string jsonPath, string pdfPath)
        {
            var generatorPath = Path.GetFullPath(_configuration["TaxReport:PdfGeneratorPath"]!);
            if (!System.IO.File.Exists(generatorPath))
            {
                _logger.LogError("PDF generator not found at {Path}", generatorPath);
                throw new TaxReportGenerationException("The PDF generator is not available.");
            }

            var jsonFullPath = Path.GetFullPath(jsonPath);
            var pdfFullPath = Path.GetFullPath(pdfPath);

            // The generator rejects an existing output file, so clear any leftover from an earlier attempt.
            if (System.IO.File.Exists(pdfFullPath))
            {
                System.IO.File.Delete(pdfFullPath);
            }

            var timeoutSeconds = _configuration.GetValue("TaxReport:TimeoutSeconds", 30);

            var result = await _processRunner.RunAsync(
                generatorPath,
                new[] { jsonFullPath, pdfFullPath },
                TimeSpan.FromSeconds(timeoutSeconds));

            report.NativeExitCode = result.TimedOut ? null : result.ExitCode;

            if (result.TimedOut)
            {
                _logger.LogError("PDF generator timed out after {Seconds}s for TaxReport {TaxReportId}", timeoutSeconds, report.Id);
                throw new TaxReportGenerationException("PDF generation timed out.");
            }

            if (result.ExitCode != 0)
            {
                _logger.LogError(
                    "PDF generator failed for TaxReport {TaxReportId} with exit code {ExitCode}: {StandardError}",
                    report.Id, result.ExitCode, Truncate(result.StandardError));
                throw new TaxReportGenerationException($"PDF generation failed (code {result.ExitCode}).");
            }

            if (!System.IO.File.Exists(pdfFullPath) || new FileInfo(pdfFullPath).Length == 0)
            {
                throw new TaxReportGenerationException("The PDF generator did not produce a file.");
            }

            report.PdfPath = pdfFullPath;
            report.Status = "READY";
            report.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("TaxReport {TaxReportId} PDF written to {Path}", report.Id, pdfFullPath);
        }

        private static string Truncate(string text) =>
            text.Length <= MaxLoggedErrorLength ? text : text[..MaxLoggedErrorLength];
    }

    // A failure whose message is safe to show to the customer (it is stored in TaxReport.ErrorMessage).
    public sealed class TaxReportGenerationException : Exception
    {
        public TaxReportGenerationException(string message) : base(message)
        {
        }
    }
}