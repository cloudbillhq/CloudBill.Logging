# CloudBill.Logging

**Organization-wide logging standard for CloudBill services.**

Provides consistent, structured logging across all CloudBill .NET applications using **Serilog** and **OpenTelemetry** with built-in support for:

- ✅ Structured logging with Serilog
- ✅ Distributed tracing with OpenTelemetry
- ✅ Correlation ID tracking across services
- ✅ AWS CloudWatch integration
- ✅ User and Tenant ID enrichment
- ✅ Request/response logging
- ✅ Colorful console output
- ✅ Industry best practices

## Quick Start

### 1. Install the Package

```bash
dotnet add package CloudBill.Logging
```

### 2. Add to Your Application

#### Web Applications

**Program.cs:**

```csharp
using CloudBill.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add CloudBill logging (Serilog + OpenTelemetry)
builder.AddCloudBillLogging(options =>
{
    options.ServiceName = "CloudBill.API";
    options.ServiceVersion = "1.0.0";
    options.EnableOpenTelemetry = true;
    options.EnableCorrelationId = true;
    options.EnableUserId = true;
});

// ... other services ...

var app = builder.Build();

try
{
    // Use request logging
    app.UseCloudBillRequestLogging();

    // ... your middleware and endpoints ...

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    CloudBillLoggingExtensions.CloseCloudBillLogging();
}
```

#### Worker Services (Background Services, ECS Tasks)

**Program.cs:**

```csharp
using CloudBill.Logging.Context;
using CloudBill.Logging.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Add CloudBill logging for workers
builder.AddCloudBillLogging(options =>
{
    options.ServiceName = "BillGen.Worker";
    options.ServiceVersion = "1.0.0";
    options.UseJsonConsoleOutput = true;  // JSON for CloudWatch
    options.EnableOpenTelemetry = true;
});

// ... other services ...

var host = builder.Build();

try
{
    await host.RunAsync();
}
finally
{
    await CloudBillLoggingExtensions.CloseCloudBillLoggingAsync();
}
```

#### Using LoggingContext in Workers

Since workers don't have HttpContext, use `LoggingContext` to set correlation IDs, tenant IDs, etc.:

```csharp
public class MyWorkerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await ReceiveMessageAsync();

            // Push context for this message - all logs will include these properties
            using (LoggingContext.Push(
                correlationId: message.RequestId.ToString(),
                tenantId: message.ClientCode,
                batchId: message.BatchId,
                documentId: message.DocumentId))
            {
                _logger.LogInformation("Processing document");
                await ProcessAsync(message);
                _logger.LogInformation("Document processed successfully");
            }
            // Context is automatically restored when disposed
        }
    }
}
```

**Convenience methods:**

```csharp
// Just set correlation ID
using (LoggingContext.PushCorrelationId("abc-123"))
{
    // ...
}

// Just set tenant ID
using (LoggingContext.PushTenantId("ACME"))
{
    // ...
}

// Set batch context
using (LoggingContext.PushBatchContext(batchId: "batch-456", documentId: "doc-789"))
{
    // ...
}
```

### 3. Configure appsettings.json

