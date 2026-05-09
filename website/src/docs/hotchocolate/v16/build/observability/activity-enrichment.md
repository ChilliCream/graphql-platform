---
title: Activity enrichment
---

Activity enrichment lets you add application context to the `Activity` spans that Hot Chocolate v16 creates with `.AddInstrumentation()`. Use this feature when your traces already show GraphQL activity, but you want to include safe tags such as correlation ID, tenant tier, client name, request source, or sanitized error categories.

Enrichment is not a replacement for logging, metrics, exporter setup, or background processing hooks. Enricher methods run synchronously during request processing, so they must be fast, minimize allocations, and avoid network or database calls.

# Setup

First, install `HotChocolate.Diagnostics` using the same version as your other Hot Chocolate packages.

<PackageInstallation packageName="HotChocolate.Diagnostics" />

Next, enable Hot Chocolate instrumentation on the GraphQL builder. Register your enricher as an application singleton and make it available to schema services:

```csharp
using HotChocolate.Diagnostics;

builder.Services.AddSingleton<ActivityEnricher, GraphQLActivityEnricher>();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddInstrumentation()
    .AddApplicationService<ActivityEnricher>();
```

To see exported spans in your traces, OpenTelemetry must subscribe to the Hot Chocolate activity source:

```csharp
using OpenTelemetry.Trace;

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());
```

For details on exporters, resources, sampling, and full tracing setup, see [OpenTelemetry](./opentelemetry).

# Minimal custom enricher

To create a custom enricher, define a type that derives from `ActivityEnricher`. In v16, the constructor receives `InstrumentationOptions`:

```csharp
using System.Diagnostics;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Diagnostics;
using Microsoft.AspNetCore.Http;

public sealed class GraphQLActivityEnricher(
    InstrumentationOptions options)
    : ActivityEnricher(options)
{
    public override void EnrichExecuteHttpRequest(
        HttpContext httpContext,
        HttpRequestKind kind,
        Activity activity)
    {
        var correlationId = GetCorrelationId(httpContext);

        if (correlationId is not null)
        {
            activity.SetTag("app.correlation_id", correlationId);
        }

        activity.SetTag("app.graphql.http_kind", kind.ToString());
    }

    private static string? GetCorrelationId(HttpContext httpContext)
    {
        var headerValue = httpContext.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault()
            ?.Trim();

        if (headerValue is { Length: > 0 and <= 128 } &&
            headerValue.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            return headerValue;
        }

        return httpContext.TraceIdentifier;
    }
}
```

Use `Activity.SetTag(...)` for attributes you want to search, filter, or group by. You may call `base` from overrides, but v16 does not require this for built-in GraphQL attributes. Hot Chocolate applies standard span attributes outside your custom enricher.

# Safety and cardinality rules

Trace attributes become part of your telemetry data surface. Treat them as you would stored operational data.

| Prefer                                                                          | Avoid                                                                   |
| ------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| Stable categories, booleans, small enums, tenant tier, client allow-list values | Raw headers, free-form input, emails, tokens, session ids, IP addresses |
| Operation name, document hash, trusted document id                              | Full GraphQL document text and variables                                |
| Error code, error category, retryable flag                                      | Exception message, stack trace, raw GraphQL error message               |
| DataLoader name, batch size, backend category                                   | DataLoader keys unless reviewed and bounded                             |
| Pseudonymous user hash or account tier                                          | Raw user id, name, email, claims, auth payloads                         |

High-cardinality tags increase backend costs and reduce the usefulness of grouping. Sensitive tags may be retained by exporters or trace backends longer than application logs.

# Add HTTP, client, and correlation tags

Use `EnrichExecuteHttpRequest(HttpContext, HttpRequestKind, Activity)` to add transport-level information. This method runs before all GraphQL operation details are available, so focus on request kind, client classification, correlation, and other transport-level values.

```csharp
public override void EnrichExecuteHttpRequest(
    HttpContext httpContext,
    HttpRequestKind kind,
    Activity activity)
{
    activity.SetTag("app.graphql.http_kind", kind.ToString());

    if (TryGetKnownClient(httpContext, out var clientName))
    {
        activity.SetTag("app.client.name", clientName);
    }

    activity.SetTag("app.correlation_id", httpContext.TraceIdentifier);
}

private static bool TryGetKnownClient(
    HttpContext httpContext,
    out string clientName)
{
    var value = httpContext.Request.Headers["X-Client-Name"]
        .FirstOrDefault();

    clientName = value switch
    {
        "mobile" => "mobile",
        "web" => "web",
        "partner" => "partner",
        _ => string.Empty
    };

    return clientName.Length > 0;
}
```

