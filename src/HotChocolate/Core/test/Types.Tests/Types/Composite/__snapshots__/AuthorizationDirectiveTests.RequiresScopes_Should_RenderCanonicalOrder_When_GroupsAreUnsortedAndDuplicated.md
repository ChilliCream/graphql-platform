# RequiresScopes_Should_RenderCanonicalOrder_When_GroupsAreUnsortedAndDuplicated

```graphql
schema {
  query: Query
}

type Query {
  field: String @requiresScopes(scopes: [["a", "b"], ["c"]])
}

"""
The @requiresScopes directive requires the client to be granted all scopes of at least
one of the scope groups.
"""
directive @requiresScopes(
  "The scope groups of which at least one must be fully granted."
  scopes: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM
```
