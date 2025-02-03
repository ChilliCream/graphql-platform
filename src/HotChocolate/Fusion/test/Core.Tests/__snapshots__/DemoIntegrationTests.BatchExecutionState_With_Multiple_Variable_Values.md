# BatchExecutionState_With_Multiple_Variable_Values

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 9,
          "column": 15
        }
      ],
      "path": [
        "userBySlug",
        "feedbacks",
        "edges",
        2,
        "node",
        "feedback",
        "buyer",
        "displayName"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 9,
          "column": 15
        }
      ],
      "path": [
        "userBySlug",
        "feedbacks",
        "edges",
        1,
        "node",
        "feedback",
        "buyer",
        "displayName"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 9,
          "column": 15
        }
      ],
      "path": [
        "userBySlug",
        "feedbacks",
        "edges",
        0,
        "node",
        "feedback",
        "buyer",
        "displayName"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Unexpected Execution Error"
    }
  ],
  "data": {
    "userBySlug": {
      "feedbacks": {
        "edges": [
          {
            "node": {
              "feedback": {
                "buyer": null
              }
            }
          },
          {
            "node": {
              "feedback": {
                "buyer": null
              }
            }
          },
          {
            "node": {
              "feedback": {
                "buyer": null
              }
            }
          }
        ]
      }
    }
  }
}
```

## Request

```graphql
{
  userBySlug(slug: "me") {
    feedbacks {
      edges {
        node {
          feedback {
            buyer {
              relativeUrl
              displayName
            }
          }
        }
      }
    }
  }
}
```

## QueryPlan Hash

```text
ECA485AE6695B779A8D5B148BC2D37D52EA81380
```

## QueryPlan

```json
{
  "document": "{ userBySlug(slug: \u0022me\u0022) { feedbacks { edges { node { feedback { buyer { relativeUrl displayName } } } } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_2",
        "document": "query fetch_userBySlug_1 { userBySlug(slug: \u0022me\u0022) { __fusion_exports__1: id } }",
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
        "subgraph": "Subgraph_3",
        "document": "query fetch_userBySlug_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on User { feedbacks { edges { node { feedback { buyer { __fusion_exports__3: id } } } } } } } }",
        "selectionSetId": 1,
        "path": [
          "node"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
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
          1
        ]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Subgraph_2",
        "document": "query fetch_userBySlug_4($__fusion_exports__3: [ID!]!) { nodes(ids: $__fusion_exports__3) { ... on User { relativeUrl __fusion_exports__2: id __fusion_exports__3: id } } }",
        "selectionSetId": 6,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__3"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Subgraph_1",
        "document": "query fetch_userBySlug_3($__fusion_exports__2: [ID!]!) { nodes(ids: $__fusion_exports__2) { ... on User { displayName __fusion_exports__2: id } } }",
        "selectionSetId": 6,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          6
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id",
    "__fusion_exports__3": "User_id"
  }
}
```

