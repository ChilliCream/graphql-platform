![GreenDonut](https://chillicream.com/img/projects/greendonut-banner.svg)

[![GitHub release](https://img.shields.io/github/release/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/GreenDonut.svg)](https://www.nuget.org/packages/GreenDonut/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/blob/master/LICENSE)

---

**Green Donut** is a port of _facebook's_ _DataLoader_ utility, written in C# for .NET Core and .NET
Framework.

> DataLoader is a generic utility to be used as part of your application's data fetching layer to
> provide a consistent API over various backends and reduce requests to those backends via batching
> and caching. -- facebook

_DataLoader_ are perfect in various client-side and server-side scenarios. Although, they are
usually know for solving the `N+1` problem in _GraphQL_ _APIs_. _DataLoader_ decouple any kind of
request in a simplified way to a backend resource like a database or a web service to reduce the
overall traffic to those resources by using two common techniques in computer science namely
batching and caching. With batching we decrease the amount of requests to a backend resource by
grouping single requests into one batch request. Whereas with caching we avoid requesting a backend
resource at all.

## Getting Started

First things first, install the package via _NuGet_.

For _.NET Core_ we use the `dotnet` _CLI_, which is perhaps the preferred way doing this.

```powershell
dotnet add package GreenDonut
```

And for _.NET Framework_ we still use the following line.

```powershell
Install-Package GreenDonut
```

People who prefer a UI to install packages might wanne use the _NuGet Package Manager_, which is
provided by _Visual Studio_.

After we have installed the package, we should probably start using it, right. We really tried to
keep the _API_ of _DataLoader_ congruent to the
[original facebook implementation which is written in JavaScript](https://github.com/facebook/dataloader),
but without making the experience for us _.NET_ developers weird.

### Example

The simplest way to get started is to create an instance of the default _DataLoader_ implementation,
which might be the right choice if you need just one type of _DataLoader_. However, if you need a
bunch of individual _DataLoader_ and/or using _DI_, which is an abbreviation for
_Dependency Injection_, you might wanne also take a look at the _Custom DataLoader_ section.

#### Create a new instance

Creating a new instance is easy as you will see in the following example. The tricky part here is to
implement our data fetching logic - here shown as `FetchUsers` - which depends on our backend
resource. Once we have done that, we just pass our fetch function into the _DataLoader_ constructor.
That's actually everything so far.

```csharp
var userLoader = new DataLoader<string, User>(FetchUsers);
```

In order to change the default behavior of a `DataLoader`, we have to create a new instance of
`DataLoaderOptions` and pass it right into the `DataLoader` constructor. Lets see how that looks
like.

```csharp
var options = new DataLoaderOptions<string>
{
    SlidingExpiration = TimeSpan.FromHours(1)
};
var userLoader = new DataLoader<string, User>(keys => FetchUsers(keys), options);
```

So, what we see here is that we have changed the `SlidingExpiration` from its default value, which
is `0` to `1 hour`. `0` means the cache entries will live forever in the cache as long as the
maximum cache size does not exceed. Whereas `1 hour` means a single cache entry will stay in the
cache as long as the entry gets touched within one hour. This is an additional feature that does not
exist in the original _facebook_ implementation.

#### Fetching data

Fetching data consists of two parts. First part is declaring your need in one or more data items by
providing one or more keys.

```csharp
await userLoader.LoadAsync("Foo", "Bar", "Baz");
```

The second part is dispatching our requested data items. There are two options. First option is
_manual dispatching_ the default behavior as of version `2.0.0`. As the name says,
_manual dispatching_ means we have to trigger the dispatching process manually; otherwise no data is
being fetched. This is actually an **important difference** to _facebook's_ original implementation,
which is written in _JavaScript_. _Facebook's_ implementation is using a trick in _NodeJs_ to
dispatch automatically. If you're interested how that works, click
[here](https://stackoverflow.com/questions/19822668/what-exactly-is-a-node-js-event-loop-tick/19823583#19823583)
to learn more about that. But now lets see how we trigger the dispatching process manually.

```csharp
await userLoader.DispatchAsync();
```

The second option is, we enable _auto dispatching_ which dispatches permanently in the background.
This process starts immediately after creating a new instance of the _DataLoader_. Lets see how
that looks like.

```csharp
var options = new DataLoaderOptions<string>
{
    AutoDispatching = true
};
var userLoader = new DataLoader<string, User>(FetchUsers, options);
```

In this case we wouldn't need to call `DispatchAsync` at all.

### Custom DataLoader

A custom _DataLoader_ is especially useful in _DI_ scenarios.

```csharp
public interface IUserDataLoader
    : IDataLoader<string, User>
{ }
```

Although the extra interface `IUserDataLoader` isn't necessarily required, we strongly recommend to
create an extra interface in this particular case because of several reasons. One reason is you
might have a handful of _DataLoader_ which implemanting a completely different data fetching logic,
but from the outside they look identic due to their identic type parameter list. That's why we
should always create a separate interface for each _DataLoader_. We just mentioned one reason here
because the explanation would go beyond the scope of custom _DataLoader_.

```csharp
public class UserDataLoader
    : DataLoaderBase<string, User>
    , IUserDataLoader
{
    protected override Task<IReadOnlyList<Result<User>>> Fetch(IReadOnlyList<string> keys)
    {
        // Here goes our data fetching logic
    }
}
```

### API

The _API_ shown here is simplified. Means we have omitted some information for brevity purpose like
type information, method overloads, return values and so on. If you're interested in those kind of
information - and we bet you're - then click [here](https://greendonut.io) for being transferred to
our documentation.

#### Events

| Name              | Description                                                                                                |
| ----------------- | ---------------------------------------------------------------------------------------------------------- |
| `RequestBuffered` | Raises when an incoming data request is added to the buffer. Will never be raised if batching is disabled. |

#### Properties

| Name               | Description                                                                                                                               |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `BufferedRequests` | Gets the current count of buffered data requests waiting for being dispatched as batches. Will always return `0` if batching is disabled. |
| `CachedValues`     | Gets the current count of cached values. Will always return `0` if caching is disabled.                                                   |

#### Methods

| Name              | Description                                                                                                                                                                                                                     |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Clear()`         | Empties the complete cache.                                                                                                                                                                                                     |
| `DispatchAsync()` | Dispatches one or more batch requests. In case of auto dispatching we just trigger an implicit dispatch which could mean to interrupt a wait delay. Whereas in a manual dispatch scenario it could mean to dispatch explicitly. |
| `LoadAsync(key)`  | Loads a single value by key. This call may return a cached value or enqueues this single request for bacthing if enabled.                                                                                                       |
| `LoadAsync(keys)` | Loads multiple values by keys. This call may return cached values and enqueues requests which were not cached for bacthing if enabled.                                                                                          |
| `Remove(key)`     | Removes a single entry from the cache.                                                                                                                                                                                          |
| `Set(key, value)` | Adds a new entry to the cache if not already exists.                                                                                                                                                                            |

### Best Practise

- Consider using a `DataLoader` instance per request if the results may differ due to user
  privileges for instance.

## Documentation

Click [here](https://greendonut.io) for more documentation.
