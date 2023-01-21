---
title: Migrate from Hot Chocolate GraphQL server 12 to 13
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 13.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code to not compile or lead to unexpected behavior at runtime, if not addressed.

## RegisterDbContext

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

## DataLoaderAttribute

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

## ITopicEventReceiver / ITopicEventSender

Previously you could use any type (`T`) as the topic for an event stream. In this release we are requiring the topic to be a `string`.

**Before**

```csharp
ITopicEventReceiver.SubscribeAsync<TTopic, TMessage>(TTopic topic, 
    CancellationToken cancellationToken);

ITopicEventSender.SendAsync<TTopic, TMessage>(TTopic topic, TMessage message,
    CancellationToken cancellationToken)
```

**After**

```csharp
ITopicEventReceiver.SubscribeAsync<TMessage>(string topicName, 
    CancellationToken cancellationToken);

ITopicEventSender.SendAsync<TMessage>(string topicName, TMessage message,
    CancellationToken cancellationToken)
```

## @authorize on types

If you previously annotated a type with `@authorize`, either directly in the schema or via `[Authorize]` or `descriptor.Authorize()`, the authorization rule was copied to each field of this type. This meant that the authorization rule would be evaluated for each selected field beneath the annotated type in a request. This is not efficient, so we switched to evaluating the authorization rule **once** on the field that returns the annotated type instead.

Lets imagine you currently have the following GraphQL schema:

```graphql
type Query {
  user: User
}

type User @authorize {
  field1: String
  field2: Int
}
```

This is how the authorization rule would be evaluated previously and now:

**Before**

```graphql
{
  user {
    # The authorization rule is evaluated here, since this field is beneath
    # the `User` type, which is annotated with @authorize
    field1
    # The authorization rule is evaluated here, since this field is beneath
    # the `User` type, which is annotated with @authorize
    field2
  }
}
```

**After**

```graphql
{
  # The authorization rule is now evaluated here, since the `user` field
  # returns the `User` type, which is annotated with @authorize
  user {
    field1
    field2
  }
}
```

This behavior shouldn't be a breaking change regarding functionality, but the authorization error will now be raised at the field returning the type instead of the field beneath the type, which might break your test assertions.

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## ScopedServiceAttribute

In this release we are deprecating the `[ScopedService]` attribute and encourage you to use `RegisterDbContext<T>(DbContextKind.Pooled)` instead.

Checkout [this part of our Entity Framework documentation](/docs/hotchocolate/v13/integrations/entity-framework/#registerdbcontext) to learn how to register your `DbContext` with `DbContextKind.Pooled`.

Afterwards you just need to update your resolvers:

**Before**

```csharp
[UseDbContext]
public IQueryable<User> GetUsers([ScopedService] MyDbContext dbContext)
    => dbContext.Users;
```

**After**

```csharp
public IQueryable<User> GetUsers(MyDbContext dbContext)
    => dbContext.Users;
```

If you've been using `[ScopedService]` without a pooled `DbContext`, you can recreate its behavior by switching it out for `[LocalState("FullName")]` (where `FullName` is the [full name](https://learn.microsoft.com/dotnet/api/system.type.fullname) of the method argument type).

## SubscribeAndResolve

**Before**

```csharp
public class Subscription
{
    [SubscribeAndResolve]
    public ValueTask<ISourceStream<Book>> BookPublished(string author,
        [Service] ITopicEventReceiver receiver)
    {
        var topic = $"{author}_PublishedBook";

        return receiver.SubscribeAsync<string, Book>(topic);
    }
}
```

**After**

```csharp
public class Subscription
{
    public ValueTask<ISourceStream<Book>> SubscribeToPublishedBooks(
        string author, ITopicEventReceiver receiver)
    {
        var topic = $"{author}_PublishedBook";

        return receiver.SubscribeAsync<Book>(topic);
    }

    [Subscribe(With = nameof(SubscribeToPublishedBooks))]
    public Book BookPublished(string author, [EventMessage] Book book)
    {
        return book;
    }
}
```

## LocalValue / ScopedValue / GlobalValue

...
