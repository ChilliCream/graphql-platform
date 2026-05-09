---
title: "Make it yours"
description: "Rename the Hot Chocolate starter domain, add one field, add one argument, replace the single sample value with a small in-memory list, and verify each change in Nitro."
---

Your Hot Chocolate server is already able to answer a GraphQL query. This optional quickstart will help you personalize the generated project, making it feel more like your own API before you continue with the main learning path.

By following these steps, you will:

- Rename the starter `book` field to a domain of your choice
- Add a new selectable field
- Add an argument so the client can choose an item
- Replace the single sample value with a small in-memory list
- Verify each change in Nitro

This guide assumes you have completed [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query) and still have the `GettingStarted` project. If your starter query is not working, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) before making changes.

# Start from a working query

For each section, use this edit loop:

1. Stop the running server with <kbd>Ctrl</kbd> + <kbd>C</kbd>.
2. Edit the C# file.
3. Run `dotnet run`.
4. Refresh the schema in Nitro or reload the browser page.
5. Run the checkpoint query.

The starter query from the previous page looked like this:

```graphql
{
  book {
    title
    author {
      name
    }
  }
}
```

This page will change that query step by step. Each change you make in C#, whether to a member or a resolver parameter, directly shapes what the GraphQL client can request.

If you added `PublishedYear` earlier, you can ignore that now. The following examples will use a new `Product` type instead of `Book`.

# Rename the sample to your domain

Pick a simple domain noun, such as `Product`, `Pet`, `Session`, or `Book`. This example uses `Product`.

First, rename `Types/Book.cs` to `Types/Product.cs` and update its contents:

```csharp
namespace GettingStarted.Types;

public record Product(string Name);
```

Next, open `Types/Query.cs` and replace the `book` resolver with a `product` resolver:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Product GetProduct()
        => new Product("Trail running shoe");
}
```

If your project uses a different namespace, keep your existing namespace and update only the type and resolver code.

You can keep `Types/Author.cs` for now, or delete it after the project builds. The `Product` example does not use it.

Restart the server:

```bash
dotnet run
```

You should see output like:

```text
Now listening on: http://localhost:5095
```

The port may differ on your machine. Open the `/graphql` URL and refresh the schema in Nitro if needed.

Try the renamed query:

```graphql
{
  product {
    name
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "name": "Trail running shoe"
    }
  }
}
```

Hot Chocolate uses .NET naming conventions in C# and GraphQL naming conventions in the schema. The C# method `GetProduct` becomes the GraphQL field `product`.

# Add a field the client can select

Add a new field to your domain object. For the product example, add a `Description` property to the `Product` record:

```csharp
namespace GettingStarted.Types;

public record Product(string Name, string Description);
```

Update `Types/Query.cs` so the resolver provides the new value:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Product GetProduct()
        => new Product(
            "Trail running shoe",
            "Lightweight shoe for dry trails.");
}
```

Restart the server and refresh Nitro's schema. Now, select both the original and new fields:

```graphql
{
  product {
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "name": "Trail running shoe",
      "description": "Lightweight shoe for dry trails."
    }
  }
}
```

GraphQL clients choose which fields to request. When you add a new .NET member and restart the server, the schema exposes a new GraphQL field. For example, a C# property named `Description` appears as `description` in the schema.

For a more detailed guide to adding fields, see [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field).

# Add an argument to select data

Fields can accept arguments. In Hot Chocolate, a resolver parameter becomes a GraphQL argument unless it is recognized as a service or special parameter.

Change `GetProduct` so the client provides an `id`:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Product? GetProduct(int id)
        => id == 1
            ? new Product(
                "Trail running shoe",
                "Lightweight shoe for dry trails.")
            : null;
}
```

The `Product?` return type means the field can return `null` if no product matches. Restart the server and refresh Nitro's schema.

Run the query with an argument:

```graphql
{
  product(id: 1) {
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "name": "Trail running shoe",
      "description": "Lightweight shoe for dry trails."
    }
  }
}
```

Try a value that does not match:

```graphql
{
  product(id: 99) {
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": null
  }
}
```

Field arguments are part of the field selection in GraphQL. Here, `id: 1` is part of the `product` field request, and the selected fields determine the response shape.

For more patterns, including variables and optional arguments, see [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument).

# Replace the sample value with a small in-memory list

The resolver now accepts an `id`, but still returns only one hardcoded item. Replace that with a small list of sample items so you can see the argument change the result.

First, add an `Id` property to `Types/Product.cs`:

```csharp
namespace GettingStarted.Types;

