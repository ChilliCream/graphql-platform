# Viewer_Bug_2

## Result

```json
{
  "data": {
    "recentTestimonial": {
      "__typename": "Testimonial"
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
{
  recentTestimonial {
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
F5E90A50E9F1CBCA4CF84D4D2A5620AD9DC96B20
```

## QueryPlan

```json
{
  "document": "{ recentTestimonial { __typename } viewer { subType { subgraphB } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query fetch_recentTestimonial_viewer_1 { recentTestimonial { __typename } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_recentTestimonial_viewer_2 { viewer { subType { subgraphB } } }",
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

