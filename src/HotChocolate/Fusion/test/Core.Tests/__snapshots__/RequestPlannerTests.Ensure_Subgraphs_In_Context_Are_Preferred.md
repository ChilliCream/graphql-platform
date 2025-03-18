# Ensure_Subgraphs_In_Context_Are_Preferred

## UserRequest

```graphql
{
  subgraph3Foo {
    name
  }
}
```

## QueryPlan

```json
{
  "document": "{ subgraph3Foo { name } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_3",
        "document": "query fetch_subgraph3Foo_1 { subgraph3Foo { __fusion_exports__1: code } }",
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
        "subgraph": "Subgraph_1",
        "document": "query fetch_subgraph3Foo_2($__fusion_exports__1: String!) { fooByCode(code: $__fusion_exports__1) { name } }",
        "selectionSetId": 1,
        "path": [
          "fooByCode"
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
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Foo_code"
  }
}
```

