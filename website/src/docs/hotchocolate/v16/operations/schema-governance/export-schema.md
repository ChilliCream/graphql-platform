---
title: Export a schema
---

Export the Hot Chocolate schema SDL from the same configured ASP.NET Core application that serves production traffic. Use that SDL file as the stable contract for pull request review, CI artifacts, schema registry validation, breaking-change checks, documentation, and client generation.

This page covers Hot Chocolate v16 server schemas. Fusion gateways use their own composition and deployment workflow. If you work on Fusion, use the Fusion documentation instead.

# Export the schema SDL

## Prerequisites

You need:

- A Hot Chocolate v16 ASP.NET Core server project.
- A reference to `HotChocolate.AspNetCore.CommandLine`.
- A `Program.cs` that returns the exit code from `RunWithGraphQLCommandsAsync(args)` or `RunWithGraphQLCommands(args)`.
- The configuration required to build the schema available in your shell or CI job.

Run the export command from the repository root and pass the server project explicitly:

```bash
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

Expected command output:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

`schema export` builds your host, resolves the Hot Chocolate request executor provider, builds the selected request executor, and writes files. The exported SDL is the public GraphQL contract for that configured server.

Return the command exit code from `Program.cs` so schema build failures fail CI instead of producing a successful job with missing or stale artifacts.

# Wire command-line export into Program.cs

Install the command-line package in the server project, then run the application through the GraphQL command wrapper.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

With this shape:

- `dotnet run` starts the server.
- `dotnet run -- schema export` runs the command and exits after writing the schema files.

Use the async form when possible because the returned `int` is visible in `Program.cs`. The synchronous `RunWithGraphQLCommands(args)` form is available too and also returns an exit code in v16.

Do not build a separate schema for CI unless you have a documented reason. A separate schema builder can drift from production configuration and give reviewers a contract that clients never see.

# Understand what gets generated

`schema export` is file-oriented. It writes the SDL and a settings file next to it.

| File                   | Purpose                                                                                                       |
| ---------------------- | ------------------------------------------------------------------------------------------------------------- |
| `schema.graphqls`      | The SDL formatted from `executor.Schema`. Hot Chocolate writes UTF-8 without BOM and adds a trailing newline. |
| `schema-settings.json` | Metadata for tools. It contains the schema name and a default HTTP transport URL.                             |

Example SDL:

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

The default URL is tool metadata. It is not discovered from Kestrel. Update it if a downstream tool uses the settings file to connect to another endpoint.

If you omit `--output`, Hot Chocolate writes `schema.graphqls` and `schema-settings.json` in the current working directory. Use `schema print` when you need SDL on stdout.

# Choose an output path deliberately

Prefer an explicit `.graphqls` file path in CI:

```bash
mkdir -p artifacts
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

Path behavior:

| Command shape               | Written files                                                                             |
| --------------------------- | ----------------------------------------------------------------------------------------- |
| No `--output`               | `./schema.graphqls` and `./schema-settings.json`                                          |
| `--output schema.graphql`   | `schema.graphql` and `schema-settings.json`                                               |
| `--output schema.graphqls`  | `schema.graphqls` and `schema-settings.json`                                              |
| `--output artifacts/schema` | `artifacts/schema.graphqls` and `artifacts/schema-settings.json`, when `artifacts` exists |
| `--output artifacts/`       | `artifacts/schema.graphqls` and `artifacts/schema-settings.json`, when `artifacts` exists |

Create the output directory before running the command. Add generated files to source control when they are your review baseline. Upload them as CI artifacts when your schema registry is the source of truth.

# Export the intended schema when several schemas exist

List registered schemas before exporting a named schema:

```bash
dotnet run -- schema list
dotnet run -- schema export --schema-name Catalog --output artifacts/catalog.graphqls
```

Example list output:

```text
_Default
Catalog
Admin
```

Without `--schema-name`, Hot Chocolate chooses `_Default` when present. If `_Default` is not registered, it chooses the first registered schema name. In CI, log `schema list` when your server hosts multiple public contracts, then export each contract to its own SDL artifact.

# Print SDL to stdout for shell pipelines

Use `schema print` when a script needs raw SDL on stdout:

```bash
dotnet run -- schema print --schema-name Catalog > artifacts/catalog.graphqls
```

`schema print` writes SDL to stdout and does not create `schema-settings.json`. It supports `--schema-name`. It does not support `--semantic-non-null` in the current v16 command source.

Use `schema export` when downstream tooling expects both the schema file and the settings file.

# Export from the configured application

Treat the exported SDL as a built artifact, not as a hand-maintained design file. Anything that affects schema construction can affect the SDL:

- Registered root types, object types, scalars, directives, and schema extensions.
- Schema options such as XML documentation and field ordering.
- Feature flags and environment-specific registrations.
- Application services required by schema-time components.
- Hot Chocolate and .NET package versions.

Set the same environment that defines the production contract:

```bash
ASPNETCORE_ENVIRONMENT=Production \
DOTNET_ENVIRONMENT=Production \
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
```

