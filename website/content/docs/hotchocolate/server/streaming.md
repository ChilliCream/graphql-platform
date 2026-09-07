---
title: "Streaming Lists"
description: "Deliver list fields incrementally with the @stream directive in Hot Chocolate: async enumerable resolvers, streamed connections, the streamed error model, and the wire format."
---

> [!EXPERIMENTAL]
> `@stream` is a draft feature of the GraphQL specification. It is disabled by default and its shape can still change between releases.

`@stream` lets a client ask for a list field in slices: the initial response carries the first items, and the remaining items arrive one by one on the same response stream. The client drives it. A field that is streamable is still delivered as a complete list when no client asks for `@stream`.

```graphql
{
  books @stream(initialCount: 1) {
    title
  }
}
```

# Enabling `@stream`

`@stream` is added to the schema through the `EnableStream` schema option, the same gate that `EnableDefer` provides for `@defer`:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o =>
    {
        o.EnableStream = true;
        o.EnableDefer = true;
    });
```

While the option is off, the directive is not part of the schema and a document that uses it fails validation. The streamed connection pattern below defers `pageInfo`, so enable `@defer` as well to use it.

# Streaming a List Field

A field is streamable when its resolver returns `IAsyncEnumerable<T>`:

```csharp
public class Query
{
    public async IAsyncEnumerable<Book> GetBooks()
    {
        await foreach (var book in _repository.ReadBooksAsync())
        {
            yield return book;
        }
    }
}
```

The field is an ordinary list field in the schema:

```graphql
type Query {
  books: [Book!]!
}
```

With `@stream`, the response becomes a stream of payloads. The first payload carries the initial slice in `data` and announces the rest of the list with a `pending` entry. Each further item arrives as an `incremental` entry that carries the item in `items`. A `completed` entry ends the stream:

```json
{"data":{"books":[{"title":"C# in Depth"}]},"pending":[{"id":"2","path":["books"]}],"hasNext":true}
{"incremental":[{"id":"2","items":[{"title":"The Pragmatic Programmer"}]}],"hasNext":true}
{"incremental":[{"id":"2","items":[{"title":"Domain-Driven Design"}]}],"hasNext":true}
{"completed":[{"id":"2"}],"hasNext":false}
```

The `id` links the entries to the `pending` entry that announced the stream, and `path` locates the streamed list in `data`. Ids come from one namespace shared with `@defer`, so a response that uses both never reuses an id.

Items are produced strictly in order. The next item is only pulled from the source once the selection set of the current item has completed, so an item never overtakes its predecessor. Several completed items can share one payload when they are ready at the same time, which makes the payload boundaries above an example rather than a guarantee. The entries and their order are what a client reads.

## Directive Arguments

```graphql
directive @stream(
  if: Boolean! = true
  label: String
  initialCount: Int! = 0
) on FIELD
```

| Argument       | Type       | Default | Description                                                        |
| -------------- | ---------- | ------- | ------------------------------------------------------------------ |
| `initialCount` | `Int!`     | `0`     | The number of items delivered inline in the initial response.      |
| `label`        | `String`   | `null`  | A client-defined label that is echoed on the `pending` entry.      |
| `if`           | `Boolean!` | `true`  | Streams the field when `true` and delivers it inline when `false`. |

`initialCount` items are delivered in `data`, and the stream carries the items after them. With the default of `0`, `data` holds an empty list and every item arrives incrementally.

The server looks one item ahead of the initial slice. A source that is exhausted within `initialCount` items is therefore delivered as a complete list in the initial response, with no `pending` entry and no stream to complete.

`if: false` delivers the whole list inline, exactly like a request without the directive. `@skip` and `@include` take precedence over `@stream`.

`label` identifies a stream on the client. The label is carried on the `pending` entry:

```json
{
  "data": { "books": [] },
  "pending": [{ "id": "2", "path": ["books"], "label": "latest" }],
  "hasNext": true
}
```

## What Can Be Streamed

`@stream` is honored on fields backed by `IAsyncEnumerable<T>` and on the `edges` and `nodes` fields of a streamed connection. On any other list field the directive is accepted and the list is delivered inline, so a client can send `@stream` without knowing how a field is implemented.

Two rules are enforced during validation:

- `@stream` on a field that is not a list type is an error.
- Two selections that merge into the same response name are an error when either of them carries `@stream`. Give the fields different aliases to fetch both.

# Streaming Connections

A connection streams when its resolver returns a `StreamPageConnection<T>`, which wraps a `StreamPage<T>`. The page reads its items from an `IAsyncEnumerable<T>` and derives the cursors and the page info from what it read:

```csharp
using GreenDonut.Data;
using HotChocolate.Types.Pagination;

