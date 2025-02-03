# BatchExecutionState_With_Multiple_Variable_Values_And_Forwarded_Variable

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
query($arg1: String, $arg2: String) {
  userBySlug(slug: "me") {
    feedbacks {
      edges {
        node {
          feedback {
            buyer {
              relativeUrl(arg: $arg1)
              displayName(arg: $arg2)
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
B6CBB5EB035C7E54DB6B10176B9A4CB57D6E1DD8
```

## QueryPlan

```json
{
  "document": "query($arg1: String, $arg2: String) { userBySlug(slug: \u0022me\u0022) { feedbacks { edges { node { feedback { buyer { relativeUrl(arg: $arg1) displayName(arg: $arg2) } } } } } } }",
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
        "document": "query fetch_userBySlug_4($__fusion_exports__3: [ID!]!, $arg1: String) { nodes(ids: $__fusion_exports__3) { ... on User { relativeUrl(arg: $arg1) __fusion_exports__2: id __fusion_exports__3: id } } }",
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
        ],
        "forwardedVariables": [
          {
            "variable": "arg1"
          }
        ]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Subgraph_1",
        "document": "query fetch_userBySlug_3($__fusion_exports__2: [ID!]!, $arg2: String) { nodes(ids: $__fusion_exports__2) { ... on User { displayName(arg: $arg2) __fusion_exports__2: id } } }",
        "selectionSetId": 6,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__2"
          }
        ],
        "forwardedVariables": [
          {
            "variable": "arg2"
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

