# Selections_On_Interface_Field_And_Concrete_Type_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "authorable": {
      "author": {
        "id": "1",
        "displayName": "string"
      },
      "title": "string"
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
    ... on Discussion {
      title
    }
  }
}
```

## QueryPlan Hash

```text
5722F5134A038091DDEBF004D5733DF1141AD163
```

## QueryPlan

```json
{
  "document": "query testQuery { authorable { author { id displayName } ... on Discussion { title } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query testQuery_1 { authorable { __typename ... on Discussion { author { id __fusion_exports__1: id } title } ... on Comment { author { id __fusion_exports__1: id } } } }",
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