public class Query
{
    public StreamPageConnection<Book> GetBooks(int first)
        => new(
            new StreamPage<Book>(
                _repository.ReadBooksAsync(take: first + 1),
                new PagingArguments(first: first),
                static book => book.Id.ToString()));
}
```

The source yields at most one item more than the page holds. That extra item decides `hasNextPage` and is not part of the page. `StreamPage<T>` paginates forward only, so `first` and `after` are supported and `last` and `before` are rejected. Pass `totalCount` to the page when the count is known.

The connection is a Relay connection with the usual `edges`, `nodes`, `pageInfo` and `totalCount` fields. The client streams the items and defers the page info, which is only known once the last item has been read:

```graphql
{
  books(first: 2) {
    edges @stream(initialCount: 1) {
      node {
        title
      }
    }
    ... @defer {
      pageInfo {
        hasNextPage
        endCursor
      }
    }
  }
}
```

```json
{"data":{"books":{"edges":[{"node":{"title":"C# in Depth"}}]}},"pending":[{"id":"2","path":["books","edges"]},{"id":"3","path":["books"]}],"hasNext":true}
{"incremental":[{"id":"2","items":[{"node":{"title":"The Pragmatic Programmer"}}]}],"hasNext":true}
{"incremental":[{"id":"3","data":{"pageInfo":{"hasNextPage":true,"endCursor":"2"}}}],"completed":[{"id":"2"},{"id":"3"}],"hasNext":false}
```

`nodes @stream` works the same way and streams the flattened nodes.

## When a Connection Is Delivered Inline

The items of a connection are read from the page once. Every consumer of that one-shot source, and every field that has to be answered before the initial response is written, forces the connection to be delivered as a complete list. The connection is delivered inline when:

- no `@stream` is requested on `edges` or `nodes`,
- both `edges` and `nodes` are selected, or one of them is selected twice under an alias,
- `pageInfo` or `totalCount` is selected without `@defer`,
- any other sibling field of the connection is selected without `@defer`.

Introspection fields such as `__typename` do not force inline delivery.

Inline delivery produces the response the same selection produces without `@stream`, down to the errors: a connection whose item source is consumed twice fails on the second consumer either way.

# Errors in a Stream

Items that were already delivered are never retracted. What a later error does to the stream depends on the error handling mode, which is set with `ModifyRequestOptions(o => o.DefaultErrorHandlingMode = ...)` and defaults to `ErrorHandlingMode.Propagate`.

| What failed                                        | `Propagate`                                            | `Null`                                                             |
| -------------------------------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------ |
| A field of an item, item type nullable             | The item is delivered as `null`, the stream continues. | The item is delivered with the field `null`, the stream continues. |
| A field of an item, item type non-null             | The item is not delivered, the stream ends.            | The item is delivered with the field `null`, the stream continues. |
| The source yielded `null` for a non-null item type | The item is not delivered, the stream ends.            | The item is delivered as `null`, the stream continues.             |
| The source itself threw                            | The stream ends.                                       | The stream ends.                                                   |

An error inside an item that keeps the stream alive travels with the chunk that carries the item:

```json
{
  "incremental": [
    {
      "id": "2",
      "items": [null],
      "errors": [
        { "message": "item field failed", "path": ["books", 1, "title"] }
      ]
    }
  ],
  "hasNext": true
}
```

An error that ends the stream is reported on the `completed` entry, and the chunk it belongs to is never emitted:

```json
{
  "completed": [
    {
      "id": "2",
      "errors": [
        { "message": "item field failed", "path": ["books", 1, "title"] }
      ]
    }
  ],
  "hasNext": false
}
```

The path of an item error points into the list and names the failing field, `["books", 1, "title"]`. The path of a failing source is the list field itself, `["books"]`, because no item was in flight when it failed.

A stream is dropped without a `completed` entry when the data it is rooted in leaves the response, for example when a null propagates through the object that holds the streamed field. The response then ends with the error of the field that failed.

When a stream ends, for any of these reasons or because the client disconnected, the enumerator of the source is disposed first and the cleanup of the resolver runs afterwards, including the release of its dependency injection scope.

# Delivery over the Transports

A streamed response needs a transport that can carry more than one payload. Over HTTP the client selects it with the `Accept` header, and `multipart/mixed`, `text/event-stream` and `application/jsonl` all carry streams. A request that accepts only `application/graphql-response+json` or only `application/json` is rejected with an operation kind not allowed error, with status `405` for `application/graphql-response+json` and status `200` for `application/json`.

Over WebSockets with the `graphql-transport-ws` protocol, each payload arrives as its own `next` message and a `complete` message ends the operation. There is no capability negotiation for incremental delivery.

The payloads on this page use the current wire format, in which incremental list entries are `{ "id": ..., "items": [...] }`. The legacy `v0.1` format is still available per request and identifies the stream by `path` instead of by `id`:

```json
{
  "incremental": [
    { "items": [{ "title": "The Pragmatic Programmer" }], "path": ["books"] }
  ],
  "hasNext": true
}
```

See [Transports](./http-transport.md#incremental-delivery-defer-stream) for the content negotiation, the format selection and the server default.

# The List Field Contract

`@stream` adds no requirement to a field. What follows holds for every list field, in every delivery mode, and the directive changes none of it.

- The field is finite. Without `@stream` the executor reads the whole source before it writes the response, so a source that never ends never produces one. Data that has no end belongs in a subscription, not in a list field.
- The field returns the same items in the same order whether it is streamed or inlined. `@stream` changes the number of payloads the items arrive in and nothing else.
- Streaming is asked for by the client. The server never turns it on by itself, and it may answer any `@stream` request with a complete list.
- Streaming changes the delivery, not the resource envelope. The same resolvers run and the same data is materialized whether a field is streamed, deferred or inlined, so a stream does not lower the memory a request needs. It holds that memory until the stream has drained rather than until the response is written.

# Next Steps

- [Transports](./http-transport.md) for content negotiation, the incremental delivery formats and the streaming transports.
- [Pagination](../fetching-data/pagination.md) for the connection model that streamed connections follow.
- [Subscriptions](../defining-a-schema/subscriptions.md) for data that has no end.
- [Options Reference](./options.md#schema-options-modifyoptions) for `EnableStream` and the other schema options.
