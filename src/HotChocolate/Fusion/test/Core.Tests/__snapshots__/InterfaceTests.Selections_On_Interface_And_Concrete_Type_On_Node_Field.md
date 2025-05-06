# Selections_On_Interface_And_Concrete_Type_On_Node_Field

## Result

```json
{
  "data": {
    "node": {
      "viewerCanVote": true,
      "viewerRating": 123.456
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
    ... on Discussion {
      viewerRating
    }
  }
}
```

## QueryPlan Hash

```text
28F6A3D397F20CDEEAB3F651FE185706BD12CA9C
```

## QueryPlan

```json
{
  "document": "query testQuery($id: ID!) { node(id: $id) { ... on Votable { viewerCanVote } ... on Discussion { viewerRating } } }",
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
              "document": "query testQuery_1($id: ID!) { node(id: $id) { ... on Discussion { viewerCanVote viewerRating __typename } } }",
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

