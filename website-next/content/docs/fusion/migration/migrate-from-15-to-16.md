---
title: Migrate Hot Chocolate Fusion from 15 to 16
---

> Note: While directives and behavior largly mirror v15, v16 is a complete re-implementation of Fusion that not only affects the gateway itself, but also the archive format and composition process. Therefore, you can't simply bump the package versions in the gateway and be done with the update. You'll need a coordinated strategy to incrementally adopt Fusion v2 in Subgraphs and their deployment process, before you can switch the gateway to v16.

<!-- TODO: High level overview over migration steps -->

Start by installing the latest `16.x.x` version of **all** of the `HotChocolate.Fusion.*` packages referenced by your project. The gateway runtime now ships in a single ASP.NET Core meta-package, `HotChocolate.Fusion.AspNetCore`, which includes the execution engine, the type system, and the ASP.NET Core integration. This means you can replace your existing references to `HotChocolate.AspNetCore` and `HotChocolate.Fusion` with a single reference to `HotChocolate.Fusion.AspNetCore`:

```diff
-<PackageReference Include="HotChocolate.AspNetCore" Version="15.x.x" />
-<PackageReference Include="HotChocolate.Fusion" Version="15.x.x" />
+<PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.x.x" />
```

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## AddFusionGatewayServer renamed to AddGraphQLGatewayServer

The entry point that adds a Fusion gateway to the service collection has been renamed and now lives in the `Microsoft.Extensions.DependencyInjection` namespace.

```diff
-builder.Services.AddFusionGatewayServer();
+builder.Services.AddGraphQLGatewayServer();
```

The builder type returned by `AddGraphQLGatewayServer` is now `IFusionGatewayBuilder` instead of the concrete `FusionGatewayBuilder`. All of the configuration extension methods now hang off this interface.

## CoreBuilder is gone — methods now hang off IFusionGatewayBuilder directly

In v15, the Fusion gateway builder exposed a `CoreBuilder` property of type `IRequestExecutorBuilder` that you used to reach Hot Chocolate's core configuration APIs (validation rules, error filters, etc.).

In v16 there is no separate underlying request executor builder. The Fusion gateway is configured exclusively via `IFusionGatewayBuilder`, and all relevant Hot Chocolate APIs (such as `DisableIntrospection`, `AddErrorFilter`, `AddSha256DocumentHashProvider`, etc.) are exposed directly on `IFusionGatewayBuilder` as Fusion-specific extension methods.

```diff
-gatewayBuilder.CoreBuilder.DisableIntrospection();
+gatewayBuilder.DisableIntrospection();
```

## ModifyFusionOptions split into ModifyOptions, ModifyRequestOptions and ModifyPlannerOptions

`FusionOptions` and `RequestExecutorOptions` no longer exist as Fusion configuration surfaces. The settings have been split across three dedicated option types and three matching `Modify*` methods on `IFusionGatewayBuilder`:

| v15 surface            | v16 surface                                      |
| ---------------------- | ------------------------------------------------ |
| `ModifyFusionOptions`  | `ModifyOptions` (cache sizes, error handling, …) |
| `ModifyRequestOptions` | `ModifyRequestOptions` (per-request settings)    |
| —                      | `ModifyPlannerOptions` (planner guardrails)      |

```diff
-gatewayBuilder
-    .ModifyFusionOptions(o =>
-    {
-        o.AllowQueryPlan = true;
-        o.IncludeDebugInfo = true;
-    })
-    .ModifyRequestOptions(o =>
-    {
-        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
-        o.PersistedOperations.OnlyAllowPersistedDocuments = false;
-        o.IncludeExceptionDetails = true;
-    });
+gatewayBuilder
+    .ModifyOptions(o =>
+    {
+        o.OperationDocumentCacheSize = 200;
+        o.OperationExecutionPlanCacheSize = 100;
+    })
+    .ModifyRequestOptions(o =>
+    {
+        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
+        o.PersistedOperations.OnlyAllowPersistedDocuments = false;
+        o.IncludeExceptionDetails = true;
+        o.AllowOperationPlanRequests = true;
+    });
```

## Internal directives hidden from schema endpoint

Previously, the `/graphql/schema.graphql` endpoint was returning the schema containing internal directives like `@authorize`. Starting with v16 the endpoint no longer includes internal directives by default.

If you need to retain the previous behavior, set `DisableInternalDirectives` to `true` through `ModifyOptions`. This treats every directive as public, even directives that explicitly call `Internal()` and regardless of `DefaultDirectiveVisibility`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.DisableInternalDirectives = true);
```

Be aware that internal directives may carry sensitive information (for example, authorization policies attached via `@authorize`). Only enable this if you understand and accept that risk.

## Cache configuration

In v15, the operation cache acted as the cache for operation plans. v16 introduces a dedicated operation plan cache.
Both document and operation plan cache are now configured on the gateway builder via `ModifyOptions` instead of as global services on the `IServiceCollection`:

```diff
-builder.Services.AddDocumentCache(capacity: 200);
-builder.Services.AddOperationCache(capacity: 100);

builder.Services
    .AddGraphQLGatewayServer()
+    .ModifyOptions(o =>
+    {
+        o.OperationDocumentCacheSize = 200;
+        o.OperationExecutionPlanCacheSize = 100;
+    });
```

If your application contains multiple Fusion gateways, the cache configuration has to be repeated for each one as the configuration is now scoped to a particular gateway.

## Document hash provider configuration

Document hash providers are no longer registered through the `IServiceCollection`. Move the call to `IFusionGatewayBuilder` instead:

```diff
-builder.Services.AddSha256DocumentHashProvider();

