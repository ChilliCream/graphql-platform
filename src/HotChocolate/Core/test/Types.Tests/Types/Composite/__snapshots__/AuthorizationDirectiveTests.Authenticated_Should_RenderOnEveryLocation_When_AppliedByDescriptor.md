# Authenticated_Should_RenderOnEveryLocation_When_AppliedByDescriptor

```graphql
schema {
  query: Query
}

type Query {
  field: String @authenticated
  thing: Thing
  paint: Paint
  custom: Custom
}

type Impl implements Thing @authenticated {
  name: String @authenticated
}

interface Thing @authenticated {
  name: String @authenticated
}

enum Paint @authenticated {
  RED
}

scalar Custom @authenticated

"The @authenticated directive requires the client to be authenticated."
directive @authenticated on
  | SCALAR
  | OBJECT
  | FIELD_DEFINITION
  | INTERFACE
  | ENUM
```
