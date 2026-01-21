using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with a CorrelationId that flows across all services.
/// Uses X-Correlation-ID header or generates a new one.
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdPropertyName = "CorrelationId";
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Try to get correlation ID from header (for cross-service tracing)
        var correlationId = httpContext.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                          ?? httpContext.TraceIdentifier; // Fallback to TraceId

        var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }
}
