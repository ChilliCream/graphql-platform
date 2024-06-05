# Query_Plan_27_Multiple_Require_Steps_From_Same_Subgraph

## UserRequest

```graphql
query Query {
  authorById(id: "1") {
    id
    name
    bio
    books {
      id
      author {
        books {
          id
        }
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query Query { authorById(id: \u00221\u0022) { id name bio books { id author { books { id } } } } }",
  "operation": "Query",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Authors",
        "document": "query Query_1 { authorById(id: \u00221\u0022) { id name bio __fusion_exports__1: id } }",
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
        "type": "Resolve",
        "subgraph": "Books",
        "document": "query Query_2($__fusion_exports__1: String!) { authorById(id: $__fusion_exports__1) { books { id __fusion_exports__2: authorId } } }",
        "selectionSetId": 1,
        "path": [
          "authorById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Authors",
        "document": "query Query_3($__fusion_exports__2: String!) { bookByAuthorId(authorId: $__fusion_exports__2) { author { __fusion_exports__3: id } } }",
        "selectionSetId": 2,
        "path": [
          "bookByAuthorId"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__2"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__3"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Books",
        "document": "query Query_4($__fusion_exports__3: String!) { authorById(id: $__fusion_exports__3) { books { id } } }",
        "selectionSetId": 3,
        "path": [
          "authorById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__3"
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
    "__fusion_exports__1": "Author_id",
    "__fusion_exports__2": "Book_authorId",
    "__fusion_exports__3": "Author_id"
  }
}
```

