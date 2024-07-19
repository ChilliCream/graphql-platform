# Query_Plan_10_Two_Mutation_Same_SubGraph

## UserRequest

```graphql
mutation AddReviews {
  a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
    review {
      body
      author {
        birthdate
      }
    }
  }
  b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
    review {
      body
      author {
        id
        birthdate
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "mutation AddReviews { a: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { birthdate } } } b: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { id birthdate } } } }",
  "operation": "AddReviews",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "mutation AddReviews_1 { a: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { __fusion_exports__1: id } } } b: addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { id __fusion_exports__2: id } } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
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
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query AddReviews_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { birthdate } }",
            "selectionSetId": 5,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query AddReviews_3($__fusion_exports__2: ID!) { userById(id: $__fusion_exports__2) { birthdate } }",
            "selectionSetId": 6,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          5,
          6
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id"
  }
}
```

