---
title: Getting Started
description: A short example doc used to exercise the docs renderer.
---

This is a placeholder docs page used while the new website is under
construction. It exists so the docs route, the sidebar, the table of contents,
and the last-updated footer all have something real to render.

## Installation

Install the package with your preferred package manager:

```bash
dotnet add package HotChocolate.AspNetCore
```

## Quick start

1. Register the server in `Program.cs`.
2. Add a query type.
3. Run the app and open the GraphQL IDE.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();

public class Query
{
    public string Hello() => "world";
}
```

## Next steps

- Define a schema.
- Wire up resolvers.
- Add authentication.
