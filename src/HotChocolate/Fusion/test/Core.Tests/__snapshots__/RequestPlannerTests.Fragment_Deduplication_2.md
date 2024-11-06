# Fragment_Deduplication_2

## UserRequest

```graphql
{
  viewer {
    unionField {
      ... on Object1 {
        __typename
        someField
      }
    }
    unionField {
      __typename
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { unionField { ... on Object1 { __typename someField } } unionField { __typename } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_viewer_1 { viewer { unionField { __typename ... on Object2 { __typename } ... on Object1 { __typename someField } } } }",
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

