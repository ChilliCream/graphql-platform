---
title: Migrate Hot Chocolate Fusion from 16.5 to 16.6
description: "Migration guide for Hot Chocolate Fusion v16.5 to v16.6: move the Aspire AppHost to the Nitro composition methods and declare the GraphQL route of every composed source schema."
---

> [!NOTE]
> The breaking changes in this release are in the Aspire integration package `HotChocolate.Fusion.Aspire`. A solution without an Aspire AppHost updates the package versions and is done.

# Update the packages

Update every Hot Chocolate Fusion package to 16.6:

```diff
   <ItemGroup>
-    <PackageReference Include="HotChocolate.Fusion.Aspire" Version="16.5.x" />
+    <PackageReference Include="HotChocolate.Fusion.Aspire" Version="16.6.x" />
   </ItemGroup>
```

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## WithGraphQLSchemaComposition renamed to WithNitroComposition

`WithGraphQLSchemaComposition` is deprecated. The only remaining overload takes composition settings as its first parameter, so a 16.5 call that omits the settings or passes the output file name positionally no longer compiles. Rename the call on the gateway resource to `WithNitroComposition`:

```diff
 builder
     .AddProject<Projects.Gateway>("gateway")
-    .WithGraphQLSchemaComposition()
+    .WithNitroComposition()
     .WithReference(products)
     .WithReference(reviews);
```

The parameters changed as well. In 16.5 the method took the output file name first and a settings struct second. 16.6 has two overloads:

```csharp
public static IResourceBuilder<T> WithNitroComposition<T>(
    this IResourceBuilder<T> builder,
    bool disableValidation = false,
    string outputFileName = "gateway.far")
    where T : IResourceWithEndpoints;

public static IResourceBuilder<T> WithNitroComposition<T>(
    this IResourceBuilder<T> builder,
    GraphQLCompositionSettings settings,
    string outputFileName = "gateway.far")
    where T : IResourceWithEndpoints;
```

A positional output file name becomes a named argument:

```diff
-    .WithGraphQLSchemaComposition("composed.far")
+    .WithNitroComposition(outputFileName: "composed.far")
```

A call that passes composition settings puts the settings first:

```diff
-    .WithGraphQLSchemaComposition(
-        settings: new GraphQLCompositionSettings
-        {
-            EnableGlobalObjectIdentification = true
-        })
+    .WithNitroComposition(
+        new GraphQLCompositionSettings
+        {
+            EnableGlobalObjectIdentification = true
+        })
```

Composition settings normally come from Nitro. Settings passed to the `GraphQLCompositionSettings` overload override them locally, so prefer the `disableValidation` overload unless you need a local override.

# Behavioral breaking changes

## Source schemas must declare their GraphQL route

Composition now requires every composed source schema to declare the path of its GraphQL endpoint. `WithGraphQLSchemaEndpoint` and `WithGraphQLSchemaFile` do not declare it. Both are marked `[Obsolete]`, and a resource registered through them still compiles but fails composition with an error like:

```text
The source schema Products of the resource products does not declare the path of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.
```

Replace both methods with `WithGraphQLHttpEndpoint`:

```diff
 var products = builder
     .AddProject<Projects.Products>("products")
-    .WithGraphQLSchemaEndpoint();
+    .WithGraphQLHttpEndpoint();
```

`WithGraphQLHttpEndpoint` declares the GraphQL route of the resource in addition to the schema download path:

```csharp
public static IResourceBuilder<T> WithGraphQLHttpEndpoint<T>(
    this IResourceBuilder<T> builder,
    string path = "/graphql",
    string? schemaPath = "/graphql/schema.graphql",
    string endpointName = "http",
    string? sourceSchemaName = null)
    where T : IResourceWithEndpoints;
```

| `WithGraphQLSchemaEndpoint` (16.5) | `WithGraphQLHttpEndpoint` (16.6)                    |
| ---------------------------------- | --------------------------------------------------- |
| not declared                       | `path`, the GraphQL route, defaults to `/graphql`   |
| `path`, the schema download path   | `schemaPath`, defaults to `/graphql/schema.graphql` |
| `endpointName`                     | `endpointName`                                      |
| `sourceSchemaName`                 | `sourceSchemaName`                                  |

A custom schema download path moves to `schemaPath`:

