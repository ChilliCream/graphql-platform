# Schema_Should_ExposeOnePageInfoType_When_ClassicPageConnectionAndStreamPageConnectionAreMixed

## OrderConnection

```graphql
"A connection to a list of items."
type OrderConnection {
  "A list of edges."
  edges: [_0_EdgeOfOrder!]
  "A flattened list of the nodes"
  nodes: [Order!]
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "Identifies the total count of items in the connection."
  totalCount: Int!
}
```

## PageInfo

```graphql
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
  "A list of cursors to continue paginating forwards."
  forwardCursors: [PageCursor!]!
  "A list of cursors to continue paginating backwards."
  backwardCursors: [PageCursor!]!
}
```

## ProductsConnection

```graphql
"A connection to a list of items."
type ProductsConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [ProductsEdge!]
  "A flattened list of the nodes."
  nodes: [Product!]
}
```

## _0_ConnectionOfProduct

```graphql
"A connection to a list of items."
type _0_ConnectionOfProduct {
  "A list of edges."
  edges: [_0_EdgeOfProduct!] @cost(weight: "10")
  "A flattened list of the nodes"
  nodes: [Product!] @cost(weight: "10")
  "Identifies the total count of items in the connection."
  totalCount: Int! @cost(weight: "10")
  "Information to aid in pagination."
  pageInfo: PageInfo!
}
```
