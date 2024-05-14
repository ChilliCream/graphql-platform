# Query_Plan_09_Two_Mutation_Same_SubGraph

## UserRequest

```graphql
mutation AddReviews {
  a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
    review {
      body
      author {
        name
      }
    }
  }
  b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
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
  "document": "mutation AddReviews { a: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } b: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } }",
  "operation": "AddReviews",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "mutation AddReviews_1 { a: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } b: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { name } } } }",
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

