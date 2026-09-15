# ResolveNodeBatch_Should_Preserve_Positions_And_Separate_Aliases_When_Configured

## Result

```json
{
  "errors": [
    {
      "message": "The node ID string has an invalid format.",
      "path": [
        "malformed"
      ],
      "extensions": {
        "originalValue": "garbage"
      }
    }
  ],
  "data": {
    "nodes": [
      {
        "id": "QmF0Y2hFbnRpdHk6eA==",
        "name": "x"
      },
      {
        "id": "QmF0Y2hFbnRpdHk6eQ==",
        "name": "y"
      },
      {
        "id": "QmF0Y2hFbnRpdHk6eA==",
        "name": "x"
      }
    ],
    "alias": [
      {
        "name": "y"
      }
    ],
    "single": {
      "name": "x"
    },
    "malformed": null
  }
}
```

## Dispatch

```json
{
  "InvocationCount": 3,
  "BatchSizes": [
    1,
    1,
    3
  ],
  "Ids": [
    "x",
    "x",
    "x",
    "y",
    "y"
  ],
  "RegularPipeline": false,
  "BatchPipeline": true
}
```

## Schema

```graphql
schema {
  query: Query
}

type Query {
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
  ready: Boolean!
}

type BatchEntity implements Node {
  id: ID!
  name: String!
}

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}
```
