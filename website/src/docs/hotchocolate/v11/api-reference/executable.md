---
title: Executable
---

The `IExecutable` and `IExecutable<T>` interfaces are intended to be used by data providers.
These interfaces can abstract any kind of data source.
The data or domain layer can wrap data in an executable and pass it to the GraphQL layer.
A GraphQL resolver that returns an `IExecutable<T>` is recognized as a list.

```csharp
public class User
{
    public string Name { get; }
}

public interface IUserRepostiory
{
    public IExecutable<User> FindAll();
}

public class Query
{
    public IExecutable<User> GetUsers([Service] IUserRepostiory repo) =>
        repo.FindAll();
}
```

```sdl
type Query {
    users: [User!]!
}
```

This abstraction can be used to completely decouple the GraphQL layer form the database-specific knowledge.

Filtering, sorting, projections et al, can pick up the executable and apply logic to it. There is still
a database-specific provider needed for these features, but it is opaque to the GraphQL layer.

The `IExecutable` is known to the execution engine. The engine calls `ToListAsync`, `FirstOrDefault` or
`SingleOrDefault` on the executable. The executable shall execute it in the most efficient way for the
database.

# API

## Source

```csharp
    object Source { get; }
```

The source property stores the current state of the executable

In the EnittyFramework executable this property holds the `IQueryable`. In the `MongoExecutable` it is the
`DbSet<T>` or the `IAggregateFluent<T>`. `Source` is deliberately read-only. If you have a custom implementation
of `IExecutable` and you want to set the `Source`, you should create a method that returns a new executable
with the new source

## ToListAsync

```csharp
    ValueTask<IList> ToListAsync(CancellationToken cancellationToken);
```

Should return a list of `<T>`.

## FirstOrDefault

```csharp
    ValueTask<IList> FirstOrDefault(CancellationToken cancellationToken);
```

Should return the first element of a sequence, or a default value if the sequence contains no elements.

## SingleOrDefault

```csharp
    ValueTask<IList> SingleOrDefault(CancellationToken cancellationToken);
```

Should return the only element of a default value if no such element exists. This method
should throw an exception if more than one element satisfies the condition.

## Print

```csharp
string Print();
```

Prints the executable in its current state

# Example

```csharp
public class EntityFrameworkExecutable<T> : QueryableExecutable<T>
{
    public IQueryable<T> Source { get; }

    object IExecutable.Source => Source;

    public EntityFrameworkExecutable(IQueryable<T> queryable) : base(queryable)
    {
    }

    /// <summary>
    /// Returns a new enumerable executable with the provided source
    /// </summary>
    /// <param name="source">The source that should be set</param>
    /// <returns>The new instance of an enumerable executable</returns>
    public QueryableExecutable<T> WithSource(IQueryable<T> source)
    {
        return new QueryableExecutable<T>(source);
    }

    public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken) =>
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
