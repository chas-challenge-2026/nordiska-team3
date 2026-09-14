using Microsoft.AspNetCore.Diagnostics;
using NordiskaPortal.API.DTOs.ErrorResponse;

namespace NordiskaPortal.API
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponseDto("An unexpected error occurred. Please try again later."),
            cancellationToken);

            return true;
        }

    }
}