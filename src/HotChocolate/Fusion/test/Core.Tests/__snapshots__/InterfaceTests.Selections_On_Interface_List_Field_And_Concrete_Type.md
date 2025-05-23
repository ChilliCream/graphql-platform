# Selections_On_Interface_List_Field_And_Concrete_Type

## Result

```json
{
  "data": {
    "votables": [
      {
        "viewerCanVote": true,
        "title": "string"
      },
      {
        "viewerCanVote": true,
        "title": "string"
      },
      {
        "viewerCanVote": true,
        "title": "string"
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
    ... on Discussion {
      title
    }
  }
}
```

## QueryPlan Hash

```text
4D65F3FA779E6BEF5CD4063FAC50D90B2479F33E
```

## QueryPlan

```json
{
  "document": "query testQuery { votables { viewerCanVote ... on Discussion { title } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { votables { __typename ... on Discussion { viewerCanVote title } ... on Comment { viewerCanVote } } }",
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