builder.Services
    .AddGraphQLGatewayServer()
+    .AddSha256DocumentHashProvider();
```

The same applies to `AddMD5DocumentHashProvider` and `AddSha1DocumentHashProvider`.

## Eager initialization by default

Previously, the Fusion gateway constructed the schema and the request executor on the first request. To get eager initialization, you had to opt in via `InitializeOnStartup` on the underlying `CoreBuilder`.

In v16, eager initialization is the default. The schema and the request executor are constructed during application startup, before Kestrel begins accepting traffic. Schema errors surface immediately when you start the gateway, rather than only when the first request arrives.

If you used `InitializeOnStartup`, remove it. If you also passed a warmup delegate, migrate it to `AddWarmupTask`:

```diff
-gatewayBuilder.CoreBuilder
-    .InitializeOnStartup(warmup: (executor, ct) => /* ... */);
+gatewayBuilder.AddWarmupTask((executor, ct) => /* ... */);
```

If you really need lazy initialization, opt out via `ModifyOptions`:

```csharp
gatewayBuilder.ModifyOptions(o => o.LazyInitialization = true);
```

## Server options now configured via ModifyServerOptions

`GraphQLServerOptions` (GET requests, multipart uploads, batching, schema requests, the embedded Nitro tool, etc.) are now configured at the schema level using `ModifyServerOptions` on `IFusionGatewayBuilder` instead of per-endpoint:

```diff
-app.MapGraphQL().WithOptions(new GraphQLServerOptions
-{
-    EnableBatching = true,
-    Tool = { Enable = false }
-});
+gatewayBuilder.ModifyServerOptions(o =>
+{
+    o.Batching = AllowedBatching.All;
+    o.Tool.Enable = false;
+});
+
+app.MapGraphQL();
```

Per-endpoint overrides are still supported but now use a delegate pattern instead of an object initializer:

```csharp
app.MapGraphQL().WithOptions(o => o.EnableGetRequests = false);
```

## Batching is now disabled by default

In v15, request batching was enabled by default (`EnableBatching = true`). In v16, batching is **disabled by default** as a security measure. The `EnableBatching` property has been replaced by `Batching`, which uses the `AllowedBatching` flags enum for fine-grained control:

```diff
-options.EnableBatching = true;
+options.Batching = AllowedBatching.All;
```

If you were relying on the previous default, you need to explicitly enable batching:

```csharp
gatewayBuilder.ModifyServerOptions(o => o.Batching = AllowedBatching.All);
```

A new `MaxBatchSize` property limits the number of operations in a single batch. The default is **1024**. Set it to `0` for unlimited.

## Configuration provider API

The configuration provider abstractions used to load and watch the Fusion gateway configuration document have been redesigned around `IFusionConfigurationProvider`.

### IObservable\<GatewayConfiguration> replaced by IFusionConfigurationProvider

The old `IObservable<GatewayConfiguration>` source has been replaced by the new `IFusionConfigurationProvider` interface, which combines `IObservable<FusionConfiguration>` with `IAsyncDisposable` and exposes the latest configuration via a `Configuration` property. The configuration payload type is now `FusionConfiguration` (a `DocumentNode` plus a `JsonDocumentOwner` for schema settings) instead of `GatewayConfiguration`.

```diff
-public class CustomConfigurationProvider : IObservable<GatewayConfiguration>
-{
-    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer) => /* ... */;
-}
+public class CustomConfigurationProvider : IFusionConfigurationProvider
+{
+    public FusionConfiguration? Configuration => /* latest snapshot */;
+
+    public IDisposable Subscribe(IObserver<FusionConfiguration> observer) => /* ... */;
+
+    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
+}
```

### RegisterGatewayConfiguration → AddConfigurationProvider

```diff
-gatewayBuilder.RegisterGatewayConfiguration(sp => new CustomConfigurationProvider());
+gatewayBuilder.AddConfigurationProvider(sp => new CustomConfigurationProvider());
```

### ConfigureFromFile → AddFileSystemConfiguration

```diff
-gatewayBuilder.ConfigureFromFile("gateway.fgp");
+gatewayBuilder.AddFileSystemConfiguration("gateway.fgp");
```

The `watchFileForUpdates` parameter is gone — file watching is the default behavior of the file-system configuration provider.

### ConfigureFromDocument → AddInMemoryConfiguration

```diff
-gatewayBuilder.ConfigureFromDocument(documentNode);
+gatewayBuilder.AddInMemoryConfiguration(documentNode);
```

`AddInMemoryConfiguration` also accepts an optional `JsonDocumentOwner` for the schema settings.

## Nitro integration

Update all `ChilliCream.Nitro.*` packages to the same version as the `HotChocolate.Fusion.AspNetCore` package. If you're referencing any packages besides `ChilliCream.Nitro.Fusion` change the package references as shown below:

| Old                            | New                                        |
| ------------------------------ | ------------------------------------------ |
| `ChilliCream.Nitro.Core`       | `ChilliCream.Nitro.GraphQL`                |
| `ChilliCream.Nitro`            | `ChilliCream.Nitro.HotChocolate`           |
| `ChilliCream.Nitro.Telemetry`  | `ChilliCream.Nitro.OpenTelemetry`          |
| `ChilliCream.Nitro.Azure.Core` | `ChilliCream.Nitro.Azure`                  |
| `ChilliCream.Nitro.*.Azure`    | `ChilliCream.Nitro.Azure` (single package) |

> Note: If you are self-hosting the Nitro backend, make sure to update it to the latest version as well. `10.1.0` is the minimum version required to work with the `ChilliCream.Nitro.*` packages.

v16 changes how Nitro is configured instead of the per gateway configuration `ConfigureFromCloud` call, you configure Nitro once on the service collection and then apply optional per gateway setting overrides.

**Before**

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(o =>
    {
        o.ApiId = "...";
        o.ApiKey = "...";
        o.Stage = "...";
    });
```

