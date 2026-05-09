---
title: Export a schema
---

You can export the Hot Chocolate schema SDL directly from your ASP.NET Core application—the same one that serves production traffic. This exported SDL file becomes your stable contract for pull request reviews, CI artifacts, schema registry validation, breaking-change checks, documentation, and client generation.

This guide focuses on Hot Chocolate v16 server schemas. If you are working with Fusion gateways, refer to the Fusion documentation, as their composition and deployment workflows differ.

# Exporting the Schema SDL

## Prerequisites

Before you begin, ensure you have:

- A Hot Chocolate v16 ASP.NET Core server project.
- A reference to `HotChocolate.AspNetCore.CommandLine` in your project.
- A `Program.cs` that returns the exit code from either `RunWithGraphQLCommandsAsync(args)` or `RunWithGraphQLCommands(args)`.
- All configuration required to build the schema available in your shell or CI job.

To export the schema, run the following command from your repository root, specifying your server project:

```bash
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

You should see output similar to:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

The `schema export` command builds your application host, resolves the Hot Chocolate request executor provider, builds the selected request executor, and writes the output files. The resulting SDL represents the public GraphQL contract for your configured server.

Make sure to return the command's exit code from `Program.cs`. This ensures that schema build failures cause CI jobs to fail, preventing missing or outdated artifacts from being treated as successful builds.

# Integrating Command-Line Export in Program.cs

First, install the command-line package in your server project. Then, update your application to run through the GraphQL command wrapper:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

With this setup:

- Running `dotnet run` starts the server as usual.
- Running `dotnet run -- schema export` executes the export command and exits after writing the schema files.

Prefer the async form (`RunWithGraphQLCommandsAsync`) when possible, as it returns an `int` exit code in `Program.cs`. The synchronous `RunWithGraphQLCommands(args)` is also available and returns an exit code in v16.

Avoid building a separate schema for CI unless you have a clear, documented reason. Using a different schema builder for CI can cause drift from your production configuration, resulting in contracts that do not match what clients actually use.

# What Gets Generated

The `schema export` command is file-oriented: it writes both the SDL and a settings file to the specified location.

| File                   | Purpose                                                                                                        |
| ---------------------- | -------------------------------------------------------------------------------------------------------------- |
| `schema.graphqls`      | The SDL generated from `executor.Schema`. Hot Chocolate writes this as UTF-8 (no BOM) with a trailing newline. |
| `schema-settings.json` | Metadata for tools, including the schema name and a default HTTP transport URL.                                |

Example SDL output:

```graphql
schema {
  query: Query
}

type Query {
  productById(id: ID!): Product
}
```

Example settings file:

```json
{
  "name": "_Default",
  "transports": {
    "http": {
      "url": "http://localhost:5000/graphql"
    }
  }
}
```

The default URL in the settings file is for tool metadata only; it is not discovered from Kestrel. Update this value if a downstream tool needs to connect to a different endpoint.

If you do not specify `--output`, Hot Chocolate writes `schema.graphqls` and `schema-settings.json` to the current working directory. Use `schema print` if you want the SDL sent to stdout instead of a file.

# Choosing an Output Path

In CI environments, always specify an explicit `.graphqls` file path:

```bash
mkdir -p artifacts
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

How the output path affects written files:

| Command shape               | Written files                                                                           |
| --------------------------- | --------------------------------------------------------------------------------------- |
| No `--output`               | `./schema.graphqls` and `./schema-settings.json`                                        |
| `--output schema.graphql`   | `schema.graphql` and `schema-settings.json`                                             |
| `--output schema.graphqls`  | `schema.graphqls` and `schema-settings.json`                                            |
| `--output artifacts/schema` | `artifacts/schema.graphqls` and `artifacts/schema-settings.json`, if `artifacts` exists |
| `--output artifacts/`       | `artifacts/schema.graphqls` and `artifacts/schema-settings.json`, if `artifacts` exists |

Always create the output directory before running the export command. Add generated files to source control if they serve as your review baseline, or upload them as CI artifacts if your schema registry is the source of truth.

# Exporting a Specific Schema When Multiple Exist

