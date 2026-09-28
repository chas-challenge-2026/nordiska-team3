namespace NordiskaPortal.API.Models
{
    public class TaxReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public int ReportYear { get; set; }
        public string Status { get; set; } = "QUEUED";
        public string? PdfPath { get; set; }
        public string? SignaturePath { get; set; }
        public string? Sha256 { get; set; }
        public int? NativeExitCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public User User { get; set; }

    }
}
