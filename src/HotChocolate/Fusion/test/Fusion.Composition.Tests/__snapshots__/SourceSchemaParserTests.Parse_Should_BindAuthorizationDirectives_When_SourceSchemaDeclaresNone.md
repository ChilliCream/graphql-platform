# Parse_Should_BindAuthorizationDirectives_When_SourceSchemaDeclaresNone

```graphql
"The @authenticated directive requires the client to be authenticated."
directive @authenticated on
  | SCALAR
  | OBJECT
  | FIELD_DEFINITION
  | INTERFACE
  | ENUM

"The @policy directive requires the client to satisfy all policies of at least one of the policy groups."
directive @policy(
  "The policy groups of which at least one must be fully satisfied."
  policies: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM

"The @requiresScopes directive requires the client to be granted all scopes of at least one of the scope groups."
directive @requiresScopes(
  "The scope groups of which at least one must be fully granted."
  scopes: [[String!]!]!
) on SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | ENUM
```
