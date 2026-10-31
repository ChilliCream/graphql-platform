# Schema_Should_ExposeSeparateConnectionPageInfo_When_CustomConnectionMixesWithClassicPaging

## ConnectionPageInfo

```graphql
"Represents the connection page info.\nThis class provides additional information about pagination in a connection."
type ConnectionPageInfo {
  "Indicates whether more edges exist following\nthe set defined by the clients arguments."
  hasNextPage: Boolean!
  "Indicates whether more edges exist prior\nthe set defined by the clients arguments."
  hasPreviousPage: Boolean!
  "When paginating backwards, the cursor to continue."
  startCursor: String
  "When paginating forwards, the cursor to continue."
  endCursor: String
}
```

## CustomConnection

```graphql
type CustomConnection {
  edges: [CustomEdge!]
  pageInfo: ConnectionPageInfo!
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
