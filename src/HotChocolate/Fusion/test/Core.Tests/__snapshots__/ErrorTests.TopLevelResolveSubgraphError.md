# TopLevelResolveSubgraphError

## User Request

```graphql
{
  viewer {
    data {
      accountValue
    }
  }
  errorField
}
```

## Result

```json
{
  "errors": [
    {
      "message": "SOME TOP LEVEL USER ERROR",
      "path": [
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "errorField"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 68
          }
        ]
      }
    }
  ],
  "data": {
    "viewer": {
      "data": {
        "accountValue": "Account"
      }
    },
    "errorField": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { data { accountValue } } errorField }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_viewer_errorField_1 { viewer { data { accountValue } } errorField }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

## QueryPlan Hash

```text
9578FF2608B68C6D9AE96CD13B57F603C4554FFF
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Accounts", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Accounts", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Accounts", types: [ "User" ]) {
  query: Query
  mutation: Mutation
}

type Query {
  errorField: String
    @resolver(subgraph: "Accounts", select: "{ errorField }")
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  userById(id: ID!): User
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ userById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  users: [User!]!
    @resolver(subgraph: "Accounts", select: "{ users }")
  usersById(ids: [ID!]!): [User!]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ usersById(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  viewer: Viewer!
    @resolver(subgraph: "Accounts", select: "{ viewer }")
}

type Mutation {
  addUser(input: AddUserInput!): AddUserPayload!
    @variable(subgraph: "Accounts", name: "input", argument: "input")
    @resolver(subgraph: "Accounts", select: "{ addUser(input: $input) }", arguments: [ { name: "input", type: "AddUserInput!" } ])
}

type AddUserPayload {
  user: User
    @source(subgraph: "Accounts")
}

type SomeData {
  accountValue: String!
    @source(subgraph: "Accounts")
}

type User implements Node
  @variable(subgraph: "Accounts", name: "User_id", select: "id")
  @resolver(subgraph: "Accounts", select: "{ userById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ usersById(ids: $User_id) }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  birthdate: Date!
    @source(subgraph: "Accounts")
  errorField: String
    @source(subgraph: "Accounts")
  id: ID!
    @source(subgraph: "Accounts")
  name: String!
    @source(subgraph: "Accounts")
  username: String!
    @source(subgraph: "Accounts")
}

type Viewer {
  data: SomeData!
    @source(subgraph: "Accounts")
  user: User
    @source(subgraph: "Accounts")
}

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}

input AddUserInput {
  birthdate: Date!
  name: String!
  username: String!
}

"The `Date` scalar represents an ISO-8601 compliant date type."
scalar Date
```

