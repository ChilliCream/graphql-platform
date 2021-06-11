---
title: Migrate from Hot Chocolate GraphQL server 11 to 12
---

This guide will walk you through the manual migration steps to get you Hot Chocolate GraphQL server to version 12.

# Scalars

We changed some defaults around scalars. These new defaults can break your existing schema but are in general better for new-comers and align better with the overall GraphQL ecosystem. You can naturally opt-out of these new defaults in order to preserve the integrity of your current schema.

## UUID

We changed the name of the UUID scalar from `Uuid` to `UUID`. In order to keep the old name do register the type manually like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType("Uuid"));
```

Further, we changed the default serialization of UUID values from format `N` (`nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn`) to format `D` (`nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn`). While the format `N` saved a few characters of payload new users in general had often issues with that format and other tooling. With the new default format new users will have an easer way to use this scalars with non ChilliCream tooling.

In order to preserve the the old format you can directly provide the format in the scalar.

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType(defaultFormat: 'N'));
```

In order to fully preserve 11 behavior do:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType("Uuid", defaultFormat: 'N'));
```

## URL

We changed the name of the URL scalar from `Url` to `URL`. In order to keep the old name do register the type manually like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UrlType("Url"));
```