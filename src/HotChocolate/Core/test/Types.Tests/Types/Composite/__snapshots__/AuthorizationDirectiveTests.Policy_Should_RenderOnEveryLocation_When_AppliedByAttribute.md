# Policy_Should_RenderOnEveryLocation_When_AppliedByAttribute

```graphql
schema {
  query: Query
}

type Query {
  field: String @policy(policies: [["p1", "p2"], ["p3"]])
  thing: Thing
  paint: Paint
  custom: Custom
}

type Impl implements Thing @policy(policies: [["p1", "p2"], ["p3"]]) {
  name: String @policy(policies: [["p1", "p2"], ["p3"]])
}

interface Thing @policy(policies: [["p1", "p2"], ["p3"]]) {
  name: String @policy(policies: [["p1", "p2"], ["p3"]])
}

enum Paint @policy(policies: [["p1", "p2"], ["p3"]]) {
  RED
}

scalar Custom @policy(policies: [["p1", "p2"], ["p3"]])

"""
The @policy directive requires the client to satisfy all policies of at least one of
the policy groups.
"""
directive @policy(
  "The policy groups of which at least one must be fully satisfied."
  policies: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM
```
