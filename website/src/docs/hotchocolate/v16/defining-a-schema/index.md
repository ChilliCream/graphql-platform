---
title: "Type System"
---

A GraphQL schema defines the contract that clients interact with. It specifies the available operations, selectable fields, accepted arguments, and the structure of responses.

Hot Chocolate enables you to define this contract in C# and review the resulting GraphQL SDL. This page serves as your guide: it introduces the main type system concepts, demonstrates how a C# model translates to SDL, and directs you to detailed pages for each modeling task.

# Understanding the Schema as the API Contract

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
| `Query`                     | Operation root type | Entry point for read operations.  | [Queries](./queries)                       |
| `Mutation`                  | Operation root type | Entry point for write operations. | [Mutations](./mutations)                   |
| `bookById` and `createBook` | Fields              | Selectable members on a type.     | [Object Types](./object-types)             |
| `id` and `input`            | Arguments           | Values supplied to a field.       | [Arguments](./arguments)                   |
| `Book` and `Author`         | Object types        | Returned data shapes.             | [Object Types](./object-types)             |
| `CreateBookInput`           | Input object type   | Structured data sent by a client. | [Input Object Types](./input-object-types) |
| `ID` and `String`           | Scalars             | Leaf values with no subfields.    | [Scalars](./scalars)                       |
| `!` and `[]`                | Type modifiers      | Non-null and list wrappers.       | [Lists and Non-Null](./lists)              |

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

# Schema Authoring Styles

Hot Chocolate offers two C# authoring styles, both of which produce GraphQL SDL.

## Implementation-First

The implementation-first approach allows you to define your GraphQL schema using standard C# types and attributes. This keeps your contract close to your domain code with minimal ceremony, provides compile-time feedback, and ensures a clear mapping between code and schema. This workflow is streamlined for most application schemas.

Common building blocks include:

- `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` for operation root fields
- `partial` source-generator classes for annotated root type classes
- Attributes such as `[GraphQLName]`, `[GraphQLDescription]`, `[GraphQLIgnore]`, `[ID]`, `[Node]`, `[InterfaceType]`, `[UnionType]`, and `[OneOf]` when the inferred schema needs guidance
- Generated dependency injection modules for GraphQL types

## Code-First

The code-first approach allows you to define GraphQL types and their structure directly in C# using the Hot Chocolate type descriptor API. This is useful when your GraphQL schema needs to differ from your C# model or when building reusable schema components.

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

# Navigating the Type System Map

The following map helps you select the next detailed page without needing to learn every rule here.

## Root Types

GraphQL defines a single root type for each operation kind. In C#, you can organize root fields across multiple semantic classes, and Hot Chocolate will merge them into the final root type.

| Element      | Purpose                                                                                | Authoring cue                                  | Learn more                       |
| ------------ | -------------------------------------------------------------------------------------- | ---------------------------------------------- | -------------------------------- |
| Query        | Read entry point. Query fields should be side-effect-free and may execute in parallel. | `[QueryType]` or `AddQueryType`.               | [Queries](./queries)             |
| Mutation     | Write entry point. Top-level mutation fields execute serially.                         | `[MutationType]` or `AddMutationType`.         | [Mutations](./mutations)         |
| Subscription | Event stream entry point.                                                              | `[SubscriptionType]` or `AddSubscriptionType`. | [Subscriptions](./subscriptions) |

## Output Types

Output types describe the shapes of data that clients can select after a field resolves.

| Element     | Use it for                                                                                                  | Learn more                     |
| ----------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------ |
| Object type | Normal returned data shapes, such as `Book` or `Author`.                                                    | [Object Types](./object-types) |
| Scalar      | Leaf values such as `String`, `Int`, `Boolean`, `ID`, `UUID`, `URI`, `DateTime`, `Any`, and custom scalars. | [Scalars](./scalars)           |
| Enum        | A closed set of symbolic values in input or output positions.                                               | [Enums](./enums)               |
| Interface   | Polymorphic output types that share fields.                                                                 | [Interfaces](./interfaces)     |
| Union       | Polymorphic output types that do not need shared fields.                                                    | [Unions](./unions)             |

