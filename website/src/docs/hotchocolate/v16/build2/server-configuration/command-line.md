---
title: Command line
---

Use Hot Chocolate command-line support when you need a repeatable schema artifact from the same ASP.NET Core application that serves GraphQL requests. The v16 server CLI is schema focused. It can export SDL files, print SDL to stdout, and list registered schemas.

Common commands:

```shell
dotnet run -- schema export
dotnet run -- schema print
dotnet run -- schema list
```

The commands build your application host, resolve the registered Hot Chocolate request executor, and build the selected schema. That means schema validation, naming conventions, type extensions, configuration, and dependency injection are the same as they are for the running server.

# Install the command-line package

Add the command-line package to the server project that owns your GraphQL schema.

<PackageInstallation packageName="HotChocolate.AspNetCore.CommandLine" />

Your server project still needs the ASP.NET Core Hot Chocolate server package, for example `HotChocolate.AspNetCore`. The Hot Chocolate server template may already include command-line support.

# Wire commands into `Program.cs`

Replace `app.Run()` with `RunWithGraphQLCommands(args)` or `RunWithGraphQLCommandsAsync(args)`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);

public sealed class Query
{
    public string Hello() => "world";
}
```

The synchronous form returns an exit code too:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return app.RunWithGraphQLCommands(args);
```

In v16, `RunWithGraphQLCommandsAsync(args)` returns `Task<int>` and `RunWithGraphQLCommands(args)` returns `int`. Return that value from `Program.cs` so shell scripts and CI receive command failures.

Command mode is selected only when the arguments start with `schema`. For all other arguments, the host runs normally and the method returns `0` when the server stops.

# Understand how commands run your app

A command such as this:

```shell
dotnet run -- schema export
```

uses this path:

1. ASP.NET Core runs `Program.cs`.
2. Your services and middleware are configured.
3. The host is built.
4. The command runner detects the `schema` command.
5. Hot Chocolate resolves `IRequestExecutorProvider`.
6. The selected request executor builds and validates the schema.
7. The command writes output or returns a nonzero exit code.

The CLI does not call a running HTTP endpoint. It builds the same app host. Required configuration, secrets, environment names, connection strings, database-dependent startup code, migrations, and cloud services can affect command success.

Keep the normal endpoint setup in place:

