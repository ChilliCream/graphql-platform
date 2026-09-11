# Transform_AuthDirectives_OnInterfaceField

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

interface Node {
  id: ID! @authenticated
  secret: String @requiresScopes(scopes: [["read:secret"]])
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
The field 'Node.id' in schema 'default' is annotated with the @authenticated directive, but it is declared on an interface type. Fusion's @policy directive does not support interface types or their fields.
The field 'Node.secret' in schema 'default' is annotated with the @requiresScopes directive, but it is declared on an interface type. Fusion's @policy directive does not support interface types or their fields.
```