**After**

```csharp
builder.Services
    .AddNitro(o =>
    {
        o.ApiId = "...";
        o.ApiKey = "...";
        o.Stage = "...";
    })
    .AddFusion();

builder.Services.AddGraphQLGatewayServer();
```

Per gateway settings can be overridden via the `ModifyNitroOptions` API. There have also been some changes to the structure of options.

**Before**

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(o =>
    {
        o.EnablePersistedQueries = true;
        o.DefaultQueryCacheExpiration = TimeSpan.FromSeconds(30);
        o.NotFoundQueryCacheExpiration = TimeSpan.FromSeconds(10);

        o.LocalFusionConfigurationFile = "gateway.fgp";

        o.Metrics.Enabled = true;
        o.Metrics.ExportIntervalMilliseconds = 1000;
        o.Metrics.ExportTimeoutMilliseconds = 400;

        o.EnableOperationReporting = true;
    });
```

**After**

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyNitroOptions(o =>
    {
        o.PersistedOperations.Enabled = true;
        o.PersistedOperations.DefaultQueryCacheExpiration = TimeSpan.FromSeconds(30);
        o.PersistedOperations.NotFoundQueryCacheExpiration = TimeSpan.FromSeconds(10);

        o.LocalFusionConfigurationFile = "gateway.fgp";

        o.Metrics.Enabled = true;
        o.Metrics.ExportIntervalMilliseconds = 1000;
        o.Metrics.ExportTimeoutMilliseconds = 400;

        o.OperationReporting.Enabled = true;
    });
```

If you were previously registering an asset cache you now do that on the `INitroBuilder` returned from `AddNitro()`:

**Before**

```csharp
builder.Services
    .AddFusionGatewayServer()
    .AddFileSystemAssetCache()
    // or
    .AddBlobStorageAssetCache()
    // or
    .AddAssetCache<CustomAssetCache>()
```

**After**

```csharp
builder.Services
    .AddNitro()
    .AddFusion()
    .AddFileSystemAssetCache()
    // or
    .AddBlobStorageAssetCache()
    // or
    .AddAssetCache<CustomAssetCache>()
```

If you were previously hand-rolling the `ConfigureOpenTelemetry*Provider(...)` configuration with `AddNitroExporter()`, you can also switch to a new `AddOpenTelemetry()` API on the `INitroBuilder` that handles the registration for you.

**Before**

```csharp
services.ConfigureOpenTelemetryMeterProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryTracerProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryLoggerProvider(x => x.AddNitroExporter());
```

**After**

```csharp
services
    .AddNitro()
    // ...
    .AddOpenTelemetry();
```

## Diagnostic listener API redesigned

Fusion diagnostics were redesigned in v16. The high-level `ExecuteFederatedQuery`, `ResolveError`, `ResolveByKeyBatchError`, `QueryPlanExecutionError`, and `SubgraphRequestError` hooks are gone. The new API is execution-stage specific.

| Before                                                                    | After                                                                                           |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `HotChocolate.Fusion.Execution.Diagnostic.IFusionDiagnosticEventListener` | `HotChocolate.Fusion.Diagnostics.IFusionExecutionDiagnosticEventListener`                       |
| `FusionDiagnosticEventListener` (base class)                              | `FusionExecutionDiagnosticEventListener` (base class)                                           |
| `ExecuteFederatedQuery(IRequestContext)`                                  | `ExecuteRequest(RequestContext)`                                                                |
| `QueryPlanExecutionError(Exception)`                                      | `PlanOperationError(RequestContext, string operationId, Exception)`                             |
| `ResolveError(Exception)` / `ResolveByKeyBatchError(Exception)`           | `ExecutionNodeError(OperationPlanContext, ExecutionNode, Exception)`                            |
| `SubgraphRequestError(string subgraphName, Exception)`                    | `SourceSchemaTransportError(OperationPlanContext, ExecutionNode, string schemaName, Exception)` |

```diff
-using HotChocolate.Fusion.Execution.Diagnostic;
+using HotChocolate.Fusion.Diagnostics;
+using HotChocolate.Fusion.Execution;
+using HotChocolate.Fusion.Execution.Nodes;

-public class DiagnosticEventListener : FusionDiagnosticEventListener
+public class DiagnosticEventListener : FusionExecutionDiagnosticEventListener
 {
-    public override IDisposable ExecuteFederatedQuery(IRequestContext context)
-        => base.ExecuteFederatedQuery(context);
+    public override IDisposable ExecuteRequest(RequestContext context)
+        => base.ExecuteRequest(context);

-    public override void QueryPlanExecutionError(Exception exception)
-        => base.QueryPlanExecutionError(exception);
+    public override void PlanOperationError(RequestContext context, string operationId, Exception error)
+        => base.PlanOperationError(context, operationId, error);

-    public override void SubgraphRequestError(string subgraphName, Exception exception)
-        => base.SubgraphRequestError(subgraphName, exception);
+    public override void SourceSchemaTransportError(OperationPlanContext context, ExecutionNode node, string schemaName, Exception error)
+        => base.SourceSchemaTransportError(context, node, schemaName, error);
 }
```

