# Viewer_Bug_1

## Result

```json
{
  "data": {
    "recentTestimonial": {
      "__typename": "Testimonial"
    },
    "viewer": {
      "exclusiveSubgraphB": "string"
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
    exclusiveSubgraphB
  }
}
```

## QueryPlan Hash

```text
4A67F4C0933C907EC1B605E7F1DB3F91C019E5A6
```

## QueryPlan

```json
{
  "document": "{ recentTestimonial { __typename } viewer { exclusiveSubgraphB } }",
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
            "document": "query fetch_recentTestimonial_viewer_2 { viewer { exclusiveSubgraphB } }",
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

