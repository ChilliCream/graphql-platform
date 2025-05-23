# Selections_On_Interface_Field_And_Concrete_Type

## Result

```json
{
  "data": {
    "votable": {
      "viewerCanVote": true,
      "title": "string"
    }
  }
}
```

## Request

```graphql
query testQuery {
  votable {
    viewerCanVote
    ... on Discussion {
      title
    }
  }
}
```

## QueryPlan Hash

```text
1CA7C10478F418DC3CF3F0896AD2C6F907424B4C
```

## QueryPlan

```json
{
  "document": "query testQuery { votable { viewerCanVote ... on Discussion { title } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { votable { __typename ... on Discussion { viewerCanVote title } ... on Comment { viewerCanVote } } }",
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

