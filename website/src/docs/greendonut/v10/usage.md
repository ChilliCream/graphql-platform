---
title: Usage
---

The simplest way to get started is to create an instance of the default
_DataLoader_ implementation, which might be the right choice if you need just
one type of _DataLoader_. However, if you need a bunch of individual
_DataLoader_ and/or using _DI_, which is an abbreviation for
_Dependency Injection_, you might want to also take a look at the
[Custom DataLoader](/docs/greendonut/v10/advanced-concepts) section.

# Create a new instance

Creating a new instance is easy as you will see in the following example. The
tricky part here is to implement our data fetching logic - here shown as
`FetchUsers` - which depends on our backend resource. Once we have done that, we
just pass our fetch function into the _DataLoader_ constructor. That's actually
everything so far.

```csharp
var userLoader = new DataLoader<string, User>(FetchUsers);
```

In order to change the default behavior of a `DataLoader`, we have to create a
new instance of `DataLoaderOptions` and pass it right into the `DataLoader`
constructor. Lets see how that looks like.

```csharp
var options = new DataLoaderOptions<string>
{
    SlidingExpiration = TimeSpan.FromHours(1)
};
var userLoader = new DataLoader<string, User>(keys => FetchUsers(keys), options);
```

So, what we see here is that we have changed the `SlidingExpiration` from its
default value, which is `0` to `1 hour`. `0` means the cache entries will live
forever in the cache as long as the maximum cache size does not exceed. Whereas
`1 hour` means a single cache entry will stay in the cache as long as the entry
gets touched within one hour. This is an additional feature that does not exist
in the original _facebook_ implementation.

# Fetching data

Fetching data consists of two parts. The first part is declaring your need in one or
more data items by providing one or more keys.

```csharp
await userLoader.LoadAsync("Foo", "Bar", "Baz");
```

The second part is dispatching our requested data items. There are two options.
The first option is _manual dispatching_ the default behavior as of version `2.0.0`.
As the name says, _manual dispatching_ means we have to trigger the dispatching
process manually; otherwise no data is being fetched. This is actually an
**important difference** to _facebook's_ original implementation, which is
written in _JavaScript_. _Facebook's_ implementation is using a trick in
_NodeJs_ to dispatch automatically. If you're interested how that works, click
[here](https://stackoverflow.com/questions/19822668/what-exactly-is-a-node-js-event-loop-tick/19823583#19823583)
to learn more about that. But now lets see how we trigger the dispatching
process manually.

```csharp
await userLoader.DispatchAsync();
```

The second option is, we enable _auto dispatching_ which dispatches permanently
in the background. This process starts immediately after creating a new instance
of the _DataLoader_. Lets see how that looks like.

```csharp
var options = new DataLoaderOptions<string>
{
    AutoDispatching = true
};
var userLoader = new DataLoader<string, User>(FetchUsers, options);
```

In this case we wouldn't need to call `DispatchAsync` at all.

> **Note**
>
> - Be careful when and how reusing `DataLoader` instances, because sometimes
>   users have different privileges. That implies perhaps a `DataLoader` on a
>   per request base. However, it really depends on your application logic and
>   the specific case you try to find a perfect solution for.
