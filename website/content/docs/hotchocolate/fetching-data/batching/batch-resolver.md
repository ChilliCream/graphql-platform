---
title: "Batch Resolvers"
metaTitle: "GraphQL Batch Resolvers: DataLoader Alternative"
description: "Use a Hot Chocolate batch resolver to resolve a GraphQL field for many parents in one call: a lighter alternative to DataLoader without caching."
---

Batch resolvers resolve a GraphQL field for many parent objects in a single call. Instead of running a resolver once per parent and batching through a [DataLoader](./dataloader.md), the execution engine collects all parent objects that reach the field and calls your method once with the full list. You do not define a DataLoader class or manage keys.

# When to Use Batch Resolvers vs DataLoaders

**Use a DataLoader** when data is loaded by key and may be requested from more than one place in a query. DataLoaders cache and deduplicate by key, so the same entity fetched in different parts of the query tree is only loaded once.

**Use a batch resolver** when the resolved value is specific to one field and does not benefit from cross-field caching. Common examples: computed values, aggregations over the parent set, or calling an external service that supports batch requests natively.

The batching models differ: a DataLoader batches by key and can merge lookups across fields, types, and depths, while a batch resolver batches by field and is invoked exactly once per field selection in an operation, with no cache involved.

# Defining a Batch Resolver

Mark a method with `[BatchResolver]`. The `[Parent]` parameter must be a list of the parent type, and the return type must be a list with one element per parent, in the same order.

**C# resolver**

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<string> GetDisplayName([Parent] List<User> users)
    {
        return users.Select(u => $"{u.FirstName} {u.LastName}").ToList();
    }
}
```

The execution engine collects all `User` parent objects being resolved for this field and calls `GetDisplayName` once with the full list. The field's GraphQL type is derived from the list's element type, so this field is a `String`.

Supported list shapes for the `[Parent]` parameter, argument parameters, and the return type are `T[]`, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, and `ImmutableArray<T>`. Other collection types like `IEnumerable<T>` are rejected when the schema is built.

> [!WARNING]
> Returning one result per parent, in parent order, is your responsibility. If an attribute-based batch resolver returns fewer elements than parents, the remaining parents silently receive `null`; extra elements are ignored. Only the code-first `ResolveBatch` API enforces the count and throws on a mismatch.

## A Real-World Example

A typical use case is an aggregate over the parent set, computed with one database query:

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    [BatchResolver]
    public static async Task<List<int>> GetProductCountAsync(
        [Parent] List<Brand> brands,
        [Service] CatalogContext context,
        CancellationToken cancellationToken)
    {
        var brandIds = brands.ConvertAll(b => b.Id);

        var counts = await context.Products
            .Where(p => brandIds.Contains(p.BrandId))
            .GroupBy(p => p.BrandId)
            .Select(g => new { BrandId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BrandId, g => g.Count, cancellationToken);

        return brands.ConvertAll(b => counts.GetValueOrDefault(b.Id, 0));
    }
}
```

No matter how many brands the query returns, `productCount` is computed with a single grouped query, and the results are mapped back to the parents positionally.

# Parameter Binding

Batch resolver parameters fall into two groups:

- **Per parent (list-typed)**: the `[Parent]` parameter and GraphQL field arguments. Both are collected as lists with one entry per parent, in the same order as the parents. An argument parameter declared as `List<string> prefix` produces a GraphQL argument `prefix: String` (the element type, not a list type), and `prefix[i]` carries the coerced argument value for the parent at index `i`.
- **Once per batch (singular)**: everything else. Services (`[Service]`), `[GlobalState]`, `[ScopedState]`, and `CancellationToken` are resolved once for the whole batch call, not per parent.

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<string> GetGreeting(
        [Parent] List<User> users,
        List<string> prefix)
    {
        var result = new List<string>();

        for (var i = 0; i < users.Count; i++)
        {
            result.Add($"{prefix[i]}, {users[i].Name}!");
        }

        return result;
    }
}
```

# Async Batch Resolvers and Services

Batch resolvers can be synchronous or return `Task<T>` or `ValueTask<T>`. Services are injected with the `[Service]` attribute:

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static async Task<List<string>> GetGreeting(
        [Parent] List<User> users,
        [Service] GreetingService greetingService,
        CancellationToken ct)
    {
        return await greetingService.GetGreetingsAsync(
            users.Select(u => u.Id).ToList(), ct);
    }
}
```

