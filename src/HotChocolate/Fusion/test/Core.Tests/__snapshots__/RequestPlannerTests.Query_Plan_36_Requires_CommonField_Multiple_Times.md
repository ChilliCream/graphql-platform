# Query_Plan_36_Requires_CommonField_Multiple_Times

## UserRequest

```graphql
query Requires {
  users {
    id
    username
    productConfigurationByUsername {
      id
    }
    productBookmarkByUsername {
      id
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query Requires { users { id username productConfigurationByUsername { id } productBookmarkByUsername { id } } }",
  "operation": "Requires",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query Requires_1 { users { id username __fusion_exports__1: username } }",
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
        "subgraph": "Products",
        "document": "query Requires_2($__fusion_exports__1: String!) { productConfigurationByUsername(username: $__fusion_exports__1) { id } productBookmarkByUsername(username: $__fusion_exports__1) { id } }",
        "selectionSetId": 1,
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_username"
  }
}
```

