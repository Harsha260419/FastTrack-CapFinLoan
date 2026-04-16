using System.Net;
using System.Text.Json;
using CapFinLoan.Application.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CapFinLoan.Application.Application.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception encountered while processing request.");
            await HandleExceptionAsync(context, ex, _environment.IsDevelopment());
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, bool includeDetails)
    {
        var (statusCode, message) = exception switch
        {
            ApplicationExceptionBase applicationException => (applicationException.StatusCode, applicationException.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            HttpRequestException => (HttpStatusCode.BadGateway, exception.Message),
            _ when exception.GetType().Name == "DbUpdateException" => (HttpStatusCode.Conflict, "Database update failed. Check duplicate or invalid data."),
            IOException => (HttpStatusCode.InternalServerError, "File operation failed. Please retry."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = includeDetails
            ? JsonSerializer.Serialize(new { message, details = exception.Message, exceptionType = exception.GetType().Name })
            : JsonSerializer.Serialize(new { message });

        return context.Response.WriteAsync(response);
    }
}