using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Infrastructure;

/// <summary>
/// Turns domain exceptions into standard ProblemDetails responses (RFC 9457),
/// so controllers don't need try/catch blocks. Like a @ControllerAdvice in Spring.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
            return false; // Unexpected error: let the default handler return a 500.

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