The new interface also exposes additional execution-stage hooks (`AddedOperationPlanToCache`, `SourceSchemaStoreError`, `SubscriptionEventError`).

### Scoped duration and error hooks

In v15, the only scoped (`IDisposable`-returning) hook on `IFusionDiagnosticEventListener` was `ExecuteFederatedQuery`, which wrapped the entire federated request. v16 broadens the scope significantly: each major execution stage and each individual execution node has its own `IDisposable`-returning hook on `IFusionExecutionDiagnosticEventListener`, so you can measure the duration of, for example, planning an operation or a single subgraph fetch in isolation. The error hooks have likewise been redesigned around the new node-based execution model.

| v15                                                    | v16                                                                                                     |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| `ExecuteFederatedQuery(IRequestContext)`               | `ExecuteRequest(RequestContext)`                                                                        |
| —                                                      | `PlanOperation(RequestContext, string operationPlanId)`                                                 |
| —                                                      | `ExecuteOperation(RequestContext)`                                                                      |
| —                                                      | `ExecuteOperationNode(OperationPlanContext, OperationExecutionNode, string schemaName)`                 |
| —                                                      | `ExecuteOperationBatchNode(OperationPlanContext, OperationBatchExecutionNode, string schemaName)`       |
| —                                                      | `ExecuteNodeFieldNode(OperationPlanContext, NodeFieldExecutionNode)`                                    |
| —                                                      | `ExecuteIntrospectionNode(OperationPlanContext, IntrospectionExecutionNode)`                            |
| —                                                      | `ExecuteSubscription(RequestContext, ulong subscriptionId)`                                             |
| —                                                      | `ExecuteSubscriptionNode(OperationPlanContext, ExecutionNode, string schemaName, ulong subscriptionId)` |
| —                                                      | `OnSubscriptionEvent(OperationPlanContext, ExecutionNode, string schemaName, ulong subscriptionId)`     |
| `QueryPlanExecutionError(Exception)`                   | `PlanOperationError(RequestContext, string operationId, Exception)`                                     |
| `ResolveError(Exception)`                              | `ExecutionNodeError(OperationPlanContext, ExecutionNode, Exception)`                                    |
| `ResolveByKeyBatchError(Exception)`                    | `ExecutionNodeError(OperationPlanContext, ExecutionNode, Exception)`                                    |
| `SubgraphRequestError(string subgraphName, Exception)` | `SourceSchemaTransportError(OperationPlanContext, ExecutionNode, string schemaName, Exception)`         |

To time individual stages of the request pipeline (parsing, validation, variable coercion) you previously had to implement Hot Chocolate's core `IExecutionDiagnosticEventListener` separately and register it alongside the Fusion-specific listener. In v16 these stages have been folded into `IFusionExecutionDiagnosticEventListener` itself, so you can remove your `IExecutionDiagnosticEventListener` implementations and move the overrides (for example `ParseDocument`, `ValidateDocument`, `CoerceVariables`) onto your `FusionExecutionDiagnosticEventListener` subclass instead.

The dedicated `SubscriptionTransportError(...)` hook from the v15 Fusion diagnostics API is also no longer exposed separately. Subscription transport failures now flow through `SourceSchemaTransportError(...)` like any other source-schema transport error.

## IRequestContext

Hot Chocolate has removed the `IRequestContext` abstraction in favor of the concrete `RequestContext` class. This applies to the Fusion diagnostic API as well:

| Before                        | After                                       |
| ----------------------------- | ------------------------------------------- |
| `context.DocumentId`          | `context.OperationDocumentInfo.Id.Value`    |
| `context.Document`            | `context.OperationDocumentInfo.Document`    |
| `context.DocumentHash`        | `context.OperationDocumentInfo.Hash.Value`  |
| `context.ValidationResult`    | `context.OperationDocumentInfo.IsValidated` |
| `context.IsCachedDocument`    | `context.OperationDocumentInfo.IsCached`    |
| `context.IsPersistedDocument` | `context.OperationDocumentInfo.IsPersisted` |

If you have a custom request middleware on the Fusion pipeline:

```diff
public class CustomRequestMiddleware
{
-   public async ValueTask InvokeAsync(IRequestContext context)
+   public async ValueTask InvokeAsync(RequestContext context)
    {
-       string documentId = context.DocumentId;
+       string documentId = context.OperationDocumentInfo.Id.Value;

        await _next(context).ConfigureAwait(false);
    }
}
```

## Clearer separation between schema and application services

Hot Chocolate has long maintained a second `IServiceProvider` for schema services, separate from the application service provider where you register your services and configuration. This schema service provider is scoped to a particular schema and contains all of the internal services for the gateway (diagnostic listeners, error filters, HTTP request interceptors, …).

To access application services within schema services like diagnostic event listeners or error filters, the v15 implementation used a combined service provider. In v16, the Fusion gateway uses the schema service provider exclusively — application services must now be explicitly cross-registered to be accessible.

