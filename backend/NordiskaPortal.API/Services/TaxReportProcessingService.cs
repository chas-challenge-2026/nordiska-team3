using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.DTOs.TaxReports;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services.Interfaces;
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

        public TaxReportProcessingService(
            ApplicationDbContext context,
            ITaxReportDataService taxReportDataService,
            IConfiguration configuration,
            ILogger<TaxReportProcessingService> logger)
        {
             _context = context;
            _taxReportDataService = taxReportDataService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TaxReport> GenerateReportAsync(Guid userId, int reportYear)
        {
            var report = new TaxReport
            {
                UserId = userId,
                ReportYear = reportYear,
                Status = "QUEUED",
            };

            _context.TaxReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("TaxReport {TaxReportId} queued for user {UserId}, year {ReportYear}", report.Id, userId, reportYear);

            report.Status = "PROCESSING";
            await _context.SaveChangesAsync();

            var input = await _taxReportDataService.GetTaxReportDataAsync(userId, reportYear, report.Id);

            var json = JsonSerializer.Serialize(input, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new Utc8601DateTimeOffsetConverter() }
            });

            var outputDirectory = _configuration["TaxReport:OutputDirectory"]!;
            Directory.CreateDirectory(outputDirectory);

            var tempPath = Path.Combine(outputDirectory, $"{report.Id}.json.tmp");
            var finalPath = Path.Combine(outputDirectory, $"{report.Id}.json");

            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // No BOM, native's JSON parser is C/C++ a leading UTF-8 BOM can cause issues
            await System.IO.File.WriteAllTextAsync(tempPath, json, utf8Bom);
            System.IO.File.Move(tempPath, finalPath, overwrite: true);

            _logger.LogInformation("TaxReport {TaxReportId} input written to {Path}", report.Id, finalPath);

            //TODO: Run pdf generator + pdf signer via native executable once natives CLI argument shape is confirmed.
            // REport stays PROCESSING rather than a misleading READY/FAILED until that exist.

            return report;
        }

        public async Task<TaxReport?> GetStatusAsync(Guid userId, Guid reportId)
        {
            return await _context.TaxReports
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);
        }
    }
}
