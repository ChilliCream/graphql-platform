---
id: migration
title: "Migrating from 0.6.x"
---

## Extended Scalars

We have added the requirement to register any Extended Scalars you may want to use. If you do not intend to override any of the Extended Scalars and would like to just use the built-in implementations, you can register all at once as shown below:

```cs
var schema = Schema.Create(c =>
{
    // Register all 6 extended scalar types
    c.RegisterExtendedScalarTypes();
});
```

## DataLoaders

We have removed the need to register _DataLoader_ before using them. We also separated execution options and services from the type system. So, with the new release you have to remove the `RegisterDataLoader` calls from your schema. Moreover, the _DataLoader_ `Fetch` method is now called `FetchAsync` and now has a `CancellationToken` parameter. The main reason to change this method was to provide the ability to abort batch operations through the use of `CancellationToken`.

Here are the steps that you have to do in order to migrate:

- Remove all `RegisterDataLoader` calls from the schema configuration.
- Add `services.AddDataLoaderRegistry();` to you dependency injection configuration.
- Update your fetch methods in your _DataLoader_.
