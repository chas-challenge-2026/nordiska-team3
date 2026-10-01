namespace NordiskaPortal.API.DTOs.Faq;

public class FaqSearchResultDto
{
    public bool MatchFound { get; set; }
    public double Score { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;
}