# UseConnection_Should_ExposeConnectionType_When_ResolverReturnsGenericConnection

## Schema

```graphql
schema {
  query: Query
}

type Query {
  books(
    "Returns the first _n_ elements from the list."
    first: Int
    "Returns the elements in the list that come after the specified cursor."
    after: String
    "Returns the last _n_ elements from the list."
    last: Int
    "Returns the elements in the list that come before the specified cursor."
    before: String
  ): BookConnection!
}

type Book {
  id: Int!
  title: String
}

"A connection to a list of items."
type BookConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [BookEdge!]
  "A flattened list of the nodes."
  nodes: [Book]
  "Identifies the total count of items in the connection."
  totalCount: Int!
}

"An edge in a connection."
type BookEdge {
  "A cursor for use in pagination."
  cursor: String!
  "The item at the end of the edge."
  node: Book
}

"Information about pagination in a connection."
type PageInfo {
  "Indicates whether more edges exist following the set defined by the clients arguments."
  hasNextPage: Boolean!
  "Indicates whether more edges exist prior the set defined by the clients arguments."
  hasPreviousPage: Boolean!
  "When paginating backwards, the cursor to continue."
  startCursor: String
  "When paginating forwards, the cursor to continue."
  endCursor: String
}
```

## Result

```json
{
  "data": {
    "books": {
      "edges": [
        {
          "cursor": "cursor1",
          "node": {
            "id": 1
          }
        }
      ],
      "nodes": [
        {
          "id": 1
        }
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": false,
        "startCursor": "cursor1",
        "endCursor": "cursor1"
      },
      "totalCount": 1
    }
  }
}
```
