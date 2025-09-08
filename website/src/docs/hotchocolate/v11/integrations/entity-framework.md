---
title: Entity Framework
---

The execution engine of Hot Chocolate executes resolvers in parallel. This can lead to exceptions because
the database context of Entity Framework cannot handle more than one request in parallel.
So if you are seeing exceptions like `A second operation started on this context before a previous operation completed.`
or `Cannot access a disposed object...` the `HotChocolate.Data.EntityFramework` package has you back.
It provides helpers that make EF integration with Hot Chocolate a breeze.

The package was build on the foundation of EntityFramework Core v5.0.0.

# Getting Started

You first need to add the package reference to your project.

<PackageInstallation packageName="HotChocolate.Data.EntityFramework" />

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
If you annotate a field with `UseDbContext()` all of this is handled for you

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseDbContext(typeof(SomeDbContext))]
    public IQueryable<User> GetUsers(
        [ScopedService] SomeDbContext someDbContext)
        => someDbContext.Users;
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .UseDbContext<SomeDbContext>()
            .Resolver((ctx) =>
            {
                return ctx.Service<SomeDbContext>().Users;
            })
    }
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support DbContext integration!

</Schema>
</ExampleTabs>

> ⚠️ **Note:** If you use more than one middleware, keep in mind that **ORDER MATTERS**. The correct order is UseDbContext > UsePaging > UseProjections > UseFiltering > UseSorting
