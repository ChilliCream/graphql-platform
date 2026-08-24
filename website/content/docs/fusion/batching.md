---
title: "Batching"
description: "How the Fusion gateway collapses many subgraph calls into fewer round trips with variable batching, request batching, and alias batching."
---

One wave of a query plan can produce many calls to the same subgraph: one entity lookup per item of a list, or several distinct lookups that all target the same service. Batching collapses those calls into fewer round trips, without changing the query plan.

# Where Batching Applies

Consider a query that reads a list of books from one subgraph and each book's rating from another:

```graphql
{
  books {
    title
    rating
  }
}
```

The gateway fetches the books from the first subgraph, then needs `rating` for every book from the second. With two books that is two lookups, with two hundred books it is two hundred. Batching sends them as one request instead; the same lookups still happen, they just travel together.

# The Three Batching Capabilities

Fusion knows three ways to put more than one call into one HTTP request. Each one is declared as a capability of the subgraph it is used against.

## Variable Batching

Variable batching sends **one operation with many sets of variables** in one request. The `variables` property carries an array instead of an object, and the subgraph runs the operation once per set:

```json
{
  "query": "query($id: ID!) { bookById(id: $id) { rating } }",
  "variables": [{ "id": "1" }, { "id": "2" }]
}
```

Variable batching is a Hot Chocolate protocol extension, not part of the GraphQL over HTTP specification, so only subgraphs that implement it can be sent this shape.

## Request Batching

Request batching sends **an array of independent requests** in one HTTP request. Each element is a complete GraphQL request with its own query and its own variables, so a batch can carry different operations:

```json
[
  {
    "query": "query($id: ID!) { bookById(id: $id) { rating } }",
    "variables": { "id": "1" }
  },
  {
    "query": "query($id: ID!) { authorById(id: $id) { name } }",
    "variables": { "id": "7" }
  }
]
```

When the subgraph supports request batching but not variable batching, the gateway flattens each operation's variable sets into one array element per set, so the batch still travels as a single request.

Request batching is also a protocol extension. Subgraphs that do not accept a JSON array as the request body cannot be sent this shape.

## Alias Batching

Alias batching needs no protocol extension. The gateway merges the calls into **one plain GraphQL operation** whose root selections are aliased per item, which any spec-compliant GraphQL server can answer.

For the two book lookups above, the gateway sends:

```graphql
query Op_68b774cc_Batch_2963222070b1fe07($_0___fusion_1_id: ID!, $_1___fusion_1_id: ID!) {
  _0_bookById: bookById(id: $_0___fusion_1_id) {
    ..._fusion_body_1
  }
  _1_bookById: bookById(id: $_1___fusion_1_id) {
    ..._fusion_body_1
  }
}

fragment _fusion_body_1 on Book {
  rating
}
```

```json
{
  "_0___fusion_1_id": "1",
  "_1___fusion_1_id": "2"
}
```

The subgraph answers with one response, and the gateway splits it by alias:

```json
{
  "data": {
    "_0_bookById": { "rating": "1" },
    "_1_bookById": { "rating": "2" }
  }
}
```

The merged operation follows three rules:

- **Each item gets a prefix.** The root selection is aliased with `_<index>_`, and the variables that vary per item are renamed with the same prefix.
- **Shared variables are declared once.** A variable the gateway forwards from the client request carries one value for the whole batch, so it keeps its name and appears once in the variable definitions.
- **Repeated bodies become one fragment.** When several items select the same body, the body is emitted once as a generated fragment and each item spreads it.

Only query operations are merged. Mutations and subscriptions are never merged, and a request that uploads files keeps its own multipart round trip while the remaining requests still share one.

# How Fusion Chooses a Protocol

The gateway decides per subgraph, from the capabilities that subgraph declares. There are two independent dimensions.

**Many variable sets of one operation.** Variable batching wins whenever the subgraph declares it. Alias batching serves this dimension when variable batching is absent.

**Distinct operations in one wave.** Request batching wins whenever the subgraph declares it. Alias batching serves this dimension when request batching is absent. When neither is available, the gateway sends one HTTP request per operation.

