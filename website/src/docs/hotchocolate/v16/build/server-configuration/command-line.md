---
title: Command line
---

Hot Chocolate provides command-line support for generating repeatable schema artifacts from the same ASP.NET Core application that serves your GraphQL requests. The server CLI focuses on schema operations: exporting SDL files, printing SDL to stdout, and listing registered schemas.

## Common Commands

```shell
dotnet run -- schema export
dotnet run -- schema print
dotnet run -- schema list
```

These commands build your application host, resolve the registered Hot Chocolate request executor, and construct the selected schema. This ensures that schema validation, naming conventions, type extensions, configuration, and dependency injection match the running server environment.

# Installing the Command-Line Package

Add the command-line package to the server project that owns your GraphQL schema:

<PackageInstallation packageName="HotChocolate.AspNetCore.CommandLine" />

Your server project must also reference the ASP.NET Core Hot Chocolate server package, such as `HotChocolate.AspNetCore`. If you used the Hot Chocolate server template, command-line support may already be included.

# Integrating Commands in `Program.cs`

Replace `app.Run()` with either `RunWithGraphQLCommands(args)` or `RunWithGraphQLCommandsAsync(args)`.

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

The synchronous form also returns an exit code:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return app.RunWithGraphQLCommands(args);
```

`RunWithGraphQLCommandsAsync(args)` returns `Task<int>`, and `RunWithGraphQLCommands(args)` returns `int`. Return this value from `Program.cs` so shell scripts and CI systems can detect command failures.

Command mode is activated only when the arguments start with `schema`. For all other arguments, the host runs as usual, and the method returns `0` when the server stops.

# How Commands Run Your Application

For example, running:

```shell
dotnet run -- schema export
```

follows this process:

1. ASP.NET Core executes `Program.cs`.
2. Your services and middleware are configured.
3. The host is built.
4. The command runner detects the `schema` command.
5. Hot Chocolate resolves `IRequestExecutorProvider`.
6. The selected request executor builds and validates the schema.
7. The command writes output or returns a nonzero exit code.

The CLI does not interact with a running HTTP endpoint. Instead, it builds the same application host. Configuration, secrets, environment names, connection strings, database-dependent startup code, migrations, and cloud services can all affect command success.

Keep your normal endpoint setup in place:

```csharp
var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Use a single GraphQL registration style per project. The minimal hosting documentation uses `builder.AddGraphQL()`. Projects based on the service collection may use `builder.Services.AddGraphQLServer()` instead.

# Exporting a Schema File

Run `schema export` to write the SDL and a companion settings file:

```shell
dotnet run -- schema export
```

By default, the command writes these files to the current working directory:

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

Use `--output` to specify a stable path, which is especially useful in CI:

```shell
dotnet run -- schema export --output ./schema/schema.graphqls
```

**Output path rules:**

| `--output` value                           | Result                                                     |
| ------------------------------------------ | ---------------------------------------------------------- |
| Not specified                              | Writes `schema.graphqls` in the current working directory. |
| Existing directory, for example `./schema` | Writes `schema.graphqls` inside that directory.            |
| File ending in `.graphql`                  | Uses that file name.                                       |
| File ending in `.graphqls`                 | Uses that file name.                                       |
| Other file name                            | Appends `.graphqls`.                                       |

The companion settings file uses the schema file's base name. For example, `./schema/products.graphqls` creates `./schema/products-settings.json`.

Create parent directories before exporting. The `schema export` command does not create missing directory paths.

# Printing a Schema to stdout

Use `schema print` for quick inspection, piping, or scripts that do not require the settings file:

```shell
dotnet run -- schema print
```

Expected SDL output for the earlier `Query` type:

```graphql
type Query {
  hello: String!
}
```

`schema print` writes SDL to stdout and does not create `schema.graphqls` or `schema-settings.json`.

# Working with Multiple Schemas

If your application registers more than one schema, list the schema names first:

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

To export a named schema, use `--schema-name`:

```shell
dotnet run -- schema export --schema-name Products --output ./schemas/products.graphqls
```

To print a named schema, use the same option:

```shell
dotnet run -- schema print --schema-name Products
```

If you omit `--schema-name`, the command uses the default schema name if it is registered, or the first registered schema name otherwise. In CI jobs with multiple schemas, always specify `--schema-name`.

# Exporting Semantic Non-Null SDL for Tooling