**appsettings.json:**

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**appsettings.Production.json:**

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.AwsCloudWatch" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "AmazonCloudWatch",
        "Args": {
          "logGroup": "/aws/cloudbill/your-service",
          "logStreamPrefix": "your-service",
          "restrictedToMinimumLevel": "Information",
          "batchSizeLimit": 100,
          "batchUploadPeriodInSeconds": 15,
          "createLogGroup": true,
          "textFormatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

## Standard Properties

All logs automatically include these standard properties:

| Property | Description | Example |
|----------|-------------|---------|
| `ServiceName` | Name of your service | `CloudBill.API` |
| `ServiceVersion` | Version of your service | `1.0.0` |
| `Environment` | Environment name | `Development`, `Production` |
| `MachineName` | Server name | `IMPERIUM`, `ip-10-0-1-23` |
| `TraceId` | ASP.NET Core TraceId | `0HN7R8K9M...` |
| `CorrelationId` | Cross-service correlation ID | Same as TraceId or from `X-Correlation-ID` header |
| `UserId` | Authenticated user ID | `user123` (from claims) |
| `TenantId` | Tenant ID (if enabled) | `tenant-abc` (from claims or header) |
| `SourceContext` | Logging class/component | `CloudBill.API.Controllers.AccountsController` |

## Logging Conventions

### Log Levels

Use these levels consistently across all services:

```csharp
// VERBOSE - Very detailed (disabled by default)
Log.Verbose("Processing item {ItemId} with details {@Details}", id, details);

// DEBUG - Diagnostic information
Log.Debug("Cache hit for key {CacheKey}", key);

// INFORMATION - Normal flow events
Log.Information("Account {AccountId} created successfully", accountId);

// WARNING - Unexpected but handled situations
Log.Warning("API rate limit approaching for tenant {TenantId}", tenantId);

// ERROR - Errors and exceptions
Log.Error(ex, "Failed to process invoice {InvoiceId}", invoiceId);

// FATAL - Critical failures
Log.Fatal(ex, "Database connection failed on startup");
```

### Structured Logging

**Always use structured logging:**

```csharp
// ✅ GOOD - Structured
Log.Information("User {UserId} updated account {AccountId}", userId, accountId);

// ❌ BAD - String concatenation
Log.Information($"User {userId} updated account {accountId}");
```

### Property Naming Convention

Use **PascalCase** for all property names:

```csharp
// ✅ GOOD
Log.Information("Processing {OrderId} for {CustomerId}", orderId, customerId);

// ❌ BAD
Log.Information("Processing {order_id} for {customer_id}", orderId, customerId);
```

### Destructuring Complex Objects

Use `@` to destructure complex objects:

```csharp
// Captures full object structure
Log.Information("Created account {@Account}", account);

// vs just ToString()
Log.Information("Created account {Account}", account);
```

## Cross-Service Correlation

To trace requests across multiple services, use the `X-Correlation-ID` header:

```csharp
// Service A
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
await client.GetAsync("https://service-b/api/endpoint");

// Service B will automatically log with the same CorrelationId
```

### CloudWatch Query Example

Find all logs for a specific request across ALL services:

```
fields @timestamp, ServiceName, @message, TraceId, CorrelationId
| filter CorrelationId = "abc-123-def-456"
| sort @timestamp desc
```

## OpenTelemetry Integration

CloudBill.Logging automatically sets up OpenTelemetry for distributed tracing.

### Viewing Traces

In **development**, traces are exported to console. In **production**, configure OTLP endpoint:

```csharp
builder.AddCloudBillLogging(options =>
{
    options.OtlpEndpoint = "http://localhost:4317"; // OTLP Collector
});
```

### AWS X-Ray Integration

To use AWS X-Ray, deploy an ADOT (AWS Distro for OpenTelemetry) Collector:

```csharp
builder.AddCloudBillLogging(options =>
{
    options.OtlpEndpoint = "http://adot-collector:4317";
});
```

## Configuration Options

```csharp
public class CloudBillLoggingOptions
{
    // Service identification
    public string ServiceName { get; set; } = "CloudBill";
    public string ServiceVersion { get; set; } = "1.0.0";

    // Features
    public bool EnableOpenTelemetry { get; set; } = true;
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableCorrelationId { get; set; } = true;
    public bool EnableTenantId { get; set; } = false;
    public bool EnableUserId { get; set; } = true;

    // OpenTelemetry
    public string? OtlpEndpoint { get; set; }

    // Exclusions
    public List<string> ExcludeRequestPaths { get; set; } = new()
    {
        "/health", "/healthz", "/ready", "/live"
    };
}
```

## CloudWatch Queries Cookbook

### Find all errors in the last hour

```
fields @timestamp, @message, ServiceName, TraceId
| filter Level = "ERROR"
| filter @timestamp > ago(1h)
| sort @timestamp desc
```

### Find slow requests (>1 second)

```
fields @timestamp, RequestPath, Elapsed, StatusCode, TraceId
| filter RequestPath like /api/
| filter Elapsed > 1000
| sort Elapsed desc
```

### Count requests by endpoint

```
fields RequestPath
| stats count() by RequestPath
| sort count desc
```

### Find all logs for a specific user

```
fields @timestamp, @message, UserId, TraceId
| filter UserId = "user123"
| sort @timestamp desc
```

### Track a request across services

```
fields @timestamp, ServiceName, @message, CorrelationId
| filter CorrelationId = "your-correlation-id"
| sort @timestamp asc
```

## Multi-Tenant Support

Enable tenant tracking for multi-tenant applications:

```csharp
builder.AddCloudBillLogging(options =>
{
    options.EnableTenantId = true;
});
```

CloudBill.Logging will extract TenantId from:
1. `X-Tenant-ID` HTTP header
2. `tenant_id` claim in JWT

```csharp
// Query logs for a specific tenant
fields @timestamp, @message, TenantId, UserId
| filter TenantId = "tenant-abc"
| sort @timestamp desc
```

## PII and Security

**DO NOT log sensitive data:**

- ❌ Passwords
- ❌ Credit card numbers
- ❌ Social security numbers
- ❌ Full API keys/tokens
- ❌ Personal health information

**Safe to log:**

- ✅ User IDs (not usernames/emails unless required)
- ✅ Transaction IDs
- ✅ Account IDs
- ✅ Masked/truncated sensitive data
- ✅ Error messages (sanitized)

```csharp
// ❌ BAD
Log.Information("User logged in with password {Password}", password);

// ✅ GOOD
Log.Information("User {UserId} logged in successfully", userId);

// ✅ GOOD - Masked
var maskedCard = $"****-****-****-{last4}";
Log.Information("Payment processed with card {CardNumber}", maskedCard);
```

## Package on Internal NuGet Feed

To use across your organization, publish to your internal NuGet feed:

```bash
dotnet pack CloudBill.Logging.csproj -c Release
dotnet nuget push bin/Release/CloudBill.Logging.1.0.0.nupkg -s https://your-nuget-feed.com
```

Or use GitHub Packages, Azure Artifacts, or AWS CodeArtifact.

## Support

For questions or issues, contact the CloudBill Development Team or open an issue on GitHub.

## License

MIT License - Copyright (c) CloudBill
