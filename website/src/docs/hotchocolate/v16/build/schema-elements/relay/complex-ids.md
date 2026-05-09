---
title: "Complex IDs"
---

Use a complex ID when a Relay node is identified by more than one domain value. For example, your database might identify a chapter using both `BookId` and `ChapterNumber`, but Relay still expects a single opaque `id: ID!` and a `node(id:)` lookup.

This page explains how to work with composite Relay IDs. If you are looking for information on global ID setup, `Node` basics, or typed ID arguments, see [Global Identifiers](./global-identifiers).

```graphql
interface Node {
  id: ID!
}

type Query {
  node(id: ID!): Node
}
```

# When to Use a Complex ID

A complex ID is a server-side value that contains all the information needed to load a specific object. Clients still interact with a single GraphQL `ID` string.

| Term                   | Meaning                                                                | Example                           |
| ---------------------- | ---------------------------------------------------------------------- | --------------------------------- |
| Domain or database key | Values used by storage or business logic.                              | `BookId = 1`, `ChapterNumber = 2` |
| Composite ID value     | One CLR value containing the key parts.                                | `new BookChapterId(1, 2)`         |
| GraphQL global ID      | Client-visible opaque `ID` string that also includes the GraphQL type. | `id: "<opaque BookChapter ID>"`   |

Choose a complex ID when the combination of values forms the stable public identity of the object.

| Good use case                         | Prefer another design when                                                              |
| ------------------------------------- | --------------------------------------------------------------------------------------- |
| Tenant ID plus local row ID.          | A stable public surrogate key exists.                                                   |
| Parent ID plus child number.          | The parts are display data, filters, or sorting values.                                 |
| SKU plus batch or region.             | The parts contain sensitive data that clients should not receive in a reversible value. |
| Natural keys that cannot be replaced. | The key parts can change during normal edits.                                           |

# Model the Key as a Single .NET Value

```csharp
#nullable enable

namespace Catalog.Types;

public readonly record struct BookChapterId(int BookId, int ChapterNumber);
```

Keep the key type small, immutable, and named to reflect the lookup it represents. The order of the parts becomes part of your ID compatibility contract once clients store IDs, so choose and maintain the order carefully.

Generated serializers are tested for composite values with `string`, `short`, `int`, `long`, `Guid`, and `bool` parts. Avoid unsupported part types, nested value objects, or nullable members unless you have verified them in your application.

# Expose a Single GraphQL ID Field

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

[Node]
public sealed class BookChapter
{
    [ID]
    public BookChapterId Id => new(BookId, ChapterNumber);

    [GraphQLIgnore]
    public int BookId { get; init; }

    public int ChapterNumber { get; init; }

    public required string Title { get; init; }

    [NodeResolver]
    public static Task<BookChapter?> GetAsync(
        BookChapterId id,
        BookChapterRepository chapters,
        CancellationToken cancellationToken)
    {
        return chapters.GetChapterAsync(
            id.BookId,
            id.ChapterNumber,
            cancellationToken);
    }
}
```

The `Id` property serves as the public Relay identity. You can hide raw key parts, expose them as business fields, or move them to a separate GraphQL model. Do not instruct clients to decode `id` to extract `BookId` or `ChapterNumber`.

Expected SDL:

```graphql
type BookChapter implements Node {
  id: ID!
  chapterNumber: Int!
  title: String!
}
```

The node resolver receives a `BookChapterId` value, not the global ID string. Query using all key parts and return `null` if the row does not exist. If `nodes(ids:)` may load many chapters, batch repository access with a [DataLoader](/docs/hotchocolate/v16/build/dataloader).

Follow these resolver rules:

| Rule                                                    | Reason                                                                |
| ------------------------------------------------------- | --------------------------------------------------------------------- |
| Make the first parameter the ID value.                  | Hot Chocolate passes the parsed node ID value first.                  |
| Name the first parameter `id` or a name ending in `Id`. | Analyzer rules use that convention for node resolvers.                |
| Do not annotate the parameter with `[ID]` or `[ID<T>]`. | The node pipeline has already parsed the global ID.                   |
| Return a nullable node type.                            | A valid ID can point to data that no longer exists or is not visible. |

# Register Composite ID Serialization

Hot Chocolate requires a node ID value serializer for each composite CLR type. Use the generated serializer if you can use the analyzer package. Write a manual serializer if you need explicit migration behavior or cannot use the analyzer.

## Generate the Serializer

```csharp
#nullable enable

