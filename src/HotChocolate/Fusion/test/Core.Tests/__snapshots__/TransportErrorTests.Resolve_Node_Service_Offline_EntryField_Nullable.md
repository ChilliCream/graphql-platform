# Resolve_Node_Service_Offline_EntryField_Nullable

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "node"
      ]
    }
  ],
  "data": {
    "node": null
  }
}
```

## Request

```graphql
{
  node(id: "QnJhbmQ6MQ==") {
    id
    ... on Brand {
      name
    }
  }
}
```

## QueryPlan Hash

```text
32501CA9B2CFE1072BCA51CC37D3C65085CC9CB5
```

## QueryPlan

```json
{
  "document": "{ node(id: \u0022QnJhbmQ6MQ==\u0022) { id ... on Brand { name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "node",
        "branches": [
          {
            "type": "Brand",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query fetch_node_1 { node(id: \u0022QnJhbmQ6MQ==\u0022) { ... on Brand { id name __typename } } }",
              "selectionSetId": 0
            }
          }
        ]
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

