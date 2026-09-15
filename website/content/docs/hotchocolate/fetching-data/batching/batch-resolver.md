---
title: "Batch Resolvers"
metaTitle: "GraphQL Batch Resolvers: DataLoader Alternative"
description: "Use a Hot Chocolate batch resolver to resolve a GraphQL field for many parents in one call: a lighter alternative to DataLoader without caching."
---

Batch resolvers resolve a GraphQL field for many parent objects in a single call. Instead of running a resolver once per parent and batching through a [DataLoader](./dataloader.md), the execution engine collects all parent objects that reach the field and calls your method once with the full list. You do not define a DataLoader class or manage keys.

# When to Use Batch Resolvers vs DataLoaders

A batch resolver batches one field selection: the execution engine collects every parent that reaches that exact selection and calls your method once, with no cache and no merging across selections.

A [DataLoader](./dataloader.md) batches and caches by key, so the same lookup is merged wherever it occurs, across selections, fields, and types.

**Use a batch resolver** when a field's value only needs computing once per selection: aggregations over the parent set, computed values, or one call to a batch-capable external service.

**Use a DataLoader** when the same entity or key can be requested from more than one place in a query and must still be loaded only once.

# Supported Combinations

The table lists every scenario a batch resolver is proven to support, and the boundaries of that support.

| Feature                                                                                                                                                    | Supported | Notes                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Per-parent field middleware (`descriptor.Use(...)`, `[UseDataLoader]`, `[UseFirstOrDefault]`, `[UseSingleOrDefault]`, or any attribute that registers one) | No        | Fails schema build with a member-naming error (`HC0134`); `[UseDataLoader]`, `[UseFirstOrDefault]`, and `[UseSingleOrDefault]` on a `[BatchResolver]` method are caught earlier, at compile time (`HC0138`). Use the batch twin instead. (PerParentMiddlewareBatchTests, BatchResolverMiddlewareNotSupportedAnalyzerTests; hc-0-6cq.10, hc-0-6cq.17)                                                                                                                                                                                 |
| Batch middleware (`UseBatch`)                                                                                                                              | Yes       | Runs once per partition, composing class and factory middleware with directive middleware in registration order. (BatchMiddlewareBatchTests; hc-0-6cq.10, hc-0-6cq.17)                                                                                                                                                                                                                                                                                                                                                               |
| Global `[UseField]` middleware                                                                                                                             | No        | Never applies to batch fields; there is no batch counterpart. (hc-0-6cq.10)                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| Authorization (`[Authorize]`)                                                                                                                              | Yes       | Supported through the native batch twin, including policy checks before and after the resolver runs. (AuthorizationBatchTests; hc-0-6cq.3, hc-0-6cq.10)                                                                                                                                                                                                                                                                                                                                                                              |
| Paging, filtering, sorting                                                                                                                                 | Yes       | Supported through batch twins for cursor paging, offset paging, filtering, and sorting; an omitted page size normalizes to the field's configured default. `PageConnection<T>` fields using `[UseConnection]` follow the declaration limits below. (CursorPagingBatchTests, OffsetPagingBatchTests, FilteringBatchTests, SortingBatchTests; hc-0-6cq.10, hc-0-6cq.13)                                                                                                                                                                |
| Projections and `QueryContext`                                                                                                                             | Yes       | Supported transparently; a field selected through any one parent's occurrence is treated as included for the whole batch. (ProjectionBatchTests; hc-0-6cq.15, hc-0-4hj.7)                                                                                                                                                                                                                                                                                                                                                            |
| `@defer`                                                                                                                                                   | Yes       | Batch fields inside a `@defer` fragment resolve normally, at the root or nested. (DeferBatchTests; hc-0-6cq.14)                                                                                                                                                                                                                                                                                                                                                                                                                      |
| Variable batching                                                                                                                                          | Yes       | One batch spans every variable set in a request, with per-entry argument scope; on mutation root fields, each set's batch still runs in sequence. (VariableBatchBatchTests; hc-0-6cq.14, hc-0-6cq.18)                                                                                                                                                                                                                                                                                                                                |
| `node` / `nodes`                                                                                                                                           | Yes       | Backed by engine batch resolvers, including per-alias dispatch, duplicate id coalescing, and authorization. (NodeResolverBatchTests; hc-0-6cq.12)                                                                                                                                                                                                                                                                                                                                                                                    |
| Batch resolver on a mutation root field                                                                                                                    | No        | Fails schema build with a member-naming error (`HC0135`); source-generated code is caught earlier, at compile time (`HC0137`). (MutationRootBatchTests; hc-0-6cq.5, hc-0-6cq.17)                                                                                                                                                                                                                                                                                                                                                     |
| Batch field below a mutation root field                                                                                                                    | Yes       | Sequential mutation root steps never share a batch with one another. (EngineBatchTests; hc-0-6cq.18)                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| Abstract parents (interface or union)                                                                                                                      | Yes       | Each concrete type behind the abstract parent batches its own resolver separately; a field declared directly on an interface batches per concrete implementing type it resolves through, not across every implementer. (AbstractParentBatchTests, InterfaceBatchTests; hc-0-jyk.7)                                                                                                                                                                                                                                                   |
| Parent shapes                                                                                                                                              | Yes       | The `[Parent]` parameter accepts `List<T>`, `IReadOnlyList<T>`, `T[]`, and `ImmutableArray<T>`; declaring it as `IList<T>` or `IEnumerable<T>` is backed by a `List<T>` instance. (EngineBatchTests; hc-0-6cq.15)                                                                                                                                                                                                                                                                                                                    |
| Return shapes                                                                                                                                              | Yes       | The return type must be a list with exactly one element per parent, in parent order; `Task<T>` and `ValueTask<T>` are unwrapped. Any other return type fails schema build with a member-naming error. A count mismatch at execution time fails every parent in the batch. (EngineBatchTests, NonListReturnBatchTests; hc-0-6cq.15)                                                                                                                                                                                                   |
| Selection APIs (`Selection`, `IsSelected`, `QueryContext`)                                                                                                 | Yes       | Bind through the same declared types as in a singular resolver; a field selected through any one parent's occurrence counts as included for the whole batch. (VariableBatchBatchResolverTests, SourceGeneratorBatchResolverTests; hc-0-6cq.15, hc-0-4hj.7, hc-0-jyk.5)                                                                                                                                                                                                                                                               |
| Executable directive with middleware on a batch selection                                                                                                  | No        | Rejected at operation compile time (`HC0136`), naming the directive and the field. (hc-0-6cq.10, hc-0-6cq.17, hc-0-4hj.5)                                                                                                                                                                                                                                                                                                                                                                                                            |
| Declaration style (attribute, source-generated, fluent)                                                                                                    | Yes       | Every row above is proven in all three styles, with two exceptions: a batch field declared directly on an interface type needs `ResolveBatchWith` (fluent) or an `[InterfaceType<T>]` source-generated partial, because implicit interface field discovery does not see static members (InterfaceBatchTests; hc-0-1aa.3); a `PageConnection<T>` field using `[UseConnection]` needs an attribute or a source-generated partial, because no public fluent `UseConnection` exists (PageConnectionBatchTests; hc-0-1aa.5). (hc-0-6cq.1) |

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

