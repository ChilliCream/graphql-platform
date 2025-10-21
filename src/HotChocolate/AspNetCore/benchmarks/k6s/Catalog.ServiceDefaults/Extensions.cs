using System.Diagnostics;
using System.Diagnostics.Metrics;
using GreenDonut;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddMeter("GreenDonut.DataLoader");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("GreenDonut.DataLoader")
                    .AddAspNetCoreInstrumentation(t =>
                        t.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

public class DataLoaderEvents : DataLoaderDiagnosticEventListener
{
    private static readonly ActivitySource s_activitySource = new("GreenDonut.DataLoader", "1.0.0");
    private static readonly Meter s_meter = new("GreenDonut.DataLoader", "1.0.0");

    private static readonly Counter<long> s_batchesExecuted =
        s_meter.CreateCounter<long>("dataloader.batches.executed", description: "Number of batches executed");

    private static readonly Counter<long> s_batchesSucceeded =
        s_meter.CreateCounter<long>("dataloader.batches.succeeded", description: "Number of batches that succeeded");

    private static readonly Counter<long> s_batchesFailed =
        s_meter.CreateCounter<long>("dataloader.batches.failed", description: "Number of batches that failed");

    private static readonly Histogram<int> s_batchSize =
        s_meter.CreateHistogram<int>("dataloader.batch.size", description: "Number of items in a batch");

    private static readonly Counter<long> s_cacheHits =
        s_meter.CreateCounter<long>("dataloader.cache.hits", description: "Number of items resolved from cache");

    private static readonly Counter<long> s_itemsSucceeded =
        s_meter.CreateCounter<long>("dataloader.items.succeeded", description: "Number of items that succeeded");

    private static readonly Counter<long> s_itemsFailed =
        s_meter.CreateCounter<long>("dataloader.items.failed", description: "Number of items that failed");

    public override IDisposable ExecuteBatch<TKey>(IDataLoader dataLoader, IReadOnlyList<TKey> keys)
    {
        s_batchesExecuted.Add(1);
        s_batchSize.Record(keys.Count);

        var activity = s_activitySource.StartActivity("ExecuteBatch");
        activity?.SetTag("dataloader.name", dataLoader.GetType().Name);
        activity?.SetTag("batch.size", keys.Count);

        return activity ?? EmptyScope;
    }

    public override void BatchResults<TKey, TValue>(IReadOnlyList<TKey> keys, ReadOnlySpan<Result<TValue?>> values) where TValue : default
    {
        s_batchesSucceeded.Add(1);

        var successCount = 0;
        var errorCount = 0;

        foreach (var result in values)
        {
            if (result.Kind == ResultKind.Error)
            {
                errorCount++;
            }
            else
            {
                successCount++;
            }
        }

        if (successCount > 0)
        {
            s_itemsSucceeded.Add(successCount);
        }

        if (errorCount > 0)
        {
            s_itemsFailed.Add(errorCount);
        }

        base.BatchResults(keys, values);
    }

    public override void BatchError<TKey>(IReadOnlyList<TKey> keys, Exception error)
    {
        s_batchesFailed.Add(1);
        Activity.Current?.AddException(error);
        base.BatchError(keys, error);
    }

    public override void ResolvedTaskFromCache(IDataLoader dataLoader, PromiseCacheKey cacheKey, Task task)
    {
        s_cacheHits.Add(1);
        base.ResolvedTaskFromCache(dataLoader, cacheKey, task);
    }
}
