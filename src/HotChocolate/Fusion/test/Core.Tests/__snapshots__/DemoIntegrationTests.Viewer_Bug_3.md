# Viewer_Bug_3

## Result

```json
{
  "data": {
    "viewer": {
      "subgraphA": "string",
      "subgraphB": "string"
    },
    "subgraphC": {
      "someField": "string",
      "anotherField": "string"
    }
  }
}
```

## Request

```graphql
{
  viewer {
    subgraphA
    subgraphB
  }
  subgraphC {
    someField
    anotherField
  }
}
```

## QueryPlan Hash

```text
0343522DE0DDC505C43E45B0E95A7AED449C0B87
```

## QueryPlan

```json
{
  "document": "{ viewer { subgraphA subgraphB } subgraphC { someField anotherField } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_3",
            "document": "query fetch_viewer_subgraphC_1 { subgraphC { someField anotherField } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query fetch_viewer_subgraphC_2 { viewer { subgraphA } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_viewer_subgraphC_3 { viewer { subgraphB } }",
            "selectionSetId": 0
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

