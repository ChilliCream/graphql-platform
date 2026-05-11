---
title: "Type System"
---

A GraphQL schema defines the contract that clients interact with. It specifies the available operations, selectable fields, accepted arguments, and the structure of responses.

Hot Chocolate enables you to define this contract in C# and review the resulting GraphQL SDL. This page serves as your guide: it introduces the main type system concepts, demonstrates how a C# model translates to SDL, and directs you to detailed pages for each modeling task.

# Begin with the client-facing contract

When working with GraphQL, everything starts with the schema. The schema defines the contract between your API and its clients, describing the available operations and the data that can be requested.

Clients interact with your API by sending GraphQL operations. They do not call your C# methods or classes directly. Instead, they rely on the schema to understand what is possible.

To introduce the main concepts of the GraphQL type system, consider the following example. This schema highlights the essential building blocks you will encounter:

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

This example presents queries, mutations, object types, input types, and scalar values. Each part of the schema plays a specific role in shaping how clients interact with your API. The table below breaks down these elements and explains their purpose within the type system.

| SDL part                    | Type system member  | What it means                     | Learn more                                 |
| --------------------------- | ------------------- | --------------------------------- | ------------------------------------------ |
| `Query`                     | Operation root type | Entry point for read operations.  | [Queries](./operations-queries)            |
| `Mutation`                  | Operation root type | Entry point for write operations. | [Mutations](./operations-mutations)        |
| `bookById` and `createBook` | Fields              | Selectable members on a type.     | [Object Types](./object-types)             |
| `id` and `input`            | Arguments           | Values supplied to a field.       | [Arguments](./arguments)                   |
| `Book` and `Author`         | Object types        | Returned data shapes.             | [Object Types](./object-types)             |
| `CreateBookInput`           | Input object type   | Structured data sent by a client. | [Input Object Types](./input-object-types) |
| `ID` and `String`           | Scalars             | Leaf values with no subfields.    | [Scalars](./scalars)                       |
| `!` and `[]`                | Type modifiers      | Non-null and list wrappers.       | [Lists and Non-Null](./lists-and-non-null) |

If you are unsure how to identify the parts of your schema, start by looking at the SDL. Elements that a client can select in a query are fields. Values that a client supplies to those fields are arguments or input fields. When you see `!` or `[]` wrapping another type, these are type modifiers that indicate non-nullability or lists.

For example, a client might send the following query and mutation:

```graphql
query {
  bookById(id: "1") {
    title
    authors {
      name
    }
  }
}

mutation {
  createBook(input: { title: "New Book", authorIds: ["2"] }) {
    id
    title
  }
}
```

In these examples, `bookById` and `createBook` are fields. The `id` and `input` values are arguments. The selections inside the curly braces, such as `title` and `authors`, are fields on the returned types.

# Select an authoring style

Hot Chocolate offers two C# authoring styles, both of which produce GraphQL SDL.

## Implementation-first

Implementation-first lets you define your GraphQL schema using standard C# types and attributes, keeping your contract close to your domain code with minimal ceremony. You get compile-time feedback, clear mapping between code and schema, and a streamlined workflow for most application schemas.

Common building blocks:

- `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` for operation root fields
- `partial` source-generator classes for annotated root type classes
- Attributes such as `[GraphQLName]`, `[GraphQLDescription]`, `[GraphQLIgnore]`, `[ID]`, `[Node]`, `[InterfaceType]`, `[UnionType]`, and `[OneOf]` when the inferred schema needs guidance
- Generated dependency injection modules for GraphQL types

## Code-first

Code-first lets you specify GraphQL types and their structure directly in C# using the Hot Chocolate type descriptor API. This approach is useful when your GraphQL schema needs to differ from your C# model, or when building reusable schema components.

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

Code-first types are typically registered explicitly:

```csharp
builder
    .AddGraphQL()
    .AddType<BookType>();
```

You can combine both styles. Many projects use implementation-first for most types and code-first for types that require descriptor APIs.

## About SDL-first authoring

This section focuses on Hot Chocolate's C# authoring models. Use the generated SDL to inspect the contract clients see. If you want to author a schema from SDL, refer to the broader documentation for a dedicated page, as this section does not cover SDL-first authoring.

# Use the type system map

The following map helps you select the next detailed page without needing to learn every rule here.

## Operation root types

| Element      | Purpose                                                                                | Authoring cue                                  | Learn more                                  |
| ------------ | -------------------------------------------------------------------------------------- | ---------------------------------------------- | ------------------------------------------- |
| Query        | Read entry point. Query fields should be side-effect-free and may execute in parallel. | `[QueryType]` or `AddQueryType`.               | [Queries](./operations-queries)             |
| Mutation     | Write entry point. Top-level mutation fields execute serially.                         | `[MutationType]` or `AddMutationType`.         | [Mutations](./operations-mutations)         |
| Subscription | Event stream entry point.                                                              | `[SubscriptionType]` or `AddSubscriptionType`. | [Subscriptions](./operations-subscriptions) |

