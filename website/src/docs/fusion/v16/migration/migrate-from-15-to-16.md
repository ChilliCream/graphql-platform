---
title: Migrate Hot Chocolate Fusion from 15 to 16
---

> This guide is still a work in progress with more updates to follow.

<!--
TODO:
- Default security
- Nitro integration
-->

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
-        o.EnableSchemaFileSupport = true;
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

A few specific options have moved or been renamed:

- `FusionOptions.AllowQueryPlan` has been replaced by the per-request `FusionRequestOptions.AllowOperationPlanRequests`. When enabled, clients can request the operation plan to be inlined into the response by sending the `Fusion-Operation-Plan: 1` header. The option defaults to `false` so the plan is not exposed unless explicitly opted in.
- `FusionOptions.IncludeDebugInfo` has been replaced by the per-request `FusionRequestOptions.CollectOperationPlanTelemetry`.
- `RequestExecutorOptions.EnableSchemaFileSupport` no longer exists. Schema file endpoints (`/graphql/schema`) are now wired up via `MapGraphQLSchema()` on the endpoint builder.

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

## ConfigureFromCloud replaced by AddNitro().AddFusion() + ModifyNitroOptions

`ConfigureFromCloud` no longer exists. In v16 the Nitro integration is registered through `INitroBuilder`, and per-gateway options are configured via `ModifyNitroOptions` on `IFusionGatewayBuilder`. The flat option surface from v15 has been split into nested option groups (`Service`, `Metrics`, `OperationReporting`, `PersistedOperations`, `OpenApi`, `Mcp`).

```diff
-gatewayBuilder.ConfigureFromCloud(x =>
-{
-    x.ApiId = "...";
-    x.ApiKey = "...";
-    x.Stage = "dev";
-
-    x.EnablePersistedQueries = true;
-    x.DefaultQueryCacheExpiration = TimeSpan.FromSeconds(30);
-    x.NotFoundQueryCacheExpiration = TimeSpan.FromSeconds(10);
-
-    x.LocalFusionConfigurationFile = "gateway.fgp";
-
-    x.Metrics.Enabled = true;
-    x.Metrics.ExportIntervalMilliseconds = 1000;
-    x.Metrics.ExportTimeoutMilliseconds = 400;
-
-    x.EnableOperationReporting = true;
-
-    x.ServerUrl = "";
-    x.TelemetryUrl = "";
-});
+builder.Services.AddNitro().AddFusion();
+
+gatewayBuilder.ModifyNitroOptions(x =>
+{
+    x.Service.ApiId = "...";
+    x.Service.ApiKey = "...";
+    x.Service.Stage = "dev";
+    x.Service.ServerUrl = "";
+    x.Service.TelemetryUrl = "";
+
+    x.Metrics.Enabled = true;
+    x.Metrics.ExportIntervalMilliseconds = 1000;
+    x.Metrics.ExportTimeoutMilliseconds = 400;
+
+    x.LocalFusionConfigurationFile = "gateway.fgp";
+});
```

`AddNitro().AddFusion()` registers Nitro and the Fusion-specific Nitro features on the application services. `ModifyNitroOptions` then configures the gateway-scoped `NitroFusionOptions`. The `NitroFusionOptions` instance exposes nested option groups for each capability:

| v15 (flat)                       | v16 (nested)                                 |
| -------------------------------- | -------------------------------------------- |
| `x.ApiId` / `ApiKey` / `Stage`   | `x.Service.ApiId` / `ApiKey` / `Stage`       |
| `x.ServerUrl` / `TelemetryUrl`   | `x.Service.ServerUrl` / `TelemetryUrl`       |
| `x.Metrics.Enabled`              | `x.Metrics.Enabled` (unchanged)              |
| `x.LocalFusionConfigurationFile` | `x.LocalFusionConfigurationFile` (unchanged) |

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

# Aspire AppHost migration

The `HotChocolate.Fusion.Aspire` integration was rewritten in v16. The package now sits on top of the standard Aspire 13 hosting model and uses the resource annotation pattern instead of a dedicated `AddFusionGateway` resource type. As a side-effect, the AppHost SDK has to be bumped from 9.x to 13.x.

```diff
-  <Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0"/>
+  <Sdk Name="Aspire.AppHost.Sdk" Version="13.0.2"/>

   <ItemGroup>
     <PackageReference Include="Aspire.Hosting.AppHost" />
     <PackageReference Include="HotChocolate.Fusion.Aspire" />
   </ItemGroup>
```

`Aspire.Hosting.AppHost` should be moved to a matching `13.x.x` version.

## AddFusionGateway / WithSubgraph removed

The v15 `AddFusionGateway<TProject>(name).WithSubgraph(...)` chain has been removed. In v16 you register the gateway as a regular `AddProject<TProject>` resource and use the new GraphQL annotations and the standard Aspire `.WithReference(...)` to wire subgraphs to the gateway:

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
+    .WithGraphQLSchemaComposition(
+        settings: new GraphQLCompositionSettings
+        {
+            EnableGlobalObjectIdentification = true,
+            EnvironmentName = "aspire"
+        })
+    .WithReference(products)
+    .WithReference(reviews)
+    .WithReference(accounts);
+
+builder.Build().Run();
```

The migration breaks down into four mechanical steps:

1. **Add `builder.AddGraphQLOrchestrator()` once at the top of `Program.cs`.** This registers the lifecycle hook that drives schema discovery and composition during AppHost startup.
2. **Annotate every subgraph project with `.WithGraphQLSchemaEndpoint()`.** This marks the resource as having a GraphQL schema endpoint that the orchestrator can fetch. The default path is `/graphql/schema.graphql` on the `http` endpoint; override via `path:` / `endpointName:` / `sourceSchemaName:` if your subgraph serves the schema elsewhere. There is also `.WithGraphQLSchemaFile("schema.graphqls")` if the schema lives next to the project on disk.
3. **Replace `AddFusionGateway<T>(name).WithSubgraph(s)` with `AddProject<T>(name).WithGraphQLSchemaComposition(...).WithReference(s)`.** `WithGraphQLSchemaComposition` opts the gateway resource into composition; the standard Aspire `WithReference` then declares the dependency on each subgraph and exposes the connection info to the gateway.
4. **Drop `.Compose()` — `builder.Build().Run()` is now sufficient.** Composition runs as part of the orchestrator's lifecycle hook.

## GraphQLCompositionSettings

`WithGraphQLSchemaComposition` accepts an optional `outputFileName` (default `gateway.far`) and a `GraphQLCompositionSettings` value:

```csharp
public struct GraphQLCompositionSettings
{
    public bool? EnableGlobalObjectIdentification { get; set; }
    public string? EnvironmentName { get; set; }
}
```
