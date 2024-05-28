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

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}

type Author {
  address: String!
  name: String!
}

type Mutation {
  bar: String!
  doSomething: String!
}

type Person implements Node & Entity {
  id: ID!
  lastName: String!
  name: String!
  address: String!
}

type Publisher {
  company: String!
  name: String!
}

type Query {
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
  fooBarBaz: String!
  foo: String!
  person: Entity
  enum: CustomEnum
  book: SomeBook!
  withDataLoader: String!
  staticField: String!
}

type SomeBook {
  title: String
  author: Author
  publisher: Publisher
}

type Subscription {
  onSomething: String!
}

enum CustomEnum {
  ABC
  DEF
}
```
