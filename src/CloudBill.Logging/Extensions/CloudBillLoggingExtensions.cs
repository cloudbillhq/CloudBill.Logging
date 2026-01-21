using CloudBill.Logging.Configuration;
using CloudBill.Logging.Enrichers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CloudBill.Logging.Extensions;

/// <summary>
/// Extension methods for adding CloudBill standard logging to applications.
/// </summary>
public static class CloudBillLoggingExtensions
{
    /// <summary>
    /// Adds CloudBill standard logging with Serilog and OpenTelemetry for Web Applications.
    /// Call this in Program.cs before building the app.
    /// </summary>
    /// <example>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddCloudBillLogging(opts => opts.ServiceName = "MyWebApi");
    /// </example>
    public static WebApplicationBuilder AddCloudBillLogging(
        this WebApplicationBuilder builder,
        Action<CloudBillLoggingOptions>? configure = null)
    {
        var options = new CloudBillLoggingOptions();
        configure?.Invoke(options);

        // Register HttpContextAccessor for enrichers
        builder.Services.AddHttpContextAccessor();

        // Configure Serilog for web apps
        ConfigureSerilogForWeb(builder.Host, options, builder.Services);

        // Configure OpenTelemetry if enabled
        if (options.EnableOpenTelemetry)
        {
            ConfigureOpenTelemetry(builder.Services, options);
        }

        return builder;
    }

    /// <summary>
    /// Adds CloudBill standard logging with Serilog and OpenTelemetry for Worker Services.
    /// Uses AsyncLocal-based LoggingContext instead of HttpContext for correlation tracking.
    /// Call this in Program.cs before building the app.
    /// </summary>
    /// <example>
    /// var builder = Host.CreateApplicationBuilder(args);
    /// builder.AddCloudBillLogging(opts =>
    /// {
    ///     opts.ServiceName = "BillGen.Worker";
    ///     opts.UseJsonConsoleOutput = true;
    /// });
    /// </example>
    public static HostApplicationBuilder AddCloudBillLogging(
        this HostApplicationBuilder builder,
        Action<CloudBillLoggingOptions>? configure = null)
    {
        var options = new CloudBillLoggingOptions();
        configure?.Invoke(options);

        // Configure Serilog for worker services
        ConfigureSerilogForWorker(builder, options);

        // Configure OpenTelemetry if enabled (without ASP.NET Core instrumentation)
        if (options.EnableOpenTelemetry)
        {
            ConfigureOpenTelemetryForWorker(builder.Services, options);
        }

        return builder;
    }

    /// <summary>
    /// Adds CloudBill standard logging with Serilog and OpenTelemetry using IHostBuilder.
    /// Use this for console apps or when using generic host directly.
    /// </summary>
    /// <example>
    /// Host.CreateDefaultBuilder(args)
    ///     .AddCloudBillLogging(opts => opts.ServiceName = "MyService")
    ///     .Build();
    /// </example>
    public static IHostBuilder AddCloudBillLogging(
        this IHostBuilder hostBuilder,
        Action<CloudBillLoggingOptions>? configure = null)
    {
        var options = new CloudBillLoggingOptions();
        configure?.Invoke(options);

        // Create bootstrap logger
        CreateBootstrapLogger(options);

        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);

            ConfigureSerilogBase(configuration, context.HostingEnvironment.EnvironmentName, options);

            // Use LoggingContext enricher for worker services
            configuration.Enrich.With(new LoggingContextEnricher());
            configuration.Enrich.With(new ServiceNameEnricher(options.ServiceName));

