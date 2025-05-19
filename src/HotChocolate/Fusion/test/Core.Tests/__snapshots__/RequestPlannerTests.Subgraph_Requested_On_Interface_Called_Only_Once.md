# Subgraph_Requested_On_Interface_Called_Only_Once

## UserRequest

```graphql
{
  books {
    author {
      name
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ books { author { name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_books_1 { books { __typename ... on ScaryBook { author { __fusion_exports__1: id } } ... on FunnyBook { author { __fusion_exports__1: id } } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Subgraph_2",
        "document": "query fetch_books_2($__fusion_exports__1: [Int!]!) { authorById(id: $__fusion_exports__1) { name __fusion_exports__1: id } }",
        "selectionSetId": 3,
        "path": [
          "authorById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          3
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Author_id"
  }
}
```

