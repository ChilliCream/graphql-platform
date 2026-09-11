# Transform_AuthDirectives_OnEnumOrScalar

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: ["@key", "@authenticated", "@requiresScopes"]
  ) {
  query: Query
}

type Query {
  color: Color
  money: Money
}

enum Color @authenticated {
  RED
  BLUE
}

scalar Money @requiresScopes(scopes: [["finance"]])

scalar FieldSet
scalar federation__Scope

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @requiresScopes(scopes: [[federation__Scope!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
```

## Errors

```text
The type 'Color' in schema 'default' is annotated with the @authenticated directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
The type 'Money' in schema 'default' is annotated with the @requiresScopes directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
```
