# RequiresScopes_Should_RenderOnEveryLocation_When_AppliedByAttribute

```graphql
schema {
  query: Query
}

type Query {
  field: String @requiresScopes(scopes: [["admin"], ["read", "write"]])
  thing: Thing
  paint: Paint
  custom: Custom
}

type Impl implements Thing
  @requiresScopes(scopes: [["admin"], ["read", "write"]]) {
  name: String @requiresScopes(scopes: [["admin"], ["read", "write"]])
}

interface Thing @requiresScopes(scopes: [["admin"], ["read", "write"]]) {
  name: String @requiresScopes(scopes: [["admin"], ["read", "write"]])
}

enum Paint @requiresScopes(scopes: [["admin"], ["read", "write"]]) {
  RED
}

scalar Custom @requiresScopes(scopes: [["admin"], ["read", "write"]])

"""
The @requiresScopes directive requires the client to be granted all scopes of at least
one of the scope groups.
"""
directive @requiresScopes(
  "The scope groups of which at least one must be fully granted."
  scopes: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM
```
