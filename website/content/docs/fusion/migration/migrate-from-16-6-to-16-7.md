---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: adopt the router-branded configuration APIs, the graphql-router template, and the packaging/type-system compatibility exceptions."
---

> [!NOTE]
> 16.7 rebrands the Fusion gateway to the Fusion router. Registration and builder extension methods keep their gateway-named overloads, marked `[Obsolete]`, so a 16.6 solution compiles unchanged with deprecation warnings. Packaging, Fusion type-system, and low-level setup APIs rename directly with no compatibility alias, and `BuildGatewayAsync` leaves the public surface. A project that treats obsolete warnings as errors needs source changes before it builds.

# Update the packages

Update every Hot Chocolate Fusion package to 16.7:

```diff
   <ItemGroup>
-    <PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.6.x" />
+    <PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.7.x" />
   </ItemGroup>
```

# Deprecations

The gateway-named registration and builder extension methods keep their complete published signatures and forward to their router-named replacements. Rename the calls when you update; leaving them as-is still compiles, with an obsolete warning.

## AddGraphQLGateway renamed to AddGraphQLRouter

On the host application builder:

```diff
 builder
-    .AddGraphQLGateway()
+    .AddGraphQLRouter()
     .AddFileSystemConfiguration("./gateway.far");
```

## AddGraphQLGatewayServer renamed to AddGraphQLRouter

On the service collection:

```diff
 services
-    .AddGraphQLGatewayServer();
+    .AddGraphQLRouter();
```

## AddGraphQLGateway on IServiceCollection renamed to AddGraphQLRouterCore

`AddGraphQLRouterCore` registers the router services without the HTTP transport:

```diff
 services
-    .AddGraphQLGateway();
+    .AddGraphQLRouterCore();
```

The three router-named methods return `IFusionRouterBuilder`. The gateway-named methods they replace keep returning `IFusionGatewayBuilder`, which `IFusionRouterBuilder` extends. Existing builder extension methods, including third-party ones written against `IFusionGatewayBuilder`, apply to both builder types, so a chained configuration call compiles unchanged whichever entry point you use.

## Builder extension compatibility across areas

`IFusionGatewayBuilder` carries `[Obsolete("Use IFusionRouterBuilder instead.")]`, but it remains the base interface `IFusionRouterBuilder` extends through 16.7. Every area that ships fluent builder extensions keeps its existing gateway-named class as an obsolete forwarder next to a new router-named class with the same method names:

| Area                                                            | Router-named class                         | Legacy class (obsolete)                     |
| --------------------------------------------------------------- | ------------------------------------------ | ------------------------------------------- |
| Core (`HotChocolate.Fusion.Execution`)                          | `CoreFusionRouterBuilderExtensions`        | `CoreFusionGatewayBuilderExtensions`        |
| ASP.NET Core (`HotChocolate.Fusion.AspNetCore`)                 | `AspNetCoreFusionRouterBuilderExtensions`  | `AspNetCoreFusionGatewayBuilderExtensions`  |
| Cache control (`HotChocolate.Fusion.Caching`)                   | `FusionCachingRouterBuilderExtensions`     | `FusionCachingGatewayBuilderExtensions`     |
| Diagnostics (`HotChocolate.Fusion.Diagnostics`)                 | `DiagnosticsFusionRouterBuilderExtensions` | `DiagnosticsFusionGatewayBuilderExtensions` |
| In-memory connector (`HotChocolate.Fusion.Connectors.InMemory`) | `InMemoryFusionRouterBuilderExtensions`    | `InMemoryFusionGatewayBuilderExtensions`    |
| MCP adapter (`HotChocolate.Fusion.Adapters.Mcp`)                | `FusionRouterBuilderExtensions`            | `FusionGatewayBuilderExtensions`            |
| OpenAPI adapter (`HotChocolate.Fusion.Adapters.OpenApi`)        | `OpenApiFusionRouterBuilderExtensions`     | `OpenApiFusionGatewayBuilderExtensions`     |

The event stream broker packages (`HotChocolate.Fusion.Subscriptions.Redis`, `.Kafka`, `.NATS`, `.AzureEventHubs`, and `.AmazonSqs`) keep a single declaring class per broker instead of splitting one: for example, `RedisEventStreamBrokerServiceCollectionExtensions` carries both the current `IFusionRouterBuilder` overload of `AddRedisEventStreamBroker` and an obsolete `IFusionGatewayBuilder` overload with the same name.