GraphQL defines one root type of each kind. Your C# code can split root fields across multiple semantic classes, and Hot Chocolate merges them into the final root type.

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

GraphQL separates input and output type systems. Input objects can use defaults, `Optional<T>`, and `@oneOf`, but those rules are detailed on the input pages.

## Type modifiers

| Modifier                | Example    | What it says                                            | Learn more                                 |
| ----------------------- | ---------- | ------------------------------------------------------- | ------------------------------------------ |
| Non-null                | `String!`  | The field or argument must not be null in the contract. | [Lists and Non-Null](./lists-and-non-null) |
| List                    | `[Book]`   | The value is a collection.                              | [Lists and Non-Null](./lists-and-non-null) |
| List plus item non-null | `[Book!]!` | The list is required, and every item is required.       | [Lists and Non-Null](./lists-and-non-null) |

Input optionality, default values, and output nullability are related but not identical. Refer to the type modifier and input object pages when the distinction matters.

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
| Dynamic schemas | Generate types from CMS, multi-tenant, or configuration-driven metadata with `ITypeModule`.              | [Dynamic Schemas](./dynamic-schemas)   |

Use type extensions for static modularity. Choose dynamic schemas only when the schema must change based on external metadata or runtime configuration.

# Choose your next page by task

If you are new to Hot Chocolate schema modeling, begin with [Queries](./operations-queries), then [Object Types](./object-types), [Arguments](./arguments), and [Lists and Non-Null](./lists-and-non-null).

| Your task                                   | Read next                                                                                                            |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Define the first read field.                | [Queries](./operations-queries), then [Object Types](./object-types), then [Arguments](./arguments).                 |
| Add a write operation.                      | [Mutations](./operations-mutations), then [Input Object Types](./input-object-types), then [Arguments](./arguments). |
| Stream events to clients.                   | [Subscriptions](./operations-subscriptions).                                                                         |
| Model returned data.                        | [Object Types](./object-types), then [Interfaces](./interfaces) or [Unions](./unions) for polymorphism.              |
| Accept complex input.                       | [Input Object Types](./input-object-types), then [Lists and Non-Null](./lists-and-non-null).                         |
| Control names, descriptions, or visibility. | [Object Types](./object-types), [Documentation Comments](./documentation-comments), and [Directives](./directives).  |
| Make IDs Relay-compatible.                  | [Relay](../../building-a-schema/relay).                                                                              |
| Split a growing schema across files.        | [Extending Types](./extending-types).                                                                                |
| Generate types from configuration.          | [Dynamic Schemas](./dynamic-schemas).                                                                                |
| Plan safe schema changes.                   | [Versioning](../../building-a-schema/versioning) and schema evolution guidance.                                      |

# Troubleshooting common first problems

## My field or type does not appear in the schema

Ensure the field is reachable from a registered schema type:

- For implementation-first root fields, confirm `[QueryType]`, `[MutationType]`, or `[SubscriptionType]` is present and the class is `partial`.
- For implementation-first projects, verify the generated `AddTypes` setup is included in schema configuration.
- For code-first descriptor types, check registration with `AddType`, `AddQueryType`, `AddMutationType`, `AddSubscriptionType`, or `AddTypeExtension` as needed.
- Review member visibility, `[GraphQLIgnore]`, explicit binding settings, and type extensions.

## The GraphQL name is not what I expected

Hot Chocolate applies naming conventions. Method and property names are camelCased. Resolver methods remove a leading `Get` and a trailing `Async`, so `GetBookByIdAsync` becomes `bookById`.

Use `[GraphQLName]` or descriptor APIs if you need a different name. Always check the generated SDL, as this is the contract clients use.

## A field or argument is nullable or non-null unexpectedly

First, check nullable reference types. With nullable reference types enabled, `string` maps to `String!`, while `string?` maps to `String`. Value types, `T?`, list item nullability, default values, and `Optional<T>` can also affect the contract.

See [Lists and Non-Null](./lists-and-non-null), [Arguments](./arguments), and [Input Object Types](./input-object-types) for detailed rules.

## I tried to use the same shape for input and output

GraphQL separates input and output type systems. An object type can expose fields with arguments and resolver logic. An input object type represents data provided by a client. Keep C# shapes separate when the GraphQL concepts differ, even if some property names match.

## I need schema changes at runtime

Most applications do not require runtime schema changes. Start by splitting static schemas with root type classes and type extensions. If schema types must come from configuration, tenant metadata, or a CMS, use [Dynamic Schemas](./dynamic-schemas) and `ITypeModule`.

# Next steps

- Build a read entry point with [Queries](./operations-queries).
- Learn how returned shapes are inferred and configured in [Object Types](./object-types).
- Add field inputs with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Strengthen the contract with [Lists and Non-Null](./lists-and-non-null).
- Move to runtime behavior with [Resolvers](../../resolvers-and-data/resolvers) and [DataLoader](../../resolvers-and-data/dataloader) after the schema shape is clear.
