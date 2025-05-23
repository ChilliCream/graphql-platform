# Selections_On_Interface_List_Field_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "authorables": [
      {
        "author": {
          "id": "3",
          "displayName": "string"
        }
      },
      {
        "author": {
          "id": "2",
          "displayName": "string"
        }
      },
      {
        "author": {
          "id": "1",
          "displayName": "string"
        }
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
0308D4AF6E31B24ABE6DE11C06361B922371BB60
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

