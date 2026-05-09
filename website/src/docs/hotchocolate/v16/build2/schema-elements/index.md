---
title: "Schema Elements"
---

A GraphQL schema is the contract your clients query against. It names the operations clients can run, the fields they can select, the arguments they can pass, and the shapes they receive in response.

Hot Chocolate v16 lets you author that contract from C# and inspect the generated GraphQL SDL. Use this page as a map. It explains the main schema element categories, shows how a small C# model becomes SDL, and points you to the detailed page for each modeling task.

# Start with the contract clients see

Clients do not call your C# methods directly. They send GraphQL operations against the schema. A compact schema can already show most schema element categories:

```graphql
type Query {
  bookById(id: ID!): Book
}

type Mutation {
  createBook(input: CreateBookInput!): Book!
}

type Book {
  id: ID!
  title: String!
  authors: [Author!]!
}

type Author {
  id: ID!
  name: String!
}

input CreateBookInput {
  title: String!
  authorIds: [ID!]!
}
```

| SDL part                    | Schema element      | What it means                     | Learn more                                 |
| --------------------------- | ------------------- | --------------------------------- | ------------------------------------------ |
| `Query`                     | Operation root type | Entry point for read operations.  | [Queries](./operations-queries)            |
| `Mutation`                  | Operation root type | Entry point for write operations. | [Mutations](./operations-mutations)        |
| `bookById` and `createBook` | Fields              | Selectable members on a type.     | [Object Types](./object-types)             |
| `id` and `input`            | Arguments           | Values supplied to a field.       | [Arguments](./arguments)                   |
| `Book` and `Author`         | Object types        | Returned data shapes.             | [Object Types](./object-types)             |
| `CreateBookInput`           | Input object type   | Structured data sent by a client. | [Input Object Types](./input-object-types) |
| `ID` and `String`           | Scalars             | Leaf values with no subfields.    | [Scalars](./scalars)                       |
| `!` and `[]`                | Type modifiers      | Non-null and list wrappers.       | [Lists and Non-Null](./lists-and-non-null) |

When you are unsure where to start, point at the SDL first. If the element is selected by a client, it is a field. If the client passes it into a field, it is an argument or input field. If it wraps another type with `!` or `[]`, it is a type modifier.

# See how C# produces the same contract

The implementation-first approach is the recommended default in the v16 schema docs. You write C# types, add Hot Chocolate attributes where the schema needs guidance, and the source generator contributes the schema setup.

```csharp
#nullable enable

using HotChocolate.Types;

public sealed class Book
{
    [ID]
    public int Id { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyList<Author> Authors { get; init; }
}

public sealed class Author
{
    [ID]
    public int Id { get; init; }

    public required string Name { get; init; }
}

public sealed class CreateBookInput
{
    public required string Title { get; init; }

    [ID<Author>]
    public required IReadOnlyList<int> AuthorIds { get; init; }
}

public interface IBookService
{
    Task<Book?> GetBookByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Book> CreateBookAsync(
        CreateBookInput input,
        CancellationToken cancellationToken);
}

[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookByIdAsync(
        [ID] int id,
        IBookService books,
        CancellationToken cancellationToken)
        => books.GetBookByIdAsync(id, cancellationToken);
}

[MutationType]
public static partial class BookMutations
{
    public static Task<Book> CreateBookAsync(
        CreateBookInput input,
        IBookService books,
        CancellationToken cancellationToken)
        => books.CreateBookAsync(input, cancellationToken);
}
```

The generated schema contains the client-visible contract:

```graphql
type Query {
  bookById(id: ID!): Book
}

type Mutation {
  createBook(input: CreateBookInput!): Book!
}

type Book {
  id: ID!
  title: String!
  authors: [Author!]!
}

type Author {
  id: ID!
  name: String!
}

input CreateBookInput {
  title: String!
  authorIds: [Int!]!
}
```

The important mappings are:

| C# authoring element                           | GraphQL schema element | Notes                                                                                            |
| ---------------------------------------------- | ---------------------- | ------------------------------------------------------------------------------------------------ |
| `[QueryType]` method                           | Field on `Query`       | `GetBookByIdAsync` becomes `bookById`.                                                           |
| `[MutationType]` method                        | Field on `Mutation`    | Top-level mutation fields execute serially.                                                      |
| Method parameter                               | Argument               | Services and framework parameters, such as `CancellationToken`, do not become GraphQL arguments. |
| Class or record returned from a resolver       | Object type            | Public properties and methods can become fields.                                                 |
| Class or record used as a resolver parameter   | Input object type      | Input and output types are separate GraphQL concepts.                                            |
| `string` with nullable reference types enabled | `String!`              | Nullable annotations influence GraphQL nullability.                                              |
| `IReadOnlyList<T>`                             | `[T]` list wrapper     | The list and item nullability are modeled separately.                                            |
| `[ID]`                                         | `ID` scalar behavior   | Use Relay guidance when you need global object identification.                                   |

Generated SDL is the artifact clients, schema registries, IDE tooling, and tests consume. Keep checking it as you model your schema.

# Choose an authoring style

