---
title: Executable
description: Learn how to use the IExecutable interface to abstract data sources in Hot Chocolate v16.
---

The `IExecutable` and `IExecutable<T>` interfaces abstract data sources in Hot Chocolate. Your data or domain layer can wrap a data source in an executable and pass it to the GraphQL layer. A resolver that returns `IExecutable<T>` is recognized as a list.

```csharp
public class User
{
    public string Name { get; }
}

public interface IUserRepository
{
    public IExecutable<User> FindAll();
}

public class Query
{
    public IExecutable<User> GetUsers(IUserRepository repo) =>
        repo.FindAll();
}
```

```sdl
type Query {
    users: [User!]!
}
```

This abstraction completely decouples the GraphQL layer from database-specific knowledge.

Filtering, sorting, and projections can pick up the executable and apply logic to it. A database-specific provider is still needed for these features, but it is opaque to the GraphQL layer.

The execution engine calls `ToListAsync`, `FirstOrDefaultAsync`, or `SingleOrDefaultAsync` on the executable. The executable runs these operations in the most efficient way for the database.

# API

## Source

```csharp
object Source { get; }
```

The `Source` property holds the current state of the executable. For Entity Framework, this holds the `IQueryable`. For MongoDB, it is the `DbSet<T>` or `IAggregateFluent<T>`. `Source` is read-only. If you have a custom `IExecutable` implementation and need to change the source, create a method that returns a new executable with the new source.

## ToListAsync

```csharp
ValueTask<IList> ToListAsync(CancellationToken cancellationToken);
```

Returns a list of items.

## FirstOrDefaultAsync

```csharp
ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);
```

Returns the first element of the sequence, or a default value if the sequence contains no elements.

## SingleOrDefaultAsync

```csharp
ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken);
```

Returns the only element of the sequence, or a default value if no element exists. Throws an exception if more than one element satisfies the condition.

## Print

```csharp
string Print();
```

Prints the executable in its current state. This is useful for debugging and logging the generated query.

# Example

The following shows the Entity Framework implementation:

```csharp
public class EntityFrameworkExecutable<T> : QueryableExecutable<T>
{
    public IQueryable<T> Source { get; }

    object IExecutable.Source => Source;

    public EntityFrameworkExecutable(IQueryable<T> queryable) : base(queryable)
    {
    }

    public QueryableExecutable<T> WithSource(IQueryable<T> source)
    {
        return new QueryableExecutable<T>(source);
    }

    public override async ValueTask<IList> ToListAsync(
        CancellationToken cancellationToken) =>
        await Source.ToListAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<object?> FirstOrDefaultAsync(
        CancellationToken cancellationToken) =>
        await Source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<object?> SingleOrDefaultAsync(
        CancellationToken cancellationToken) =>
        await Source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override string Print() => Source.ToQueryString();
}
```

# Troubleshooting

**Executable returns all items instead of applying filters**
Verify that you registered the correct filtering provider for your database (e.g., `AddMongoDbFiltering()` for MongoDB). The `IExecutable` must be paired with the appropriate data middleware.

**"Sequence contains more than one element" exception**
This occurs when `SingleOrDefaultAsync` finds multiple results. Verify that your query criteria produce a unique result, or use `FirstOrDefaultAsync` instead.

# Next Steps

- [Entity Framework integration](/docs/hotchocolate/v16/integrations/entity-framework) for EF Core executables
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb) for MongoDB executables
- [Filtering](/docs/hotchocolate/v16/fetching-data/filtering) for applying filters to executables