If your server registers multiple schemas, list them before exporting to ensure you select the correct one:

```bash
dotnet run -- schema list
dotnet run -- schema export --schema-name Catalog --output artifacts/catalog.graphqls
```

Example output from `schema list`:

```text
_Default
Catalog
Admin
```

If you do not specify `--schema-name`, Hot Chocolate exports `_Default` if it exists, or the first registered schema otherwise. In CI, always log the output of `schema list` when your server hosts multiple public contracts, and export each contract to its own SDL artifact.

# Printing SDL to stdout for Shell Pipelines

If you need the raw SDL on stdout for use in scripts or pipelines, use `schema print`:

```bash
dotnet run -- schema print --schema-name Catalog > artifacts/catalog.graphqls
```

The `schema print` command writes only the SDL to stdout and does not create a `schema-settings.json` file. It supports `--schema-name`, but does not support `--semantic-non-null` in v16. Use `schema export` if downstream tools require both the schema file and the settings file.

# Exporting from the Configured Application

Treat the exported SDL as a build artifact, not a hand-maintained design file. Any change that affects schema construction will affect the SDL, including:

- Registered root types, object types, scalars, directives, and schema extensions
- Schema options (such as XML documentation and field ordering)
- Feature flags and environment-specific registrations
- Application services required by schema-time components
- Hot Chocolate and .NET package versions

Always set the environment to match your production contract when exporting:

```bash
ASPNETCORE_ENVIRONMENT=Production \
DOTNET_ENVIRONMENT=Production \
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

You should not need production secrets to build the schema. While resolver service injection is rarely required for SDL export, schema services, diagnostic listeners, error filters, interceptors, and other schema-time components may need application services. In v16, register these with `AddApplicationService<T>()`:

```csharp
builder.Services.AddSingleton<SchemaMetadataProvider>();

builder.AddGraphQL()
    .AddApplicationService<SchemaMetadataProvider>()
    .AddQueryType<Query>();
```

Settings like Nitro UI enablement, allowed GET operations, and introspection permissions do not affect the schema shape. However, schema download endpoints and internal directive visibility can change what users can download from a running server.

# Including or Hiding Internal Directives

By default, Hot Chocolate v16 hides internal directives from the public SDL. This prevents metadata such as authorization policies from appearing in schema downloads or exported public SDL files.

Keep this default for governance artifacts that leave your trusted boundary. If you have a trusted internal workflow that requires internal directives, you can opt in explicitly:

```csharp
builder.AddGraphQL()
    .ModifyOptions(o => o.DisableInternalDirectives = true);
```

Be careful: setting `DisableInternalDirectives` to `true` disables the hiding of internal directives and treats them as public. Only use this option if you understand that internal directives may expose sensitive implementation or policy details.

# Exporting Semantic Non-Null SDL

Some migration or compatibility workflows require nullable output fields annotated with `@semanticNonNull`. To export this shape, use the `--semantic-non-null` flag:

```bash
dotnet run -- schema export --output artifacts/schema.semantic.graphqls --semantic-non-null
```

Example SDL output:

```graphql
directive @semanticNonNull(levels: [Int!] = [0]) on FIELD_DEFINITION

type Query {
  product: Product @semanticNonNull
}
```

This flag rewrites output field non-null wrappers as nullable fields annotated with `@semanticNonNull`. Use this only if a downstream client or compatibility workflow requires these annotations. Do not mix standard and semantic-non-null exports as the same baseline. Name the artifacts clearly if you produce both.

# Making SDL Output Stable for CI Review

Schema exports are most useful when their diffs are repeatable and stable:

```bash
mkdir -p artifacts
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

git diff --exit-code -- artifacts/schema.graphqls artifacts/schema-settings.json
```

The `git diff` command will fail if the generated SDL or settings file changes, signaling that the pull request needs review.

To ensure stable output:

- Pin the .NET SDK using `global.json`.
- Keep Hot Chocolate package versions consistent across all environments.
- Run exports from a clean checkout and a fixed working directory.
- Set environment variables and app settings explicitly.
- Avoid timestamps, random ordering, environment-specific descriptions, and reflection-order assumptions in your schema configuration.
- Consider sorting fields by name to reduce noisy diffs:

```csharp
builder.AddGraphQL()
    .ModifyOptions(o => o.SortFieldsByName = true);
