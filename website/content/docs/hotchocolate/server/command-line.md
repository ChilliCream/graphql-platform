---
title: Command Line
description: "Manage GraphQL schemas from the CLI with the HotChocolate.AspNetCore.CommandLine package: run schema export for CI/CD pipelines and schema registry workflows."
---

The `HotChocolate.AspNetCore.CommandLine` package extends the `IHostBuilder` interface with a command-line interface for managing GraphQL schemas. This extension lets you export schemas directly from the command line, which is useful for CI/CD pipelines and schema registry workflows.

# Setup

Here is an example of using the `HotChocolate.AspNetCore.CommandLine` package with a minimal API:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

`RunWithGraphQLCommandsAsync` returns a `Task<int>` (and the synchronous `RunWithGraphQLCommands` returns `int`). Return this exit code from your `Program.cs` so that command failures signal an error to shell scripts, CI/CD pipelines, and other tools.

# Commands

## Schema Export Command

The `schema export` command exports the GraphQL schema. By default, the schema is printed to the console. You can specify an output file using the `--output` option.

```shell
dotnet run -- schema export --output schema.graphql
```

**Options**

- `--output`: The path to the file where the schema is exported. If no output path is specified, the schema prints to the console.
- `--schema-name`: The name of the schema to export. If no schema name is specified, the default schema is exported.
- `--semantic-non-null`: Rewrites the exported schema to strip non-null wrappers from output fields and apply the `@semanticNonNull` directive instead. Useful for clients that still rely on `@semanticNonNull` annotations.
- `--spec-version`: Downgrades the exported SDL to the grammar of a GraphQL specification edition so older parsers and validators can accept it. Supported values are `october-2021` (also `2021-10`) and `september-2025` (also `2025-09`); values are case-insensitive.

For example, export SDL compatible with the October 2021 edition:

```shell
dotnet run -- schema export --output schema.graphql --spec-version october-2021
```

When you select a specification edition, unsupported directive locations, including `DIRECTIVE_DEFINITION`, are removed from directive definitions, as are directives applied to directive definitions. For `october-2021`, `@oneOf` applications are removed, and `@deprecated` on arguments and input fields is removed with its reason folded into the description. Specification scalar and directive definitions are never printed. Custom directives, including `@semanticNonNull`, stay in the exported schema.

Without `--spec-version`, the schema export remains unchanged. The startup warmup export always writes the native schema.

# Next Steps

- [Warmup](./warmup.md) for details on startup behavior and schema initialization.
- [Migrate from v15 to v16](../migrating/migrate-from-15-to-16.md#noteworthy-changes) for the full list of changes to `RunWithGraphQLCommandsAsync`.