A static call against a gateway-named class keeps its exact published signature, parameter names, and defaults, and compiles with an obsolete warning. A custom builder that implements only `IFusionGatewayBuilder` keeps working through those retained methods; it is not upgraded to `IFusionRouterBuilder` and must not be cast to it. A third-party extension method that still returns `IFusionGatewayBuilder` continues a legacy-typed chain through the retained methods, with the same expected obsolete warnings, rather than being required to return `IFusionRouterBuilder` before 17.

## NodeResolution.Gateway renamed to NodeResolution.Router

```diff
 var settings = new GraphQLCompositionSettings
 {
-    NodeResolution = NodeResolution.Gateway
+    NodeResolution = NodeResolution.Router
 };
```

`NodeResolution.Gateway` is now an obsolete alias with the same underlying value as `NodeResolution.Router` (`SourceSchema` keeps value `1`). The `--node-resolution` CLI option and the `node-resolution` composition setting still accept `gateway` alongside `router`.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## BuildGatewayAsync is no longer public

`BuildGatewayAsync` built a service provider from the service collection and resolved the router executor. In 16.7, it and its `BuildRouterAsync` replacement are internal. If you called `BuildGatewayAsync` directly, resolve the executor through `IRequestExecutorProvider` instead:

```diff
-var executor = await services.BuildGatewayAsync(cancellationToken);
+var executor = await services
+    .BuildServiceProvider()
+    .GetRequiredService<IRequestExecutorProvider>()
+    .GetExecutorAsync(cancellationToken: cancellationToken);
```

## Packaging, type-system, and low-level setup APIs rename without a compatibility alias

The rename treats packaging internals, the Fusion execution type system, and the raw setup plumbing beneath the builder as non-user-facing, so these rename directly instead of keeping an obsolete alias:

| 16.6                                                      | 16.7                              |
| --------------------------------------------------------- | --------------------------------- |
| `GatewayConfiguration`                                    | `RouterConfiguration`             |
| `SupportedGatewayFormats`                                 | `SupportedRouterFormats`          |
| `TryGetGatewayConfigurationAsync`                         | `TryGetRouterConfigurationAsync`  |
| `isGatewayField` constructor parameter / `IsGatewayField` | `isRouterField` / `IsRouterField` |
| `FusionGatewaySetup`                                      | `FusionRouterSetup`               |

Code that references these names directly, which is uncommon outside of Fusion's own packaging and execution internals, needs a source update. Ordinary builder configuration calls are unaffected, and the persisted archive contents these APIs read and write are unchanged: the JSON property is still `supportedGatewayFormats`, and the execution schema directive is still `fusion__gateway_field`.

# Noteworthy changes

## Template renamed to graphql-router

The `graphql-gateway` template in the `HotChocolate.Templates` package is now `graphql-router`:

```diff
-dotnet new graphql-gateway
+dotnet new graphql-router
```

A new project scaffolds `AddGraphQLRouter()` and loads `./gateway.far`, the same default the `graphql-gateway` template used.

## Archive default stays gateway.far

The composed Fusion archive still defaults to `gateway.far` in 16.7:

- `WithNitroComposition` in `HotChocolate.Fusion.Aspire` writes `gateway.far` unless you pass `outputFileName`.
- `nitro fusion compose` writes `gateway.far` when `--archive` points to a directory.
- `nitro fusion download` writes `gateway.far` unless you pass `--output-file`.
- Legacy `.fgp` downloads keep the `gateway.fgp` extension.

Changing the default to `graph.far` is planned for a future release and is not part of 16.7. If you want to adopt the new name early, pass the file name explicitly wherever you compose, download, or load the archive:

```diff
 builder
     .AddGraphQLRouter()
-    .AddFileSystemConfiguration("./gateway.far");
+    .AddFileSystemConfiguration("./graph.far");
```

## CLI and settings inputs keep accepting gateway

`--node-resolution` and `nitro fusion settings set node-resolution` accept `router`, `gateway`, and `source-schema`. `gateway` is a legacy alias for `router` and keeps working until 17; prefer `router` in new scripts. Nitro API creation accepts `--kind router` alongside the existing `--kind gateway` value; both map to the same backend API kind, and the GraphQL identifiers Nitro sends over the wire are unchanged.

# Version 17

The gateway-named registration and builder extension methods, the `IFusionGatewayBuilder` interface, the `NodeResolution.Gateway` alias, and the legacy `gateway` CLI and settings input are all retained through 16.x and planned for removal in 17, together with the `graph.far` archive-default change described above. None of that removal happens in 16.7.
