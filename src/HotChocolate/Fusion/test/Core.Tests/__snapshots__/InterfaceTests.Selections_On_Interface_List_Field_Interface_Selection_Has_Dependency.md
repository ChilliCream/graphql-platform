# Selections_On_Interface_List_Field_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "authorables": [
      {
        "author": {
          "id": "1",
          "displayName": "string"
        },
      },
      {
        "author": {
          "id": "1",
          "displayName": "string"
        },
      },
      {
        "author": {
          "id": "1",
          "displayName": "string"
        },
      }
    ]
  }
}
```

## Request

```graphql
query testQuery {
  authorables {
    author {
      id
      displayName
    }
  }
}
```

## QueryPlan Hash

```text
F4039AE7E0301C2C3A6B64A727FD0898C6ED5C3E
```

## QueryPlan

```json
{
  "document": "query testQuery { authorables { author { id displayName } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { authorables { __typename ... on Discussion { author { id __fusion_exports__1: id } } ... on Comment { author { id __fusion_exports__1: id } } } }",
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
        "type": "Parallel",
        "nodes": [
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Subgraph_2",
            "document": "query testQuery_2($__fusion_exports__1: [ID!]!) { authorsById(ids: $__fusion_exports__1) { displayName __fusion_exports__1: id } }",
            "selectionSetId": 3,
            "path": [
              "authorsById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Subgraph_2",
            "document": "query testQuery_3($__fusion_exports__1: [ID!]!) { authorsById(ids: $__fusion_exports__1) { displayName __fusion_exports__1: id } }",
            "selectionSetId": 3,
            "path": [
              "authorsById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
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

