using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.API.Middleware;

/// <summary>
/// Converts unhandled exceptions into consistent RFC 7807 ProblemDetails responses, so the
/// API never leaks stack traces and clients get predictable, machine-readable error shapes.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var problem = MapToProblem(context, ex);

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, problem.GetType()));
    }

    private ProblemDetails MapToProblem(HttpContext context, Exception ex)
    {
        switch (ex)
        {
            case NotFoundException notFound:
                return Build(StatusCodes.Status404NotFound, "Resource not found", notFound.Message);

            case ValidationException validation:
                return new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                };

            case UnauthorizedAccessException:
                return Build(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this resource.");

            default:
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
                return Build(StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.");
        }
    }

    private static ProblemDetails Build(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