If you read distributed context or `Activity.Current.Baggage`, only copy validated, bounded values into tags. Hot Chocolate v16 does not provide a separate baggage-specific enricher API.

# Add request and tenant tags

For tenant or user context, normalize values before execution begins. An HTTP or WebSocket interceptor can validate request data and place safe values into global state. The enricher can then read from `RequestContext.ContextData`.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

public static class GraphQLStateKeys
{
    public const string TenantTier = "TenantTier";
    public const string RequestSource = "RequestSource";
}

public sealed class TenantHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        requestBuilder.SetGlobalState(
            GraphQLStateKeys.TenantTier,
            ResolveTenantTier(context));

        requestBuilder.TryAddGlobalState(
            GraphQLStateKeys.RequestSource,
            "public-api");

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }

    private static string ResolveTenantTier(HttpContext context)
        => context.User.HasClaim("plan", "enterprise")
            ? "enterprise"
            : "standard";
}
```

Read these normalized values in `EnrichExecuteRequest(RequestContext, Activity)`:

```csharp
using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;

public override void EnrichExecuteRequest(
    RequestContext context,
    Activity activity)
{
    if (context.ContextData.TryGetValue(
            GraphQLStateKeys.TenantTier,
            out var tier) &&
        tier is string tenantTier)
    {
        activity.SetTag("app.tenant.tier", tenantTier);
    }

    if (context.ContextData.TryGetValue(
            GraphQLStateKeys.RequestSource,
            out var source) &&
        source is string requestSource)
    {
        activity.SetTag("app.request.source", requestSource);
    }
}
```

Prefer using tier, region, deployment ring, or a pseudonymous user hash over raw tenant and user identifiers. Avoid tagging names, emails, tokens, raw claims, session IDs, or arbitrary headers.

# Add operation metadata without duplicating built-ins

Hot Chocolate v16 already emits core GraphQL attributes, including:

- `graphql.operation.type`
- `graphql.operation.name`
- `graphql.document.hash`
- `graphql.document.id`
- `graphql.error.count`
- `graphql.processing.type`

The root GraphQL span name is intentionally low cardinality, using the operation type when available (such as `query`, `mutation`, or `subscription`). The operation name is included as an attribute.

Use operation-phase hooks for application-specific classifications that Hot Chocolate cannot determine itself.

```csharp
public override void EnrichCompileOperation(
    RequestContext context,
    Activity activity)
{
    if (context.ContextData.TryGetValue("OperationCategory", out var value) &&
        value is string category)
    {
        activity.SetTag("app.operation.category", category);
    }
}
```

Common operation hooks include:

| Hook                                                            | Use for                                                  |
| --------------------------------------------------------------- | -------------------------------------------------------- |
| `EnrichParseDocument(RequestContext, Activity)`                 | Document parsing span context                            |
| `EnrichValidateDocument(RequestContext, Activity)`              | Validation span context                                  |
| `EnrichAnalyzeOperationCost(RequestContext, Activity)`          | Cost analysis phase context                              |
| `EnrichOperationCost(RequestContext, double, double, Activity)` | Safe classification based on field and type cost         |
| `EnrichCoerceVariables(RequestContext, Activity)`               | Variable coercion phase context, not raw variable values |
| `EnrichCompileOperation(RequestContext, Activity)`              | Operation compilation context                            |
| `EnrichExecuteOperation(RequestContext, Activity)`              | Operation execution context                              |

# Add resolver tags carefully

`EnrichResolveFieldValue(IMiddlewareContext, Activity)` runs for resolver field spans. Resolver spans can be high in volume, and `ActivityScopes.Default` includes `ResolveFieldValue` by default. Keep resolver enrichment minimal.

Hot Chocolate already emits field attributes such as:

- `graphql.field.alias`
- `graphql.field.path`
- `graphql.field.name`
- `graphql.field.parent_type`
- `graphql.field.schema_coordinate`

Add only bounded application classifications.

```csharp
using HotChocolate.Resolvers;

public override void EnrichResolveFieldValue(
    IMiddlewareContext context,
    Activity activity)
{
    if (context.Selection.Field.Name.Equals("products"))
    {
        activity.SetTag("app.data.domain", "catalog");
    }
}
```

Avoid raw arguments, result values, object IDs, serialized objects, per-user data, and expensive checks. If resolver-level tags make traces too large, remove the tags or reduce `InstrumentationOptions.Scopes`.

# Add DataLoader tags

Use `EnrichExecuteBatch<TKey>(IDataLoader, IReadOnlyList<TKey>, Activity)` for DataLoader batch spans. Hot Chocolate emits `graphql.dataloader.name` and `graphql.dataloader.batch.size`. If `IncludeDataLoaderKeys` is enabled, it also emits `graphql.dataloader.batch.keys`, which is often sensitive and high cardinality.

```csharp
using GreenDonut;

