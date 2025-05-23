# Selections_On_Interface_On_Node_Field

## Result

```json
{
  "data": {
    "node": {
      "viewerCanVote": true
    }
  }
}
```

## Request

```graphql
query testQuery($id: ID!) {
  node(id: $id) {
    ... on Votable {
      viewerCanVote
    }
  }
}
```

## QueryPlan Hash

```text
3CF058C093DFE8590D606A05C32269BE73512B34
```

## QueryPlan

```json
{
  "document": "query testQuery($id: ID!) { node(id: $id) { ... on Votable { viewerCanVote } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "node",
        "branches": [
          {
            "type": "Discussion",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query testQuery_1($id: ID!) { node(id: $id) { ... on Discussion { viewerCanVote __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Comment",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query testQuery_2($id: ID!) { node(id: $id) { ... on Comment { viewerCanVote __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
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

