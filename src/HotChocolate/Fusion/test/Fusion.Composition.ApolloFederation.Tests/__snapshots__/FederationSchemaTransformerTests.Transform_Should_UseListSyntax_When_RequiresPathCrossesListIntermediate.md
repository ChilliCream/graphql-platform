# Transform_Should_UseListSyntax_When_RequiresPathCrossesListIntermediate

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_lineBySku(sku: ID!): Line @internal @lookup
  fusion__lookup_orderById(id: ID!): Order @internal @lookup
  order(id: ID!): Order
}

type Info {
  lines: [Line]
}

type Line @key(fields: "sku") {
  sku: ID!
}

type Order @key(fields: "id") {
  id: ID!
  info: Info @external
  summary(
    info: InfoInput_1946937291 @require(field: "{ lines: info.lines[{ sku: sku }] }")
  ): Boolean
}

input InfoInput_1946937291 {
  lines: [LineInput_622943509]
}

input LineInput_622943509 {
  sku: ID
}
```