public record Product(int Id, string Name, string Description);
```

Then update `Types/Query.cs`:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    private static readonly Product[] s_products =
    {
        new(
            1,
            "Trail running shoe",
            "Lightweight shoe for dry trails."),
        new(
            2,
            "Hydration pack",
            "Small pack for longer runs.")
    };

    public static Product? GetProduct(int id)
        => s_products.FirstOrDefault(product => product.Id == id);
}
```

This is sample data for learning purposes, not a production data access pattern. You will connect resolvers to real services and databases later.

Restart the server and refresh Nitro's schema. Query the first product:

```graphql
{
  product(id: 1) {
    id
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "id": 1,
      "name": "Trail running shoe",
      "description": "Lightweight shoe for dry trails."
    }
  }
}
```

Now change only the argument value:

```graphql
{
  product(id: 2) {
    id
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "id": 2,
      "name": "Hydration pack",
      "description": "Small pack for longer runs."
    }
  }
}
```

This demonstrates the core request flow:

1. The client passes a GraphQL argument.
2. Hot Chocolate maps that argument to the C# resolver parameter.
3. The resolver selects an item.
4. The client's selection set shapes the JSON response.

To see how this pattern connects to real data, continue with [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview).

# Check your mini API

Before moving on, confirm each checkpoint:

| Checkpoint | How to verify |
| --- | --- |
| The app builds and runs. | `dotnet run` prints `Now listening on:`. |
| Nitro uses the current schema. | Refresh the schema or reload the `/graphql` page after each restart. |
| The renamed field appears. | Nitro autocompletes `product`. |
| The new field appears. | Nitro autocompletes `description` inside `product { ... }`. |
| The argument appears. | Nitro accepts `product(id: 1)`. |
| The in-memory data has more than one item. | `id: 1` and `id: 2` return different products. |

Your final operation should look like this:

```graphql
{
  product(id: 2) {
    id
    name
    description
  }
}
```

Expected response:

```json
{
  "data": {
    "product": {
      "id": 2,
      "name": "Hydration pack",
      "description": "Small pack for longer runs."
    }
  }
}
```

If any checkpoint fails, use the troubleshooting table below or see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Troubleshoot small edits

| Symptom | Likely cause | What to do |
| --- | --- | --- |
| The app no longer builds after renaming. | A C# reference still uses `Book`, `Author`, `GetBook`, or the old record constructor. | Follow the compiler error, update the remaining reference, and run `dotnet run` again. |
| Nitro still shows `book`. | The server is still running the old build, or Nitro has not refreshed the schema. | Stop the server, run `dotnet run`, then refresh Nitro's schema or reload the browser page. |
| The query says `Cannot query field`. | The GraphQL operation uses the old field name or the field was added to a different type. | Use `product`, `name`, `description`, and later `id`. Check casing. |
| The query says a required argument was not provided. | `GetProduct(int id)` became a field with a required `id` argument. | Pass the argument as `product(id: 1)`. |
| The result is `null`. | No sample product matches the argument value. | Use `id: 1` or `id: 2`, then check that the lookup compares `product.Id` to `id`. |
| The response does not include a field you expected. | The field is not in the selection set. | Add the field inside `product { ... }` and run the operation again. |

# Keep learning from here

You have now worked with four beginner concepts: names, fields, arguments, and the data source for a resolver.

Choose your next step:

- **Add fields in a guided path.** See [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field).
- **Add input to fields.** See [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument).
- **Preview real data.** See [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview).
- **Choose a larger path.** Go to [Next steps](/docs/hotchocolate/v16/get-started/next-steps).
- **Review the GraphQL language behind the examples.** See the [GraphQL specification](https://spec.graphql.org/) or the [GraphQL learning site](https://graphql.org/learn/).
