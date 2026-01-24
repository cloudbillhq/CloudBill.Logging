namespace CloudBill.Logging.Configuration;

/// <summary>
/// Configuration options for CloudBill logging.
/// </summary>
public class CloudBillLoggingOptions
{
    /// <summary>
    /// The name of the service (e.g., "CloudBill.API", "CloudBill.Worker").
    /// Used for identifying logs from different services.
    /// </summary>
    public string ServiceName { get; set; } = "CloudBill";

    /// <summary>
    /// The version of the service (e.g., "1.0.0").
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Enable OpenTelemetry tracing.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// OpenTelemetry exporter endpoint (OTLP).
    /// Leave null to use console exporter only.
    /// For AWS: Use AWS Distro for OpenTelemetry (ADOT) Collector endpoint.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Enable request logging for HTTP requests.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Exclude paths from request logging (e.g., health checks).
    /// </summary>
    public List<string> ExcludeRequestPaths { get; set; } = new()
    {
        "/health",
        "/healthz",
        "/ready",
        "/live"
    };

    /// <summary>
    /// Enable correlation ID tracking across services.
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>
    /// Enable tenant ID tracking for multi-tenant applications.
    /// </summary>
    public bool EnableTenantId { get; set; } = false;

    /// <summary>
    /// Enable user ID tracking from authenticated users.
    /// </summary>
    public bool EnableUserId { get; set; } = true;

    /// <summary>
    /// Enable account ID tracking (MasterAccountId) for customer portal applications.
    /// Use this instead of or in addition to UserId when the authenticated entity is a billing account.
    /// </summary>
    public bool EnableAccountId { get; set; } = false;

    /// <summary>
    /// Output logs in JSON format (CompactJsonFormatter) for CloudWatch/structured logging.
    /// Recommended for production and ECS/container environments.
    /// </summary>
    public bool UseJsonConsoleOutput { get; set; } = false;
}
