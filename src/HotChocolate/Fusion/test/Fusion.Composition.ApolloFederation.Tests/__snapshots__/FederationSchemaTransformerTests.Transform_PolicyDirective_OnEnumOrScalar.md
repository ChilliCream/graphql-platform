# Transform_PolicyDirective_OnEnumOrScalar

## Apollo Federation SDL

```graphql
schema
  @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
  @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
  query: Query
}

type Query {
  color: Color
  money: Money
}

enum Color @policy(policies: [["internal"]]) {
  RED
  BLUE
}

scalar Money @policy(policies: [["finance"]])

scalar FieldSet
scalar federation__Policy

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
directive @policy(policies: [[federation__Policy!]!]!) repeatable
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
```

## Errors

```text
The type 'Color' in schema 'default' is annotated with the @policy directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
The type 'Money' in schema 'default' is annotated with the @policy directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
```
