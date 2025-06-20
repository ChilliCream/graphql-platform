# Viewer_Bug_2

## Result

```json
{
  "data": {
    "exclusiveSubgraphA": {
      "__typename": "ExclusiveSubgraphA"
    },
    "viewer": {
      "subType": {
        "subgraphB": "string"
      }
    }
  }
}
```

## Request

```graphql
query testQuery {
  exclusiveSubgraphA {
    __typename
  }
  viewer {
    subType {
      subgraphB
    }
  }
}
```

## QueryPlan Hash

```text
9D04321969F1B9D45B0322BFB1B85DD104943530
```

## QueryPlan

```json
{
  "document": "query testQuery { exclusiveSubgraphA { __typename } viewer { subType { subgraphB } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query testQuery_1 { exclusiveSubgraphA { __typename } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query testQuery_2 { viewer { subType { subgraphB } } }",
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

