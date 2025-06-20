# Viewer_Bug_1

## Result

```json
{
  "data": {
    "exclusiveSubgraphA": {
      "__typename": "ExclusiveSubgraphA"
    },
    "viewer": {
      "exclusiveSubgraphB": "string"
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
    exclusiveSubgraphB
  }
}
```

## QueryPlan Hash

```text
1A0ADF8B4DE457E0B7C85E08324A394F9F2FEB91
```

## QueryPlan

```json
{
  "document": "query testQuery { exclusiveSubgraphA { __typename } viewer { exclusiveSubgraphB } }",
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
            "document": "query testQuery_2 { viewer { exclusiveSubgraphB } }",
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

