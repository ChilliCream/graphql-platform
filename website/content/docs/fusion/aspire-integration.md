---
title: "Aspire Integration"
description: "Integrate Fusion composition into a .NET Aspire AppHost with `HotChocolate.Fusion.Aspire`: one build step composes subgraph schemas into a gateway archive."
---

Nitro gives you full control over composition, but during active development you want a tighter loop. Every time you change a type, add a field, or adjust a resolver, you need to re-export the schema, re-compose, and restart the gateway. That friction adds up.

The `HotChocolate.Fusion.Aspire` package integrates composition into the .NET Aspire AppHost. When you build the AppHost, the orchestrator starts your subgraphs, extracts their source schemas, composes them into a Fusion archive, and writes it to the gateway project directory. One build step replaces the manual export-compose-restart cycle. You can also mix live subgraphs with pre-exported schema files, letting you develop against a full composite schema even when you only run a subset of services locally.

If your graph is published to Nitro, the AppHost can pull the fusion configuration of a stage instead and compose the subgraphs you run locally into it. You then develop and debug your subgraph inside the full graph without checking a single foreign schema file into your repository. See [Composing Against the Graph in Nitro](#composing-against-the-graph-in-nitro).

# Prerequisites

You need a .NET Aspire AppHost project. If you do not have one yet, create it with:

```bash
dotnet new aspire-apphost -n AppHost
```

Add the Fusion Aspire package to the AppHost project:

```bash
cd AppHost
dotnet add package HotChocolate.Fusion.Aspire
```

Your subgraph projects need the `HotChocolate.AspNetCore.CommandLine` package so the orchestrator can extract their schemas. If you followed the [Getting Started](./getting-started.md) tutorial, your subgraphs already have this.

# Setting Up the AppHost

The AppHost wires together your subgraphs and gateway. Three extension methods configure the composition pipeline.

**C# configuration**

```csharp filename="AppHost/Program.cs"
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

# Live Schema Extraction

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

# Working with Partial Graphs

You do not need to run every subgraph locally. When your system has many subgraphs but you only develop on a few, use `WithGraphQLSchemaFile()` to include pre-exported schema files for the subgraphs you are not running.

```csharp filename="AppHost/Program.cs"
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

# Composing Against the Graph in Nitro

`WithGraphQLSchemaFile()` keeps the schemas of the services you do not run in your repository. When your graph is published to Nitro, you can pull them instead. The AppHost downloads the fusion configuration of a stage for each gateway and composes the subgraphs you run locally into it. Everything you do not run stays in the composed schema and keeps pointing at the URL the configuration carries, so a query can traverse the whole graph while only your own subgraph runs on your machine.

Sign in once with the Nitro CLI ([installation](./cli.md#installation)). The AppHost reads the session that the CLI stores and never signs in on its own:

```bash
nitro login
```

Then bind the AppHost to a stage and tell the gateway which Nitro API carries its fusion configuration:

```csharp filename="AppHost/Program.cs"
var builder = DistributedApplication.CreateBuilder(args);

builder.AddNitro("dev");

var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaEndpoint();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithNitroApiId("QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==")
    .WithGraphQLSchemaComposition()
    .WithReference(productsApi);

builder.Build().Run();
```

- **`AddNitro(stage)`** registers the same orchestrator as `AddGraphQLOrchestrator()` and binds the distributed application to one stage in Nitro. Calling both methods is safe and registers the orchestrator once. Calling `AddNitro` twice with different stage names throws while the AppHost builds, because a run composes against a single stage. Keep `AddGraphQLOrchestrator()` when you compose only the subgraphs you run locally: it never contacts Nitro.
- **`WithNitroApiId(apiId)`** selects the Nitro API whose fusion configuration a gateway composes against. The API id is the id that the Nitro dashboard and the Nitro CLI report for the API, the same value that `--api-id` takes. Calling the method again replaces the previously configured id. On a resource that is not composed, the API id is accepted as identity metadata for your own tooling and the pull and compose flow ignores it. If you set an API id without calling `AddNitro`, the resource console tells you that it cannot take effect.

## What Happens When a Gateway Starts

Before the orchestrator starts a gateway process that carries an API id:

1. The fusion configuration for that API id and the `AddNitro` stage is downloaded from Nitro. The download runs while the orchestrator waits for the local subgraphs to become healthy, so the two waits do not add up.
2. Each source schema of the distributed application replaces the source schema of the same name in the downloaded configuration. A source schema whose name does not appear there is added to the composition.
3. Each source schema that the distributed application runs is reached at the allocated HTTP endpoint of its Aspire resource. The orchestrator combines that endpoint with the path of the `url` in the subgraph's `schema-settings.json`, or with `/graphql` when the settings define no usable path.
4. Each source schema that only the downloaded configuration carries is external. The gateway reaches it at its `devUrl`, or at its `url` when no `devUrl` is defined. Composition logs a warning for every external source schema without a `devUrl`, because a deployed URL is often not reachable from a developer machine. A subgraph that runs in the local AppHost but has no allocated HTTP endpoint at composition time cannot receive an injected URL either, so it is treated like an external schema for URL resolution, which is why such a resource can also trigger the missing-devUrl warning. See [`transports.http.devUrl`](./cli.md#transports-http-devurl).
5. The composed archive is written to the gateway project directory as usual. The gateway console then reports which fusion configuration it composed against, when that configuration was downloaded, and which external source schemas it carries together with the URLs they resolved to.

The downloaded configuration is the only base the composition builds on. What a previous composition wrote to `gateway.far` is never an input again, so a source schema that was removed or renamed upstream also disappears from your next run.

Variable substitution follows the same split as the URL resolution. The settings of the subgraphs you run resolve against `EnvironmentName` (see [Composition Settings](#composition-settings)), while the settings that the downloaded configuration carries resolve against the stage name you passed to `AddNitro`.

A composition or download failure fails only the gateway it belongs to. The rest of the distributed application keeps running.

## Matching Source Schemas by Name

Replacement matches source schemas by name, and the name of a local resource is determined before the configuration is downloaded:

- `WithGraphQLSchemaEndpoint()` uses the `name` from the subgraph's `schema-settings.json`. A `sourceSchemaName` that disagrees with it fails the composition.
- `WithGraphQLSchemaFile()` uses `sourceSchemaName`, or the Aspire resource name when you do not pass one, and does not check it against the settings file.

A local source schema whose name does not match the one in Nitro is added next to it instead of replacing it, which usually surfaces as a composition error about a conflicting field. Pass `sourceSchemaName` explicitly when the Aspire resource name differs from the published source schema name.

## Rebuilding a Subgraph

A rebuild or restart of a local subgraph recomposes the gateway schema exactly as it does without Nitro, and the recomposition reuses the fusion configuration that was downloaded when the gateway started. A run therefore downloads once per gateway, and your inner loop stays as fast as a local-only composition. Restart the AppHost to pick up what was published to the stage in the meantime.

When a gateway never acquired a configuration because it failed to start, its recompositions are skipped with a log entry instead of composing against a stale base.

## Working Offline

Every downloaded fusion configuration is cached per Nitro API URL, API id, and stage, next to the Nitro CLI configuration (`~/.config/nitro/cache/fusion` on macOS and Linux, `%APPDATA%\nitro\cache\fusion` on Windows). The cache lives outside your repository, so it survives a clean and is shared across worktrees.

When the download fails for any reason, including no network, a rejected or expired sign-in, and a stage without a fusion configuration, the gateway composes against the cached copy. Its console gets a warning that the configuration could not be refreshed, names the timestamp of the cached copy, and tells you to run `nitro login` when the sign-in expired. Only when there is no cached copy at all does the gateway fail to start with that reason as its error.

## Continuous Integration and Self-Hosted Nitro

Two environment variables configure the integration. They carry the same names and the same meaning as in the Nitro CLI, so a shell that is set up for the CLI also configures the AppHost.

| Variable          | Purpose                                                                                                                                                                               |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NITRO_API_KEY`   | Authenticates with an API key instead of an interactive session. It takes precedence over the CLI session file. Set it where `nitro login` cannot run, for example in CI.             |
| `NITRO_CLOUD_URL` | Points the integration at a self-hosted Nitro instance. Without it, the integration uses the API URL that `nitro login` stored, and `https://api.chillicream.com` when there is none. |

# Composition Settings

`WithGraphQLSchemaComposition()` accepts a settings parameter that controls composition behavior.

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition(
        settings: new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true,
            NodeResolution = NodeResolution.SourceSchema,
            EnvironmentName = "Local"
        })
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

- **`EnableGlobalObjectIdentification`** enables the `Node` interface and Relay-style global object IDs in the composite schema. Set this to `true` if your subgraphs use the `[NodeResolver]` pattern or source-schema node resolution.
- **`NodeResolution`** controls whether the gateway decodes `Query.node` identifiers or forwards them to a source schema. If you do not set it, composition uses the archive's stored value, or `NodeResolution.Gateway` when the archive has no value. `NodeResolution.SourceSchema` requires `EnableGlobalObjectIdentification = true`.
- **`AllowNonResolvableInterfaceObjects`** allows Apollo Federation interface objects with `resolvable: false` keys to compose when Fusion cannot build a route to their projected fields. The default is `false`. Enabling it can move an unresolved selection from composition time to a field error at runtime. See [Allow Non-Resolvable Interface Objects](./connectors/apollofederation.md#allow-non-resolvable-interface-objects).
- **`ShareableFieldRuntimeTypeRouting`** controls how Fusion routes type-conditioned selections for Apollo Federation shareable fields that return an interface or union. `SourceLocal` is the default and follows the source that resolves the field. `CommonRuntimeTypes` routes type-conditioned selections only for runtime types common to Apollo providers already in the current provider scope or directly reachable from it through one entity lookup. At an operation root, Fusion considers all non-external providers. See [Shareable Abstract Field Routing](./connectors/apollofederation.md#shareable-abstract-field-routing).
- **`EnvironmentName`** selects the environment in `schema-settings.json` that `{{VARIABLE_NAME}}` placeholders resolve against. It defaults to `Aspire`, and the lookup is case sensitive, so the environment name in the settings file has to be spelled exactly like the configured value. You do not need an environment just to point URLs at local ports: the orchestrator injects the allocated endpoint of each locally running subgraph into the composed configuration and only keeps the path of the configured `url`.

The output file name defaults to `gateway.far`. You can change it if needed:

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition(outputFileName: "composed.far")
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

# How Composition Fits the Dev Loop

With Aspire, your inner dev loop looks like this:

1. Change code in a subgraph (add a field, modify a type, adjust a resolver).
2. Build and run the AppHost.
3. The orchestrator starts your subgraphs, extracts their schemas, and composes the Fusion archive.
4. The gateway loads the new archive and exposes the updated composite schema.
5. Open Nitro at the gateway endpoint and query immediately.

With `AddNitro`, step 3 composes on top of the fusion configuration that was downloaded when the gateway started, so the loop stays the same while your subgraph runs inside the full graph.

If composition fails (for example, a field conflict or a missing lookup), the orchestrator logs the error on the gateway console and the gateway fails to start. Every other resource keeps running, so you can fix the issue and restart the gateway, which composes again. You get the same composition validation as the Nitro CLI, integrated into your build step.

# Next Steps

- **Need to compose without Aspire?** See the Nitro CLI composition workflow in [Adding a Subgraph](./adding-a-subgraph.md).
- **Need entity resolution patterns?** See [Entities and Lookups](./entities-and-lookups.md) for public vs. internal lookups, composite keys, and the node pattern.
- **Need cross-subgraph field dependencies?** See [Data Requirements](./data-requirements-and-mapping.md) for `@require` and FieldSelectionMap patterns.
- **Need visibility controls?** See [Schema Exposure and Evolution](./schema-exposure-and-evolution.md) for `@inaccessible`, `@internal`, `@deprecated`, `@requiresOptIn`, and `@override`.
