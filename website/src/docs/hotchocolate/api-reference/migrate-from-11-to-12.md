---
title: Migrate from Hot Chocolate GraphQL server 11 to 12
---

This guide will walk you through the manual migration steps to get your Hot Chocolate GraphQL server to version 12.

# Scalars

We changed some defaults around scalars. These new defaults can break your existing schema but are, in general, better for newcomers and align better with the overall GraphQL ecosystem. Of course, you can naturally opt out of these new defaults to preserve your current schema's integrity.

## UUID

We changed the name of the UUID scalar from `Uuid` to `UUID`. In order to keep the old name do register the type manually like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType("Uuid"));
```

Further, we changed the default serialization of UUID values from format `N` (`nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn`) to format `D` (`nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn`). While the format `N` saved a few payload characters, new users, in general, often had issues with that format and another tooling. New users will have an easier way to use these scalars with non ChilliCream tooling with the new default format.

To preserve the old format, you can directly provide the format in the scalar.

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