using HotChocolate.Types.Relay;

builder
    .AddGraphQL()
    .AddGlobalObjectIdentification()
    .AddNodeIdValueSerializerFrom<BookChapterId>();
```

`AddNodeIdValueSerializerFrom<TValue>()` is an analyzer hook. Add `HotChocolate.Types.Analyzers` with the same version as your Hot Chocolate packages. When the analyzer runs, it replaces the hook with registration for a generated serializer based on `CompositeNodeIdValueSerializer<T>`.

| Requirement              | Value                                                                                     |
| ------------------------ | ----------------------------------------------------------------------------------------- |
| Package                  | `HotChocolate.Types.Analyzers`                                                            |
| Registration API         | `AddNodeIdValueSerializerFrom<BookChapterId>()`                                           |
| Verified part types      | `string`, `short`, `int`, `long`, `Guid`, `bool`                                          |
| Missing analyzer symptom | `NotImplementedException` with a message that `HotChocolate.Types.Analyzers` is required. |

## Write the Serializer Manually

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed class BookChapterIdSerializer
    : CompositeNodeIdValueSerializer<BookChapterId>
{
    protected override NodeIdFormatterResult Format(
        Span<byte> buffer,
        BookChapterId value,
        out int written)
    {
        if (TryFormatIdPart(buffer, value.BookId, out var bookIdLength)
            && TryFormatIdPart(
                buffer[bookIdLength..],
                value.ChapterNumber,
                out var chapterNumberLength))
        {
            written = bookIdLength + chapterNumberLength;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.BufferTooSmall;
    }

    protected override bool TryParse(
        ReadOnlySpan<byte> buffer,
        out BookChapterId value)
    {
        if (TryParseIdPart(buffer, out int bookId, out var consumed)
            && TryParseIdPart(
                buffer[consumed..],
                out int chapterNumber,
                out _))
        {
            value = new BookChapterId(bookId, chapterNumber);
            return true;
        }

        value = default;
        return false;
    }
}
```

Register the manual serializer with the schema:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification()
    .AddNodeIdValueSerializer<BookChapterIdSerializer>();
