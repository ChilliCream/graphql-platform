---
title: "Lists and Non-Null"
---

Lists (`[]`) and non-null (`!`) are GraphQL type modifiers. They define whether a value can be `null`, whether a value is a collection, and whether a collection can contain `null` items.

When deciding how to use lists and non-null, start by asking:

1. Can the field or argument itself be `null`?
2. If the value is a list, can the list contain `null` items?
3. Does your C# type, attribute, or descriptor configuration reflect this contract?

```graphql
type Query {
  products: [Product!]!
  productById(id: ID!): Product
}

type Product {
  name: String!
  tags: [String!]!
}
```

In this schema, `products` always returns a list where every item is a `Product`. `productById` may return `null` if the product does not exist. `Product.name` always has a value, and `Product.tags` always returns a list of non-null strings.

# Reading Wrapped Types: Inside Out

To interpret GraphQL wrappers, start with the named type and read outward. For example, `Product` is the item type, `[Product]` wraps it in a list, and `!` makes the preceding wrapper non-null.

| GraphQL type  | Value can be null | Items can be null | Use when                                                                  |
| ------------- | ----------------- | ----------------- | ------------------------------------------------------------------------- |
| `Product`     | Yes               | Not a list        | A missing value is valid, such as a lookup that finds no row.             |
| `Product!`    | No                | Not a list        | The field or argument must always have a value.                           |
| `[Product]`   | Yes               | Yes               | Both absence and `null` entries have meaning. This is uncommon.           |
| `[Product]!`  | No                | Yes               | The collection exists, but individual positions can be unavailable.       |
| `[Product!]`  | Yes               | No                | The whole collection may be absent, but entries are valid when present.   |
| `[Product!]!` | No                | No                | A typical collection field. Return an empty list when there are no items. |

For nested lists, apply the same logic one layer at a time:

```graphql
matrix: [[Int!]!]!
```

This means:

1. The outer list is non-null.
2. Each inner list is non-null.
3. Each integer item is non-null.

# Let C# Nullability Drive the SDL

Hot Chocolate infers list and non-null wrappers from your C# types. Enable nullable reference types so both the compiler and Hot Chocolate interpret the same nullability contract.

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

You can also enable nullable reference types in a single file with `#nullable enable`.

## Mapping Value Types

Value types are non-null unless you use `Nullable<T>` or `?`.

| C# type | GraphQL type | Meaning           |
| ------- | ------------ | ----------------- |
| `int`   | `Int!`       | Required integer. |
| `int?`  | `Int`        | Nullable integer. |
| `bool`  | `Boolean!`   | Required boolean. |
| `bool?` | `Boolean`    | Nullable boolean. |

Custom scalar bindings can change the scalar name, but the nullable value type rule still determines the wrapper.

## Mapping Reference Types with Nullable Reference Types Enabled

| C# type    | GraphQL type | Meaning                |
| ---------- | ------------ | ---------------------- |
| `string`   | `String!`    | Required string.       |
| `string?`  | `String`     | Nullable string.       |
| `Product`  | `Product!`   | Required object value. |
| `Product?` | `Product`    | Nullable object value. |

## Mapping Reference Types Without Nullable Reference Types

Without nullable reference type metadata, Hot Chocolate cannot distinguish `string` from `string?`.

| C# type   | GraphQL type | Meaning                           |
| --------- | ------------ | --------------------------------- |
| `string`  | `String`     | Nullable string by default.       |
| `Product` | `Product`    | Nullable object value by default. |

If your schema shows `String` where you expected `String!`, check your nullable reference type settings first.

# Modeling List and Item Nullability

Hot Chocolate exposes supported `IEnumerable<T>` shapes as GraphQL lists. The collection type determines the list wrapper, and the item type determines the item wrapper.

| C# type with nullable reference types enabled | GraphQL type  | Meaning                                         |
| --------------------------------------------- | ------------- | ----------------------------------------------- |
| `List<Product>`                               | `[Product!]!` | Non-null list of non-null products.             |
| `Product[]`                                   | `[Product!]!` | Non-null array of non-null products.            |
| `IEnumerable<Product>`                        | `[Product!]!` | Non-null enumerable of non-null products.       |
| `IReadOnlyList<Product>`                      | `[Product!]!` | Non-null read-only list of non-null products.   |
| `IQueryable<Product>`                         | `[Product!]!` | Non-null queryable of non-null products.        |
| `List<Product?>`                              | `[Product]!`  | Non-null list that can contain `null` products. |
| `List<Product>?`                              | `[Product!]`  | Nullable list of non-null products.             |
| `List<Product?>?`                             | `[Product]`   | Nullable list that can contain `null` products. |
| `string?[]?`                                  | `[String]`    | Nullable array that can contain `null` strings. |

