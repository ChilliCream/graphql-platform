---
title: Command Line
---

The `HotChocolate.AspNetCore.CommandLine` package extends the `IHostBuilder` interface with a command-line interface for managing GraphQL schemas. This extension lets you export schemas directly from the command line, which is useful for CI/CD pipelines and schema registry workflows.

# Setup

Here is an example of using the `HotChocolate.AspNetCore.CommandLine` package with a minimal API:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGraphQLServer().AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

In v16, `RunWithGraphQLCommandsAsync` returns a `Task<int>` (and the synchronous `RunWithGraphQLCommands` returns `int`). Return this exit code from your `Program.cs` so that command failures signal an error to shell scripts, CI/CD pipelines, and other tools.

# Commands

## Schema Export Command

The `schema export` command exports the GraphQL schema. By default, the schema is printed to the console. You can specify an output file using the `--output` option.

```shell
dotnet run -- schema export --output schema.graphql
```

**Options**

- `--output`: The path to the file where the schema is exported. If no output path is specified, the schema prints to the console.
- `--schema-name`: The name of the schema to export. If no schema name is specified, the default schema is exported.

# Troubleshooting

## Exit code not propagated

If your CI/CD pipeline does not detect command failures, verify that you return the result of `RunWithGraphQLCommandsAsync`. The method now returns an exit code, and not returning it causes the process to always exit with `0`.

## Schema export fails with errors

Schema errors surface at startup in v16 due to eager initialization. Fix the schema errors reported in the console output before exporting.

# Next Steps

- [Warmup](/docs/hotchocolate/v16/server/warmup) for details on startup behavior and schema initialization.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#noteworthy-changes) for the full list of changes to `RunWithGraphQLCommandsAsync`.
