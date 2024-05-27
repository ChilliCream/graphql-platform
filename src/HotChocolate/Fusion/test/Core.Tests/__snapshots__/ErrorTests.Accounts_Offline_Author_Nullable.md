# Accounts_Offline_Author_Nullable

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 13
        }
      ],
      "path": [
        "reviews",
        3,
        "author",
        "birthdate"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 13
        }
      ],
      "path": [
        "reviews",
        2,
        "author",
        "birthdate"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 13
        }
      ],
      "path": [
        "reviews",
        1,
        "author",
        "birthdate"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 13
        }
      ],
      "path": [
        "reviews",
        0,
        "author",
        "birthdate"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Internal Execution Error"
    }
  ],
  "data": {
    "reviews": [
      {
        "body": "Love it!",
        "author": null
      },
      {
        "body": "Too expensive.",
        "author": null
      },
      {
        "body": "Could be better.",
        "author": null
      },
      {
        "body": "Prefer something else.",
        "author": null
      }
    ]
  }
}
```

## Request

```graphql
query ReformatIds {
  reviews {
    body
    author? {
      birthdate
    }
  }
}
```

## QueryPlan Hash

```text
44090BD107B85A967BEBFFCA8844F840215A117F
```

## QueryPlan

```json
{
  "document": "query ReformatIds { reviews { body author? { birthdate } } }",
  "operation": "ReformatIds",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "query ReformatIds_1 { reviews { body author? { __fusion_exports__1: id } } }",
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
        "subgraph": "Accounts",
        "document": "query ReformatIds_2($__fusion_exports__1: [ID!]!) { usersById(ids: $__fusion_exports__1) { birthdate __fusion_exports__1: id } }",
        "selectionSetId": 2,
        "path": [
          "usersById"
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
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