```diff
builder.Services.AddSingleton<MyService>();
builder.Services.AddGraphQLGatewayServer()
+   .AddApplicationService<MyService>()
    .AddDiagnosticEventListener<MyDiagnosticEventListener>();

public class MyDiagnosticEventListener(MyService service) : FusionExecutionDiagnosticEventListener;
```

If you're using any of the following Fusion configuration APIs, ensure that the application services required for their activation are registered via `AddApplicationService<T>()`:

- `AddHttpRequestInterceptor`
- `AddErrorFilter`
- `AddDiagnosticEventListener`
- `AddOperationPlannerInterceptor`

If you need to access the application service provider from within the schema service provider, use:

```csharp
IServiceProvider applicationServices = schemaServices.GetRootServiceProvider();
```

<!-- ## AddServiceDiscoveryRewriter is gone

The `AddServiceDiscoveryRewriter` extension has been removed. It rewrote the gateway configuration to make HTTP clients use ASP.NET Core service discovery. There is currently no direct replacement on `IFusionGatewayBuilder`; configure your `HttpClient` registrations to use service discovery directly via `Microsoft.Extensions.ServiceDiscovery` instead, and reference the resulting client by name from your subgraph configuration.

## IConfigurationRewriter / ConfigurationRewriter is gone

The `IConfigurationRewriter` interface and the `ConfigurationRewriter` base class (from `HotChocolate.Fusion.Metadata`) have been removed. These types let you rewrite the Fusion gateway document and HTTP/WebSocket subgraph client configuration just before it was applied.

In v16, configuration is delivered through `IFusionConfigurationProvider`, which already gives you full access to the underlying `DocumentNode` and `JsonDocumentOwner`. If you previously used a configuration rewriter, wrap an `IFusionConfigurationProvider` (or implement one yourself) and rewrite the document there before forwarding it to subscribers. -->

## Experimental @semanticNonNull support removed