## Fields and Arguments

Fields and arguments define how clients interact with your schema. Fields are selectable members on types, while arguments allow clients to supply values to those fields.

| Element  | Purpose                                                                                                             | Learn more                                                                     |
| -------- | ------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| Field    | A selectable member on a root type or object type. Root fields start operations. Nested fields shape returned data. | [Queries](./queries), [Mutations](./mutations), [Object Types](./object-types) |
| Argument | A value supplied to a field. Common uses include lookup IDs, filters, paging arguments, and mutation payloads.      | [Arguments](./arguments)                                                       |

Not every C# method parameter becomes an argument. Service parameters, parent values, cancellation tokens, and resolver context parameters are resolver concerns.

## Input Types

Input types define the shapes of data that clients can send to your API, such as arguments and input objects for mutations and queries.

| Element           | Use it for                                                                  | Learn more                                 |
| ----------------- | --------------------------------------------------------------------------- | ------------------------------------------ |
| Argument          | A scalar, enum, ID, list, or input object supplied to a field.              | [Arguments](./arguments)                   |
| Input object type | Structured payloads for mutations, filters, and other complex field inputs. | [Input Object Types](./input-object-types) |

GraphQL separates input and output type systems. Input objects can use defaults, `Optional<T>`, and `@oneOf`, but those rules are detailed on the input pages.

## Type Modifiers

Type modifiers such as non-null and list indicate whether a field or argument is required or can accept multiple values.

| Modifier                | Example    | What it says                                            | Learn more                    |
| ----------------------- | ---------- | ------------------------------------------------------- | ----------------------------- |
| Non-null                | `String!`  | The field or argument must not be null in the contract. | [Lists and Non-Null](./lists) |
| List                    | `[Book]`   | The value is a collection.                              | [Lists and Non-Null](./lists) |
| List plus item non-null | `[Book!]!` | The list is required, and every item is required.       | [Lists and Non-Null](./lists) |

Input optionality, default values, and output nullability are related but not identical. Refer to the type modifier and input object pages when the distinction matters.

## Schema Organization and Advanced Modeling

Organize your schema for maintainability and support advanced modeling scenarios using type extensions, Relay helpers, and dynamic schemas.

| Topic           | Use it for                                                                                               | Learn more                           |
| --------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| Type extensions | Split large object or root type definitions across classes. Extensions are merged into the final schema. | [Object Types](./object-types)       |
| Relay helpers   | Use stable IDs, global object identification, `node`, `nodes`, `[ID]`, `[Node]`, and `[NodeResolver]`.   | [Relay](./relay)                     |
| Dynamic schemas | Generate types from CMS, multi-tenant, or configuration-driven metadata with `ITypeModule`.              | [Dynamic Schemas](./dynamic-schemas) |

## Contract Metadata and Lifecycle

Metadata and lifecycle features help you document, annotate, and evolve your schema safely over time.

| Topic                     | Use it for                                                                          | Learn more                                |
| ------------------------- | ----------------------------------------------------------------------------------- | ----------------------------------------- |
| Descriptions              | Add schema documentation from XML comments, `[GraphQLDescription]`, or descriptors. | [Documentation Comments](./documentation) |
| Directives                | Add schema metadata or executable behavior, depending on the directive kind.        | [Directives](./directives)                |
| Deprecation and evolution | Communicate lifecycle changes and plan compatible schema updates.                   | [Versioning](./versioning)                |

Use type extensions for static modularity. Choose dynamic schemas only when the schema must change based on external metadata or runtime configuration.

# Next steps

- Build a read entry point with [Queries](./queries).
- Learn how returned shapes are inferred and configured in [Object Types](./object-types).
- Add field inputs with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Strengthen the contract with [Lists and Non-Null](./lists).
- Move to runtime behavior with [Resolvers](../fetching-data/resolvers) and [DataLoader](../fetching-data/batching/dataloader) after the schema shape is clear.
