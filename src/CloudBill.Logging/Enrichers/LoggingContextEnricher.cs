using CloudBill.Logging.Context;
using Serilog.Core;
using Serilog.Events;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with properties from the ambient LoggingContext.
/// Use this enricher in worker services where HttpContext is not available.
/// </summary>
public class LoggingContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = LoggingContext.Current;
        if (context == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(context.CorrelationId))
        {
            var property = propertyFactory.CreateProperty("CorrelationId", context.CorrelationId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(context.TenantId))
        {
            var property = propertyFactory.CreateProperty("TenantId", context.TenantId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(context.UserId))
        {
            var property = propertyFactory.CreateProperty("UserId", context.UserId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(context.BatchId))
        {
            var property = propertyFactory.CreateProperty("BatchId", context.BatchId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(context.DocumentId))
        {
            var property = propertyFactory.CreateProperty("DocumentId", context.DocumentId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (context.AdditionalProperties != null)
        {
            foreach (var kvp in context.AdditionalProperties)
            {
                var property = propertyFactory.CreateProperty(kvp.Key, kvp.Value, destructureObjects: true);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}
