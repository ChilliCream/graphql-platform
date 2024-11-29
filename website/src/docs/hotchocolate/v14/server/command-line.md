---
title: Command Line
---

# Overview

The `HotChocolate.AspNetCore.CommandLine` package extends the `IHostBuilder` interface, offering a command-line interface for managing GraphQL schemas.
This extension provides a seamless experience for developers, allowing them to export their schemas directly from the command line, which can be beneficial for CI/CD.

# Setup the Command Line Interface

Here's an example of using the `HotChocolate.AspNetCore.CommandLine` package with a minimal API and a simple setup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGraphQLServer().AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommandsAsync(args);
```

# Commands

## Schema Export Command

The `schema export` command exports the GraphQL schema. By default, the schema is printed to the console. However, you can specify an output file using the `--output` option.

```shell
dotnet run -- schema export --output schema.graphql
```

**Options**

- `--output`: The path to the file where the schema should be exported. If no output path is specified, the schema will be printed to the console.
- `--schema-name`: The name of the schema to be exported. If no schema name is specified, the default schema will be exported.
