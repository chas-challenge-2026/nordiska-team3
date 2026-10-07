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
        private readonly ApplicationDbContext _context;
        private readonly ITaxReportDataService _taxReportDataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaxReportProcessingService> _logger;
        private readonly TaxReportQueue _queue;

        public TaxReportProcessingService(
            ApplicationDbContext context,
            ITaxReportDataService taxReportDataService,
            IConfiguration configuration,
            ILogger<TaxReportProcessingService> logger, TaxReportQueue queue)
        {
             _context = context;
            _taxReportDataService = taxReportDataService;
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

                // Stays PROCESSING here (not a misleading READY) the JSON step succeeded,
                // native just hasn't run yet. Genuine failures are caught below and marked FAILED.
            }

            catch (Exception ex)
            {
                report.Status = "FAILED";
                report.ErrorMessage = ex.Message;
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
    }
}