The following example demonstrates common output fields:

```csharp
#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed class Product
{
    [ID]
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

[QueryType]
public static partial class ProductQueries
{
    public static IReadOnlyList<Product> GetProducts()
        => new[]
        {
            new Product
            {
                Id = 1,
                Name = "Trail Backpack",
                Description = "Weather resistant",
                Tags = new[] { "Outdoor", "Travel" }
            }
        };

    public static Product? GetProductById([ID] int id)
        => id == 1
            ? new Product
            {
                Id = 1,
                Name = "Trail Backpack",
                Tags = new[] { "Outdoor", "Travel" }
            }
            : null;

    public static IReadOnlyList<IReadOnlyList<int>> GetMatrix()
        => new List<IReadOnlyList<int>>
        {
            new[] { 1, 2 },
            new[] { 3, 4 }
        };
}
```

Expected SDL shape:

```graphql
type Query {
  products: [Product!]!
  productById(id: ID!): Product
  matrix: [[Int!]!]!
}

type Product {
  id: ID!
  name: String!
  description: String
  tags: [String!]!
}
```

Large production collections often use pagination. Pagination changes the field shape to a connection or collection segment, but you still need to apply list and non-null rules for fields, edge nodes, and arguments. See [Pagination](/docs/hotchocolate/v16/build/pagination) when a field can return many rows.

# Choosing the Right List Contract

Prefer the weakest contract that still accurately describes the data to clients. Non-null is a runtime guarantee and a commitment for schema evolution.

| Scenario                            | Prefer                                   | Why                                                                                   |
| ----------------------------------- | ---------------------------------------- | ------------------------------------------------------------------------------------- |
| Search results, related items, tags | `[T!]!`                                  | Clients can iterate without null checks. Return an empty list for no items.           |
| Lookup by id                        | `T`                                      | Not found is a valid result.                                                          |
| Ranked slots or sparse data         | `[T]!`                                   | The list exists, but some positions can be unavailable.                               |
| Optional relationship collection    | `[T!]`                                   | The server may not load or expose the collection, but entries are valid when present. |
| Unreliable upstream boundary        | Nullable field or nullable list boundary | Partial data can survive when one dependency fails.                                   |

Before publishing a field, review this checklist:

1. If absence and empty mean the same, expose a non-null list and return an empty collection.
2. If `null` entries have no useful meaning, make items non-null.
3. If some positions can be unavailable while the list remains useful, allow nullable items.
4. If an upstream system cannot guarantee a value, keep a nullable boundary where partial data should remain visible.
5. Be cautious with non-null on root fields and object relationships that may disappear or become permission-dependent.

# Overriding Wrappers When Inference Is Not Enough

Override the inferred type when the public GraphQL contract differs from the CLR shape. This is common with legacy models lacking nullable reference types, generated DTOs with inaccurate annotations, external storage models, or descriptor-based schema modules.

Explicit GraphQL type configuration defines the schema contract. Use it thoughtfully, and always check the generated SDL.

## Overriding with Attributes

Use `[GraphQLNonNullType]` to make the current wrapper non-null. Use `[GraphQLType<T>]` when you need to specify the exact GraphQL type, including list and item wrappers.

```csharp
#nullable enable

using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class Movie
{
    [GraphQLNonNullType]
    public string? Title { get; init; }

    [GraphQLType<NonNullType<ListType<NonNullType<StringType>>>>]
    public IReadOnlyList<string>? Genres { get; init; }
}
```

Expected SDL shape:

```graphql
type Movie {
  title: String!
  genres: [String!]!
}
```

`[GraphQLNonNullType]` can target properties, methods, and parameters. It can also rewrite deeper wrappers, but prefer `[GraphQLType<T>]` when specifying an exact type is clearer.

```csharp
[GraphQLNonNullType(false, false)]
public string?[]? Tags { get; init; }
```

Expected SDL for `tags`:

```graphql
tags: [String!]!
```

`[Required]` can also affect non-null inference. For GraphQL schema design, prefer C# nullable annotations or explicit GraphQL type configuration.

## Overriding with Descriptors

Use descriptor APIs when you keep schema configuration in type modules.

```csharp
using System.Collections.Generic;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class Movie
{
    public string? Title { get; init; }

    public IReadOnlyList<string>? Genres { get; init; }
}

public sealed class MovieType : ObjectType<Movie>
{
    protected override void Configure(IObjectTypeDescriptor<Movie> descriptor)
    {
        descriptor.Field(t => t.Title).Type<NonNullType<StringType>>();

        descriptor
            .Field(t => t.Genres)
            .Type<NonNullType<ListType<NonNullType<StringType>>>>();
    }
}
```

Expected SDL shape:

```graphql
type Movie {
  title: String!
  genres: [String!]!
}
```