public override void EnrichExecuteBatch<TKey>(
    IDataLoader dataLoader,
    IReadOnlyList<TKey> keys,
    Activity activity)
{
    var backend = dataLoader.GetType().Name switch
    {
        "ProductsByIdDataLoader" => "sql",
        "InventoryBySkuDataLoader" => "cache",
        _ => "other"
    };

    activity.SetTag("app.dataloader.backend", backend);
}
```

Do not copy batch keys into custom tags. Use backend category, loader family, or a small batch classification instead.

# Add sanitized error metadata

Error hooks are useful for adding categories, retry information, and domain-safe codes. They are not intended for exception details.

```csharp
public override void EnrichRequestError(
    RequestContext context,
    IError error,
    Activity activity)
{
    var category = error.Code switch
    {
        "AUTH_NOT_AUTHORIZED" => "authorization",
        "TENANT_NOT_ALLOWED" => "tenant",
        "VALIDATION_FAILED" => "validation",
        _ => "graphql"
    };

    activity.SetTag("app.error.category", category);
    activity.SetTag("app.error.retryable", false);
}
```

Relevant error hooks include:

- `EnrichHttpRequestError(HttpContext, IError, Activity)`
- `EnrichHttpRequestError(HttpContext, Exception, Activity)`
- `EnrichParserErrors(HttpContext, IReadOnlyList<IError>, Activity)`
- `EnrichRequestError(RequestContext, Exception, Activity)`
- `EnrichRequestError(RequestContext, IError, Activity)`
- `EnrichValidationErrors(RequestContext, IReadOnlyList<IError>, Activity)`
- `EnrichResolverError(IMiddlewareContext, IError, Activity)`
- `EnrichBatchDispatchError(Exception, Activity)`

Hot Chocolate marks relevant spans as errors and sets `error.type` from the exception type, GraphQL error code, or a fallback value. The root GraphQL span can include `graphql.error.count`. Root `graphql.error` events are capped by `InstrumentationOptions.MaxErrorEvents`, which defaults to `10`. Set this to `0` to suppress root error events while keeping the count tag.

# Add bounded custom events

Tags describe a span, while events represent timestamped milestones within a span. Use events for a small number of application-specific milestones, not for logs or unbounded collections.

```csharp
public override void EnrichExecuteRequest(
    RequestContext context,
    Activity activity)
{
    if (context.ContextData.TryGetValue("TenantSource", out var value) &&
        value is string source)
    {
        var tags = new ActivityTagsCollection
        {
            ["app.tenant.source"] = source
        };

        activity.AddEvent(new ActivityEvent("tenant.resolved", tags: tags));
    }
}
```

Event names and attributes should follow the same privacy and cardinality rules as tags. Keep the number of events bounded. Hot Chocolate already emits selected framework events for error, document, and DataLoader lifecycle moments, so custom events should focus on application milestones.

# Conditional enrichment and overhead

Use conditions to skip unnecessary work when a tag is not needed.

```csharp
public override void EnrichResolveFieldValue(
    IMiddlewareContext context,
    Activity activity)
{
    if (!activity.IsAllDataRequested)
    {
        return;
    }

    if (context.Selection.Field.Name.Equals("products"))
    {
        activity.SetTag("app.data.domain", "catalog");
    }
}
```

Guidelines:

- Do not call external services from an enricher.
- Do not serialize variables, result objects, headers, or claims.
- Use constants for tag names.
- Prefer small `switch` expressions or precomputed request state.
- Be careful with resolver and DataLoader hooks, as they can run many times per operation.
- Use `InstrumentationOptions.Scopes` to disable spans and related hook calls you do not need.

# Built-in options that affect enrichment

| Option                  | Default                  | What it controls                                                                        | Recommendation                                                 |
| ----------------------- | ------------------------ | --------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `Scopes`                | `ActivityScopes.Default` | Which Hot Chocolate activities are created                                              | Start with default, then enable specific scopes for a question |
| `RequestDetails`        | `RequestDetails.Default` | Request ID, hash, operation name, extensions, variables, or document depending on flags | Review every included value before production export           |
| `IncludeDocument`       | `false`                  | Adds `graphql.document.body` from parsed document info                                  | Keep off in production unless approved                         |
| `IncludeDataLoaderKeys` | `false`                  | Adds `graphql.dataloader.batch.keys`                                                    | Keep off unless keys are safe and bounded                      |
| `MaxErrorEvents`        | `10`                     | Maximum root `graphql.error` events                                                     | Keep bounded, use `0` to suppress events                       |

`RequestDetails.Default` includes `Id`, `Hash`, `OperationName`, and `Extensions`. `RequestDetails.All` also includes `Variables` and `Document`. Variables, extensions, and documents can contain user or authentication-adjacent data.

Common scopes and their related hooks:

| Scope                | Hook impact                                       |
| -------------------- | ------------------------------------------------- |
| `ExecuteHttpRequest` | HTTP request span and HTTP request enricher hooks |
| `ParseHttpRequest`   | HTTP parsing span and parser error hooks          |
| `FormatHttpResponse` | HTTP response formatting span                     |
| `ExecuteRequest`     | GraphQL request execution span and request hooks  |
| `ParseDocument`      | Document parsing span                             |
| `ValidateDocument`   | Validation span and validation error hooks        |
| `AnalyzeComplexity`  | Cost analysis span and operation cost hooks       |
| `CoerceVariables`    | Variable coercion span                            |
| `CompileOperation`   | Operation compilation span                        |
| `ExecuteOperation`   | Operation execution span                          |
| `ResolveFieldValue`  | Resolver field spans and resolver hooks           |
| `DataLoaderBatch`    | DataLoader batch spans and dispatch hooks         |

# Hook reference

| Phase              | Method                                                                                      | Common use                                       |
| ------------------ | ------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| HTTP               | `EnrichExecuteHttpRequest(HttpContext, HttpRequestKind, Activity)`                          | Correlation, client classification, request kind |
| HTTP               | `EnrichSingleRequest(HttpContext, GraphQLRequest, Activity)`                                | Single GraphQL request metadata                  |
| HTTP               | `EnrichBatchRequest(HttpContext, IReadOnlyList<GraphQLRequest>, Activity)`                  | Batch classification without raw request bodies  |
| HTTP               | `EnrichOperationBatchRequest(HttpContext, GraphQLRequest, IReadOnlyList<string>, Activity)` | Operation batch classification                   |
| HTTP               | `EnrichParseHttpRequest(HttpContext, Activity)`                                             | HTTP parsing span metadata                       |
| HTTP               | `EnrichFormatHttpResponse(HttpContext, Activity)`                                           | Response formatting metadata                     |
| Execution          | `EnrichExecuteRequest(RequestContext, Activity)`                                            | Tenant tier, source, request classification      |
| Execution          | `EnrichParseDocument(RequestContext, Activity)`                                             | Document parsing classification                  |
| Execution          | `EnrichValidateDocument(RequestContext, Activity)`                                          | Validation classification                        |
| Execution          | `EnrichValidationErrors(RequestContext, IReadOnlyList<IError>, Activity)`                   | Sanitized validation category                    |
| Execution          | `EnrichAnalyzeOperationCost(RequestContext, Activity)`                                      | Cost phase classification                        |
| Execution          | `EnrichOperationCost(RequestContext, double, double, Activity)`                             | Bounded cost bucket                              |
| Execution          | `EnrichCoerceVariables(RequestContext, Activity)`                                           | Variable coercion phase metadata                 |
| Execution          | `EnrichCompileOperation(RequestContext, Activity)`                                          | Compilation and operation category               |
| Execution          | `EnrichExecuteOperation(RequestContext, Activity)`                                          | Operation execution category                     |
| Resolver           | `EnrichResolveFieldValue(IMiddlewareContext, Activity)`                                     | Cheap resolver classification                    |
| Resolver           | `EnrichResolverError(IMiddlewareContext, IError, Activity)`                                 | Sanitized resolver error category                |
| DataLoader         | `EnrichExecuteBatch<TKey>(IDataLoader, IReadOnlyList<TKey>, Activity)`                      | Backend or loader family classification          |
| DataLoader         | `EnrichRunBatchDispatchCoordinator(Activity)`                                               | Dispatch coordinator metadata                    |
| DataLoader         | `EnrichBatchDispatchError(Exception, Activity)`                                             | Sanitized batch dispatch error category          |
| Document and cache | `EnrichDocumentNotFoundInStorage(RequestContext, OperationDocumentId, Activity)`            | Trusted document miss classification             |
| Document and cache | `EnrichUntrustedDocumentRejected(RequestContext, Activity)`                                 | Rejected document classification                 |
| Document and cache | `EnrichAddedDocumentToCache(RequestContext, Activity)`                                      | Document cache classification                    |
| Document and cache | `EnrichAddedOperationToCache(RequestContext, Activity)`                                     | Operation cache classification                   |
| Subscription       | `EnrichOnSubscriptionEvent(RequestContext, ulong, Activity)`                                | Subscription event classification                |

# Test span tags

You can test enrichment by collecting activities with an `ActivityListener`. Focus your test on the tags you add, not on a vendor backend.

```csharp
using System.Diagnostics;
using HotChocolate.Diagnostics;
using Xunit;

