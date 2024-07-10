# Arguments_Can_Be_Declared_On_The_Stream_Schema

```graphql
schema {
  query: Query
  subscription: MySubscription
}

type MySubscription {
  onMessage(userId: String!): String!
  onSysMessage: String!
  onFixedMessage: String!
  onInferTopic: String!
  onExplicit: String!
  onExplicitNonGeneric: String!
  onExplicitNonGenericSync: String!
  onExplicitSync: String!
  onArguments(arg: String!): String!
}

type Query {
  a: String
}
```
