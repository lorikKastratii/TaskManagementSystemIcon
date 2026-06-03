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
        ProblemDetails problem;

        switch (ex)
        {
            case NotFoundException notFound:
                problem = Build(StatusCodes.Status404NotFound, "Resource not found", notFound.Message);
                break;

            case ValidationException validation:
                var vpd = new ValidationProblemDetails(
                    validation.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                };
                problem = vpd;
                break;

            case UnauthorizedAccessException:
                problem = Build(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this resource.");
                break;

            default:
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
                problem = Build(StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.");
                break;
        }

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, problem.GetType()));
    }

    private static ProblemDetails Build(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
