# Query_Plan_08_Single_Mutation

## UserRequest

```graphql
mutation AddReview {
  addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
    review {
      body
      author {
        name
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "mutation AddReview { addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } }",
  "operation": "AddReview",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "mutation AddReview_1 { addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } }",
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

