using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace CloudBill.Logging.Enrichers;

/// <summary>
/// Enriches log events with AccountId (MasterAccountId) for billing/customer tracking.
/// Useful for customer portals where the authenticated entity is a billing account.
/// </summary>
public class AccountIdEnricher : ILogEventEnricher
{
    private const string AccountIdPropertyName = "AccountId";
    private const string AccountIdClaimType = "account_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountIdEnricher(IHttpContextAccessor httpContextAccessor)
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

        var accountId = httpContext.User.FindFirst(AccountIdClaimType)?.Value;

        if (!string.IsNullOrEmpty(accountId))
        {
            var property = propertyFactory.CreateProperty(AccountIdPropertyName, accountId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
