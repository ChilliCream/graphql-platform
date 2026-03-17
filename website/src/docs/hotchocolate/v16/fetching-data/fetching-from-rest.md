---
title: "Fetching from REST"
---

GraphQL requires knowledge of the types it returns at build time. When wrapping a REST API, the most reliable approach is to generate a typed .NET client from an OpenAPI specification and inject it into your resolvers.

# Generating a Client from OpenAPI

If your REST endpoint exposes an OpenAPI specification (Swagger), you can generate a fully typed .NET client for it.

## Step 1: Get the OpenAPI Specification

Download the `swagger.json` from your REST endpoint:

```bash
curl -o swagger.json http://localhost:5000/swagger/v1/swagger.json
```

## Step 2: Generate the Client

Use the NSwag CLI tool to generate a C# client:

```bash
dotnet new tool-manifest
dotnet tool install NSwag.ConsoleCore
dotnet nswag swagger2csclient /input:swagger.json /classname:TodoService /namespace:TodoReader /output:TodoService.cs
```

This generates a `TodoService.cs` file with a typed client for your REST API. The generated client requires `Newtonsoft.Json`:

<PackageInstallation packageName="Newtonsoft.Json" external />

# Exposing the REST API

Register the generated client in your DI container and inject it into your resolvers.

<ExampleTabs>
<Implementation>

```csharp
// Types/TodoQueries.cs
[QueryType]
public static partial class TodoQueries
{
    public static async Task<ICollection<TodoItem>> GetTodosAsync(
        TodoService service,
        CancellationToken ct)
        => await service.GetAllAsync(ct);

    public static async Task<TodoItem> GetTodoByIdAsync(
        long id,
        TodoService service,
        CancellationToken ct)
        => await service.GetByIdAsync(id, ct);
}
```

```csharp
// Program.cs
builder.Services.AddHttpClient<TodoService>();

builder.Services
    .AddGraphQLServer()
    .AddTypes();
```

</Implementation>
<Code>

```csharp
// Types/TodoQueries.cs
public class TodoQueries
{
    public async Task<ICollection<TodoItem>> GetTodosAsync(
        TodoService service,
        CancellationToken ct)
        => await service.GetAllAsync(ct);

    public async Task<TodoItem> GetTodoByIdAsync(
        long id,
        TodoService service,
        CancellationToken ct)
        => await service.GetByIdAsync(id, ct);
}

// Types/TodoQueriesType.cs
public class TodoQueriesType : ObjectType<TodoQueries>
{
    protected override void Configure(IObjectTypeDescriptor<TodoQueries> descriptor)
    {
        descriptor
            .Field(f => f.GetTodoByIdAsync(default, default!, default))
            .Type<TodoType>();

        descriptor
            .Field(f => f.GetTodosAsync(default!, default))
            .Type<ListType<TodoType>>();
    }
}
```

```csharp
// Program.cs
builder.Services.AddHttpClient<TodoService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<TodoQueriesType>();
```

</Code>
</ExampleTabs>

You can now open Nitro on your GraphQL server at `/graphql` and query your REST data:

```graphql
{
  todoById(id: 1) {
    id
    isComplete
    name
  }
  todos {
    id
    isComplete
    name
  }
}
```

# Using DataLoaders with REST

When multiple GraphQL fields resolve data from the same REST endpoint, use a [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader) to batch and deduplicate calls. This prevents sending redundant HTTP requests for the same resource.

```csharp
// DataLoaders/TodoByIdDataLoader.cs
public class TodoByIdDataLoader : BatchDataLoader<long, TodoItem>
{
    private readonly TodoService _service;

    public TodoByIdDataLoader(
        TodoService service,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _service = service;
    }

    protected override async Task<IReadOnlyDictionary<long, TodoItem>> LoadBatchAsync(
        IReadOnlyList<long> keys,
        CancellationToken ct)
    {
        var todos = await _service.GetByIdsAsync(keys, ct);
        return todos.ToDictionary(t => t.Id);
    }
}
```

# Troubleshooting

## Generated client throws serialization errors

Verify that the `Newtonsoft.Json` package is installed and the generated client version matches the OpenAPI spec version. Regenerate the client if the REST API schema has changed.

## HTTP calls are slow or timing out

Register the generated client with `AddHttpClient<T>()` to leverage `HttpClientFactory`, which manages connection pooling and lifetime. Set appropriate timeouts on the `HttpClient`.

## N+1 requests to the REST API

If you see one HTTP request per item in a list, add a DataLoader to batch the calls. Without batching, each GraphQL field triggers a separate REST call.

# Next Steps

- **Need to batch REST calls?** See [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader).
- **Need to fetch from a database instead?** See [Fetching from Databases](/docs/hotchocolate/v16/fetching-data/fetching-from-databases).
- **Need to understand resolvers?** See [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers).
