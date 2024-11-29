# Query_Plan_11_Two_Mutation_Two_SubGraph

## UserRequest

```graphql
mutation AddReviewAndUser {
  addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
    review {
      body
      author {
        id
        birthdate
      }
    }
  }
  addUser(input: { name: "foo", username: "foo", birthdate: "abc" }) {
    user {
      name
      reviews {
        body
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "mutation AddReviewAndUser { addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { id birthdate } } } addUser(input: { name: \u0022foo\u0022, username: \u0022foo\u0022, birthdate: \u0022abc\u0022 }) { user { name reviews { body } } } }",
  "operation": "AddReviewAndUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "mutation AddReviewAndUser_1 { addReview(input: { body: \u0022foo\u0022, authorId: 1, upc: 1 }) { review { body author { id __fusion_exports__1: id } } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "mutation AddReviewAndUser_2 { addUser(input: { name: \u0022foo\u0022, username: \u0022foo\u0022, birthdate: \u0022abc\u0022 }) { user { name __fusion_exports__2: id } } }",
        "selectionSetId": 0,
        "provides": [
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
            "document": "query AddReviewAndUser_3($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { birthdate } }",
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
            "subgraph": "Reviews",
            "document": "query AddReviewAndUser_4($__fusion_exports__2: ID!) { authorById(id: $__fusion_exports__2) { reviews { body } } }",
            "selectionSetId": 4,
            "path": [
              "authorById"
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
          4,
          5
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