The `[Parent]` parameter and list-typed argument parameters accept four shapes: `List<T>`, `IReadOnlyList<T>`, `T[]`, and `ImmutableArray<T>`. Declaring one as `IList<T>` or `IEnumerable<T>` is also accepted, backed by a `List<T>` instance.

The return type must be a list with exactly one element per parent, in the same order as the parents; `Task<T>` and `ValueTask<T>` are unwrapped, so an async method returning either is treated the same as one returning the list directly. Any other return type, including `IEnumerable<T>`, fails schema build with an error naming the method.

A field's `Selection`, `IsSelected`, and `QueryContext` parameters bind through the same declared types as in a singular resolver: a field is treated as selected for the whole batch if any one parent's occurrence selects it.

> [!WARNING]
> Returning one result per parent, in parent order, is your responsibility. A returned list whose count does not match the parent count fails every parent in the batch with an error, the same as an unhandled exception.

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

`ResolveBatchWith<T>` accepts synchronous methods and methods returning `Task<T>` or `ValueTask<T>` alike.

# Limitations

- Standard field middleware does not run for batch resolver fields; see [Supported Combinations](#supported-combinations) for the batch twin of each middleware-based feature.
- Services, state, and `CancellationToken` are bound once per batch, not per parent. When the field uses a resolver-level dependency injection scope, one scope is shared by the whole batch.
- An unhandled exception fails the whole batch. Use `ResolveBatch` with `ResolverResult.Fail` for per-parent errors.

# Next Steps

- **Loading data by key with caching?** See [DataLoader](./dataloader.md).
- **New to batching?** See the [Batching overview](./index.md) for the N+1 background.
- **Need to understand resolver basics?** See [Resolvers](../../resolvers/index.md).
