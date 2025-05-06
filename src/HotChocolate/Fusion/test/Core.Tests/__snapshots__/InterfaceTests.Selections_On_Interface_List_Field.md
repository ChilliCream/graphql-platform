# Selections_On_Interface_List_Field

## Result

```json
{
  "data": {
    "votables": [
      {
        "viewerCanVote": true
      },
      {
        "viewerCanVote": true
      },
      {
        "viewerCanVote": true
      }
    ]
  }
}
```

## Request

```graphql
query testQuery {
  votables {
    viewerCanVote
  }
}
```

## QueryPlan Hash

```text
7EFD46AEA82988A46595243F7DCA1E17868E19D7
```

## QueryPlan

```json
{
  "document": "query testQuery { votables { viewerCanVote } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { votables { __typename ... on Discussion { viewerCanVote } ... on Comment { viewerCanVote } } }",
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