Avoid requiring production secrets to build the schema. Resolver service injection is not usually needed to print SDL, but schema services, diagnostic listeners, error filters, interceptors, and other schema-time components can require application services. In v16, cross-register application services needed by schema services with `AddApplicationService<T>()`.

```csharp
builder.Services.AddSingleton<SchemaMetadataProvider>();

builder.AddGraphQL()
    .AddApplicationService<SchemaMetadataProvider>()
    .AddQueryType<Query>();
```

Runtime-only HTTP settings such as Nitro UI enablement, allowed GET operations, and introspection permissions do not change the schema shape. Schema download endpoints and internal directive visibility can change what users can download from a running server.

# Include or hide internal directives

Hot Chocolate v16 hides internal directives from public SDL by default. This protects metadata such as authorization policies from appearing in schema downloads or exported public SDL.

Keep the default for governance artifacts that leave a trusted boundary. If a trusted internal workflow requires internal directives, opt in deliberately:

```csharp
builder.AddGraphQL()
    .ModifyOptions(o => o.DisableInternalDirectives = true);
```

The option name is important: setting `DisableInternalDirectives` to `true` disables the hiding of internal directives and treats them as public. Only use it when you understand that internal directives can expose sensitive implementation or policy details.

# Export semantic non-null SDL when a downstream tool requires it

Some migration or compatibility workflows consume nullable output fields annotated with `@semanticNonNull`. Export that shape with `--semantic-non-null`:

```bash
dotnet run -- schema export --output artifacts/schema.semantic.graphqls --semantic-non-null
```

Expected SDL shape:

```graphql
directive @semanticNonNull(levels: [Int!] = [0]) on FIELD_DEFINITION

type Query {
  product: Product @semanticNonNull
}
```

The flag rewrites output field non-null wrappers to nullable fields annotated with `@semanticNonNull`. Use it only when a downstream client or compatibility workflow still requires those annotations. Do not mix normal and semantic-non-null exports as the same baseline. Name the artifacts distinctly if you produce both.

# Make SDL output stable for CI review

A schema export is most useful when the diff is repeatable:

```bash
mkdir -p artifacts
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

git diff --exit-code -- artifacts/schema.graphqls artifacts/schema-settings.json
```

The diff command fails when generated SDL or settings changed and the pull request needs review.

For stable output:

- Pin the .NET SDK with `global.json`.
- Keep Hot Chocolate package versions consistent across machines.
- Run from a clean checkout and a fixed working directory.
- Set environment variables and app settings explicitly.
- Avoid timestamps, random ordering, environment-specific descriptions, and reflection-order assumptions in schema configuration.
- Consider sorting fields if ordering creates noisy diffs.

```csharp
builder.AddGraphQL()
    .ModifyOptions(o => o.SortFieldsByName = true);
```

Decide whether `schema-settings.json` belongs in the review diff. Teams that use it for client tooling often commit it. Teams that use a registry as the source of truth often upload it as an artifact and compare only SDL.

# Compare an export against the main-branch baseline

When the baseline is checked in, regenerate it and use Git to detect contract changes:

```bash
dotnet run --project src/Catalog.Api -- schema export --output schema.graphqls
git diff --exit-code -- schema.graphqls
```

When the baseline is produced from the main branch, compare your artifact with the branch baseline:

```bash
dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
git diff --exit-code origin/main -- artifacts/schema.graphqls
```

Treat the SDL diff as the review entry point. A text diff shows what changed. Registry and breaking-change tools decide whether known clients remain safe. Upload the exported SDL as a CI artifact even when validation fails so reviewers can inspect the exact contract.

# Use exported SDL in governance workflows

The schema is the contract between your server and its clients. Schema changes usually fall into three categories:

- Breaking changes, such as removing a field or making an output field nullable in a way clients cannot accept.
- Non-breaking changes, such as adding an optional field.
- Dangerous changes, such as adding an enum value that existing clients may not handle.

A typical governance workflow is:

1. Export SDL from the configured server.
2. Store it as a CI artifact or checked-in baseline.
3. Compare it against the previous contract.
4. Validate it with a schema registry or breaking-change checker.
5. Upload it to the registry when the release build creates a versioned artifact.
6. Publish or promote the schema for the target stage after checks pass.
7. Use the published schema for client generation and client contract checks.

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

Use the Nitro schema registry docs for authentication, API setup, stages, approvals, and detailed validation behavior. This page focuses on producing the Hot Chocolate server SDL artifact that those workflows consume.

# Alternative: export on startup

Use `ExportSchemaOnStartup()` when you want the application to write SDL as part of request executor initialization:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup("./schema.graphqls");
```

You can also export the semantic non-null shape:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup("./schema.semantic.graphqls", semanticNonNull: true);
```

The startup exporter uses the same file exporter as `schema export`. It runs during executor initialization and again when the request executor is rebuilt at runtime.

Use `skipIf` to avoid writing files in environments where it is not wanted:

```csharp
builder.AddGraphQL()
    .ExportSchemaOnStartup(
        "./schema.graphqls",
        skipIf: !builder.Environment.IsDevelopment());
```

Prefer CLI export for CI gates because it is explicit, exits after writing files, and produces artifacts without starting a long-running server.

