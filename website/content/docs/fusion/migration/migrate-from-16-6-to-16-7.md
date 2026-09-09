---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: adopt the router-branded configuration APIs, the graphql-router template, and the graph.far archive name."
---

> [!NOTE]
> 16.7 rebrands the Fusion gateway to the Fusion router. The gateway-named registration methods keep working and are marked `[Obsolete]`, so a 16.6 solution compiles with deprecation warnings. `BuildGatewayAsync` is the only removal.

# Update the packages

Update every Hot Chocolate Fusion package to 16.7:

```diff
   <ItemGroup>
-    <PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.6.x" />
+    <PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.7.x" />
   </ItemGroup>
```

# Deprecations

The gateway-named registration methods forward to their router-named replacements and are marked `[Obsolete]`. Rename the calls when you update.

## AddGraphQLGateway renamed to AddGraphQLRouter

On the host application builder:

```diff
 builder
-    .AddGraphQLGateway()
+    .AddGraphQLRouter()
     .AddFileSystemConfiguration("./graph.far");
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

All three methods still return `IFusionGatewayBuilder`, so chained configuration calls compile unchanged.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## BuildGatewayAsync removed

`BuildGatewayAsync` built a service provider from the service collection and resolved the router executor. It is no longer part of the public API. Resolve the executor through `IRequestExecutorProvider`:

```diff
-var executor = await services.BuildGatewayAsync(cancellationToken);
+var executor = await services
+    .BuildServiceProvider()
+    .GetRequiredService<IRequestExecutorProvider>()
+    .GetExecutorAsync(cancellationToken: cancellationToken);
```

# Behavioral breaking changes

## Default archive name is graph.far

The composed Fusion archive defaults to `graph.far` instead of `gateway.far`:

- `WithNitroComposition` in `HotChocolate.Fusion.Aspire` writes `graph.far` unless `outputFileName` is passed.
- `nitro fusion compose` writes `graph.far` when `--archive` points to a directory.
- `nitro fusion download` writes `graph.far` unless `--output-file` is passed.

A router that loads the archive by its old default name no longer finds it after the next composition. Update the file system configuration to the new name, or pass the old name explicitly to the composition:

```diff
 builder
     .AddGraphQLRouter()
-    .AddFileSystemConfiguration("./gateway.far");
+    .AddFileSystemConfiguration("./graph.far");
```

The deprecated `WithGraphQLSchemaComposition` keeps the `gateway.far` default.

# Noteworthy changes

## Template renamed to graphql-router

The `graphql-gateway` template in the `HotChocolate.Templates` package is now `graphql-router`:

```diff
-dotnet new graphql-gateway
+dotnet new graphql-router
```

A new project scaffolds `AddGraphQLRouter()` and loads `./graph.far`.
