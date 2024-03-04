# SchemaSnapshot

```graphql
schema {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

interface Entity {
  name: String!
}

type Mutation {
  bar: String!
  doSomething: String!
}

type Person implements Entity {
  name: String!
  address: String!
  lastName: String!
}

type Query {
  foo: String!
  person: Entity
  enum: CustomEnum
  book: SomeBook!
  withDataLoader: String!
  staticField: String!
}

type SomeBook {
  title: String
}

type Subscription {
  onSomething: String!
}

enum CustomEnum {
  ABC
  DEF
}
```
