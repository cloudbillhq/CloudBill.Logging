using Serilog.Core;
using Serilog.Events;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with the service name for multi-service environments.
/// </summary>
public class ServiceNameEnricher : ILogEventEnricher
{
    private const string ServiceNamePropertyName = "ServiceName";
    private readonly string _serviceName;

    public ServiceNameEnricher(string serviceName)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var property = propertyFactory.CreateProperty(ServiceNamePropertyName, _serviceName);
        logEvent.AddPropertyIfAbsent(property);
    }
}
