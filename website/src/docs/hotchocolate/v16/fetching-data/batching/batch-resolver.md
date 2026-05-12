---
title: "Batch Resolvers"
---

Batch resolvers are an alternative to DataLoaders for cases where you want to resolve a field for multiple parent objects in a single method call without defining a separate DataLoader class. Instead of each resolver running independently and batching through a DataLoader, the execution engine collects all parent objects and calls your resolver once with the full list.

# When to Use Batch Resolvers vs DataLoaders

**Use a DataLoader** when the batched data is reused across multiple fields or resolvers. DataLoaders cache by key, so the same entity fetched in different parts of the query tree is only loaded once.

**Use a batch resolver** when the resolved value is specific to one field and does not benefit from cross-field caching. Common examples: computed values, string formatting, or calling an external service that supports batch requests natively.

# Defining a Batch Resolver

Mark a method with `[BatchResolver]`. The `[Parent]` parameter and all arguments must be list types. The return type must also be a list, with one element per parent.

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

The execution engine collects all `User` parent objects being resolved in the current wave and calls `GetDisplayName` once with the full list. The returned list must have the same count and order as the input list.

# Batch Resolvers with Services and Arguments

Batch resolvers support dependency injection and field arguments. Arguments that are list types are collected from each parent context.

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static async Task<List<string>> GetGreeting(
        [Parent] List<User> users,
        GreetingService greetingService,
        CancellationToken ct)
    {
        return await greetingService.GetGreetingsAsync(
            users.Select(u => u.Id).ToList(), ct);
    }
}
```

# Handling Errors in Batch Resolvers

Use `ResolverResult` to return per-item errors without failing the entire batch. Each element in the returned list can be either a success or an error.

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<ResolverResult> GetVerificationStatus([Parent] List<User> users)
    {
        return users.Select<User, ResolverResult>(user =>
        {
            if (user.Email is null)
                return ResolverResult.Fail(
                    ErrorBuilder.New()
                        .SetMessage("User has no email address.")
                        .Build());

            return ResolverResult.Ok(user.IsVerified ? "verified" : "pending");
        }).ToList();
    }
}
```

# Code-First Batch Resolvers

In the code-first approach, use `ResolveBatch` on the field descriptor.

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

You can also point to an external method with `ResolveBatchWith<T>`.

```csharp
descriptor
    .Field("greeting")
    .ResolveBatchWith<UserNode>(t => t.GetGreeting(default!));
```

# Next Steps

- **Need to understand the N+1 problem?** See [DataLoader](/docs/hotchocolate/v16/fetching-data/batching/dataloader) for key-based batching with caching.
- **Need to understand resolver basics?** See [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers).
