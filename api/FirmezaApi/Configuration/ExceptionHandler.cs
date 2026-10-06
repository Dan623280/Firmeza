using Firmeza.Application.Common;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Firmeza.Api.Configuration;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = 500;
        var code = "server_error";
        var message = "An unexpected error occurred.";
        if (exception is RequestException request)
        {
            status = request.Status;
            code = request.Code;
            message = request.Message;
        }
        else if (exception is DomainException domain)
        {
            code = domain.Error.Code;
            message = domain.Message;
            status = code == "validation_failed" ? 400 : 409;
        }
        else if (exception is OverflowException)
        {
            status = 400;
            code = "invalid_amount";
            message = "The requested amount exceeds supported limits.";
        }
        else if (exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } })
        {
            status = 409;
            code = "duplicate_resource";
            message = "A record with the same unique identifier already exists.";
        }
        else if (exception is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected })
        {
            status = 409;
            code = "concurrent_operation";
            message = "A concurrent operation conflicted; retry the request.";
        }
        if (status == 500)
            logger.LogError(exception, "Unhandled API error {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = code, Detail = message, Extensions = { { "traceId", context.TraceIdentifier } } }, ct);
        return true;
    }
}