> [!WARNING]
> Annotate custom service parameters with `[Service]`. Without it, the source generator classifies the parameter as a per-parent GraphQL argument and generates broken code. Well-known infrastructure types like `CancellationToken` are recognized without an attribute.

# Handling Errors

If a batch resolver throws an unhandled exception, the entire batch fails: every parent in the batch receives the same error and a `null` result.

To report an error for individual parents while the rest of the batch resolves normally, use the code-first `ResolveBatch` API with `ResolverResult`. Each element of the returned list is either `ResolverResult.Ok(value)` or `ResolverResult.Fail(error)`:

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("verificationStatus")
            .Type<StringType>()
            .ResolveBatch(contexts =>
            {
                var results = new ResolverResult[contexts.Count];

                for (var i = 0; i < contexts.Count; i++)
                {
                    var user = contexts[i].Parent<User>();

                    results[i] = user.Email is null
                        ? ResolverResult.Fail(
                            ErrorBuilder.New()
                                .SetMessage("User has no email address.")
                                .Build())
                        : ResolverResult.Ok(user.IsVerified ? "verified" : "pending");
                }

                return new ValueTask<IReadOnlyList<ResolverResult>>(results);
            });
    }
}
```

A failed element becomes a GraphQL error at that specific parent's path, while the other parents keep their data:

**Response**

```json
{
  "errors": [
    {
      "message": "User has no email address.",
      "path": ["users", 1, "verificationStatus"]
    }
  ],
  "data": {
    "users": [
      { "verificationStatus": "verified" },
      { "verificationStatus": null }
    ]
  }
}
```

> [!WARNING]
> `ResolverResult` only works with the code-first `ResolveBatch` API. Returning `List<ResolverResult>` from a `[BatchResolver]`-attributed method (or through `ResolveBatchWith`) is not unwrapped: every element fails leaf-value coercion with an `EXEC_INVALID_LEAF_VALUE` error.

# Code-First Batch Resolvers

In the code-first approach, use `ResolveBatch` on the field descriptor. The delegate receives one `IResolverContext` per parent and must return exactly one `ResolverResult` per context, in the same order. A count mismatch throws an `InvalidOperationException` at execution time.

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("displayName")
            .Type<StringType>()
            .ResolveBatch(contexts =>
            {
                var results = new ResolverResult[contexts.Count];

                for (var i = 0; i < contexts.Count; i++)
                {
                    var user = contexts[i].Parent<User>();
                    results[i] = ResolverResult.Ok($"{user.FirstName} {user.LastName}");
                }

                return new ValueTask<IReadOnlyList<ResolverResult>>(results);
            });
    }
}
```

You can also point a field at an existing batch resolver method with `ResolveBatchWith<T>`:

```csharp
descriptor
    .Field("displayName")
    .ResolveBatchWith<UserNode>(t => t.GetDisplayName(default!));
```

`ResolveBatchWith<T>` only supports synchronous methods. Pointing it at a method that returns `Task<T>` or `ValueTask<T>` throws a `SchemaException` when the schema is built. Async batch resolvers must be defined with the `[BatchResolver]` attribute or as a `ResolveBatch` delegate.

# Limitations

- Regular field middleware does not run for batch resolver fields. Middleware-based features such as `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, and `[UseSorting]` are not compatible with `[BatchResolver]` or `ResolveBatch`.
- Services, state, and `CancellationToken` are bound once per batch, not per parent. When the field uses a resolver-level dependency injection scope, one scope is shared by the whole batch.
- An unhandled exception fails the whole batch. Use `ResolveBatch` with `ResolverResult.Fail` for per-parent errors.

# Next Steps

- **Loading data by key with caching?** See [DataLoader](./dataloader.md).
- **New to batching?** See the [Batching overview](./index.md) for the N+1 background.
- **Need to understand resolver basics?** See [Resolvers](../../resolvers/index.md).
