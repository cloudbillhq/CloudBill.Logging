using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with TenantId for multi-tenant applications.
/// Extracts from X-Tenant-ID header or claims.
/// </summary>
public class TenantIdEnricher : ILogEventEnricher
{
    private const string TenantIdPropertyName = "TenantId";
    private const string TenantIdHeaderName = "X-Tenant-ID";
    private const string TenantIdClaimType = "tenant_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantIdEnricher(IHttpContextAccessor httpContextAccessor)
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

        // Try header first, then claims
        var tenantId = httpContext.Request.Headers[TenantIdHeaderName].FirstOrDefault()
                      ?? httpContext.User?.FindFirst(TenantIdClaimType)?.Value;

        if (!string.IsNullOrEmpty(tenantId))
        {
            var property = propertyFactory.CreateProperty(TenantIdPropertyName, tenantId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