Hot Chocolate v16 supports two C# authoring styles. Both produce GraphQL SDL.

## Implementation-first: recommended default

Use implementation-first when you want your schema to follow ordinary C# types with low ceremony. This is the primary path used throughout the v16 schema docs.

Common building blocks include:

- `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` for operation root fields.
- `partial` source-generator classes for annotated root type classes.
- Attributes such as `[GraphQLName]`, `[GraphQLDescription]`, `[GraphQLIgnore]`, `[ID]`, `[Node]`, `[InterfaceType]`, `[UnionType]`, and `[OneOf]` when the inferred schema needs guidance.
- Generated `AddTypes` setup for annotated implementation-first types.

Use this style for application schemas, compile-time feedback, and teams that prefer to keep the GraphQL contract close to the C# implementation.

## Code-first: descriptor control

Use code-first when you need descriptor-level control, when the GraphQL shape differs from the C# shape, or when you build reusable schema infrastructure.

```csharp
using HotChocolate.Types;

public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field(t => t.Title)
            .Type<NonNullType<StringType>>()
            .Description("The title displayed to readers.");
    }
}
```

Code-first types are usually registered explicitly:

```csharp
builder
    .AddGraphQL()
    .AddType<BookType>();
```

You can mix both styles. Many projects use implementation-first for most schema elements and code-first for selected types that need descriptor APIs.

## About SDL-first authoring

This section focuses on Hot Chocolate's C# authoring models. Use the generated SDL to inspect the contract clients see. If you want to author a schema from SDL, check the broader v16 documentation for a dedicated page before assuming it is covered by this schema-elements section.

# Use the schema element map

The following map helps you choose the next detailed page without learning every rule on this page.

## Operation root types

| Element      | Purpose                                                                                | v16 authoring cue                              | Learn more                                  |
| ------------ | -------------------------------------------------------------------------------------- | ---------------------------------------------- | ------------------------------------------- |
| Query        | Read entry point. Query fields should be side-effect-free and may execute in parallel. | `[QueryType]` or `AddQueryType`.               | [Queries](./operations-queries)             |
| Mutation     | Write entry point. Top-level mutation fields execute serially.                         | `[MutationType]` or `AddMutationType`.         | [Mutations](./operations-mutations)         |
| Subscription | Event stream entry point.                                                              | `[SubscriptionType]` or `AddSubscriptionType`. | [Subscriptions](./operations-subscriptions) |

GraphQL has one root type of each kind. Your C# code can split root fields across multiple semantic classes, and Hot Chocolate merges them into the final root type.

## Fields and arguments

| Element  | Purpose                                                                                                             | Learn more                                                                                           |
| -------- | ------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Field    | A selectable member on a root type or object type. Root fields start operations. Nested fields shape returned data. | [Queries](./operations-queries), [Mutations](./operations-mutations), [Object Types](./object-types) |
| Argument | A value supplied to a field. Common uses include lookup IDs, filters, paging arguments, and mutation payloads.      | [Arguments](./arguments)                                                                             |

Not every C# method parameter becomes an argument. Service parameters, parent values, cancellation tokens, and resolver context parameters are resolver concerns.

## Output types

| Element     | Use it for                                                                                                  | Learn more                     |
| ----------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------ |
| Object type | Normal returned data shapes, such as `Book` or `Author`.                                                    | [Object Types](./object-types) |
| Interface   | Polymorphic output types that share fields.                                                                 | [Interfaces](./interfaces)     |
| Union       | Polymorphic output types that do not need shared fields.                                                    | [Unions](./unions)             |
| Enum        | A closed set of symbolic values in input or output positions.                                               | [Enums](./enums)               |
| Scalar      | Leaf values such as `String`, `Int`, `Boolean`, `ID`, `UUID`, `URI`, `DateTime`, `Any`, and custom scalars. | [Scalars](./scalars)           |

Output types describe what clients can select after a field resolves.

## Input types

| Element           | Use it for                                                                  | Learn more                                 |
| ----------------- | --------------------------------------------------------------------------- | ------------------------------------------ |
| Argument          | A scalar, enum, ID, list, or input object supplied to a field.              | [Arguments](./arguments)                   |
| Input object type | Structured payloads for mutations, filters, and other complex field inputs. | [Input Object Types](./input-object-types) |

GraphQL separates input and output type systems. Input objects can use defaults, `Optional<T>`, and `@oneOf`, but those rules belong on the input pages.

## Type modifiers

| Modifier                | Example    | What it says                                            | Learn more                                 |
| ----------------------- | ---------- | ------------------------------------------------------- | ------------------------------------------ |
| Non-null                | `String!`  | The field or argument must not be null in the contract. | [Lists and Non-Null](./lists-and-non-null) |
| List                    | `[Book]`   | The value is a collection.                              | [Lists and Non-Null](./lists-and-non-null) |
| List plus item non-null | `[Book!]!` | The list is required, and every item is required.       | [Lists and Non-Null](./lists-and-non-null) |

Input optionality, default values, and output nullability are related, but they are not the same rule. Follow the type modifier and input object pages when the difference matters.

## Contract metadata and lifecycle

