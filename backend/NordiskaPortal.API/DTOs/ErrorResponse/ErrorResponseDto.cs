using System.Text.Json.Serialization;

namespace NordiskaPortal.API.DTOs.ErrorResponse;

// Error body for failed requests. CorrelationId is only filled in for unexpected errors (500),
// so the customer can quote it and the team can find the matching log lines.
public sealed record ErrorResponseDto(
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CorrelationId = null);