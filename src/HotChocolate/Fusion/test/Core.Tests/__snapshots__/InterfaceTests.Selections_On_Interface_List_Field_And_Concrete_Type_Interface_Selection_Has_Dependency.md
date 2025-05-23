# Selections_On_Interface_List_Field_And_Concrete_Type_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "authorables": [
      {
        "author": {
          "id": "3",
          "displayName": "string"
        },
        "title": "string"
      },
      {
        "author": {
          "id": "2",
          "displayName": "string"
        },
        "title": "string"
      },
      {
        "author": {
          "id": "1",
          "displayName": "string"
        },
        "title": "string"
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
    ... on Discussion {
      title
    }
  }
}
```

## QueryPlan Hash

```text
5B15C51B0C560FDA557AEC4D18F7435E73169D29
```

## QueryPlan

```json
{
  "document": "query testQuery { authorables { author { id displayName } ... on Discussion { title } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { authorables { __typename ... on Discussion { author { id __fusion_exports__1: id } title } ... on Comment { author { id __fusion_exports__1: id } } } }",
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