Hot Chocolate v15 included experimental support for the `@semanticNonNull` directive, which let you mark fields as semantically non-null while still returning `null` (rather than propagating to the parent) when a resolver errored. This feature has been removed in v16 in favor of the [`onError` proposal](https://github.com/graphql/graphql-spec/pull/1163).

If you previously opted in to this feature on the Fusion gateway, remove the option:

```diff
gatewayBuilder
    .ModifyOptions(o =>
    {
-       o.EnableSemanticNonNull = true;
    });
```

If you still need to keep the behavior of not propagating nulls for errors on non-null fields, set the `DefaultErrorHandlingMode` to `ErrorHandlingMode.Null`:

```csharp
gatewayBuilder.ModifyRequestOptions(o => o.DefaultErrorHandlingMode = ErrorHandlingMode.Null);
```

### Clients that still need a schema with @semanticNonNull annotations

If you have a client that still relies on the schema being annotated with `@semanticNonNull`, you have a few options to obtain such a schema.

**Schema snapshot tests**

If you produce a schema string for snapshot tests via `ISchemaDefinition.ToString()`, switch to `SchemaFormatter` with `RewriteToSemanticNonNull` enabled:

```csharp
string schemaStr = SchemaFormatter.FormatAsString(
    schema,
    new SchemaFormatterOptions { RewriteToSemanticNonNull = true });
```

**Downloading the schema from the gateway**

If you're using `MapGraphQLSchema()` to expose the gateway schema at `/graphql/schema`, you can additionally call `MapGraphQLSemanticNonNullSchema()` to expose a variant annotated with `@semanticNonNull` at `/graphql/semantic-non-null-schema.graphql`:

```csharp
app.MapGraphQLSchema();
app.MapGraphQLSemanticNonNullSchema();
```

# Noteworthy changes

## Concurrent execution gate

Hot Chocolate v16 introduces a concurrency gate that limits how many GraphQL operations execute at the same time. The gate sits in the request pipeline just before operation execution and applies uniformly to queries, mutations, subscription handshakes, and each subscription event.

For the Fusion gateway, configure the limit through `ModifyServerOptions`:

```csharp
gatewayBuilder.ModifyServerOptions(o => o.MaxConcurrentExecutions = 128);
```

The default is **64**. Operations that arrive while the gate is full queue up and run as slots free. Set the limit to `null` to disable the gate entirely.

Every execution is bounded by the `ExecutionTimeout` option (default 30 seconds). The budget covers both the time an execution spends waiting for a concurrency slot and the time it spends running.

## Parser limits

The parser now enforces a maximum recursion depth of **200** by default, a maximum of **4** directives per location, and a fragment visit budget of **1,000** per operation. These limits also apply to documents handled by the Fusion gateway. If your operations legitimately exceed these limits, raise them via `ModifyParserOptions` / `ModifyValidationOptions`:

```csharp
gatewayBuilder
    .ConfigureValidation((_, b) => b.ModifyOptions(o => o.MaxAllowedFragmentVisits = 5_000));
```

## RunWithGraphQLCommandsAsync returns exit code

`RunWithGraphQLCommandsAsync` and `RunWithGraphQLCommands` now return exit codes (`Task<int>` and `int` respectively). Update your `Program.cs` if you forward these to the host:

```diff
-await app.RunWithGraphQLCommandsAsync(args);
+return await app.RunWithGraphQLCommandsAsync(args);
```

# Aspire

The Aspire integration changed in v16. There is no separate `AddFusionGateway` resource anymore. The gateway and subgraphs are now regular Aspire projects.

First, update the AppHost SDK and Aspire hosting package to 13.x:

```diff
-  <Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0"/>
+  <Sdk Name="Aspire.AppHost.Sdk" Version="13.0.2"/>

   <ItemGroup>
     <PackageReference Include="Aspire.Hosting.AppHost" />
     <PackageReference Include="HotChocolate.Fusion.Aspire" />
   </ItemGroup>
```

`Aspire.Hosting.AppHost` should use a matching `13.x.x` version.

Then update the AppHost setup. Add the GraphQL orchestrator, tell Aspire where to get each subgraph schema, and reference those subgraphs from the gateway:

```diff
-var products = builder.AddProject<Projects.Products>("products");
-var reviews = builder.AddProject<Projects.Reviews>("reviews");
-var accounts = builder.AddProject<Projects.Accounts>("accounts");
-
-builder
-    .AddFusionGateway<Projects.Gateway>("gateway")
-    .WithSubgraph(products)
-    .WithSubgraph(reviews)
-    .WithSubgraph(accounts);
-
-builder.Build().Compose().Run();
+builder.AddGraphQLOrchestrator();
+
+var products = builder.AddProject<Projects.Products>("products")
+    .WithGraphQLSchemaEndpoint();
+
+var reviews = builder.AddProject<Projects.Reviews>("reviews")
+    .WithGraphQLSchemaEndpoint();
+
+var accounts = builder.AddProject<Projects.Accounts>("accounts")
+    .WithGraphQLSchemaEndpoint();
+
+builder
+    .AddProject<Projects.Gateway>("gateway")
+    .WithGraphQLSchemaComposition()
+    .WithReference(products)
+    .WithReference(reviews)
+    .WithReference(accounts);
+
+builder.Build().Run();
```

`builder.AddGraphQLOrchestrator()` installs the startup hook that runs schema discovery and composition. You no longer call `.Compose()`. `builder.Build().Run()` is enough.

`WithGraphQLSchemaEndpoint()` downloads the subgraph schema from `/graphql/schema.graphql` at startup. If your schema endpoint uses a different path, pass it explicitly:

```csharp
builder
    .AddProject<Projects.Products>("products")
    .WithGraphQLSchemaEndpoint(path: "/schema.graphql");
```

If you keep schema files on disk, use `WithGraphQLSchemaFile()` instead. It looks for `schema.graphqls` in the subgraph project directory by default:

```csharp
builder
    .AddProject<Projects.Products>("products")
    .WithGraphQLSchemaFile(fileName: "./dir/schema.graphqls");
```

To create that file automatically when a subgraph starts, call `ExportSchemaOnStartup()` on the subgraph's `IRequestExecutorBuilder`.

Pass composition options to `WithGraphQLSchemaComposition`:

```csharp
builder
    .AddProject<Projects.Gateway>("gateway")
    .WithGraphQLSchemaComposition(
        settings: new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true
        });
```

Each subgraph also needs an `Aspire` environment in `schema-settings.json`. This is the local GraphQL endpoint the composed gateway configuration uses when it runs under Aspire:

```diff
{
  "name": "my-subgraph",
+  "transports": {
+    "http": {
+      "url": "{{API_URL}}"
+    }
+  },
+  "environments": {
+    "Aspire": {
+      "API_URL": "http://localhost:5000/graphql"
+    }
+  }
}
```

## Per repository migration

<!-- TODO: At the start we want to check for and collect satisfiability issues so we can work on them

exmaple ci output:

Validating Fusion configuration of API 'QXBpCmcwMTlkMmIzMGUzNGY3YzQ2OTBjNTgxOTNkYjI1M2EyZg==' against stage 'dev'
├── Downloading existing configuration from 'dev'
│   └── ✓ Downloaded existing legacy v1 configuration from 'dev'.
├── Composing new configuration
│   └── ✕ Failed to compose new configuration.
└── ✕ Failed to validate the Fusion configuration.

## Composition log

❌ [ERR] Unable to access the field 'Review.productVariant'.
     Unable to transition between schemas 'REVIEWS' and 'PRODUCTS' for access to field 'PRODUCTS:Review.productVariant<Product>'.
       No lookups found for type 'Review' in schema 'PRODUCTS'. (UNSATISFIABLE)
Satisfiability validation failed.

 -->

### Migrate subgraph-config.json

For each subgraph in your repository, the existing `subgraph-config.json` file needs to be migrated to the new `schema-settings.json` format.

You can run the following command in the root of your repository and it will find all `subgraph-config.json` files and automatically convert them into `schema-settings.json` files:

```bash
dnx ChilliCream.Nitro.CommandLine fusion migrate subgraph-config
```

> Note: If you can't use .NET 10 / `dnx` you can also install `ChilliCream.Nitro.CommandLine` via `dotnet tool install` and then invoke it via `dotnet nitro ...`.

If you need to do this conversion manually: Create a `schema-settings.json` file next to each `subgraph-config.json` with the following changes:

```diff
 {
-  "subgraph": "products",
-  "http": {
-    "baseAddress": "http://products/graphql"
-  }
+  "version": "1.0.0",
+  "name": "products",
+  "transports": {
+    "http": {
+      "url": "http://products/graphql"
+    }
+  }
 }
```

> Note: By default the Fusion v2 composition assums your subgraph is compatible with the latest features. By adding `"version": "1.0.0"` we tell the composition that this is a legacy (Fusion v1) subgraph, which relaxes certain validations like `@shareable` and re-creates inferences that were present in Fusion v1, like fields ending in `ById` being inferred as `@lookup`.

If your subgraph is using a version older than the latest HotChocolate v15 or your subgraph uses an entirely different technology, you also need to disable variable batching in `schema-settings.json`.

```diff
 {
   "version": "1.0.0",
   "name": "products",
   "transports": {
     "http": {
-      "url": "http://products/graphql"
+      "url": "http://products/graphql",
+      "capabilities": {
+        "batching": {
+          "variableBatching": false
+        }
+      }
     }
   }
 }
```

#### Environment-specific configuration

Fusion v1 let you set environment-specific values from the pipeline with `fusion subgraph config set`:

```bash
dotnet fusion subgraph config set http \
    --url https://dev.example.com/graphql \
    -c subgraph.fsp
```

Fusion v2 moves these values into `schema-settings.json`. Replace anything that varies between environments with a `{{PLACEHOLDER}}` token, then list the per-environment values under a top-level `environments` section:

```diff
{
  "version": "1.0.0",
  "name": "products",
  "transports": {
    "http": {
-      "url": "https://example.com/graphql"
+      "url": "{{URL}}"
    }
-  }
+  },
+  "environments": {
+    "dev": {
+      "URL": "https://dev.example.com/graphql"
+    },
+    "prod": {
+      "URL": "https://prod.example.com/graphql"
+    }
+  }
}
```

Composition resolves the placeholders against a chosen environment. Pass `--environment <environment>` to `nitro fusion compose` to select one explicitly, or rely on `nitro fusion publish`, which derives the environment from its `--stage` value. When publishing through Nitro, the keys under `environments` therefore need to match the stage names defined in Nitro.

### Update subgraph

The concept of batch resolvers like `productByIds(ids: [ID!]!)` does no longer exist in Fusion v2. Batching is done on the transport level through [variable and request batching](https://github.com/graphql/graphql-over-http/blob/fb404ac12dde473f3d9f5a1b1026574c7475e1e4/spec/Appendix%20B%20--%20Variable%20Batching.md). This means singular fields like `Query.productById(id: ID!): Product` are invoked with a list of IDs instead of a plural `Query.productsById(ids: [ID!]!): [Product!]` field. Checkout [this GitHub issue](https://github.com/graphql/composite-schemas-spec/issues/25#issue-2173900758) for details on this decision.

Since you don't want multiple invocations of the `Query.productById` field during a single request to hit the database multiple times, you need to ensure your `Query` root fields and `[NodeResolver]` implementations (powering the `Query.node(id: ID!): Node` field) are using [`DataLoader`](/docs/hotchocolate/resolvers-and-data/dataloader). This is a best practice and ensures the performance of your server does not degrade in comparison to the previous batching fields.

If an entity currently only has batch `Query` root fields in your subgraph, you'll also have to add a singular field:

```diff
 type Query {
   productsById(ids: [ID!]!): [Product!] @lookup @internal
+  productByid(id: ID!): Product @lookup @internal
 }
```

Variable and request batching aren't enabled by default, so you also need to update your `Program.cs` to enable it:

```diff
- app.MapGraphQL();
+ app.MapGraphQL().WithOptions(new GraphQLServerOptions { EnableBatching = true })`.
```

If you want to, you can also now [migrate the subgraph to Hot Chocolate v16](#migrate-subgraph-to-v16), but it's not required at this point.

### Migrate workflows

The migration to Fusion v2 is designed to happen one subgraph repository at a time. While some of your subgraphs are still on v15 and others are already on v16, the gateway needs to keep working for both. The workflow changes in this section ensure that both archive formats stay available side-by-side until every subgraph has been migrated and the gateway itself is cut over.

In Fusion v15 each subgraph pipeline produces a Fusion gateway package (`.fgp`) and publishes it back to Nitro as the latest archive. In Fusion v16 the equivalent artifact is the Fusion archive (`.far`). To bridge the two formats during the transition, the v15 compose step is kept in place and the freshly composed `.fgp` is embedded into the published `.far` through the `--legacy-v1-archive` option. v15 gateways continue to download the embedded `.fgp`, v16 gateways download the `.far` directly.

A typical subgraph repository has two workflows that need updating: the **deployment workflow** that publishes the subgraph's archive to Nitro and the **PR validation workflow** that ensures the composed schema introduces no breaking changes. The same transition strategy applies to both, only the final Nitro command changes while the existing v15 download and compose steps stay in place.

#### Deployment workflow

In practice this means three changes to your existing deployment pipeline:

1. **Add** a step in the build job that uploads the source schema to Nitro, so the v16 publish can reference it.
2. **Keep** the v15 compose step in the deploy job. It is still responsible for producing an up-to-date `.fgp`.
3. **Replace** the final `dotnet nitro fusion-configuration publish commit` with `dotnet nitro fusion publish` and pass the freshly composed `.fgp` via `--legacy-v1-archive`.

Below is the existing v15 pipeline for reference:

```bash
# BUILD JOB
dotnet run --project ./src/SubgraphA -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ./src/SubgraphA

# DEPLOY JOB
dotnet fusion subgraph config set http \
  --url <subgraph-url> \
  -c ${{ github.workspace }}/subgraph/subgraph-a.fsp
dotnet nitro fusion-configuration publish begin \
  --tag <tag> \
  --api-id <api-id> \
  --subgraph-name subgraph-a \
  --stage <stage> \
  --api-key <api-key>
dotnet nitro fusion-configuration publish start \
  --api-key <api-key>
dotnet nitro fusion-configuration download \
  --api-id <api-id> \
  --stage <stage> \
  --output-file ./gateway.fgp \
  --api-key <api-key>
dotnet fusion compose \
  -p ./gateway.fgp \
  --enable-nodes \
  -s ${{ github.workspace }}/subgraph
dotnet nitro fusion-configuration publish commit \
  --configuration ./gateway.fgp \
  --api-key <api-key>
```

##### Upload the source schema in the build job

Add a step to the build job that uploads the exported source schema to Nitro. The `tag` is later used by the publish step to find the matching upload.

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-upload@v16
  with:
    tag: <tag>
    api-id: <api-id>
    api-key: <api-key>
    source-schema-files: |
      ./src/SubgraphA/schema.graphql
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion upload \
  --tag "<tag>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --source-schema-file "./src/SubgraphA/schema.graphql"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

> Note: The `dotnet fusion subgraph pack` step is still required while the v15 compose step runs in the deploy job, since v15 composition consumes the `.fsp` archive. It can be removed once the subgraph is migrated to v16 and the v15 compose step is dropped (see [Cleanup](#cleanup)).

##### Replace `publish commit` with `nitro fusion publish` in the deploy job

In the deploy job, leave the existing v15 commands that download the latest `.fgp` and run v15 composition untouched. Only the trailing `dotnet nitro fusion-configuration publish commit` is removed:

<!--
TODO: This should remove all of the steps that deal with the registry. just download and compose
      Discuss this with Pascal if we should just cancel instead to keep the idempotency
 -->

```diff
- dotnet nitro fusion-configuration publish commit \
-   --configuration ./gateway.fgp \
-   --api-key <api-key>
```

Replace it with `dotnet nitro fusion publish`, passing the freshly composed `gateway.fgp` via `--legacy-v1-archive`. This composes a new `.far`, embeds the `.fgp` inside it, and uploads the result as the latest archive.

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-publish@v16
  with:
    tag: <tag>
    stage: <stage>
    api-id: <api-id>
    api-key: <api-key>
    legacy-v1-archive: ./gateway.fgp
    source-schemas: |
      subgraph-a
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion publish \
  --tag "<tag>" \
  --stage "<stage>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --source-schema "subgraph-a" \
  --legacy-v1-archive "./gateway.fgp"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

> Note: `dotnet nitro fusion publish` should run **after** the subgraph application has been deployed. Once it succeeds, the new archive becomes the latest in Nitro and the gateway will start routing traffic against the new schema, so the subgraph must already be reachable at that URL.

> Note: `--legacy-v1-archive` is only required during the transition. Once every subgraph has been migrated to v16 and the gateway has been cut over to consume `.far` directly, the v15 compose step and the `--legacy-v1-archive` option can be removed (see [Cleanup](#cleanup)).

#### PR validation workflow

In addition to the deployment workflow, most subgraph repositories have a PR validation workflow that downloads the latest archive, runs composition with the proposed change, and verifies that the composed schema introduces no breaking changes. Below are the relevant v15 steps for reference:

```bash
dotnet run --project ./src/SubgraphA -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ./src/SubgraphA
dotnet nitro fusion-configuration download \
  --api-id <api-id> \
  --stage <stage> \
  --output-file ./gateway.fgp \
  --api-key <api-key>
dotnet fusion compose \
  --package-file ./gateway.fgp \
  --enable-nodes \
  --subgraph-package-file ./src/SubgraphA/subgraph-a.fsp
dotnet nitro fusion-configuration validate \
  --stage <stage> \
  --api-id <api-id> \
  --configuration ./gateway.fgp \
  --api-key <api-key>
```

As with the deployment workflow, the v15 download and compose steps stay in place during the transition so the v15 composition path keeps being validated. Only the final `dotnet nitro fusion-configuration validate` is replaced by `dotnet nitro fusion validate`. Pass the freshly composed `gateway.fgp` via `--legacy-v1-archive` so the validation also covers the embedded v15 archive:

```diff
- dotnet nitro fusion-configuration validate \
-   --stage <stage> \
-   --api-id <api-id> \
-   --configuration ./gateway.fgp \
-   --api-key <api-key>
```

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-validate@v16
  with:
    stage: <stage>
    api-id: <api-id>
    api-key: <api-key>
    legacy-v1-archive: ./gateway.fgp
    source-schema-files: |
      ./src/SubgraphA/schema.graphql
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion validate \
  --stage "<stage>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --legacy-v1-archive "./gateway.fgp" \
  --source-schema-file "./src/SubgraphA/schema.graphql"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

### Migrate subgraph to v16

<!-- TODO: Link to Hot Chocolate migration guide and mention that the version: 1.0.0 should be removed. Also `AddSourceSchemaDefaults` (TODO: Check if this modifies the batching already) -->

## Migrate gateway

TODO

## Cleanup

Remove `HotChocolate.Fusion.CommandLine` and `ChilliCream.Nitro.CLI` from pipelines.

<!-- TODO: If people don't use our GitHub actions how can they use `ChilliCream.Nitro.CLI` and `ChilliCream.Nitro.CommandLine` side-by-side as both map to `dotnet nitro`? -->
