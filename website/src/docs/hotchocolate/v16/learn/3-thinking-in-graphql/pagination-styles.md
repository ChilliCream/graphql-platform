---
title: "Pagination styles"
description: "Choose the cursor, offset, connection, total count, and ordering contract that fits a Hot Chocolate v16 GraphQL field."
---

# Pagination is part of the API contract

When a field can return many items, you need to decide what the schema guarantees to clients. This decision comes before you select attributes, middleware, or database techniques. Pagination is a schema and client-experience choice. It shapes the operations clients write, how caches merge list results, the workload a request can trigger, and how safely you can evolve the field later.

A list field must offer a consistent way for clients to request a slice and continue from that slice. Consider these decisions separately:

| Decision | Question to answer |
| --- | --- |
| Navigation model | Does the client continue from the current page, show nearby page buttons, or jump to any page number? |
| GraphQL shape | Should the field return a flat list, a connection, or a collection segment? |
| Metadata | Does the client need cursors, `pageInfo`, `totalCount`, or relative page cursors? |
| Hot Chocolate implementation | Should you use cursor paging, offset paging, relative cursors, providers, or field options? |

For new public or evolving APIs, cursor pagination with a connection-style result is usually the best starting point. Use a plain list field only when the list is naturally bounded and expected to remain so. Avoid plain lists for result sets that may grow, because changing `[Book!]!` to `BooksConnection` later alters the contract for every client.

If you are not ready to page a field internally, you can still use a connection-shaped response for a growing list. This leaves room for pagination metadata without first teaching clients a flat list contract.

Before choosing options, write a one-sentence summary for the field:

```text
Clients load more activity items from the current position.
```

This sentence tells you more about the client experience than the backing table does.

# Start from the client pagination experience

Choose the style that matches the user's navigation needs. The UI's requirements often matter more than the storage technology at this stage.

| Client experience | Usually choose | Why |
| --- | --- | --- |
| Feed, activity stream, timeline, mobile list, or "load more" button | Cursor pagination with a connection | The client continues from the last item it received. |
| Product list with next and previous buttons | Cursor pagination with a connection | The client moves through the list without needing a global page number. |
| Search results with nearby page buttons | Cursor pagination with relative cursors | Numbered page-window controls can use returned page cursors. |
| Admin grid with "go to page 25" | Offset pagination | The product requirement is a direct positional jump. |
| Report over bounded or stable data | Offset pagination, often with count | Numeric positions and totals may be part of the reporting task. |
| Relay-compatible client or shared GraphQL tooling | Connection-shaped cursor pagination | The response follows a widely used GraphQL pagination convention. |

Numbered buttons do not always require offset pagination. A UI that shows "previous 3, current, next 3" often needs a page window around the current slice, not an arbitrary jump to any position in the full result set.

Checkpoint: Identify the main client experience. Then decide if the UI needs nearby pages, arbitrary jumps, standardized metadata, or exact totals.

# Choose cursor pagination for stable traversal

