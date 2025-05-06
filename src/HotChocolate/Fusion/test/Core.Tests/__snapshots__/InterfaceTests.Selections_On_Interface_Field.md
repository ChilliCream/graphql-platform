# Selections_On_Interface_Field

## Result

```json
{
  "data": {
    "votable": {
      "viewerCanVote": true
    }
  }
}
```

## Request

```graphql
query testQuery {
  votable {
    viewerCanVote
  }
}
```

## QueryPlan Hash

```text
65545BDD897AE06E71508E6C5BDF6F3E24513EBC
```

## QueryPlan

```json
{
  "document": "query testQuery { votable { viewerCanVote } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { votable { __typename ... on Discussion { viewerCanVote } ... on Comment { viewerCanVote } } }",
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

