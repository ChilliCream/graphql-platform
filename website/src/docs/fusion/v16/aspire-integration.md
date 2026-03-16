---
title: "Aspire Integration"
---

Nitro gives you full control over composition, but during active development you want a tighter loop. Every time you change a type, add a field, or adjust a resolver, you need to re-export the schema, re-compose, and restart the gateway. That friction adds up.

The `HotChocolate.Fusion.Aspire` package integrates composition into the .NET Aspire AppHost. When you build the AppHost, the orchestrator starts your subgraphs, extracts their source schemas, composes them into a Fusion archive, and writes it to the gateway project directory. One build step replaces the manual export-compose-restart cycle. You can also mix live subgraphs with pre-exported schema files, letting you develop against a full composite schema even when you only run a subset of services locally.

## Prerequisites

You need a .NET Aspire AppHost project. If you do not have one yet, create it with:

```bash
dotnet new aspire-apphost -n AppHost
```

Add the Fusion Aspire package to the AppHost project:

```bash
cd AppHost
dotnet add package HotChocolate.Fusion.Aspire --version 16.0.0-p.11.36
```

Your subgraph projects need the `HotChocolate.AspNetCore.CommandLine` package so the orchestrator can extract their schemas. If you followed the [Getting Started](/docs/fusion/v16/getting-started) tutorial, your subgraphs already have this.

## Setting Up the AppHost

The AppHost wires together your subgraphs and gateway. Three extension methods configure the composition pipeline.

**C# configuration**

```csharp
// AppHost/Program.cs

var builder = DistributedApplication.CreateBuilder(args);

builder.AddGraphQLOrchestrator();

var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaEndpoint();

var reviewsApi = builder
    .AddProject<Projects.Reviews>("reviews-api")
    .WithGraphQLSchemaEndpoint();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition()
    .WithReference(productsApi)
    .WithReference(reviewsApi);

builder.Build().Run();
```

Four things to notice:

- **`AddGraphQLOrchestrator()`** registers the composition orchestrator with the Aspire eventing system. Call this once on the application builder.
- **`WithGraphQLSchemaEndpoint()`** marks a subgraph as having a live schema endpoint. The orchestrator waits for the subgraph to start, then fetches the source schema over HTTP.
- **`WithGraphQLSchemaComposition()`** marks the gateway as needing composition. The orchestrator discovers all referenced subgraphs, extracts their schemas, composes them, and writes a `gateway.far` file to the gateway project directory.
- **`WithReference()`** is standard Aspire. It tells the orchestrator which subgraphs to include in composition for this gateway.

When you build and run the AppHost, the orchestrator handles the entire composition pipeline automatically. No manual `nitro fusion compose` step needed.

## Live Schema Extraction

By default, `WithGraphQLSchemaEndpoint()` fetches the source schema from `/graphql/schema.graphql` on each subgraph. Hot Chocolate subgraphs expose this endpoint automatically when they include the `HotChocolate.AspNetCore.CommandLine` package.

The orchestrator starts each subgraph, waits for it to become healthy, then makes an HTTP GET request to the schema endpoint. If the subgraph is not ready within the timeout, the orchestrator reports an error and stops the AppHost.

You can customize the schema path, the endpoint name, and the source schema name:

```csharp
var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaEndpoint(
        path: "/graphql/schema.graphql",
        endpointName: "http",
        sourceSchemaName: "Products");
```

All three parameters have sensible defaults. The `sourceSchemaName` defaults to the resource name (`"products-api"` in this example). Override it when you want the source schema name to differ from the Aspire resource name.

## Working with Partial Graphs

You do not need to run every subgraph locally. When your system has many subgraphs but you only develop on a few, use `WithGraphQLSchemaFile()` to include pre-exported schema files for the subgraphs you are not running.

```csharp
// AppHost/Program.cs

var builder = DistributedApplication.CreateBuilder(args);

builder.AddGraphQLOrchestrator();

// Subgraphs you are actively developing: live extraction
var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaEndpoint();

var reviewsApi = builder
    .AddProject<Projects.Reviews>("reviews-api")
    .WithGraphQLSchemaEndpoint();

// Subgraphs from other teams: use pre-exported schema files
var shippingApi = builder
    .AddProject<Projects.Shipping>("shipping-api")
    .WithGraphQLSchemaFile();

var accountsApi = builder
    .AddProject<Projects.Accounts>("accounts-api")
    .WithGraphQLSchemaFile();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition()
    .WithReference(productsApi)
    .WithReference(reviewsApi)
    .WithReference(shippingApi)
    .WithReference(accountsApi);

builder.Build().Run();
```

The orchestrator extracts schemas from the live subgraphs over HTTP and reads the file-based schemas from each project's directory. Both are fed into the same composition step. The result is a complete composite schema that includes all subgraphs, even though only some are running locally.

`WithGraphQLSchemaFile()` looks for `schema.graphqls` and its companion `schema-settings.json` in the subgraph's project directory. These are the same files that `dotnet run -- schema export` produces. Keep them checked into source control so that other team members can compose against them without running those services.

You can customize the file name:

```csharp
var shippingApi = builder
    .AddProject<Projects.Shipping>("shipping-api")
    .WithGraphQLSchemaFile(
        fileName: "schema.graphqls",
        sourceSchemaName: "Shipping");
```

## Composition Settings

`WithGraphQLSchemaComposition()` accepts a settings parameter that controls composition behavior.

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition(
        settings: new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true,
            EnvironmentName = "aspire"
        })
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

- **`EnableGlobalObjectIdentification`** enables the `Node` interface and Relay-style global object IDs in the composite schema. Set this to `true` if your subgraphs use the `[NodeResolver]` pattern.
- **`EnvironmentName`** selects the environment for variable substitution in `schema-settings.json`. For example, if your settings file defines an `"aspire"` environment with local URLs, the orchestrator uses those URLs instead of the defaults.

The output file name defaults to `gateway.far`. You can change it if needed:

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition(outputFileName: "composed.far")
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

## How Composition Fits the Dev Loop

With Aspire, your inner dev loop looks like this:

1. Change code in a subgraph (add a field, modify a type, adjust a resolver).
2. Build and run the AppHost.
3. The orchestrator starts your subgraphs, extracts their schemas, and composes the Fusion archive.
4. The gateway loads the new archive and exposes the updated composite schema.
5. Open Nitro at the gateway endpoint and query immediately.

If composition fails (for example, a field conflict or a missing lookup), the orchestrator logs the error and stops the AppHost. Fix the issue and rebuild. You get the same composition validation as the Nitro CLI, integrated into your build step.

## Next Steps

- **Need to compose without Aspire?** See the Nitro CLI composition workflow in [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph).
- **Need entity resolution patterns?** See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) for public vs. internal lookups, composite keys, and the node pattern.
- **Need cross-subgraph field dependencies?** See [Data Requirements](/docs/fusion/v16/data-requirements-and-mapping) for `@require` and FieldSelectionMap patterns.
- **Need visibility controls?** See [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution) for `@inaccessible`, `@internal`, `@deprecated`, `@requiresOptIn`, and `@override`.