            // Add console sink with JSON format for CloudWatch
            if (options.UseJsonConsoleOutput)
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                configuration.WriteTo.Console();
            }
        });

        return hostBuilder;
    }

    private static void ConfigureSerilogForWeb(
        IHostBuilder hostBuilder,
        CloudBillLoggingOptions options,
        IServiceCollection services)
    {
        // Create bootstrap logger
        CreateBootstrapLogger(options);

        hostBuilder.UseSerilog((context, serviceProvider, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(serviceProvider);

            ConfigureSerilogBase(configuration, context.HostingEnvironment.EnvironmentName, options);

            // Add HTTP-based enrichers for web apps
            if (options.EnableCorrelationId)
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                configuration.Enrich.With(new CorrelationIdEnricher(httpContextAccessor));
            }

            if (options.EnableTenantId)
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                configuration.Enrich.With(new TenantIdEnricher(httpContextAccessor));
            }

            if (options.EnableUserId)
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                configuration.Enrich.With(new UserIdEnricher(httpContextAccessor));
            }

            configuration.Enrich.With(new ServiceNameEnricher(options.ServiceName));

            // Add console sink with JSON format for CloudWatch
            if (options.UseJsonConsoleOutput)
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                configuration.WriteTo.Console();
            }
        });
    }

    private static void ConfigureSerilogForWorker(
        HostApplicationBuilder builder,
        CloudBillLoggingOptions options)
    {
        // Create bootstrap logger
        CreateBootstrapLogger(options);

        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services);

            ConfigureSerilogBase(configuration, builder.Environment.EnvironmentName, options);

            // Use LoggingContext enricher for worker services (AsyncLocal-based)
            configuration.Enrich.With(new LoggingContextEnricher());
            configuration.Enrich.With(new ServiceNameEnricher(options.ServiceName));

            // Add console sink with JSON format for CloudWatch
            if (options.UseJsonConsoleOutput)
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                configuration.WriteTo.Console();
            }
        });
    }

    private static void CreateBootstrapLogger(CloudBillLoggingOptions options)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Log.Information("Starting {ServiceName} v{ServiceVersion}", options.ServiceName, options.ServiceVersion);
    }

    private static void ConfigureSerilogBase(
        LoggerConfiguration configuration,
        string environmentName,
        CloudBillLoggingOptions options)
    {
        configuration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", options.ServiceName)
            .Enrich.WithProperty("ServiceVersion", options.ServiceVersion)
            .Enrich.WithProperty("Environment", environmentName);
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services, CloudBillLoggingOptions options)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(options.ServiceName)
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !options.ExcludeRequestPaths.Any(excluded =>
                                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
                        };
                        opts.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                // Add OTLP exporter if endpoint is configured
                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
                else
                {
                    // Use console exporter for development
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                // Only add exporter if OTLP endpoint is configured
                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });
    }

    private static void ConfigureOpenTelemetryForWorker(IServiceCollection services, CloudBillLoggingOptions options)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(options.ServiceName)
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });
    }

    /// <summary>
    /// Adds CloudBill standard request logging middleware.
    /// Call this after app.UseCors() and before endpoints.
    /// Only applicable to web applications.
    /// </summary>
    public static IApplicationBuilder UseCloudBillRequestLogging(
        this IApplicationBuilder app,
        CloudBillLoggingOptions? options = null)
    {
        options ??= new CloudBillLoggingOptions();

        if (!options.EnableRequestLogging)
        {
            return app;
        }

        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            opts.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode > 499
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode > 399
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;

            opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

                if (options.EnableCorrelationId)
                {
                    var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                      ?? httpContext.TraceIdentifier;

                    if (!httpContext.Response.HasStarted)
                    {
                        httpContext.Response.Headers["X-Correlation-ID"] = correlationId;
                    }

                    diagnosticContext.Set("CorrelationId", correlationId);
                }
            };
        });

        return app;
    }

    /// <summary>
    /// Ensures Serilog is properly flushed on application shutdown.
    /// Call this after app.Run() in the finally block.
    /// </summary>
    public static void CloseCloudBillLogging()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Async version of CloseCloudBillLogging.
    /// </summary>
    public static async Task CloseCloudBillLoggingAsync()
    {
        await Log.CloseAndFlushAsync();
    }
}