```graphql
query GetUsers($after: String) {
  users(first: 10, after: $after) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Cursor pagination answers: where do I continue from?

A cursor is an opaque value representing a position in an ordered result set. Clients use `first` and `after` to move forward. If the field supports backward movement, clients use `last` and `before`.

Cursor pagination is a good fit for public APIs, long-lived APIs, changing lists, feeds, timelines, activity logs, search results, mobile clients, partner integrations, and "load more" experiences. It works best when the client cares about stable traversal rather than a numeric position.

Cursors are not encoded page numbers. Clients must store and send the cursors returned by the API, not construct them or depend on their format.

Stable ordering is part of the contract. If a field sorts only by a non-unique value like `createdAt`, two items with the same timestamp can move across page boundaries. Add a deterministic tie-breaker such as `id`.

```graphql
type Query {
  activities(first: Int, after: String): ActivityConnection!
}
```

```text
Contract: newest activity first, then by id.
```

Cursor pagination often maps well to seek or keyset pagination in the data source. The database can continue after the last seen key instead of skipping rows as pages get deeper. For more on this, see Markus Winand's [Use The Index, Luke](https://use-the-index-luke.com/no-offset) on avoiding large offsets.

Checkpoint: Name the field's deterministic sort order and decide if forward-only navigation is enough.

# Use relative cursors for page-window controls

```graphql
query GetProducts($after: String) {
  products(first: 20, after: $after) {
    nodes {
      name
    }
    pageInfo {
      backwardCursors {
        page
        cursor
      }
      forwardCursors {
        page
        cursor
      }
    }
  }
}
```

Many UIs show a nearby page window: previous 3, current, next 3. That experience does not always require offset paging.

Hot Chocolate supports relative cursor fields on `pageInfo`, including `forwardCursors` and `backwardCursors` when `EnableRelativeCursors` is enabled. Treat this as a schema and client pattern built on cursor-style windows, not as a separate GraphQL standard. A relative page cursor carries a display `page` number and the opaque `cursor` to request that page.

Use the `nodes` selection for the current slice. Use `pageInfo.forwardCursors` and `pageInfo.backwardCursors` to render nearby page buttons. When the user selects a button, send the returned cursor in the next request.

This keeps the navigation contract cursor-based while giving the UI familiar page-window controls. It is still not the same as jumping directly to page 50 without first reaching the surrounding window.

Use [Nitro operations](/docs/nitro/documents/operations/) to prototype the query with different `first`, `after`, `last`, and `before` variables. Compare consecutive `pageInfo.forwardCursors` and `pageInfo.backwardCursors` values in the [response pane](/docs/nitro/documents/response/) with the client team.

Checkpoint: decide whether your numbered UI needs nearby page navigation or true arbitrary jumps.

# Choose offset pagination for true positional jumps

```graphql
query GetUsers {
  users(skip: 20, take: 10) {
    items {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

Offset pagination answers: which numbered slice do I want?

The client sends a position such as `skip` or offset and a page size such as `take` or limit. This fits internal admin tables, reporting screens, bounded data sets, stable data, exports, and clients that must jump to page 5 or page 50 without visiting nearby pages first.

Hot Chocolate offset paging returns a `CollectionSegment`, not a Relay-style connection. The generated segment contains `items` for the current slice and `pageInfo` with offset paging metadata. When enabled, the segment can also expose `totalCount`.

Offset pagination is valid when direct page jumps are the product requirement. The trade-offs are different:

| Trade-off | What to review |
| --- | --- |
| Changing data | Inserts and deletes can shift positions between requests. |
| Deep pages | Large offsets can be costly depending on the data source. |
| API contract | Numeric positions may become part of client behavior. |
| Counts | Direct page-number UI often asks for `totalCount` or a last page number. |

Compare offset with relative cursors before choosing. If the UI only needs nearby page buttons around the current slice, cursor pagination with relative cursors may fit better. If the UI must jump directly to any page number, offset is the clearer contract.

Checkpoint: confirm whether direct page jumps matter more than stable traversal through changing data.

# Use connections when clients need pagination metadata

```graphql
query GetGroups($after: String) {
  groups(first: 10, after: $after) {
    nodes {
      id
      name
    }
    edges {
      cursor
      node {
        id
      }
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}
```

A connection is a wrapper around a paginated list. It is a GraphQL response convention, not a database strategy. Hot Chocolate's cursor paging follows the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm) and generates the fuller connection shape with `nodes`, `edges`, and `pageInfo`.

| Term | Meaning |
| --- | --- |
| Connection | The paginated wrapper around the list field. |
| Node | The item in the list. |
| Edge | A positioned relationship to a node. It can hold the node, its cursor, and relationship metadata. |
| Cursor | The opaque position value for an edge or page. |
| `pageInfo` | Metadata for continuing through the list. |

Use `nodes` when the client needs item data only. Keep `edges` available when the client needs per-item cursors or data about the relationship.

For example:

```text
User -> Membership edge { role, joinedAt } -> Group
```

The `Group` node describes the group. The membership edge describes the relationship between the user and the group. Edge fields are for data such as role, membership date, permission, rank, relevance score, or match reason. When the same node type appears through different relationships, prefer relation-specific connection and edge types such as `UserGroupsConnection` and `ProjectMembersConnection`.

`pageInfo` commonly exposes `hasNextPage`, `hasPreviousPage`, `startCursor`, and `endCursor`. Hot Chocolate can also expose relative cursor fields on `pageInfo` when that feature is enabled.

If your product chooses offset pagination but wants a custom connection-shaped schema, name that as a schema decision rather than Hot Chocolate's default offset result. Hot Chocolate offset paging uses `CollectionSegment` with `items` and `pageInfo`; Relay-style connections use `nodes`, `edges`, and `pageInfo`. Use the [Relay docs](/docs/hotchocolate/v16/building-a-schema/relay/) when you need global object identification or Relay-specific interoperability.

Checkpoint: decide whether clients need `nodes`, `edges`, `pageInfo`, relative cursors, `totalCount`, or Relay-compatible conventions.

# Decide whether the field should expose total count

`totalCount` is a product and performance decision. Do not treat it as a default checkbox.

Expose `totalCount` when the UI truly needs an exact count, such as "showing 1-10 of 245", a progress indicator, a report, or direct page-number navigation. Omit it when the experience is a feed, timeline, or load-more list that only needs to know whether another page exists.

Review these questions before adding it:

| Question | Why it matters |
| --- | --- |
| Does the UI display an exact total? | If not, `hasNextPage` may be enough. |
| Does the UI need the last page number? | Last-page navigation often depends on count. |
| Can the backing source count efficiently? | Large, filtered, authorized, or high-traffic lists can make counts expensive. |
| Can the count become stale? | Data may change between the count and the next page request. |
| Could the count reveal information? | Exact counts may expose data volume a caller should not infer. |
| Is a nearby page window enough? | Relative cursors may reduce the need for exact totals. |

Once you expose `totalCount`, clients may build UI, caching, tests, and navigation around it. Removing it or weakening its meaning later can be a breaking change.

For the Hot Chocolate option that adds `totalCount`, see the [Pagination reference](/docs/hotchocolate/v16/resolvers-and-data/pagination/#total-count).

Checkpoint: choose one outcome for the field: expose count, defer count, omit count, or use relative cursors without an exact total.

# Make ordering part of the contract

Pagination, sorting, filtering, and authorization form one list contract. Cursor pagination, relative cursors, and offset pagination all depend on predictable ordering.

```text
Less stable: sort by createdAt
More stable: sort by createdAt, then id
```

Cursor pagination needs a deterministic order because each cursor is meaningful within an ordered result set. Relative page cursors are meaningful only within the same ordered and filtered result set. Offset pagination also depends on order, but changing data can shift numeric positions between requests.

Filtering changes the result set, so it changes page boundaries, cursors, counts, and page windows. Authorization can also change which items appear on a page. Apply filtering and authorization consistently before slicing so cursors do not reveal, skip, or duplicate items the current viewer should not see.

Client caches should key paged results by the field and every argument that changes membership or order: paging arguments, filters, sorting, and viewer context such as tenant, locale, authorization scope, or feature flags. A cursor is reusable only for the same field, arguments, sort, filters, and viewer context. When any of those values change, discard the old cursor and request a new window.

Document these choices for every growing list field:

| Contract point | Example |
| --- | --- |
| Default sort | `createdAt DESC` |
| Tie-breaker | `id DESC` |
| Filter behavior | Cursors are valid for the current `where` arguments. |
| Authorization behavior | Pages contain only items visible to the caller. |
| Cursor validity | Cursors are opaque and valid only for the same field, filters, sort, and viewer context. |

Hot Chocolate data middleware has an expected order when you combine paging, projections, filtering, and sorting. Use the reference pages for implementation details: [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/). The [API options](/docs/hotchocolate/v16/api-reference/options/) page lists `EnableRelativeCursors` for schemas that expose relative cursor fields.

Implementation boundary: prefer provider-composed paging when the source is `IQueryable` or another provider-supported queryable source. Let Hot Chocolate compose paging with projections, filtering, and sorting so the provider can translate the combined operation and fetch the requested slice. If the source is an external API, a service call, or a DataLoader result, do not page after loading the full collection into memory. Pass paging arguments, filters, sorting, and viewer context to the backing source, or return a pre-sliced `Connection<T>` or `CollectionSegment<T>` with correct page metadata.

Checkpoint: name the stable order before exposing the field.

# Choose a pagination style for the field

Use this table as the final design choice.

| Field need | Choose | Add |
| --- | --- | --- |
| Public API list | Cursor pagination | Connection shape |
| Feed or timeline | Cursor pagination | `pageInfo.hasNextPage` and `pageInfo.endCursor` |
| Product results with nearby numbered controls | Cursor pagination | Connection shape and relative cursors |
| Admin grid with direct page jumps | Offset pagination | Count if the UI needs last-page controls |
| Report over stable data | Offset pagination | `totalCount` when the report requires it |
| Relay client | Cursor pagination | Connection shape and Relay-compatible identity choices |
| Bounded list that will stay small | Plain list | Clear schema description and tests |

Before you implement, review the checklist:

- Who consumes this field: public clients, partner clients, first-party apps, or internal tools?
- Does the data change while users page through it?
- Does the UI continue, show nearby page buttons, or jump to arbitrary pages?
- Does the client need `nodes`, `edges`, `pageInfo`, relative cursors, or `totalCount`?
- What is the default stable order and tie-breaker?
- How do filters and authorization affect the result set?
- Can you change this contract later without breaking clients?

Rule of thumb: for new public Hot Chocolate APIs, choose cursor pagination with a connection-style result. Add relative cursors for nearby page-window controls. Use offset pagination when arbitrary positional jumps are a hard requirement.

Use [Nitro operations](/docs/nitro/documents/operations/) for a quick design check with the client team. Run the proposed query, vary page variables, compare consecutive responses in the [response pane](/docs/nitro/documents/response/), and confirm the response shape contains only the metadata the UX needs.

# Common pagination problems to recognize early

| Symptom | Likely cause | Fix | Verify |
| --- | --- | --- | --- |
| Items are skipped or repeated between pages | Offset pagination over changing data, or cursor pagination without stable ordering | Prefer cursor pagination for changing data and define deterministic sorting | Request consecutive pages after inserting or deleting an item in the result range |
| The UI needs "previous 3, current, next 3" buttons | The team treats page-window UX as offset-only | Evaluate cursor pagination with relative cursors | Confirm the connection exposes the needed `pageInfo` cursor fields |
| The client cannot jump to page 50 | Cursor pagination and relative cursors are continuation-based | Use offset pagination if arbitrary page jumps are required | Confirm the UI truly needs arbitrary jumps rather than nearby navigation |
| `totalCount` is slow | Counting the full filtered or authorized result set requires extra work | Expose count only when the UX requires it, defer it, omit it, or use a reporting path | Compare the page query with and without count on representative data |
| The client sees `edges`, `nodes`, `pageInfo`, `forwardCursors`, and `backwardCursors` and does not know what to query | The connection exposes item data, continuation metadata, and optional page-window metadata | Select `nodes` for item data, `pageInfo` for continuation, relative cursors for page windows, and `edges` for per-item cursors or edge metadata | Try a minimal selection in [Nitro operations](/docs/nitro/documents/operations/) before adding fields to client code |
| Cursors behave differently after sorting changes | Cursors are meaningful only within a stable ordered result set | Treat sorting as part of the pagination contract | Document and test the default order for the field |

# Next steps

- Build the guided implementation in [Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-add-pagination/).
- Configure cursor pagination, total count, providers, names, page sizes, and nullable cursor keys in [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/).
- Add relative page cursors with the Hot Chocolate paging options when the UI needs nearby page-window controls.
- Add stable ordering with [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/).
- Review how filters change result sets in [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/).
- Connect pagination to resolver and middleware behavior in [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/).
- Review client cache and operation behavior in [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/).
- Read the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm) and Hot Chocolate [Relay](/docs/hotchocolate/v16/building-a-schema/relay/) docs when Relay interoperability matters.
