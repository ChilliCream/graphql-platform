---
title: Migrate from Hot Chocolate GraphQL server 12 to 13
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 13.

# RegisterDbContext

We changed the default [DbContextKind](/docs/hotchocolate/v13/integrations/entity-framework#dbcontextkind) from [DbContextKind.Synchronized](/docs/hotchocolate/v13/integrations/entity-framework#dbcontextkindsynchronized) to [DbContextKind.Resolver](/docs/hotchocolate/v13/integrations/entity-framework#dbcontextkindresolver). If the instance of your `DbContext` doesn't need to be the same for each executed resolver during a request, this should lead to a performance improvement.

To restore the v12 default behavior, pass the [DbContextKind.Synchronized](/docs/hotchocolate/v13/integrations/entity-framework#dbcontextkindsynchronized) to the `RegisterDbContext<T>` call.

**Before**

```csharp
services.AddGraphQLServer()
    .RegisterDbContext<DbContext>()
```

**After**

```csharp
services.AddGraphQLServer()
    .RegisterDbContext<DbContext>(DbContextKind.Synchronized)
```

> Note: Only add this, if your application requires it. You're better off with the new default otherwise.

# DataLoaderAttribute

Previously you might have annotated [DataLoaders](/docs/hotchocolate/v13/fetching-data/dataloader) in your resolver method signature with the `[DataLoader]` attribute. This attribute has been removed in v13 and can be safely removed from your code.

**Before**

```csharp
public async Task<User> GetUserByIdAsync(string id, [DataLoader] UserDataLoader loader)
    => await loader.LoadAsync(id);
```

**After**

```csharp
public async Task<User> GetUserByIdAsync(string id, UserDataLoader loader)
    => await loader.LoadAsync(id);
```