```

Use `TryFormatIdPart` and `TryParseIdPart` for each part. Keep the order consistent in both `Format` and `TryParse`. Do not parse the bytes by splitting on a separator. The helper methods handle part boundaries and escaping for supported part types, including string values that may contain delimiter-like characters.

# Query a Node by a Single ID

Clients send and store a single opaque ID string.

```graphql
query GetBookChapter($id: ID!) {
  node(id: $id) {
    id
    __typename
    ... on BookChapter {
      title
      chapterNumber
    }
  }
}
```

Variables:

```json
{
  "id": "<opaque BookChapter ID>"
}
```

Response:

```json
{
  "data": {
    "node": {
      "id": "<same opaque BookChapter ID>",
      "__typename": "BookChapter",
      "title": "The Beginning",
      "chapterNumber": 1
    }
  }
}
```

Clients should not decode, split, compare by decoded parts, or assemble this ID. If a client needs the chapter number for display, expose `chapterNumber` as a separate field.

# Keep Composite IDs Opaque and Stable

| Situation                      | Recommended                                           | Avoid                                                    |
| ------------------------------ | ----------------------------------------------------- | -------------------------------------------------------- |
| Client needs to refetch.       | Store and pass `id`.                                  | Pass `bookId` and `chapterNumber` as the Relay identity. |
| Client needs a display number. | Expose `chapterNumber` as its own field.              | Decode it from `id`.                                     |
| Serializer shape changes.      | Parse old and new values during migration.            | Break stored IDs without a compatibility window.         |
| GraphQL type name changes.     | Treat the node type name as part of ID compatibility. | Rename node types without an ID migration plan.          |
| Tenant appears in the key.     | Use an authorization-safe public identity.            | Expose sensitive tenant names through reversible values. |

Before deploying a complex ID, review this checklist:

- Keep the composite value type stable.
- Keep part order stable.
- Keep the GraphQL node type name stable.
- Be careful when changing default node ID serializer options, URL-safe Base64 settings, or legacy serializer settings.
- Use a public surrogate key if composite parts should not leave the server.

# Troubleshooting Complex IDs

| Symptom                                                     | Likely cause                                                                                                           | Fix                                                                                                          |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `AddNodeIdValueSerializerFrom<TValue>()` throws at runtime. | `HotChocolate.Types.Analyzers` is missing, or source generator interception did not run.                               | Add the analyzer package or register a manual `CompositeNodeIdValueSerializer<T>`.                           |
| `node(id:)` returns a GraphQL error.                        | The ID is malformed, the ID points to a different GraphQL type, or no value serializer handles the composite CLR type. | Verify the node type, the serializer registration, and the ID value type.                                    |
| `node(id:)` returns `null`.                                 | The ID parsed, but your resolver found no visible row.                                                                 | Check data, authorization, and repository lookup logic.                                                      |
| Analyzer reports a node resolver parameter issue.           | The first resolver parameter has the wrong name or has `[ID]` / `[ID<T>]`.                                             | Rename it to `id` or a name ending in `Id`, and remove ID attributes.                                        |
| IDs changed after deployment.                               | The value type shape, part order, GraphQL type name, generated serializer, or node ID serializer options changed.      | Restore compatibility or add migration parsing before emitting the new shape.                                |
| String key parts contain colons or backslashes.             | Custom parsing treats the serialized value as a public format.                                                         | Use `TryFormatIdPart` and `TryParseIdPart`; do not split manually.                                           |
| Raw key parts still appear in the schema.                   | Your GraphQL model exposes storage details.                                                                            | Hide implementation details with `[GraphQLIgnore]`, object type configuration, or a dedicated GraphQL model. |

# API Summary

| API                                       | Purpose                                             | Notes                                                                           |
| ----------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------------------------- |
| `[ID]`                                    | Expose the composite CLR value as one GraphQL `ID`. | Basic global ID usage is covered in [Global Identifiers](./global-identifiers). |
| `[Node]`                                  | Make the type implement `Node`.                     | Keep the node resolver focused on loading by the composite value.               |
| `[NodeResolver]`                          | Mark the method used by `node(id:)`.                | The first parameter receives the deserialized composite value.                  |
| `CompositeNodeIdValueSerializer<T>`       | Base class for manual multi-part ID serializers.    | Use helper methods for each supported part.                                     |
| `AddNodeIdValueSerializerFrom<TValue>()`  | Register an analyzer-generated value serializer.    | Requires `HotChocolate.Types.Analyzers`.                                        |
| `AddNodeIdValueSerializer<TSerializer>()` | Register a manual value serializer.                 | The serializer must implement `INodeIdValueSerializer`.                         |
| `AddDefaultNodeIdSerializer(...)`         | Configure global node ID serializer options.        | Treat option changes as a migration concern.                                    |

# Next Steps

| If you need to...                                       | Go to                                                 |
| ------------------------------------------------------- | ----------------------------------------------------- |
| Configure global IDs, ID arguments, and `Node` refetch. | [Global Identifiers](./global-identifiers)            |
| Review the Relay pattern set.                           | [Relay](./)                                           |
| Batch composite node lookups.                           | [DataLoader](/docs/hotchocolate/v16/build/dataloader) |
| Page through collections of nodes.                      | [Connections](./connections)                          |