```

Decide whether to include `schema-settings.json` in your review diffs. Teams that use it for client tooling often commit it, while teams using a registry as the source of truth may upload it as an artifact and compare only the SDL.

# Comparing Exports Against the Main-Branch Baseline

If you check in your baseline SDL, regenerate it and use Git to detect contract changes:

```bash
dotnet run --project src/Catalog.Api -- schema export --output schema.graphqls
git diff --exit-code -- schema.graphqls
```

If your baseline is produced from the main branch, compare your artifact with the branch baseline:

```bash
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
git diff --exit-code origin/main -- artifacts/schema.graphqls
```

Use the SDL diff as your review entry point. A text diff shows what changed. Registry and breaking-change tools help determine if known clients remain safe. Always upload the exported SDL as a CI artifact, even if validation fails, so reviewers can inspect the exact contract.

# Using Exported SDL in Governance Workflows

Your schema is the contract between your server and its clients. Schema changes typically fall into three categories:

- Breaking changes (for example, removing a field or making an output field nullable in a way clients cannot handle)
- Non-breaking changes (such as adding an optional field)
- Dangerous changes (such as adding an enum value that existing clients may not expect)

A typical governance workflow includes:

1. Exporting the SDL from your configured server
2. Storing it as a CI artifact or checked-in baseline
3. Comparing it against the previous contract
4. Validating it with a schema registry or breaking-change checker
5. Uploading it to the registry when the release build creates a versioned artifact
6. Publishing or promoting the schema for the target stage after checks pass
7. Using the published schema for client generation and contract checks

Example Nitro handoff:

```bash
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls

nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "dev" \
  --schema-file artifacts/schema.graphqls

nitro schema upload \
  --api-id "$NITRO_API_ID" \
  --tag "$GITHUB_SHA" \
  --schema-file artifacts/schema.graphqls

nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "$GITHUB_SHA" \
  --stage "dev"
```

Refer to the Nitro schema registry documentation for details on authentication, API setup, stages, approvals, and validation. This page focuses on producing the Hot Chocolate server SDL artifact that these workflows consume.

# Alternative: Exporting on Startup

You can use `ExportSchemaOnStartup()` to write the SDL as part of request executor initialization:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup("./schema.graphqls");
```

To export the semantic non-null shape on startup:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup("./schema.semantic.graphqls", semanticNonNull: true);
```

The startup exporter uses the same file exporter as `schema export` and runs during executor initialization, as well as when the request executor is rebuilt at runtime.

You can use `skipIf` to prevent writing files in certain environments:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup(
        "./schema.graphqls",
        skipIf: !builder.Environment.IsDevelopment());
```

For CI gates, prefer the CLI export. It is explicit, exits after writing files, and produces artifacts without starting a long-running server.

# Alternative: Downloading SDL from a Running Server

You can also download the SDL from a running server for diagnostics or local tooling.

To expose a dedicated schema endpoint:

```csharp
app.MapGraphQLSchema("/graphql/schema");
```

Then download the SDL using curl:

```bash
curl http://localhost:5000/graphql?sdl -o schema.graphqls
curl http://localhost:5000/graphql/schema.graphql -o schema.graphqls
curl http://localhost:5000/graphql/schema -o schema.graphqls
```

`MapGraphQL()` enables `?sdl` downloads by default when schema requests are enabled. It also supports schema file paths like `/graphql/schema.graphql`, `/graphql/schema`, and `/graphql/schema/` if schema file support is enabled. `MapGraphQLSchema()` exposes a dedicated SDL endpoint, with the default route `/graphql/sdl` or a custom route if specified.

Two server options control this behavior:

| Option                    | Controls                                                |
| ------------------------- | ------------------------------------------------------- |
| `EnableSchemaRequests`    | Whether SDL download requests are handled               |
| `EnableSchemaFileSupport` | Whether schema file downloads return SDL instead of 404 |

