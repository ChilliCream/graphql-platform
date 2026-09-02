---
title: "Local GraphQL Federation Development with Aspire"
description: "Develop GraphQL subgraphs locally in isolation, then compose them into your full graph and test changes with Aspire and ChilliCream's Fusion gateway."
---

Fusion's local development experience is built on [Aspire](https://aspire.dev): an AppHost, written in C# or TypeScript, starts your subgraphs and composes them into a running gateway. Without it, every change to a type, a field, or a resolver means re-exporting the schema, re-composing with the Nitro CLI, and restarting the gateway.

The `HotChocolate.Fusion.Aspire` package integrates composition into the Aspire AppHost. When you run the AppHost, the orchestrator starts your subgraphs, fetches their source schemas from live endpoints, composes them into a Fusion archive, and writes it to the gateway project directory. This startup flow replaces the manual export-compose-restart cycle. Mark each local subgraph with `WithGraphQLHttpEndpoint()` so composition uses the schema exposed by the running service. The examples on this page show both AppHost languages.

If different teams own subgraphs in separate repositories, bind each team's AppHost to a shared Nitro stage. The AppHost composes the live schemas from the team's local subgraphs with the stage's published fusion configuration. That configuration supplies the schemas and routes for the remaining subgraphs. You can then develop and debug your team's subgraphs inside the full graph without copying another team's schema artifacts into your repository. See [Developing Across Teams and Repositories](#developing-across-teams-and-repositories).

# Prerequisites

You need an Aspire AppHost project with the `HotChocolate.Fusion.Aspire` package.

<LanguageTabs>
<CSharp>

If you do not have an AppHost yet, create it with:

```bash
dotnet new aspire-apphost -n AppHost
```

Add the Fusion Aspire package to the AppHost project:

```bash
cd AppHost
dotnet add package HotChocolate.Fusion.Aspire
```

</CSharp>
<TypeScript>

If you do not have an AppHost yet, create it with the Aspire CLI:

```bash
aspire new aspire-ts-empty -n AppHost -o .
```

`aspire add` does not list `HotChocolate.Fusion.Aspire`, so add the package to the `packages` section of `aspire.config.json` by hand:

```json filename="aspire.config.json"
{
  "packages": {
    "HotChocolate.Fusion.Aspire": "<version>"
  }
}
```

Then restore the AppHost, which regenerates the typed SDK under `.aspire/modules`:

```bash
aspire restore
```

</TypeScript>
</LanguageTabs>

