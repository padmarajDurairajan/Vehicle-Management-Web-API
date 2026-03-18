using System.Diagnostics;

namespace VehicleManagementApi.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Request started. Method={Method} Path={Path} TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "Request finished. Method={Method} Path={Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs} TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}