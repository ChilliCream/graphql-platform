# Compose_With_Tag

## Schema Document

```graphql
schema {
  query: Query
}

type Query {
  node(id: ID!): Node
  userById(id: ID!): User
  userByName(name: String!): User
  users(after: String before: String first: Int last: Int): UsersConnection
}

type PageInfo {
  endCursor: String
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
}

type User implements Node {
  birthdate: String!
  displayName: String!
  id: ID!
}

type UsersConnection {
  edges: [UsersEdge!]
  nodes: [User!]
  pageInfo: PageInfo!
}

type UsersEdge {
  cursor: String!
  node: User!
}

interface Node {
  id: ID!
}

scalar DateTime
```

## Fusion Graph Document

```graphql
schema @fusion(version: 1) @transport(subgraph: "accounts", group: "Fusion", location: "https:\/\/localhost:3000\/graphql", kind: "HTTP") @node(subgraph: "accounts", types: [ "User" ]) {
  query: Query
}

type Query {
  node(id: ID!): Node @variable(subgraph: "accounts", name: "id", argument: "id") @resolver(subgraph: "accounts", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  userById(id: ID!): User @variable(subgraph: "accounts", name: "id", argument: "id") @resolver(subgraph: "accounts", select: "{ userById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  userByName(name: String!): User @variable(subgraph: "accounts", name: "name", argument: "name") @resolver(subgraph: "accounts", select: "{ userByName(name: $name) }", arguments: [ { name: "name", type: "String!" } ])
  users(after: String before: String first: Int last: Int): UsersConnection @variable(subgraph: "accounts", name: "after", argument: "after") @variable(subgraph: "accounts", name: "before", argument: "before") @variable(subgraph: "accounts", name: "first", argument: "first") @variable(subgraph: "accounts", name: "last", argument: "last") @resolver(subgraph: "accounts", select: "{ users(after: $after, before: $before, first: $first, last: $last) }", arguments: [ { name: "after", type: "String" }, { name: "before", type: "String" }, { name: "first", type: "Int" }, { name: "last", type: "Int" } ])
}

type PageInfo {
  endCursor: String @source(subgraph: "accounts")
  hasNextPage: Boolean! @source(subgraph: "accounts")
  hasPreviousPage: Boolean! @source(subgraph: "accounts")
  startCursor: String @source(subgraph: "accounts")
}

type User implements Node @variable(subgraph: "accounts", name: "User_id", select: "id") @variable(subgraph: "accounts", name: "User_name", select: "name") @resolver(subgraph: "accounts", select: "{ userById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ]) @resolver(subgraph: "accounts", select: "{ userByName(name: $User_name) }", arguments: [ { name: "User_name", type: "String!" } ]) @resolver(subgraph: "accounts", select: "{ nodes(ids: $User_id) { ... on User { ... User } } }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  birthdate: String! @source(subgraph: "accounts")
  displayName: String! @source(subgraph: "accounts")
  id: ID! @source(subgraph: "accounts")
}

type UsersConnection {
  edges: [UsersEdge!] @source(subgraph: "accounts")
  nodes: [User!] @source(subgraph: "accounts")
  pageInfo: PageInfo! @source(subgraph: "accounts")
}

type UsersEdge {
  cursor: String! @source(subgraph: "accounts")
  node: User! @source(subgraph: "accounts")
}

interface Node {
  id: ID!
}

scalar DateTime
```

## accounts Subgraph Configuration

```json
{
  "Name": "accounts",
  "Schema": "type Query {\n  node(id: ID!): Node\n  nodes(ids: [ID!]!): [Node]!\n  userById(id: ID!): User\n  userByName(name: String!): User\n  users(first: Int after: String last: Int before: String): UsersConnection\n}\n\ntype User implements Node {\n  id: ID!\n  name: String! @tag(name: \"internal\")\n  displayName: String!\n  birthdate: String!\n}\n\ninterface Node {\n  id: ID!\n}\n\ntype UsersConnection {\n  pageInfo: PageInfo!\n  edges: [UsersEdge!]\n  nodes: [User!]\n}\n\ntype UsersEdge {\n  cursor: String!\n  node: User!\n}\n\ntype PageInfo {\n  hasNextPage: Boolean!\n  hasPreviousPage: Boolean!\n  startCursor: String\n  endCursor: String\n}\n\nscalar DateTime\n\ndirective @tag(name: String!) repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION",
  "Extensions": [],
  "Clients": [
    {
      "ClientName": null,
      "BaseAddress": "https://localhost:3000/graphql"
    }
  ],
  "ConfigurationExtensions": null
}
```