[Fact]
public async Task ExecuteRequest_Should_AddTenantTierTag_When_TenantTierExists()
{
    // arrange
    var activities = new List<Activity>();
    using var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "HotChocolate.Diagnostics",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = activity => activities.Add(activity)
    };

    ActivitySource.AddActivityListener(listener);

    var executor = await new ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .AddInstrumentation()
        .AddApplicationService<ActivityEnricher>()
        .Services
        .AddSingleton<ActivityEnricher, GraphQLActivityEnricher>()
        .BuildServiceProvider()
        .GetRequiredService<IRequestExecutorResolver>()
        .GetRequestExecutorAsync();

    // act
    await executor.ExecuteAsync(
        OperationRequestBuilder.New()
            .SetDocument("{ products { name } }")
            .SetGlobalState(GraphQLStateKeys.TenantTier, "enterprise")
            .Build());

    // assert
    Assert.Contains(
        activities,
        activity => activity.Tags.Any(tag =>
            tag.Key == "app.tenant.tier" &&
            tag.Value == "enterprise"));
}
```

If your test project uses a shared server fixture, add the listener before the request and inspect stopped activities after the request completes.

# Troubleshooting

| Symptom                                      | Likely cause                                            | Solution                                                                             |
| -------------------------------------------- | ------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Custom enricher is never called              | `.AddInstrumentation()` is missing                      | Add instrumentation to the GraphQL builder                                           |
| Enricher is registered but not used          | `.AddApplicationService<ActivityEnricher>()` is missing | Cross-register the singleton with schema services                                    |
| No Hot Chocolate spans appear in the backend | OpenTelemetry is not listening to Hot Chocolate         | Add `.AddHotChocolateInstrumentation()` to tracing                                   |
| A specific hook is not called                | The related `ActivityScopes` flag is disabled           | Enable the specific scope or move the tag to an enabled phase                        |
| Resolver tags create too much telemetry      | Resolver spans are high volume                          | Remove resolver tags or disable `ResolveFieldValue`                                  |
| DataLoader keys appear                       | `IncludeDataLoaderKeys` is enabled                      | Disable it unless keys are approved for export                                       |
| Raw documents or variables appear            | Verbose request details are enabled                     | Disable `RequestDetails.Document`, `RequestDetails.Variables`, and `IncludeDocument` |
| Tags are missing in the backend              | Sampler, exporter, or backend filters dropped them      | Check local console or OTLP output and backend attribute filters                     |
| Tag value is missing                         | The value was `null` or unsupported                     | Set tags only when a safe non-null value exists                                      |
| Error events are noisy                       | `MaxErrorEvents` is too high for the workload           | Lower the cap or set `MaxErrorEvents = 0`                                            |

# When diagnostic events are a better fit

Choose diagnostic events instead of activity enrichment when you need to:

- Write custom logs with `ILogger`.
- Publish counters or histograms with `Meter`.
- Run in-process policy code at lifecycle boundaries.
- Perform queued or asynchronous work after a diagnostic moment.
- Observe lifecycle events without creating or modifying spans.

Hot Chocolate v16 provides `ServerDiagnosticEventListener`, `ExecutionDiagnosticEventListener`, and `DataLoaderDiagnosticEventListener`. Register listeners with `.AddDiagnosticEventListener<T>()`. See [Diagnostic events](./diagnostic-events) for listener patterns.

# Next steps

- See [Observability](./) for the full Hot Chocolate observability map.
- Visit [OpenTelemetry](./opentelemetry) for exporters, resources, sampling, and span attributes.
- Read [Diagnostic events](./diagnostic-events) for custom logs, metrics, and lifecycle listeners.
- Review [Interceptors](../server-configuration/interceptors) and [Global state](../server-configuration/global-state) for normalizing request metadata before execution.
- Learn about [Request context](../execution-engine/request-context) for `RequestContext.ContextData` and execution state.
- Explore [DataLoader](../dataloader) for batching behavior.
- Check [Trusted documents](../security/trusted-documents) for document IDs, hashes, and untrusted document rejection.
- See [Performance](../performance) for tips on reducing overhead in production.