Use `--semantic-non-null` only when a migration or downstream tool requires SDL with `@semanticNonNull` annotations:

```shell
dotnet run -- schema export --output schema.graphql --semantic-non-null
```

This option rewrites the exported SDL. It is not needed for standard schema export and does not affect runtime nullability behavior.

# Validating Schema Startup in CI

The command runner returns a nonzero exit code if schema startup fails. You can use this to validate the server schema during builds:

```shell
dotnet restore
dotnet build --no-restore
dotnet run --project ./src/MyApi --no-build -- schema export --output ./schema/schema.graphqls
git diff --exit-code -- schema/schema.graphqls schema/schema-settings.json
```

The process is:

1. Build the server project.
2. Run `schema export` from CI.
3. Allow the command to fail the job if the schema cannot be built.
4. Compare the generated SDL and settings file with committed artifacts.
5. Review schema changes before publishing or deploying.

Pass all required environment variables and configuration that the server needs at startup. For example, set `ASPNETCORE_ENVIRONMENT`, connection strings, feature flags, and secrets used by service registration.

Keep registry publishing, Fusion composition, client operation validation, and trusted document publishing in separate, tool-specific steps. The Hot Chocolate server CLI does not provide built-in commands for those workflows.

# Choosing the Right Schema Output Workflow

| Need                                                                           | Use                                                             |
| ------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| Commit or review SDL files                                                     | `dotnet run -- schema export`                                   |
| Pipe SDL to another command                                                    | `dotnet run -- schema print`                                    |
| Discover schema names                                                          | `dotnet run -- schema list`                                     |
| Expose SDL over HTTP at runtime                                                | `MapGraphQLSchema()` or endpoint SDL support                    |
| Export during application startup                                              | `ExportSchemaOnStartup(...)`                                    |
| Validate against a schema registry, publish to Nitro, or compose Fusion graphs | Registry, Nitro, Fusion, or other external tooling after export |
| Validate client operations or trusted documents                                | The relevant client, registry, or trusted document workflow     |

# Command Reference

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

# Troubleshooting Command-Line Schema Workflows

## `No schemas registered.`

The host built, but no GraphQL schema was registered with dependency injection.

Ensure your command path runs `AddGraphQL()` or `AddGraphQLServer()`. Also check environment-specific service registration, as CI may use a different `ASPNETCORE_ENVIRONMENT` value than local development.

## The Command Fails During Startup

CLI commands build the same host as the server. Startup code can fail before Hot Chocolate exports SDL.

Provide all required CI configuration and connection strings. Avoid destructive startup side effects in schema export jobs. If a side effect cannot be avoided, `args.IsGraphQLCommand()` is available for advanced startup branching, but keep the GraphQL schema registration path consistent.

## The Schema Is Invalid and CI Fails

The command returns a nonzero exit code if schema creation fails. Run the same command locally, inspect the schema exception, and fix the type registration, resolver signature, schema option, or dependency that caused schema initialization to fail.

## Files Were Written to the Wrong Folder

`schema export` uses the current working directory when `--output` is not specified. `dotnet run --project ./src/MyApi -- schema export` still uses the shell's current working directory for relative output paths.

Prefer an explicit output path in scripts:

```shell
dotnet run --project ./src/MyApi -- schema export --output ./schema/schema.graphqls
```

## The Wrong Schema Was Exported

If multiple schemas are registered and the command selects a default, run `schema list` and then pass `--schema-name` in every script.

```shell
dotnet run -- schema list
dotnet run -- schema export --schema-name Products --output ./schemas/products.graphqls
```

## The Output Path Did Not Behave as Expected

If you specify an existing directory, `schema.graphqls` is written inside that directory. A file name without `.graphql` or `.graphqls` will have `.graphqls` appended. Create parent directories before running the command.

## A Registry or Composition Tool Cannot Find the Settings File

Custom schema file names also change the companion settings file name. Keep the schema and settings files together, or configure the consuming tool to read the generated names.

# Next Steps

- Review schema changes with [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution).
- Learn when to export during startup with [Warmup](/docs/hotchocolate/v16/build/performance/warmup).
- Expose SDL over HTTP with [Endpoint mapping](/docs/hotchocolate/v16/build/server-configuration/endpoints).
- Use schema artifacts with trusted document workflows in [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents).
- Review v16 command runner changes in [Migrate from v15 to v16](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16).
