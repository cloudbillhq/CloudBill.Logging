using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with UserId from authenticated user claims.
/// </summary>
public class UserIdEnricher : ILogEventEnricher
{
    private const string UserIdPropertyName = "UserId";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Try standard claim types
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? httpContext.User.FindFirst("sub")?.Value
                   ?? httpContext.User.FindFirst("user_id")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var property = propertyFactory.CreateProperty(UserIdPropertyName, userId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
