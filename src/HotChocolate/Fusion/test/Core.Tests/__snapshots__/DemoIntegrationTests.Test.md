# Test

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 11,
          "column": 5
        }
      ],
      "path": [
        "productsB",
        2,
        "price"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 11,
          "column": 5
        }
      ],
      "path": [
        "productsB",
        1,
        "price"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 11,
          "column": 5
        }
      ],
      "path": [
        "productsB",
        0,
        "price"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "productsA": [
      {
        "id": "1",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      },
      {
        "id": "2",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      },
      {
        "id": "3",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      }
    ],
    "productsB": [
      null,
      null,
      null
    ]
  }
}
```

## Request

```graphql
{
  productsA {
    id
    name
    price
    reviewCount
  }
  productsB {
    id
    name
    price
    reviewCount
  }
}
```

## QueryPlan Hash

```text
57DE21D1B552226339A985FBFA65E0DA2E33703C
```

## QueryPlan

```json
{
  "document": "{ productsA { id name price reviewCount } productsB { id name price reviewCount } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_productsA_productsB_1 { productsA { id name __fusion_exports__1: id } productsB { id name __fusion_exports__2: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
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
        "type": "Parallel",
        "nodes": [
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productsA_productsB_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on Product { price reviewCount __fusion_exports__1: id } } }",
            "selectionSetId": 1,
            "path": [
              "nodes"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productsA_productsB_3($__fusion_exports__2: [ID!]!) { nodes(ids: $__fusion_exports__2) { ... on Product { price reviewCount __fusion_exports__2: id } } }",
            "selectionSetId": 2,
            "path": [
              "nodes"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1,
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