Disabling introspection does not disable SDL downloads. Introspection controls GraphQL `__schema` and `__type` queries, while SDL downloads are endpoint behavior. For deterministic CI, prefer CLI export, as it does not depend on server URL, authentication, or network access.

# Alternative: Snapshotting the Schema in Tests

You can use schema snapshots in your tests to catch unintended schema changes early:

```csharp
// Tests/SchemaTests.cs
public class SchemaTests
{
    [Fact]
    public async Task Schema_Should_Match_Snapshot_When_ServerSchemaChanges()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act & assert
        executor.Schema.MatchSnapshot();
    }
}
```

The `executor.Schema.MatchSnapshot()` method serializes the schema SDL and compares it with a stored snapshot. Use schema snapshots to catch local changes, and rely on exported SDL and registry checks to govern the deployed contract.

Use `executor.Schema.ToString()` only for targeted assertions or utilities. If your test executor is configured differently from `Program.cs`, make that difference clear in the test name or setup helper.

# Troubleshooting

| Symptom                                                     | Likely cause                                                                                             | Fix                                                                                                                                                                                    |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `No schemas registered.`                                    | The host did not register a Hot Chocolate request executor.                                              | Ensure the server calls `AddGraphQL()` or `AddGraphQLServer()` and registers at least one root type. Run `dotnet run -- schema list`.                                                  |
| Export succeeds locally but fails in CI.                    | Missing environment variables, appsettings files, generated source, or schema-time services.             | Set `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT`, provide safe configuration, build generated sources, and use `AddApplicationService<T>()` for schema-time application services. |
| The wrong schema was exported.                              | Multiple schemas are registered and the default selection chose `_Default` or the first name.            | Run `schema list` and pass `--schema-name`.                                                                                                                                            |
| SDL differs between machines.                               | Different SDK or package versions, environments, conditional registrations, XML docs, or ordering noise. | Pin versions, set environment explicitly, avoid nondeterministic schema metadata, and consider `SortFieldsByName`.                                                                     |
| Internal directives are missing.                            | v16 hides internal directives from public SDL by default.                                                | Keep them hidden for public governance SDL, or set `DisableInternalDirectives = true` only for trusted internal exports.                                                               |
| `schema print` did not create settings JSON.                | `schema print` writes raw SDL to stdout.                                                                 | Use `schema export` when downstream tooling expects the settings file.                                                                                                                 |
| Output path is unexpected or the export fails.              | Directory handling and extension rules can be misread.                                                   | Create the output directory first and pass an explicit `.graphqls` file path.                                                                                                          |
| `curl` download returns 404.                                | Schema requests or file support are disabled, or the route is not mapped.                                | Check `MapGraphQL()`, `MapGraphQLSchema()`, `EnableSchemaRequests`, and `EnableSchemaFileSupport`.                                                                                     |
| Exported SDL does not match what clients see in production. | Export used a different environment, feature flags, schema options, or schema name.                      | Export with the intended production-contract configuration and document any intentionally different governance profile.                                                                |

Useful diagnostic commands:

```bash
dotnet run -- schema list
dotnet run -- schema export --help
dotnet run -- schema print --schema-name Catalog | head
```

# Verifying the Export

Before relying on the exported artifact, verify that:

- The command exits with code `0`.
- The expected `.graphqls` file and `*-settings.json` file exist.
- The SDL contains the expected root operation types.
- The selected schema name matches the contract you intend to govern.
- CI stores the SDL as an artifact or compares it with a reviewed baseline.
- Registry or breaking-change checks run against the same file you exported.

# Next Steps

- Learn more about command details in [Command Line](/docs/hotchocolate/v16/server/command-line).
- Compare HTTP download controls in [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapgraphqlschema).
- Review startup export in [Warmup](/docs/hotchocolate/v16/server/warmup#exporting-the-schema-on-startup).
- Add schema snapshots with [Testing](/docs/hotchocolate/v16/guides/testing#test-schema-shape).
- Plan safe changes with [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Separate introspection from SDL downloads in [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection).
- Use Nitro registry commands from [Nitro schema CLI](/docs/nitro/cli/schema) and registry concepts from [Schema Registry](/docs/nitro/apis/schema-registry).