The two decisions are taken independently, so a subgraph that declares only one of the two protocol extensions uses it for its own dimension and alias batching for the other. A batch that would merge fewer than two items is sent on the subgraph's native protocol instead.

# Configuring Batching Capabilities

Batching capabilities live under `capabilities.batching` in a source schema's HTTP transport settings:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "http://products/graphql",
      "capabilities": {
        "batching": {
          "variableBatching": true,
          "requestBatching": true,
          "aliasBatching": true
        }
      }
    }
  }
}
```

All three flags are optional and independent. A flag you declare always wins; a flag you omit keeps its default.

## Defaults

The default depends on how the source schema is connected:

| Source schema                             | `variableBatching` | `requestBatching` | `aliasBatching` |
| ----------------------------------------- | ------------------ | ----------------- | --------------- |
| GraphQL (the default connector)           | on                 | on                | off             |
| Apollo Federation (see the section below) | off                | off               | on              |

The default GraphQL connector assumes a subgraph that implements both protocol extensions, which a Hot Chocolate source schema does. An Apollo Federation subgraph is only assumed to speak plain GraphQL.

The connector kind only sets the starting value, so partial settings mix with it per flag:

- `{ "requestBatching": true }` on an Apollo Federation subgraph yields request batching **and** alias batching, and request batching is then preferred for distinct operations.
- `{ "aliasBatching": false }` on an Apollo Federation subgraph yields no batching at all.
- `{ "variableBatching": false, "requestBatching": false }` on an Apollo Federation subgraph still leaves alias batching on. Declare `"aliasBatching": false` as well to turn batching off completely.

> [!NOTE]
> The settings template a Hot Chocolate subgraph exports declares all three flags explicitly, so a gateway that uses it does not fall back to the defaults.

# Apollo Federation Subgraphs

An Apollo Federation subgraph is entered through the `_entities` field, and the entity keys travel in its `representations` argument. One `_entities` operation therefore already resolves many entities with a single variable set, so variable batching never applies on this path.

That leaves distinct operations: several `_entities` calls with different sub-selections, or several different lookups against the same subgraph in one wave. For those, the gateway uses request batching if the subgraph declares it, alias batching otherwise, and one request per operation when neither is available.

By default only alias batching is on, so two `_entities` calls in one wave travel as one merged operation:

```graphql
query Op_88f73202_Batch_d9cfb37825dbe30d($_0_representations: [_Any!]!, $_1_representations: [_Any!]!) {
  _0__entities: _entities(representations: $_0_representations) {
    ... on Child {
      b: value(suffix: "!")
    }
  }
  _1__entities: _entities(representations: $_1_representations) {
    ... on Child {
      a: value
    }
  }
}
```

See the [Apollo Federation Connector](./connectors/apollofederation.md#batching) page for the connector-specific settings.

# Errors in a Batched Request

A merged operation produces a single response, and the gateway attributes its errors by path: the first segment of an error path is the alias of the item, so the error is routed to that item and rewritten to the item's own field name. An error without a path applies to the whole request and is handed to every item of the batch.

The result is the same as if the calls had been sent separately. In the two-book example, an error on the second book's `rating` appears on that book alone:

```json
{
  "data": {
    "books": [
      { "title": "C# in Depth", "rating": "1" },
      { "title": "The Lord of the Rings", "rating": null }
    ]
  },
  "errors": [
    {
      "message": "Rating unavailable.",
      "path": ["books", 1, "rating"]
    }
  ]
}
```

Independent of batching, the `capabilities.onError` setting selects the error handling mode the gateway asks a subgraph for (`"propagate"` or `"null"`). It is not set by default, so the gateway sends no `onError` value and the subgraph applies its own behavior.

# Next Steps

- **"I want to tune the transport itself"**: [Performance Tuning](./performance-tuning.md) covers the named `HttpClient`, HTTP/2, request deduplication, and concurrency limits.
- **"I want to understand where the lookups come from"**: [Entities and Lookups](./entities-and-lookups.md) explains how the gateway enters a subgraph by key.
- **"I run Apollo Federation subgraphs"**: [Apollo Federation Connector](./connectors/apollofederation.md) covers the connector end to end.
