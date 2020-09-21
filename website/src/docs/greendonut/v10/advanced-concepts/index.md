---
title: Custom DataLoader
---

A custom _DataLoader_ is a class that derives from
`DataLoaderBase<TKey, TValue>` and implements `FetchData`. This is very useful,
not just in _DI_ (Dependency Injection) scenarios, because the data fetching
logic is defined inside the custom _DataLoader_ itself; therefore, must not be
provided repeatedly when creating new instances.

Creating a custom _DataLoader_ is not that difficult. First of all we _should_
create a dedicated marker interface for separation purposes.

```csharp
public interface IUserDataLoader
    : IDataLoader<string, User>
{ }
```

Although the extra interface `IUserDataLoader` isn't necessarily required, we
strongly recommend to create an extra interface in this particular case because
of several reasons. One reason is you might have a handful of _DataLoader_ which
implementing a completely different data fetching logic, but from the outside
they look identic due to their identic type parameter list. That's why we should
always create a separate interface for each _DataLoader_. We just mentioned one
reason here because the explanation would go beyond the scope of custom
_DataLoader_.

Last but not least, we have to create a new class deriving from
`DataLoaderBase<TKey, TValue>` and implementing our marker interface.

```csharp
public class UserDataLoader
    : DataLoaderBase<string, User>
    , IUserDataLoader
{
    protected override Task<IReadOnlyList<Result<User>>> Fetch(
        IReadOnlyList<string> keys)
    {
        // Here goes our data fetching logic
    }
}
```
