# CloudBill.Logging

[![NuGet](https://img.shields.io/nuget/v/CloudBill.Logging.svg)](https://github.com/cloudbillhq/CloudBill.Logging/packages)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Organization-wide logging standard for CloudBill services.**

Provides consistent, structured logging across all CloudBill .NET applications using **Serilog** and **OpenTelemetry** with built-in support for:

- Structured logging with Serilog
- Distributed tracing with OpenTelemetry
- Correlation ID tracking across services
- AWS CloudWatch integration
- **Web Applications** (ASP.NET Core)
- **Worker Services** (Background services, ECS tasks)
- Tenant and User ID enrichment

## Installation

```bash
dotnet add package CloudBill.Logging --version 1.1.0
```

Add the GitHub Packages source to your `nuget.config`:

```xml
<packageSources>
  <add key="github-cloudbill" value="https://nuget.pkg.github.com/cloudbillhq/index.json" />
</packageSources>
```

## Quick Start

### Web Applications

```csharp
using CloudBill.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddCloudBillLogging(options =>
{
    options.ServiceName = "MyWebApi";
    options.ServiceVersion = "1.0.0";
});

var app = builder.Build();
app.UseCloudBillRequestLogging();
app.Run();
```

### Worker Services (ECS, Background Services)

```csharp
using CloudBill.Logging.Context;
using CloudBill.Logging.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddCloudBillLogging(options =>
{
    options.ServiceName = "MyWorker";
    options.ServiceVersion = "1.0.0";
    options.UseJsonConsoleOutput = true; // JSON for CloudWatch
});

var host = builder.Build();
await host.RunAsync();
```

### Using LoggingContext in Workers

Since workers don't have HttpContext, use `LoggingContext` to set correlation data:

```csharp
using CloudBill.Logging.Context;

// All logs within this scope include CorrelationId, TenantId, etc.
using (LoggingContext.Push(
    correlationId: message.RequestId,
    tenantId: message.ClientCode,
    batchId: message.BatchId,
    documentId: message.DocumentId))
{
    _logger.LogInformation("Processing document");
    await ProcessAsync(message);
}
```

## Configuration Options

```csharp
builder.AddCloudBillLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.UseJsonConsoleOutput = true;     // JSON for CloudWatch
    options.EnableOpenTelemetry = true;      // Distributed tracing
    options.EnableCorrelationId = true;      // Track across services
    options.EnableTenantId = true;           // Multi-tenant support
    options.OtlpEndpoint = "http://...";     // OpenTelemetry collector
});
```

## CloudWatch Logs Queries

```sql
-- Find all logs for a request
fields @timestamp, @message, TenantId, DocumentId
| filter CorrelationId = "abc-123"
| sort @timestamp desc

-- Errors by tenant
fields @timestamp, @message
| filter TenantId = "ACME" and Level = "Error"
| sort @timestamp desc
```

## Documentation

See [detailed documentation](src/CloudBill.Logging/README.md) for:
- Full configuration options
- CloudWatch query examples
- PII and security guidelines
- OpenTelemetry integration

## License

MIT License - Copyright (c) CloudBill 2026