# Alternative: download SDL from a running server

You can download SDL from a running server for diagnostics or local tooling.

Expose a dedicated schema endpoint:

```csharp
app.MapGraphQLSchema("/graphql/schema");
```

Download SDL:

```bash
curl http://localhost:5000/graphql?sdl -o schema.graphqls
curl http://localhost:5000/graphql/schema.graphql -o schema.graphqls
curl http://localhost:5000/graphql/schema -o schema.graphqls
```

`MapGraphQL()` enables `?sdl` downloads by default when schema requests are enabled. It also supports schema file paths such as `/graphql/schema.graphql`, `/graphql/schema`, and `/graphql/schema/` when schema file support is enabled. `MapGraphQLSchema()` exposes a dedicated SDL endpoint. Its default route is `/graphql/sdl`, and a custom route serves SDL at that exact route.

Two server options matter:

| Option                    | Controls                                                 |
| ------------------------- | -------------------------------------------------------- |
| `EnableSchemaRequests`    | Whether SDL download requests are handled.               |
| `EnableSchemaFileSupport` | Whether schema file downloads return SDL instead of 404. |

Disabling introspection does not disable SDL downloads. Introspection controls GraphQL `__schema` and `__type` queries. SDL downloads are endpoint behavior. Prefer CLI export for deterministic CI because it does not depend on server URL, authentication, or network access.

# Alternative: snapshot the schema in tests

Use schema snapshots to catch unintended schema changes close to the code:

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

`executor.Schema.MatchSnapshot()` serializes the schema SDL and compares it with a stored snapshot. Use schema snapshots to catch unintended local changes. Use exported SDL and registry checks to govern the deployed contract.

Use `executor.Schema.ToString()` only for targeted assertions or utilities. If the test executor is configured differently from `Program.cs`, make that difference visible in the test name or setup helper.

# Troubleshooting

| Symptom                                                     | Likely cause                                                                                                       | Fix                                                                                                                                                                                    |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `No schemas registered.`                                    | The host used by the command did not register a Hot Chocolate request executor.                                    | Ensure the server calls `AddGraphQL()` or `AddGraphQLServer()` and registers at least one root type. Run `dotnet run -- schema list`.                                                  |
| Export succeeds locally but fails in CI.                    | Missing environment variables, appsettings files, generated source, or schema-time services.                       | Set `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT`, provide safe configuration, build generated sources, and use `AddApplicationService<T>()` for schema-time application services. |
| The wrong schema was exported.                              | Multiple schemas are registered and default selection chose `_Default` or the first name.                          | Run `schema list` and pass `--schema-name`.                                                                                                                                            |
| SDL differs between machines.                               | Different SDK or package versions, different environments, conditional registrations, XML docs, or ordering noise. | Pin versions, set environment explicitly, avoid nondeterministic schema metadata, and consider `SortFieldsByName`.                                                                     |
| Internal directives are missing.                            | v16 hides internal directives from public SDL by default.                                                          | Keep them hidden for public governance SDL, or set `DisableInternalDirectives = true` only for trusted internal exports.                                                               |
| `schema print` did not create settings JSON.                | `schema print` writes raw SDL to stdout.                                                                           | Use `schema export` when downstream tooling expects the settings file.                                                                                                                 |
| Output path is unexpected or the export fails.              | Directory handling and extension rules can be misread.                                                             | Create the output directory first and pass an explicit `.graphqls` file path.                                                                                                          |
| `curl` download returns 404.                                | Schema requests or file support are disabled, or the route is not mapped.                                          | Check `MapGraphQL()`, `MapGraphQLSchema()`, `EnableSchemaRequests`, and `EnableSchemaFileSupport`.                                                                                     |
| Exported SDL does not match what clients see in production. | Export used a different environment, feature flags, schema options, or schema name.                                | Export with the intended production-contract configuration and document any intentionally different governance profile.                                                                |

Useful diagnostic commands:

```bash
dotnet run -- schema list
dotnet run -- schema export --help
dotnet run -- schema print --schema-name Catalog | head
```

# Verify the export

Before you rely on the artifact, check that:

- The command exits with code `0`.
- The expected `.graphqls` file and `*-settings.json` file exist.
- The SDL contains the expected root operation types.
- The selected schema name matches the contract you intend to govern.
- CI stores the SDL as an artifact or compares it with a reviewed baseline.
- Registry or breaking-change checks run against the same file you exported.

# Next steps

- Learn the command details in [Command Line](/docs/hotchocolate/v16/server/command-line).
- Compare HTTP download controls in [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapgraphqlschema).
- Review startup export in [Warmup](/docs/hotchocolate/v16/server/warmup#exporting-the-schema-on-startup).
- Add schema snapshots with [Testing](/docs/hotchocolate/v16/guides/testing#test-schema-shape).
- Plan safe changes with [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Separate introspection from SDL downloads in [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection).
- Use Nitro registry commands from [Nitro schema CLI](/docs/nitro/cli/schema) and registry concepts from [Schema Registry](/docs/nitro/apis/schema-registry).
