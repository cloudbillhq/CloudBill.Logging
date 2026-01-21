using Microsoft.AspNetCore.Http;

namespace CloudBill.Logging.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID.
/// Extracts from X-Correlation-ID header or generates a new one.
/// Adds the correlation ID to the response headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Store in HttpContext for access by enrichers
        context.Items["CorrelationId"] = correlationId;

        await _next(context);
    }
}
