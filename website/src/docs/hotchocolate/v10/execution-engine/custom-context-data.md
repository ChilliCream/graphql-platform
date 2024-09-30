---
title: Custom Context Data
---

When implementing custom middleware, it can be useful to be able to store some custom state on the context. This could be to build up a cache or other state data. Hot Chocolate has two types of context stores that we can use.

# Global Context Data

The global context data is a thread-safe dictionary that is available though the `IQueryContext` and the `IResolverContext`. This means we are able to share context data between query middleware components and field middleware components.

One common use case is to aggregate some state when the GraphQL request is created and use it in field middleware or in the resolver.

In order to intercept the request creation we can add an `IQueryRequestInterceptor` to our services and there build up our custom state.

```csharp
services.AddQueryRequestInterceptor((ctx, builder, ct) =>
{
    builder.SetProperty("Foo", new Foo());
    return Task.CompletedTask;
});
```

We can access the initial provided data in a query middleware, field middleware or our resolver.

Query Middleware Example:

```csharp
builder.Use(next => context =>
{
    // access data
    var foo = (Foo)context.ContextData["Foo"];

    // set new data
    context.ContextData["Bar"] = new Bar();

    return next.Invoke(context);
});
```

Field Middleware Example:

```csharp
SchemaBuilder.New()
  .Use(next => context =>
  {
      // access data
      var foo = (Foo)context.ContextData["Foo"];

      // set new data
      context.ContextData["Bar"] = new Bar();

      return next.Invoke(context);
  })
  .Create();
```

Resolver Example:

```csharp
public Task<string> MyResolver([State("Foo")]Foo foo)
{
  ...
}
```

# Scoped Context Data

The scoped context data is a immutable dictionary and is only available through the `IResolverContext`.

Scoped state allows us to aggregate state for our child field resolvers.

Let's say we have the following query:

```graphql
{
  a {
    b {
      c
    }
  }
  d {
    e {
      f
    }
  }
}
```

If the `a`-resolver would put something on the scoped context its sub-tree could access that data. This means, `b` and `c` could access the data but `d`, `e` and `f` would _NOT_ be able to access the data, their dictionary is still unchanged.

```csharp
context.ScopedContextData = context.ScopedContextData.SetItem("foo", "bar");
```
