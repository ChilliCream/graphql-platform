# Policy_Should_PreserveExistingDirective_When_SubsequentGroupIsEmpty

```graphql
schema {
  query: Query
}

type Query {
  field: String @policy(policies: [["read"]])
}

"""
The @policy directive requires the client to satisfy all policies of at least one of
the policy groups.
"""
directive @policy(
  "The policy groups of which at least one must be fully satisfied."
  policies: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM
```
