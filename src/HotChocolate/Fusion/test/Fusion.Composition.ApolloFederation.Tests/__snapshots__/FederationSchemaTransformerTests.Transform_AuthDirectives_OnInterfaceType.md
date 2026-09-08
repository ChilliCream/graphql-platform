# Transform_AuthDirectives_OnInterfaceType

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
  node: Node
}

interface Node @authenticated {
  id: ID!
}

interface Asset @requiresScopes(scopes: [["read:asset"]]) {
  id: ID!
}

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
The type 'Node' in schema 'default' is annotated with the @authenticated directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
The type 'Asset' in schema 'default' is annotated with the @requiresScopes directive. Fusion's @policy directive supports only the OBJECT and FIELD_DEFINITION locations.
```
