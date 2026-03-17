---
title: "Overview"
---

A GraphQL schema defines the contract between your server and its clients. It declares what data clients can query, what mutations they can perform, and what events they can subscribe to. In Hot Chocolate, you build the schema from C# code, and the framework translates your types and methods into a GraphQL schema at build time.

This page explains how that translation works and how to choose between the two supported approaches. The sub-pages in this section cover each schema element in detail.

# How Hot Chocolate Maps C# to GraphQL

Hot Chocolate inspects your C# types and produces GraphQL equivalents. Understanding the mapping rules makes the rest of the documentation predictable.

**Types.** A C# class or record becomes a GraphQL object type. Each public property or method becomes a field. The GraphQL type name matches the C# type name.

```csharp
// Types/Book.cs
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public Author Author { get; set; }
}
```

```graphql
type Book {
  id: Int!
  title: String!
  author: Author!
}
```

**Nullability.** Non-nullable C# types (like `int` and `string` with nullable reference types enabled) become non-null GraphQL types (`Int!`, `String!`). Nullable C# types (`int?`, `string?`) become nullable GraphQL types.

**Naming.** Method names are converted to camelCase. The `Get` prefix and `Async` suffix are stripped. `GetBookByIdAsync` becomes `bookById`.

**Scalars.** C# primitives map to GraphQL scalars: `string` to `String`, `int` to `Int`, `float`/`double` to `Float`, `bool` to `Boolean`. `DateTime`, `Guid`, `Uri`, and other .NET types have built-in scalar mappings. See [Scalars](/docs/hotchocolate/v16/defining-a-schema/scalars) for the full list.

**Collections.** `List<T>`, `IEnumerable<T>`, arrays, and other collection types become GraphQL list types (`[T]`).

# Two Approaches

Hot Chocolate supports two approaches to defining a schema. Both produce the same GraphQL output. They differ in how much control you want over the mapping.

## Implementation-first (recommended)

You write standard C# classes and decorate them with attributes. A source generator handles the rest. This is the approach used throughout this documentation.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

The source generator produces a `productById` field on the Query type, infers the return type as `Product`, and registers everything at build time. You do not write type descriptors or SDL.

**When to use:** Most of the time. It keeps your schema close to your domain code and lets the tooling handle the translation. The source generator catches errors at compile time rather than at startup.

## Code-first

You create classes that inherit from `ObjectType<T>` and configure each field explicitly through a descriptor API. This decouples the GraphQL schema shape from your C# model.

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(p => p.Id)
            .Type<NonNullType<IdType>>();

        descriptor
            .Field(p => p.Name)
            .Type<NonNullType<StringType>>();
    }
}
```

**When to use:** When you need the GraphQL schema to differ significantly from your C# model, when you are building shared infrastructure that generates schemas programmatically, or when you need access to descriptor APIs that do not have attribute equivalents.

Both approaches can coexist in the same project. You can use implementation-first for most types and switch to code-first for specific cases.

# Schema Elements

A GraphQL schema is built from a small set of elements. Each has its own page with full examples in both approaches.

## Root Types

Every schema has up to three root types that serve as entry points for operations.

| Root Type    | Purpose                             | Attribute            | Page                                                                    |
| ------------ | ----------------------------------- | -------------------- | ----------------------------------------------------------------------- |
| Query        | Read data. Runs fields in parallel. | `[QueryType]`        | [Queries](/docs/hotchocolate/v16/defining-a-schema/queries)             |
| Mutation     | Write data. Runs fields serially.   | `[MutationType]`     | [Mutations](/docs/hotchocolate/v16/defining-a-schema/mutations)         |
| Subscription | Stream real-time events.            | `[SubscriptionType]` | [Subscriptions](/docs/hotchocolate/v16/defining-a-schema/subscriptions) |

Only the Query type is required. Add Mutation and Subscription types as needed.

## Output Types

These types describe the shape of data returned to clients.

| Type        | C# Mapping                                             | Page                                                                  |
| ----------- | ------------------------------------------------------ | --------------------------------------------------------------------- |
| Object type | Class or record with public properties/methods         | [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types) |
| Interface   | C# interface or abstract class                         | [Interfaces](/docs/hotchocolate/v16/defining-a-schema/interfaces)     |
| Union       | Multiple object types grouped together                 | [Unions](/docs/hotchocolate/v16/defining-a-schema/unions)             |
| Enum        | C# enum                                                | [Enums](/docs/hotchocolate/v16/defining-a-schema/enums)               |
| Scalar      | Primitive or custom type (String, Int, DateTime, etc.) | [Scalars](/docs/hotchocolate/v16/defining-a-schema/scalars)           |

## Input Types

These types describe the shape of data sent by clients.

| Type              | Purpose                                                | Page                                                                              |
| ----------------- | ------------------------------------------------------ | --------------------------------------------------------------------------------- |
| Arguments         | Parameters on a field                                  | [Arguments](/docs/hotchocolate/v16/defining-a-schema/arguments)                   |
| Input object type | Complex argument payloads (commonly used in mutations) | [Input Object Types](/docs/hotchocolate/v16/defining-a-schema/input-object-types) |

## Type Modifiers

Modifiers wrap other types to change their nullability or turn them into lists.

| Modifier | GraphQL      | C#                          | Page                                                          |
| -------- | ------------ | --------------------------- | ------------------------------------------------------------- |
| Non-null | `String!`    | `string` (with NRT enabled) | [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null) |
| Nullable | `String`     | `string?`                   | [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null) |
| List     | `[String!]!` | `List<string>`              | [Lists](/docs/hotchocolate/v16/defining-a-schema/lists)       |

## Organizing Your Schema

| Topic                        | Purpose                                                      | Page                                                                        |
| ---------------------------- | ------------------------------------------------------------ | --------------------------------------------------------------------------- |
| Extending types              | Split a type definition across multiple classes              | [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types) |
| Directives                   | Add metadata or alter runtime behavior                       | [Directives](/docs/hotchocolate/v16/defining-a-schema/directives)           |
| Global object identification | Stable IDs and the `node` field for Relay-compatible clients | [Relay](/docs/hotchocolate/v16/defining-a-schema/relay)                     |

# Next Steps

- **"I want to define my first query."** Start with [Queries](/docs/hotchocolate/v16/defining-a-schema/queries). It covers the `[QueryType]` attribute, naming conventions, and how to register multiple query classes.

- **"I want to fetch data from a database."** See [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers) for how fields load data, and [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader) for batching.

- **"I want to understand how my C# types become GraphQL types."** Read [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types) for a detailed walkthrough of the mapping rules.
