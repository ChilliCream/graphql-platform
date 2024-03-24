# Resolve_ResolveByKey_Sequence_Second_Service_Offline_Field_NonNull

## User Request

```graphql
{
  reviews {
    body
    author! {
      birthdate
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 7
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
          "column": 7
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
          "column": 7
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
          "column": 7
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
  "data": null
}
```

## QueryPlan

```json
{
  "document": "{ reviews { body author! { birthdate } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "query fetch_reviews_1 { reviews { body author! { __fusion_exports__1: id } } }",
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
        "document": "query fetch_reviews_2($__fusion_exports__1: [ID!]!) { usersById(ids: $__fusion_exports__1) { birthdate __fusion_exports__1: id } }",
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