Each subgraph project needs a `schema-settings.json` file in its project directory so the orchestrator can identify the source schema and choose how to fetch it. Generate this file with the `schema export` command from `HotChocolate.AspNetCore.CommandLine`. If you followed [Getting Started](./getting-started.md#export-the-schema), you already have it.

# Setting Up the AppHost

The AppHost wires together your subgraphs and gateway. A few resource extension methods configure the composition pipeline.

<LanguageTabs>
<CSharp>

```csharp filename="AppHost/Program.cs"
var builder = DistributedApplication.CreateBuilder(args);

builder.AddNitro();

var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLHttpEndpoint();

var reviewsApi = builder
    .AddProject<Projects.Reviews>("reviews-api")
    .WithGraphQLHttpEndpoint();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithNitroComposition()
    .WithReference(productsApi)
    .WithReference(reviewsApi);

builder.Build().Run();
```

</CSharp>
<TypeScript>

```typescript filename="apphost.mts"
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

await builder.addNitro();

const productsApi = await builder
  .addProject("products-api", "../Products/Products.csproj")
  .withGraphQLHttpEndpoint();

const reviewsApi = await builder
  .addProject("reviews-api", "../Reviews/Reviews.csproj")
  .withGraphQLHttpEndpoint();

await builder
  .addProject("gateway-api", "../Gateway/Gateway.csproj")
  .withNitroComposition()
  .withReference(productsApi)
  .withReference(reviewsApi);

await builder.build().run();
```

</TypeScript>
</LanguageTabs>

Four things to notice:

- **`AddNitro()`** adds the Nitro resource and registers the composition orchestrator with the Aspire eventing system. Calling it again returns the same resource.
- **`WithGraphQLHttpEndpoint()`** declares the GraphQL route of a subgraph and the path its source schema is downloaded from. The orchestrator waits for the subgraph to start, then fetches the source schema over HTTP.
- **`WithNitroComposition()`** marks the gateway as needing composition. The orchestrator discovers all referenced subgraphs, extracts their schemas, composes them, and writes a `gateway.far` file to the gateway project directory.
- **`WithReference()`** is standard Aspire. It tells the orchestrator which subgraphs to include in composition for this gateway.

In a TypeScript AppHost, the same extension methods surface camelCased, optional parameters are gathered into a single options object, and every chained call is awaited.

When you build and run the AppHost, the orchestrator handles the entire composition pipeline automatically. No manual `nitro fusion compose` step needed.

# Live Schema Extraction

`WithGraphQLHttpEndpoint()` declares two paths: `path` is the GraphQL route the subgraph serves (`/graphql` by default), and `schemaPath` is where the schema document is downloaded from (`/graphql/schema.graphql` by default). An Apollo Federation subgraph serves its schema through the GraphQL endpoint at `path` via `_service.sdl`, so `schemaPath` is ignored for it. The orchestrator reads `schema-settings.json` to tell the two kinds of subgraphs apart.

The orchestrator starts each subgraph, waits for it to become healthy, then fetches its schema. If the subgraph is not ready within the timeout, the orchestrator reports an error and the dependent gateway fails to start. Other resources continue running.

You can customize the GraphQL route, the schema download path, and the endpoint name. You can also provide the expected source schema name to validate the fetched schema:

<LanguageTabs>
<CSharp>

```csharp
var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLHttpEndpoint(
        path: "/graphql",
        schemaPath: "/graphql/schema.graphql",
        endpointName: "http",
        sourceSchemaName: "Products");
```

</CSharp>
<TypeScript>

```typescript
const productsApi = await builder
  .addProject("products-api", "../Products/Products.csproj")
  .withGraphQLHttpEndpoint({
    path: "/graphql",
    schemaPath: "/graphql/schema.graphql",
    endpointName: "http",
    sourceSchemaName: "Products",
  });
```

</TypeScript>
</LanguageTabs>

Every parameter has a default, so pass only what differs. The source schema name comes from `schema-settings.json`. When you pass `sourceSchemaName`, it acts as an assertion and must match that configured name exactly. It does not rename the source schema, so you can usually omit it.

# Developing Across Teams and Repositories

In an organization where teams own subgraphs in separate repositories, each team can keep an AppHost alongside the subgraphs it develops. Register each local subgraph with `WithGraphQLHttpEndpoint()` so the AppHost fetches its live schema. Bind the AppHost to the graph's shared Nitro stage to compose those local schemas with the published fusion configuration. Nitro supplies the remaining source schemas and routes, so the gateway still represents the full graph while only your team's subgraphs run on your machine.

Sign in once with the Nitro CLI ([installation](./cli.md#installation)). The AppHost reads the session that the CLI stores and never signs in on its own:

```bash
nitro login
```

Then declare the Nitro API and its stages, and select the stage that is the composition base of the gateway:

<LanguageTabs>
<CSharp>

```csharp filename="AppHost/Program.cs"
var builder = DistributedApplication.CreateBuilder(args);

var nitro = builder.AddNitro();

var productsFusionApi = nitro
    .AddApi("products-fusion")
    .WithNitroApiId("QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==");

var devStage = productsFusionApi.AddStage("dev");
productsFusionApi.AddStage("production");

var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLHttpEndpoint();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithNitroComposition()
    .WithNitroCompositionBase(devStage)
    .WithReference(productsApi);

builder.Build().Run();
```

</CSharp>
<TypeScript>

```typescript filename="apphost.mts"
import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const nitro = await builder.addNitro();

const productsFusionApi = await nitro
  .addApi("products-fusion")
  .withNitroApiId("QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==");

const devStage = await productsFusionApi.addStage("dev");
await productsFusionApi.addStage("production");

const productsApi = await builder
  .addProject("products-api", "../Products/Products.csproj")
  .withGraphQLHttpEndpoint();

await builder
  .addProject("gateway-api", "../Gateway/Gateway.csproj")
  .withNitroComposition()
  .withNitroCompositionBase(devStage)
  .withReference(productsApi);

await builder.build().run();
```

</TypeScript>
</LanguageTabs>

- **`AddNitro()`** adds the declarative Nitro root and registers the composition orchestration once.
- **`AddApi(name)`** declares a Nitro API. **`WithNitroApiId(apiId)`** belongs to that API declaration and carries the id that the Nitro dashboard and the Nitro CLI report for the API, the same value that `--api-id` takes. A stage whose API has no id is reported on the gateway console, because it cannot take effect.
- **`AddStage(name)`** declares a stage of that API. A stage is both a local composition base and a publishing target.
- **`WithNitroCompositionBase(stage)`** selects the stage whose fusion configuration the gateway composes on top of. Different gateways can select different stages, or stages of different APIs. Selecting a second stage for the same gateway throws while the AppHost builds, because a gateway composes against a single stage.

## What Happens When a Gateway Starts

Before the orchestrator starts a gateway process with a Nitro composition base:

1. The fusion configuration of the selected API and stage is downloaded from Nitro. The download runs while the orchestrator waits for the local subgraphs to become healthy, so the two waits do not add up.
2. Each source schema of the distributed application replaces the source schema of the same name in the downloaded configuration. A source schema whose name does not appear there is added to the composition.
3. Each source schema that the distributed application runs is reached at the allocated HTTP endpoint of its Aspire resource. The orchestrator combines that endpoint with the path of the `url` in the subgraph's `schema-settings.json`, or with `/graphql` when the settings define no usable path.
4. Each source schema that only the downloaded configuration carries is external. The gateway reaches it at its `devUrl`, or at its `url` when no `devUrl` is defined. Composition logs a warning for every external source schema without a `devUrl`, because a deployed URL is often not reachable from a developer machine. A subgraph that runs in the local AppHost but has no allocated HTTP endpoint at composition time cannot receive an injected URL either, so it is treated like an external schema for URL resolution, which is why such a resource can also trigger the missing-devUrl warning. See [`transports.http.devUrl`](./cli.md#transports-http-devurl).
5. The composed archive is written to the gateway project directory as usual. The gateway console then reports which fusion configuration it composed against, when that configuration was downloaded, and which external source schemas it carries together with the URLs they resolved to.

The downloaded configuration is the only base the composition builds on. What a previous composition wrote to `gateway.far` is never an input again, so a source schema that was removed or renamed upstream also disappears from your next run.

Variable substitution follows the same split as the URL resolution. The settings of the subgraphs you run resolve against the `Aspire` environment in `schema-settings.json`, while the settings that the downloaded configuration carries resolve against the name of the selected stage.

A composition or download failure fails only the gateway it belongs to. The rest of the distributed application keeps running.

## Matching Source Schemas by Name

Replacement matches source schemas by name. `WithGraphQLHttpEndpoint()` uses the `name` from the subgraph's `schema-settings.json`. If you pass `sourceSchemaName`, it must match that name exactly or composition fails. It does not rename the source schema.

A local source schema whose configured name does not match the one in Nitro is added next to it instead of replacing it, which usually surfaces as a composition error about a conflicting field. Make sure the `name` in `schema-settings.json` matches the published source schema name.

## Rebuilding a Subgraph

A rebuild or restart of a local subgraph recomposes the gateway schema exactly as it does without Nitro, and the recomposition reuses the fusion configuration that was downloaded when the gateway started. A run therefore downloads once per gateway, and your inner loop stays as fast as a local-only composition. To pick up what was published to the stage in the meantime, use the gateway's Recompose command described below, or restart the AppHost.

When a gateway never acquired a configuration because it failed to start, later composition attempts are skipped with a log entry instead of composing against a stale base.

You do not need to stop the run to restart one subgraph. While `aspire run` keeps the AppHost in the foreground, restart the subgraph from a second terminal with the Aspire CLI:

```bash
aspire resource products-api restart
```

The command executes against the running AppHost. The orchestrator sees the restart, recomposes the schema of every gateway that references the subgraph, and logs the recomposition on the gateway console. When a recomposition fails, the gateway keeps the previous schema. The dashboard offers the same action as Restart in the resource submenu on the Resources page.

The gateway additionally exposes a Recompose command. It downloads a fresh fusion configuration first, so it also picks up what was published to the stage since the run started, without an AppHost restart. Invoke it from the resource submenu of the gateway or from the CLI:

```bash
aspire resource gateway-api recompose
```

## Working Offline

Every downloaded fusion configuration is cached per Nitro API URL, API id, and stage, next to the Nitro CLI configuration (`~/.config/nitro/cache/fusion` on macOS and Linux, `%APPDATA%\nitro\cache\fusion` on Windows). The cache lives outside your repository, so it survives a clean and is shared across working trees.

When the download fails for any reason, including no network, a rejected or expired sign-in, and a stage without a fusion configuration, the gateway composes against the cached copy. Its console gets a warning that the configuration could not be refreshed, names the timestamp of the cached copy, and tells you to run `nitro login` when the sign-in expired. Only when there is no cached copy at all does the gateway fail to start with that reason as its error.

## Continuous Integration and Self-Hosted Nitro

Two environment variables configure the AppHost's connection to Nitro. They carry the same names and the same meaning as in the Nitro CLI, so a shell that is set up for the CLI also configures the AppHost.

| Variable          | Purpose                                                                                                                                                                       |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NITRO_API_KEY`   | Authenticates with an API key instead of an interactive session. It takes precedence over the CLI session file. Set it where `nitro login` cannot run, for example in CI.     |
| `NITRO_CLOUD_URL` | Points the AppHost at a self-hosted Nitro instance. Without it, the AppHost uses the API URL that `nitro login` stored, and `https://api.chillicream.com` when there is none. |

# Composition Settings

Composition settings are configured in Nitro. Open the gateway document and go to Settings > Schema Registry > Composition to control how source schemas are merged when composing the gateway's configuration.

![The Composition settings page of a gateway in Nitro, with toggles for global object identification and removing unreferenced definitions, merge behavior for @cacheControl and @tag, and an excluded-by-tag list](../../../public/images/fusion-docs/nitro-composition-settings.webp)

The page carries toggles for global object identification and for removing unreferenced definitions, merge behavior selections for the `@cacheControl` and `@tag` directives, and a tag list that excludes tagged definitions from the composite schema.

Settings apply at publish time. The banner on the page states that changes take effect from the next publish onward, and only when publishing with the `nitro-fusion-publish` GitHub Action, the `NitroFusionPublish` Azure DevOps task, or the `nitro fusion publish` CLI command. A stage keeps the fusion configuration it already carries until that next publish, so changing a setting does not alter what a bound AppHost downloads in the meantime.

The output file name defaults to `gateway.far`. You can change it if needed:

<LanguageTabs>
<CSharp>

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithNitroComposition(outputFileName: "composed.far")
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

</CSharp>
<TypeScript>

```typescript
await builder
  .addProject("gateway-api", "../Gateway/Gateway.csproj")
  .withNitroComposition({ outputFileName: "composed.far" })
  .withReference(productsApi)
  .withReference(reviewsApi);
```

</TypeScript>
</LanguageTabs>

# How Composition Fits the Dev Loop

With Aspire, your inner dev loop looks like this:

1. Change code in a subgraph (add a field, modify a type, adjust a resolver).
2. Build and run the AppHost.
3. The orchestrator starts your subgraphs, extracts their schemas, and composes the Fusion archive.
4. The gateway loads the new archive and exposes the updated composite schema.
5. Open Nitro at the gateway endpoint and query immediately.

With `WithNitroCompositionBase`, step 3 composes on top of the fusion configuration that was downloaded when the gateway started, so the loop stays the same while your subgraph runs inside the full graph.

If composition fails (for example, a field conflict or a missing lookup), the orchestrator logs the error on the gateway console and the gateway fails to start. Every other resource keeps running, so you can fix the issue and restart the gateway, which composes again. You get the same composition validation as the Nitro CLI, integrated into your build step.

## Validating Changes Against the Stage

When a gateway selects a Nitro stage with `WithNitroCompositionBase` and the API of that stage carries an id, every composition is followed by a validation of the composed gateway schema against that stage. The AppHost uploads the schema to Nitro, and Nitro compares it with the schema version and the clients registered for the API and stage:

- Schema changes are classified as breaking, dangerous, or safe.
- Every registered client is checked with its persisted operations. A violated operation is reported with its hash, its deployed tags, and the message, code, path, and location of each error.

Validation is observational. The composed archive is installed before validation starts, so a finding never blocks the gateway, and the gateway serves the newly composed schema either way. Findings surface in two places:

- The gateway console logs the findings, grouped by client and operation.
- The Aspire dashboard shows a notification that names the gateway and the stage and links to the console logs. When a later composition passes again, a follow-up notification reports the recovery. A run that never violates anything triggers no notifications.

A composition that yields an unchanged gateway schema is not validated again, so a restart without schema changes sends no request to Nitro. Validation runs only while the AppHost runs; `aspire publish` never validates.

To turn validation off for a gateway:

<LanguageTabs>
<CSharp>

```csharp
builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithNitroComposition(disableValidation: true);
```

</CSharp>
<TypeScript>

```typescript
await builder
  .addProject("gateway-api", "../Gateway/Gateway.csproj")
  .withNitroComposition({ disableValidation: true });
```

</TypeScript>
</LanguageTabs>

Disabling validation turns off only the schema upload and the reports. Composition, the configuration download, and the archive install are unaffected.

# Deploying with Aspire

The same AppHost that drives your dev loop can also drive a release. Instead of composing on your machine, the integration uploads each source schema to Nitro under an immutable tag, then composes and publishes from those exact versions inside your deployment job.

The work is split across two commands, and both are opt-in. They are not attached to `aspire publish` or `aspire deploy`, so nothing contacts Nitro unless you ask for it by name:

| Command                    | Runs in        | What it does                                                                   |
| -------------------------- | -------------- | ------------------------------------------------------------------------------ |
| `aspire do fusion-upload`  | Build job      | Produces source artifacts and reconciles each `name@tag` version in Nitro.     |
| `aspire do fusion-publish` | Deployment job | Downloads those exact versions, composes them, and publishes to a Nitro stage. |

The two jobs never exchange a file. Nitro is the handoff: the build job writes immutable versions, the deployment job reads them back by tag. Both jobs evaluate the same AppHost, so they must run from the same commit.

## Declaring APIs and Stages

Publishing uses the same Nitro resource graph as [Developing Across Teams and Repositories](#developing-across-teams-and-repositories). It describes each API and every stage that the AppHost is allowed to publish to, and the same stage resources can be selected as local composition bases.

<LanguageTabs>
<CSharp>

```csharp filename="AppHost/Program.cs"
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder
    .AddNitro()
    .WithNitroCloudUrl("https://api.chillicream.com")
    .WithNitroApiKey(nitroApiKey);

var productsApi = nitro
    .AddApi("products-fusion")
    .WithNitroApiId("QXBpCnByb2R1Y3Rz");

productsApi.AddStage("development")
    .WithApproval(waitForApproval: false);

productsApi.AddStage("production")
    .WithApproval(waitForApproval: true)
    .WithForcePublish(force: false);
```

</CSharp>
<TypeScript>

```typescript filename="apphost.mts"
const nitroApiKey = await builder.addParameter("nitroApiKey", { secret: true });

const nitro = await builder
  .addNitro()
  .withNitroCloudUrl("https://api.chillicream.com")
  .withNitroApiKey(nitroApiKey);

const productsApi = await nitro
  .addApi("products-fusion")
  .withNitroApiId("QXBpCnByb2R1Y3Rz");

await (await productsApi.addStage("development"))
  .withApproval({ waitForApproval: false });

await (await productsApi.addStage("production"))
  .withApproval({ waitForApproval: true })
  .withForcePublish({ force: false });
```

</TypeScript>
</LanguageTabs>

- **`AddApi()`** declares one Nitro API. Every API requires its own Nitro API id.
- **`AddStage()`** declares one publishable stage. An API with no stages is rejected as an invalid pipeline declaration.
- **`WithApproval()`** makes the publication wait for a human to approve it in Nitro before it commits.
- **`WithForcePublish()`** permits publication after a known validation failure. Reserve it for an explicit operational policy.

The cloud URL can also come from `Nitro:CloudUrl` or `NITRO_CLOUD_URL`, as described in [Continuous Integration and Self-Hosted Nitro](#continuous-integration-and-self-hosted-nitro). API ids stay on their API declarations so an AppHost can model more than one API.

`WithNitroApiKey()` is optional. When it is omitted, the publishing commands first check `Nitro:ApiKey` and `NITRO_API_KEY`, then use the active Nitro CLI session. This makes an explicit API key the normal choice for non-interactive CI while allowing a signed-in developer to run the same commands locally.

A stage composes with an environment named after the stage itself, so a stage called `production` resolves `{{VARIABLE_NAME}}` placeholders against the `production` environment in `schema-settings.json`.

## Preparing Sources for Publishing

Publishing composes from artifacts, not from running services, so each source needs a `schema-settings.json` whose `url` points at the address the deployed gateway will call. A loopback address such as `http://localhost:5000/graphql` is rejected, because it cannot be reachable from production. The source name has to be unique and match the `name` property in that file exactly.

You can supply the schema two ways:

- **`WithGraphQLSchemaExport()`** runs `dotnet run -- schema export` on the source project for every upload, reusing the project's `schema-settings.json`. The project has to be set up for that command, which means the `HotChocolate.AspNetCore.CommandLine` package and returning `app.RunWithGraphQLCommandsAsync(args)` from `Program.cs`. See [Command Line](../hotchocolate/server/command-line.md).
- **`WithGraphQLHttpEndpoint()`** downloads the schema over HTTP. The endpoint has to be reachable from the build agent and bound to a fixed target port, because the publishing pipeline does not start your resources. Pass `sourceSchemaName` explicitly so the deployment job can request the same immutable source without reading the build checkout.

The export takes an optional schema name that defaults to the Aspire resource name. Pass it to select a named schema, or when the source name differs from the resource name:

```csharp filename="AppHost/Program.cs"
var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaExport("Products");
```

The effective source name has to match the `name` in `schema-settings.json` exactly. `WithGraphQLSchemaEndpoint()` and `WithGraphQLSchemaFile()` still work for a checked-in schema file, but both are retired in favor of the two options above.

## Running the Two Jobs

The commands receive `tag` and `stage` when they run. These values are invocation inputs, not resources in the AppHost topology. Prefer environment variables for CI.

**Build job**

```bash
export NITRO_TAG="$GITHUB_SHA"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

**Deployment job**

```bash
export NITRO_STAGE="production"
export NITRO_TAG="$GITHUB_SHA"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-publish \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

Parameters can also be forwarded to the AppHost after `--`:

```bash
aspire do fusion-publish -- \
  --stage=production \
  --tag="$GITHUB_SHA"
```

`fusion-upload` requires `tag`. `fusion-publish` requires both `tag` and `stage`, and fails when the named stage is not declared on every participating API. The equivalent environment variables are `NITRO_TAG` and `NITRO_STAGE`.

Use one tag for the upload and for every stage in the rollout. Promoting a release to a second stage is the deployment job again with a different stage and the same tag, which is what makes a promotion provably the same configuration.

# Next Steps

- **Need to compose without Aspire?** See the Nitro CLI composition workflow in [Adding a Subgraph](./adding-a-subgraph.md).
- **Need entity resolution patterns?** See [Entities and Lookups](./entities-and-lookups.md) for public vs. internal lookups, composite keys, and the node pattern.
- **Need cross-subgraph field dependencies?** See [Data Requirements](./data-requirements-and-mapping.md) for `@require` and FieldSelectionMap patterns.
- **Need visibility controls?** See [Schema Exposure and Evolution](./schema-exposure-and-evolution.md) for `@inaccessible`, `@internal`, `@deprecated`, `@requiresOptIn`, and `@override`.
- **Driving the run from the terminal?** [`aspire run`](https://aspire.dev/reference/cli/commands/aspire-run/) and [`aspire resource`](https://aspire.dev/reference/cli/commands/aspire-resource/) in the Aspire CLI reference apply to C# and TypeScript AppHosts alike.