```csharp
var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Use one GraphQL registration style in a project. The v16 minimal hosting docs use `builder.AddGraphQL()`. Service-collection based projects may use `builder.Services.AddGraphQLServer()` instead.

# Export a schema file

Run `schema export` to write SDL and a companion settings file.

```shell
dotnet run -- schema export
```

By default, the command writes these files in the current working directory:

```text
schema.graphqls
schema-settings.json
```

It also prints the exported file names:

```text
Exported Files:
- /path/to/project/schema.graphqls
- /path/to/project/schema-settings.json
```

The settings file contains the schema name and a default HTTP transport URL:

```json
{
  "name": "default",
  "transports": {
    "http": {
      "url": "http://localhost:5000/graphql"
    }
  }
}
```

Use `--output` when you want a stable path, especially in CI:

```shell
dotnet run -- schema export --output ./schema/schema.graphqls
```

Output path rules:

| `--output` value                           | Result                                                     |
| ------------------------------------------ | ---------------------------------------------------------- |
| Not specified                              | Writes `schema.graphqls` in the current working directory. |
| Existing directory, for example `./schema` | Writes `schema.graphqls` inside that directory.            |
| File ending in `.graphql`                  | Uses that file name.                                       |
| File ending in `.graphqls`                 | Uses that file name.                                       |
| Other file name                            | Appends `.graphqls`.                                       |

The companion settings file uses the schema file base name. For example, `./schema/products.graphqls` creates `./schema/products-settings.json`.

Create parent directories before you export. Do not rely on `schema export` to create missing directory paths.

# Print a schema to stdout

Use `schema print` for quick inspection, piping, or scripts that do not need the settings file.

```shell
dotnet run -- schema print
```

Expected SDL shape for the earlier `Query` type:

```graphql
type Query {
  hello: String!
}
```

`schema print` writes SDL to stdout. It does not create `schema.graphqls` or `schema-settings.json`.

# Work with multiple schemas

If your app registers more than one schema, list the schema names first.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("Products")
    .AddQueryType<ProductsQuery>()
    .Services
    .AddGraphQL("Reviews")
    .AddQueryType<ReviewsQuery>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

```shell
dotnet run -- schema list
```

Expected output:

```text
Products
Reviews
```

Export a named schema with `--schema-name`:

```shell
dotnet run -- schema export --schema-name Products --output ./schemas/products.graphqls
```

Print a named schema with the same option:

```shell
dotnet run -- schema print --schema-name Products
```

If you omit `--schema-name`, the command uses the default schema name when it is registered. Otherwise, it uses the first registered schema name. In multi-schema CI jobs, always pass `--schema-name`.

# Export semantic non-null SDL when a tool needs it

Use `--semantic-non-null` only when a migration or downstream tool expects SDL with `@semanticNonNull` annotations.

```shell
dotnet run -- schema export --output schema.graphql --semantic-non-null
```

This option rewrites the exported SDL. It is not required for normal schema export and it does not change runtime nullability behavior.

# Validate schema startup from CI

The command runner returns a nonzero exit code when schema startup fails. You can use that behavior to validate the server schema during builds.

```shell
dotnet restore
dotnet build --no-restore
dotnet run --project ./src/MyApi --no-build -- schema export --output ./schema/schema.graphqls
git diff --exit-code -- schema/schema.graphqls schema/schema-settings.json
```

The flow is:

1. Build the server project.
2. Run `schema export` from CI.
3. Let the command fail the job if the schema cannot be built.
4. Compare the generated SDL and settings file with committed artifacts.
5. Review schema changes before you publish or deploy.

Pass the same required environment and configuration that the server needs at startup. For example, set `ASPNETCORE_ENVIRONMENT`, connection strings, feature flags, and secrets used by service registration.

Keep registry publishing, Fusion composition, client operation validation, and trusted document publishing in separate tool-specific steps. The Hot Chocolate server CLI does not provide built-in commands for those workflows.

# Use the right schema output workflow

| Need                                                                           | Use                                                             |
| ------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| Commit or review SDL files                                                     | `dotnet run -- schema export`                                   |
| Pipe SDL to another command                                                    | `dotnet run -- schema print`                                    |
| Discover schema names                                                          | `dotnet run -- schema list`                                     |
| Expose SDL over HTTP at runtime                                                | `MapGraphQLSchema()` or endpoint SDL support                    |
| Export during application startup                                              | `ExportSchemaOnStartup(...)`                                    |
| Validate against a schema registry, publish to Nitro, or compose Fusion graphs | Registry, Nitro, Fusion, or other external tooling after export |
| Validate client operations or trusted documents                                | The relevant client, registry, or trusted document workflow     |

# Command reference

| Command         | Purpose                                  | Options                                                                   | Output                                                        |
| --------------- | ---------------------------------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------- |
| `schema export` | Write SDL and a companion settings file. | `--output <output>`, `--schema-name <schema-name>`, `--semantic-non-null` | Schema file, settings file, and an `Exported Files:` message. |
| `schema print`  | Write SDL to stdout.                     | `--schema-name <schema-name>`                                             | SDL on stdout.                                                |
| `schema list`   | List registered schema names.            | None                                                                      | One schema name per line, or `No schemas registered.`         |

Use command help to inspect the installed package version:

```shell
dotnet run -- schema -h
dotnet run -- schema export -h
dotnet run -- schema print -h
```

# Troubleshoot command-line schema workflows

## `No schemas registered.`

The host built, but no GraphQL schema was registered with dependency injection.

Check that your command path runs `AddGraphQL()` or `AddGraphQLServer()`. Also check environment-specific service registration, because CI may use a different `ASPNETCORE_ENVIRONMENT` value than local development.

## The command fails during startup

CLI commands build the same host as the server. Startup code can fail before Hot Chocolate exports SDL.

Provide required CI configuration and connection strings. Avoid destructive startup side effects in schema export jobs. If a side effect is unavoidable, `args.IsGraphQLCommand()` is available for advanced startup branching, but keep the GraphQL schema registration path consistent.

## The schema is invalid and CI fails

The command returns a nonzero exit code when schema creation fails. Run the same command locally, inspect the schema exception, and fix the type registration, resolver signature, schema option, or dependency that caused schema initialization to fail.

## Files were written to the wrong folder

`schema export` uses the current working directory when `--output` is not specified. `dotnet run --project ./src/MyApi -- schema export` still uses the shell's current working directory for relative output paths.

Prefer an explicit output path in scripts:

```shell
dotnet run --project ./src/MyApi -- schema export --output ./schema/schema.graphqls
```

## The wrong schema was exported

Multiple schemas were registered and the command selected a default. Run `schema list`, then pass `--schema-name` in every script.

```shell
dotnet run -- schema list
dotnet run -- schema export --schema-name Products --output ./schemas/products.graphqls
```

## The output path did not behave as expected

An existing directory path writes `schema.graphqls` inside that directory. A file name without `.graphql` or `.graphqls` gets `.graphqls` appended. Create parent directories before running the command.

## A registry or composition tool cannot find the settings file

Custom schema file names also change the companion settings file name. Keep the schema and settings files together, or configure the consuming tool to read the generated names.

# Next steps

- Review schema changes with [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution).
- Learn when to export during startup with [Warmup](/docs/hotchocolate/v16/server/warmup).
- Expose SDL over HTTP with [Endpoint mapping](/docs/hotchocolate/v16/build2/server-configuration/endpoints).
- Use schema artifacts with trusted document workflows in [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents).
- Review v16 command runner changes in [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).
