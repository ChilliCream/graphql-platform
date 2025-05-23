# Selections_On_Interface_Field_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "authorable": {
      "author": {
        "id": "1",
        "displayName": "string"
      }
    }
  }
}
```

## Request

```graphql
query testQuery {
  authorable {
    author {
      id
      displayName
    }
  }
}
```

## QueryPlan Hash

```text
E1E94C2534B99BC1CF6E9A80F111DBAE1A83C80C
```

## QueryPlan

```json
{
  "document": "query testQuery { authorable { author { id displayName } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { authorable { __typename ... on Discussion { author { id __fusion_exports__1: id } } ... on Comment { author { id __fusion_exports__1: id } } } }",
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
        "subgraph": "Subgraph_2",
        "document": "query testQuery_2($__fusion_exports__1: ID!) { authorById(id: $__fusion_exports__1) { displayName } }",
        "selectionSetId": 3,
        "path": [
          "authorById"
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

