using Firmeza.Application.Exceptions;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Firmeza.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { context.Response.StatusCode = 499; }
        catch (Exception ex)
        {
            var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
            var status = ex switch
            {
                BusinessException b => b.StatusCode,
                NotFoundException => 404,
                DbUpdateConcurrencyException => 409,
                _ when pg?.SqlState is "23505" or "40001" or "40P01" => 409,
                _ => 500
            };
            if (status == 500)
                log.LogError(ex, "Error en {Path}", context.Request.Path);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = status == 500 ? "Ocurrió un error inesperado." : status == 409 ? "Conflicto de datos" : ex.Message, Detail = status == 500 ? "Intenta de nuevo o consulta al administrador." : pg != null ? "Registro duplicado o modificación simultánea. Actualiza e intenta nuevamente." : ex.Message, Extensions = { { "traceId", context.TraceIdentifier } } });
        }
    }
}