```diff
 var products = builder
     .AddProject<Projects.Products>("products")
-    .WithGraphQLSchemaEndpoint(path: "/schema.graphql");
+    .WithGraphQLHttpEndpoint(path: "/graphql", schemaPath: "/schema.graphql");
```

An Apollo Federation source schema serves its schema through the GraphQL endpoint at `path`, so `schemaPath` is ignored for it.

## AddNitro declares APIs and stages as resources

In 16.5, `AddNitro(stage)` bound the whole distributed application to one Nitro stage and `WithNitroApiId` on a gateway selected the API it composed against. 16.6 replaces both with a declarative resource graph: `AddNitro()` adds a Nitro resource, `AddApi` declares an API on it, `WithNitroApiId` carries the API id, and `AddStage` declares a stage that a gateway selects with `WithNitroCompositionBase`.

```diff
-builder.AddNitro("dev");
+var nitro = builder.AddNitro();
+
+var devStage = nitro
+    .AddApi("products-fusion")
+    .WithNitroApiId("QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==")
+    .AddStage("dev");

 builder
     .AddProject<Projects.Gateway>("gateway")
-    .WithNitroApiId("QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==")
     .WithNitroComposition()
+    .WithNitroCompositionBase(devStage)
     .WithReference(products);
```

Because the stage is a resource, one AppHost can model several APIs and stages, and different gateways can compose against different ones. Selecting a second stage for the same gateway throws while the AppHost builds. See [Local Development](../local-development.md#developing-across-teams-and-repositories) for the full workflow.

The seed update overload keeps its configure callback and now takes the portal URL first:

```csharp
public static IResourceBuilder<NitroResource> AddNitro(
    this IDistributedApplicationBuilder builder,
    Uri? portalUrl,
    Action<NitroSeedUpdateOptions> configureSeedUpdates);
```

File-based source schemas are being retired. Replace `WithGraphQLSchemaFile` with `WithGraphQLHttpEndpoint`, which downloads the schema from the running resource instead of reading it from the project directory:

```diff
 var products = builder
     .AddProject<Projects.Products>("products")
-    .WithGraphQLSchemaFile("schema.graphqls");
+    .WithGraphQLHttpEndpoint();
```

# Noteworthy changes

## AddGraphQLOrchestrator deprecated in favor of AddNitro

`AddGraphQLOrchestrator` is marked `[Obsolete]` and forwards to `AddNitro`. Rename the call:

```diff
-builder.AddGraphQLOrchestrator();
+builder.AddNitro();
```

## Nitro schema validation during composition

When a gateway selects a Nitro stage with `WithNitroCompositionBase` and the API of that stage carries an id, composition validates the composed schema through Nitro. `WithNitroComposition(disableValidation: true)` turns the validation off.

## Polyglot AppHost support

The Aspire integration now exports its extension methods to polyglot AppHosts. In a TypeScript AppHost the methods surface through the generated Aspire SDK with camel-cased names and options objects.

# Upgrading from a 16.6 preview

The 16.6 preview builds (`16.6.0-p.x`) shipped intermediate APIs that changed before the release. Skip this section when you upgrade from 16.5.

## AddNitroComposition replaced by the declarative Nitro resources

A preview build shipped `AddNitroComposition(stage, portalUrl, seedUpdates)`. The release replaced it with `AddNitro()` and the declarative API and stage resources described in [AddNitro declares APIs and stages as resources](#addnitro-declares-apis-and-stages-as-resources):

```diff
-builder.AddNitroComposition("dev", portalUrl);
+var devStage = builder
+    .AddNitro(portalUrl)
+    .AddApi("products-fusion")
+    .WithNitroApiId(apiId)
+    .AddStage("dev");
```

The seed update settings are configured with a callback again, and the `NitroSeedUpdateOptions` properties are settable:

```diff
-builder.AddNitroComposition(
-    "dev",
-    portalUrl,
-    new NitroSeedUpdateOptions { AutoUpdate = false });
+builder.AddNitro(
+    portalUrl,
+    options => options.AutoUpdate = false);
```

## WithNitroApiId moved to the Nitro API resource

`WithNitroApiId` is no longer an extension of a gateway resource builder. It belongs to the API declaration that `AddApi` returns:

```csharp
public static IResourceBuilder<NitroApiResource> WithNitroApiId(
    this IResourceBuilder<NitroApiResource> builder,
    string apiId);
```
