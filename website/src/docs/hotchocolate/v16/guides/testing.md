---
title: "Testing"
---

Testing a GraphQL server means testing resolvers, the schema shape, and the execution pipeline. Hot Chocolate provides test infrastructure for all three. This page walks through the patterns you need to write reliable tests for a Hot Chocolate server.

# Set Up a Test Executor

The foundation for all integration tests is an `IRequestExecutor`. You build one from a `ServiceCollection` the same way you configure the server in `Program.cs`, but without the ASP.NET Core host.

```csharp
// Tests/ProductTests.cs
public class ProductTests
{
    [Fact]
    public async Task Get_Product_Returns_Name()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ product { name } }");

        // assert
        Assert.NotNull(result);
    }
}
```

You can register any services your resolvers depend on before calling `AddGraphQLServer()`. This lets you inject real or mock implementations.

```csharp
// Tests/ProductTests.cs
var executor = await new ServiceCollection()
    .AddSingleton<ICatalogService>(new FakeCatalogService())
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .BuildRequestExecutorAsync();
```

# Execute Test Queries

Use `executor.ExecuteAsync()` to run a GraphQL operation and get back an `IExecutionResult`. For type-safe access to the result, call `ExpectOperationResult()`:

```csharp
// Tests/ProductTests.cs
[Fact]
public async Task Get_Product_Returns_Expected_Data()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddSingleton<ICatalogService>(new FakeCatalogService())
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ product { name price } }");

    // assert
    var operationResult = result.ExpectOperationResult();
    Assert.Null(operationResult.Errors);
}
```

## Pass Variables

To pass variables, use `OperationRequestBuilder`:

```csharp
// Tests/ProductTests.cs
[Fact]
public async Task Get_Product_By_Id()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddSingleton<ICatalogService>(new FakeCatalogService())
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync(
        OperationRequestBuilder.New()
            .SetDocument("query($id: Int!) { productById(id: $id) { name } }")
            .SetVariableValues(new Dictionary<string, object?> { { "id", 42 } })
            .Build());

    // assert
    var operationResult = result.ExpectOperationResult();
    Assert.Null(operationResult.Errors);
}
```

# Snapshot Testing with CookieCrumble

Asserting on individual fields works for small results, but GraphQL responses can be large and nested. Snapshot testing captures the entire response and compares it against a stored baseline. Hot Chocolate uses [CookieCrumble](/docs/hotchocolate/v16/testing) for this.

## File-Based Snapshots

Call `MatchSnapshot()` on the result. The first run creates a snapshot file in a `__snapshots__/` directory next to your test file. Subsequent runs compare against that file.

```csharp
// Tests/ProductTests.cs
[Fact]
public async Task Get_Product_Snapshot()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddSingleton<ICatalogService>(new FakeCatalogService())
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ product { name price } }");

    // assert
    result.MatchSnapshot();
}
```

When the schema changes and the response shape changes with it, delete the old snapshot file and re-run the test. CookieCrumble creates a new snapshot with the updated output.

## Inline Snapshots

For smaller results, inline the expected output directly in your test. This keeps the expectation visible next to the assertion.

```csharp
// Tests/ProductTests.cs
[Fact]
public async Task Get_Product_Inline()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddSingleton<ICatalogService>(new FakeCatalogService())
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ product { name } }");

    // assert
    result.MatchInlineSnapshot(
        """
        {
          "data": {
            "product": {
              "name": "Widget"
            }
          }
        }
        """);
}
```

# Test Resolvers in Isolation

Integration tests run the full execution pipeline, which is thorough but slower. When you want fast feedback on resolver logic, test the method directly.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProductById(int id, ICatalogService catalog)
        => catalog.GetById(id);
}
```

```csharp
// Tests/ProductQueriesTests.cs
public class ProductQueriesTests
{
    [Fact]
    public void GetProductById_Returns_Product_When_Found()
    {
        // arrange
        var catalog = new FakeCatalogService();
        catalog.Add(new Product { Id = 1, Name = "Widget" });

        // act
        var result = ProductQueries.GetProductById(1, catalog);

        // assert
        Assert.NotNull(result);
        Assert.Equal("Widget", result.Name);
    }

    [Fact]
    public void GetProductById_Returns_Null_When_Not_Found()
    {
        // arrange
        var catalog = new FakeCatalogService();

        // act
        var result = ProductQueries.GetProductById(999, catalog);

        // assert
        Assert.Null(result);
    }
}
```

This approach is useful for resolvers that contain business logic. For resolvers that are thin wrappers around a service call, integration tests through the executor provide more value.

# Test Schema Shape

When you want to catch unintended schema changes (renamed fields, changed nullability, missing types), snapshot the schema SDL.

```csharp
// Tests/SchemaTests.cs
public class SchemaTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act & assert
        executor.Schema.MatchSnapshot();
    }
}
```

`executor.Schema.MatchSnapshot()` serializes the schema to SDL and compares it against the stored snapshot. If you add a field, rename a type, or change nullability, the snapshot test fails and shows the diff. Review the diff to confirm the change is intentional, then update the snapshot.

You can also use `executor.Schema.ToString()` to get the SDL as a string if you need to inspect it programmatically:

```csharp
// Tests/SchemaTests.cs
[Fact]
public async Task Schema_Contains_Product_Type()
{
    var executor = await new ServiceCollection()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .BuildRequestExecutorAsync();

    var sdl = executor.Schema.ToString();

    Assert.Contains("type Product", sdl);
}
```

# Test Middleware and Error Handling

If you register custom field middleware or error filters, test them through the execution pipeline.

## Custom Middleware

Register middleware in the test executor the same way you register it in `Program.cs`, then execute a query that exercises it.

```csharp
// Tests/LoggingMiddlewareTests.cs
[Fact]
public async Task Logging_Middleware_Does_Not_Alter_Result()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseField<LoggingMiddleware>()
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ product { name } }");

    // assert
    var operationResult = result.ExpectOperationResult();
    Assert.Null(operationResult.Errors);
}
```

## Error Filters

To verify that your error filter transforms errors correctly, trigger an error in a resolver and assert on the error message in the result.

```csharp
// Tests/ErrorFilterTests.cs
[Fact]
public async Task Error_Filter_Masks_Internal_Errors()
{
    // arrange
    var executor = await new ServiceCollection()
        .AddGraphQLServer()
        .AddQueryType<QueryWithError>()
        .AddErrorFilter(error =>
            error.WithMessage("An unexpected error occurred."))
        .BuildRequestExecutorAsync();

    // act
    var result = await executor.ExecuteAsync("{ failingField }");

    // assert
    var operationResult = result.ExpectOperationResult();
    Assert.NotNull(operationResult.Errors);
    Assert.Equal(
        "An unexpected error occurred.",
        operationResult.Errors[0].Message);
}
```

# Next Steps

- **Error handling reference:** [Error Handling Guide](/docs/hotchocolate/v16/guides/error-handling) covers error types, error filters, and how to structure error responses.
- **CookieCrumble:** The snapshot testing framework lives in `src/CookieCrumble/` in the repository. Explore the source for advanced snapshot configuration.
- **Schema evolution:** [Schema Evolution Guide](/docs/hotchocolate/v16/guides/schema-evolution) covers deprecation, opt-in features, and managing schema changes over time.
