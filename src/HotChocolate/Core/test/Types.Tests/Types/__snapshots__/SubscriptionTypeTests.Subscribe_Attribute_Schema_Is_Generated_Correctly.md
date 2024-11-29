# Subscribe_Attribute_Schema_Is_Generated_Correctly

```graphql
schema {
  query: Query
  mutation: MyMutation
  subscription: MySubscription
}

type MyMutation {
  writeBoolean(userId: String! message: Boolean!): Boolean!
  writeMessage(userId: String! message: String!): String!
  writeSysMessage(message: String!): String!
  writeFixedMessage(message: String!): String!
  writeOnInferTopic(message: String!): String!
  writeOnExplicit(message: String!): String!
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