Use this wrapper reference when configuring scalar fields and lists:

| Intended SDL | Descriptor wrapper                               | Attribute type wrapper                                             |
| ------------ | ------------------------------------------------ | ------------------------------------------------------------------ |
| `String!`    | `NonNullType<StringType>`                        | `[GraphQLNonNullType]` or `[GraphQLType<NonNullType<StringType>>]` |
| `[String]`   | `ListType<StringType>`                           | `[GraphQLType<ListType<StringType>>]`                              |
| `[String!]`  | `ListType<NonNullType<StringType>>`              | `[GraphQLType<ListType<NonNullType<StringType>>>]`                 |
| `[String!]!` | `NonNullType<ListType<NonNullType<StringType>>>` | `[GraphQLType<NonNullType<ListType<NonNullType<StringType>>>>]`    |

# Using Non-Null in Arguments

The same modifiers apply to field arguments. While input object field design is covered on the input pages, keep these argument rules in mind:

| GraphQL input type | Client may omit it | Client may send `null` | Notes                                                  |
| ------------------ | ------------------ | ---------------------- | ------------------------------------------------------ |
| `String`           | Yes                | Yes                    | Nullable and optional.                                 |
| `String!`          | No                 | No                     | Required when no default value exists.                 |
| `String = "all"`   | Yes                | Yes                    | Omitted uses the default.                              |
| `String! = "all"`  | Yes                | No                     | Omitted uses the default, explicit `null` is rejected. |

Example resolver arguments:

```csharp
#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace Catalog.Types;

[QueryType]
public static partial class ProductSearchQueries
{
    public static IReadOnlyList<Product> SearchProducts(
        string name,
        string? category,
        [DefaultValue(10)] int limit,
        IReadOnlyList<int> brandIds)
        => Array.Empty<Product>();
}
```

Expected SDL shape:

```graphql
type Query {
  searchProducts(
    name: String!
    category: String
    limit: Int! = 10
    brandIds: [Int!]!
  ): [Product!]!
}
```

A non-null argument without a default is required. A non-null argument with a default can be omitted, but a client still cannot send `null` for it.

For input object fields, omitted values, explicit `null`, defaults, and `Optional<T>`, see [Input Object Types](./input-object-types).

# Understanding the Runtime Cost of Non-Null

A non-null field is a runtime guarantee. If a resolver returns `null` for that field, GraphQL records an execution error and null propagates to the nearest nullable parent boundary. Keep a nullable boundary where partial data should remain visible.

For more on resolver behavior and error customization, see [Resolvers](/docs/hotchocolate/v16/build/resolvers) and [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).

# Troubleshooting Nullability Surprises

## My schema shows `String` instead of `String!`

Check the following:

- Nullable reference types are enabled in the project or file.
- Generated or partial code carries the expected annotations.
- No descriptor or attribute override changed the field type.
- An explicit `StringType` wrapper did not replace an inferred `NonNullType<StringType>` wrapper.

## My list is non-null, but its items are nullable

Look for nullable item annotations such as `List<string?>` or `string?[]`. Also check explicit wrappers. `.Type<ListType<StringType>>()` produces `[String]` unless you add `NonNullType<StringType>` for the item.

Use this when the list can be nullable but every item must be present:

```csharp
descriptor.Field(t => t.Tags).Type<ListType<NonNullType<StringType>>>();
```

Use this when both the list and every item must be present:

```csharp
descriptor
    .Field(t => t.Tags)
    .Type<NonNullType<ListType<NonNullType<StringType>>>>();
```

## My list items are non-null, but the list itself is nullable

Check for nullable collection annotations such as `List<string>?`, `IReadOnlyList<string>?`, or `string[]?`. In descriptor code, add an outer `NonNullType<T>` wrapper when the list itself must be non-null.

## A client cannot omit an argument or send `null`

Check the argument type and default value in the SDL:

- `String!` means the client must provide a non-null value.
- `String` accepts omission and explicit `null`.
- `String! = "all"` allows omission but rejects explicit `null`.

See [Arguments](./arguments) for resolver argument binding. Use [Input Object Types](./input-object-types) for structured input, defaults, and `Optional<T>`.

## Returning `null` removed more data than expected

This is null propagation from a non-null violation. Move a nullable boundary to the level where partial data should remain visible, or change the resolver so it always fulfills the non-null contract.

# Next Steps

- Model returned shapes with [Object Types](./object-types).
- Choose scalar leaf types with [Scalars](./scalars).
- Configure field inputs with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Use [Pagination](/docs/hotchocolate/v16/build/pagination), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for large collection fields.
- Plan compatible changes with [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning).
- Review runtime behavior in [Resolvers](/docs/hotchocolate/v16/build/resolvers) and [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).
