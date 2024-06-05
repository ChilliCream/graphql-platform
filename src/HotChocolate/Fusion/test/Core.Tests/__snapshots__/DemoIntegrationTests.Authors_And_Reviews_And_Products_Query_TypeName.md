# Authors_And_Reviews_And_Products_Query_TypeName

## Result

```json
{
  "data": {
    "__typename": "Query",
    "topProducts": [
      {
        "__typename": "Product",
        "reviews": [
          {
            "__typename": "Review",
            "author": {
              "__typename": "User"
            }
          },
          {
            "__typename": "Review",
            "author": {
              "__typename": "User"
            }
          }
        ]
      },
      {
        "__typename": "Product",
        "reviews": [
          {
            "__typename": "Review",
            "author": {
              "__typename": "User"
            }
          }
        ]
      }
    ]
  }
}
```

## Request

```graphql
query TopProducts {
  __typename
  topProducts(first: 2) {
    __typename
    reviews {
      __typename
      author {
        __typename
      }
    }
  }
}
```

## QueryPlan Hash

```text
5262A2743338E9E565B8B4DEDFB63E8DC8AE2FC4
```

## QueryPlan

```json
{
  "document": "query TopProducts { __typename topProducts(first: 2) { __typename reviews { __typename author { __typename } } } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Introspect",
            "document": "{ __typename }"
          },
          {
            "type": "Resolve",
            "subgraph": "Products",
            "document": "query TopProducts_1 { topProducts(first: 2) { __typename __fusion_exports__1: id } }",
            "selectionSetId": 0,
            "provides": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
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
        "subgraph": "Reviews2",
        "document": "query TopProducts_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { reviews { __typename author { __typename } } } }",
        "selectionSetId": 1,
        "path": [
          "productById"
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
    "__fusion_exports__1": "Product_id"
  }
}
```

