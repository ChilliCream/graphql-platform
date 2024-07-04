# Authors_And_Reviews_And_Products_Query_TopProducts

## Result

```json
{
  "data": {
    "topProducts": [
      {
        "name": "Table",
        "reviews": [
          {
            "body": "Love it!",
            "author": {
              "name": "@ada"
            }
          },
          {
            "body": "Prefer something else.",
            "author": {
              "name": "@alan"
            }
          }
        ]
      },
      {
        "name": "Couch",
        "reviews": [
          {
            "body": "Too expensive.",
            "author": {
              "name": "@alan"
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
  topProducts(first: 2) {
    name
    reviews {
      body
      author {
        name
      }
    }
  }
}
```

## QueryPlan Hash

```text
C5341AF7D1F79A5A4C17FED3BE9B194FBF30C918
```

## QueryPlan

```json
{
  "document": "query TopProducts { topProducts(first: 2) { name reviews { body author { name } } } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query TopProducts_1 { topProducts(first: 2) { name __fusion_exports__1: id } }",
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
        "subgraph": "Reviews2",
        "document": "query TopProducts_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { reviews { body author { name } } } }",
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