| Topic                     | Use it for                                                                          | Learn more                                         |
| ------------------------- | ----------------------------------------------------------------------------------- | -------------------------------------------------- |
| Descriptions              | Add schema documentation from XML comments, `[GraphQLDescription]`, or descriptors. | [Documentation Comments](./documentation-comments) |
| Directives                | Add schema metadata or executable behavior, depending on the directive kind.        | [Directives](./directives)                         |
| Deprecation and evolution | Communicate lifecycle changes and plan compatible schema updates.                   | [Versioning](../../building-a-schema/versioning)   |

## Schema organization and advanced modeling

| Topic           | Use it for                                                                                               | Learn more                             |
| --------------- | -------------------------------------------------------------------------------------------------------- | -------------------------------------- |
| Type extensions | Split large object or root type definitions across classes. Extensions are merged into the final schema. | [Extending Types](./extending-types)   |
| Relay helpers   | Use stable IDs, global object identification, `node`, `nodes`, `[ID]`, `[Node]`, and `[NodeResolver]`.   | [Relay](../../building-a-schema/relay) |
| Dynamic schemas | Generate schema elements from CMS, multi-tenant, or configuration-driven metadata with `ITypeModule`.    | [Dynamic Schemas](./dynamic-schemas)   |

Use type extensions for normal static modularity. Reach for dynamic schemas only when the schema itself must change from external metadata or runtime configuration.

# Choose your next page by task

If you are new to Hot Chocolate schema modeling, start with [Queries](./operations-queries), then [Object Types](./object-types), [Arguments](./arguments), and [Lists and Non-Null](./lists-and-non-null).

| Your task                                    | Read next                                                                                                            |
| -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Define the first read field.                 | [Queries](./operations-queries), then [Object Types](./object-types), then [Arguments](./arguments).                 |
| Add a write operation.                       | [Mutations](./operations-mutations), then [Input Object Types](./input-object-types), then [Arguments](./arguments). |
| Stream events to clients.                    | [Subscriptions](./operations-subscriptions).                                                                         |
| Model returned data.                         | [Object Types](./object-types), then [Interfaces](./interfaces) or [Unions](./unions) for polymorphism.              |
| Accept complex input.                        | [Input Object Types](./input-object-types), then [Lists and Non-Null](./lists-and-non-null).                         |
| Control names, descriptions, or visibility.  | [Object Types](./object-types), [Documentation Comments](./documentation-comments), and [Directives](./directives).  |
| Make IDs Relay-compatible.                   | [Relay](../../building-a-schema/relay).                                                                              |
| Split a growing schema across files.         | [Extending Types](./extending-types).                                                                                |
| Generate schema elements from configuration. | [Dynamic Schemas](./dynamic-schemas).                                                                                |
| Plan safe schema changes.                    | [Versioning](../../building-a-schema/versioning) and schema evolution guidance.                                      |

# Troubleshoot common first problems

## My field or type does not appear in the schema

Check that the field is reachable from a registered schema type:

- For implementation-first root fields, verify `[QueryType]`, `[MutationType]`, or `[SubscriptionType]` is present and the class is `partial`.
- For implementation-first projects, verify the generated `AddTypes` setup is part of schema configuration.
- For code-first descriptor types, verify registration with `AddType`, `AddQueryType`, `AddMutationType`, `AddSubscriptionType`, or `AddTypeExtension` as appropriate.
- Check member visibility, `[GraphQLIgnore]`, explicit binding settings, and type extensions.

## The GraphQL name is not what I expected

Hot Chocolate applies naming conventions. Method and property names are camelCased. Resolver methods strip a leading `Get` and a trailing `Async`, so `GetBookByIdAsync` becomes `bookById`.

Use `[GraphQLName]` or descriptor APIs when the schema needs a different name. Then check the generated SDL, because the SDL is the contract clients use.

## A field or argument is nullable or non-null unexpectedly

Check nullable reference types first. With nullable reference types enabled, `string` maps to `String!`, while `string?` maps to `String`. Value types, `T?`, list item nullability, default values, and `Optional<T>` can also affect the final contract.

Use [Lists and Non-Null](./lists-and-non-null), [Arguments](./arguments), and [Input Object Types](./input-object-types) for the detailed rules.

## I tried to use the same shape for input and output

GraphQL has separate input and output type systems. An object type can expose fields with arguments and resolver behavior. An input object type represents data supplied by a client. Keep the C# shapes separate when the GraphQL concepts differ, even if some property names match.

## I need schema changes at runtime

Most applications do not need runtime schema changes. Split static schemas with root type classes and type extensions first. If the set of schema types must come from configuration, tenant metadata, or a CMS, use [Dynamic Schemas](./dynamic-schemas) and `ITypeModule`.

# Next steps

- Build a read entry point with [Queries](./operations-queries).
- Learn how returned shapes are inferred and configured in [Object Types](./object-types).
- Add field inputs with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Tighten the contract with [Lists and Non-Null](./lists-and-non-null).
- Move into runtime behavior with [Resolvers](../../resolvers-and-data/resolvers) and [DataLoader](../../resolvers-and-data/dataloader) after the schema shape is clear.
