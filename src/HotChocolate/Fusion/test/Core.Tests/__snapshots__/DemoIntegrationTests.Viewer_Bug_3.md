# Viewer_Bug_3

## Result

```json
{
  "errors": [
    {
      "message": "The field `viewer` does not exist on the type `Viewer`.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "viewer"
      ],
      "extensions": {
        "type": "Viewer",
        "field": "viewer",
        "responseName": "viewer",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 4,
          "column": 5
        }
      ],
      "path": [
        "viewer",
        "subgraphB"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": null
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
89E310ABAAB1592F3550B4B6772D3D740DACBD2B
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
            "document": "query fetch_viewer_subgraphC_3 { viewer { viewer { subgraphB } } }",
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

