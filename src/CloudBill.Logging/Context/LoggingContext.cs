namespace CloudBill.Logging.Context;

/// <summary>
/// Provides an ambient context for logging properties that flows with async operations.
/// Use this in worker services where HttpContext is not available.
/// </summary>
/// <example>
/// using (LoggingContext.Push(correlationId: "abc-123", tenantId: "ACME"))
/// {
///     // All logs within this scope will include CorrelationId and TenantId
///     await ProcessMessageAsync();
/// }
/// </example>
public static class LoggingContext
{
    private static readonly AsyncLocal<LoggingContextData?> _current = new();

    /// <summary>
    /// Gets the current logging context data, or null if not set.
    /// </summary>
    public static LoggingContextData? Current => _current.Value;

    /// <summary>
    /// Pushes a new logging context scope. Dispose the returned object to restore the previous context.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracing across services.</param>
    /// <param name="tenantId">Tenant/client identifier for multi-tenant applications.</param>
    /// <param name="userId">User identifier if available.</param>
    /// <param name="batchId">Batch identifier for batch processing operations.</param>
    /// <param name="documentId">Document identifier being processed.</param>
    /// <param name="additionalProperties">Additional custom properties to include in logs.</param>
    /// <returns>A disposable that restores the previous context when disposed.</returns>
    public static IDisposable Push(
        string? correlationId = null,
        string? tenantId = null,
        string? userId = null,
        string? batchId = null,
        string? documentId = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var previous = _current.Value;

        _current.Value = new LoggingContextData
        {
            CorrelationId = correlationId ?? previous?.CorrelationId,
            TenantId = tenantId ?? previous?.TenantId,
            UserId = userId ?? previous?.UserId,
            BatchId = batchId ?? previous?.BatchId,
            DocumentId = documentId ?? previous?.DocumentId,
            AdditionalProperties = MergeProperties(previous?.AdditionalProperties, additionalProperties)
        };

        return new ContextScope(previous);
    }

    /// <summary>
    /// Sets only the correlation ID, preserving other context values.
    /// </summary>
    public static IDisposable PushCorrelationId(string correlationId)
        => Push(correlationId: correlationId);

    /// <summary>
    /// Sets only the tenant ID, preserving other context values.
    /// </summary>
    public static IDisposable PushTenantId(string tenantId)
        => Push(tenantId: tenantId);

    /// <summary>
    /// Sets batch processing context with batch ID and optional document ID.
    /// </summary>
    public static IDisposable PushBatchContext(string batchId, string? documentId = null)
        => Push(batchId: batchId, documentId: documentId);

    /// <summary>
    /// Clears all context data.
    /// </summary>
    public static void Clear()
    {
        _current.Value = null;
    }

    private static Dictionary<string, object>? MergeProperties(
        Dictionary<string, object>? existing,
        Dictionary<string, object>? incoming)
    {
        if (existing == null && incoming == null)
            return null;

        var merged = new Dictionary<string, object>(existing ?? []);

        if (incoming != null)
        {
            foreach (var kvp in incoming)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly LoggingContextData? _previous;
        private bool _disposed;

        public ContextScope(LoggingContextData? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = _previous;
        }
    }
}

/// <summary>
/// Data stored in the logging context.
/// </summary>
public sealed class LoggingContextData
{
    /// <summary>
    /// Correlation ID for tracing requests across services.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Tenant/client identifier for multi-tenant applications.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// User identifier if available.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Batch identifier for batch processing operations.
    /// </summary>
    public string? BatchId { get; init; }

    /// <summary>
    /// Document identifier currently being processed.
    /// </summary>
    public string? DocumentId { get; init; }

    /// <summary>
    /// Additional custom properties.
    /// </summary>
    public Dictionary<string, object>? AdditionalProperties { get; init; }
}
