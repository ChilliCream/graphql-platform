---
title: Entity Framework
---

EF Core has seen huge adoption in the .NET world.  
The execution engine of HotChocolate executes resolvers in parallel. This can lead to exceptions because
the database context of Entity Framework cannot handle more than one request in parallel. 
So if you are seeing exceptions like `A second operation started on this context before a previous operation completed.`
or `Cannot access a disposed object...` the `HotChocolate.Data.EnityFramework` package has you back.
It provides helpers that make EF integration with HotChocolate a breeze.

The package was build on the foundation of EntityFramework Core v5.0.0.

# Getting Started
You first need to add the package reference to you project. You can do this with the `dotnet` cli:

```
  dotnet add package HotChocolate.Data.EntityFramework
```

The execution engine needs more than one database context. You should register the database context
in a pool rather than transient. During execution database contexts are taken from this pool and returned
once the resolver is completed. This has a smaller memory impact, as creating a new context for each resolver.

```csharp
services.AddPooledDbContextFactory<SomeDbContext>(b => b /*your configuration */)
```

> ⚠️ **Note:** The configuration of `AddPooledDbContextFactory` replaces the `OnConfiguring` method of the `DBContext`. 
> You have to move the configuration to the factory method if you use `OnConfiguring`


# Using the DBContext
A resolver has to get a database context from the pool, execute the query and then return the context back to the
pool. 
If you annotate you field with `UseDbContext()` all of this is handeled for you

**Code First**
```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UseDbContext<SomeDbContext>()
            .Resolver((ctx, ct) =>
                {
                    return ctx.Service<SomeDbContext>().Users;
                })
    }
}
```

**Pure Code First**
```csharp
public class Query
{
    [UseDbContext(typeof(SomeDbContext))]
    public IQueryable<User> GetUsers([ScopedService] SomeDbContext someDbContext)
    {
        return someDbContext.Users;
    }
}
```
