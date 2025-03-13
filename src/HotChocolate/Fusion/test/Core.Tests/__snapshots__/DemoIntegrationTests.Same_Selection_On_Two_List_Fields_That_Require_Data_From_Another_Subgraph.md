# Same_Selection_On_Two_List_Fields_That_Require_Data_From_Another_Subgraph

## Result

```json
{
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
      {
        "id": "4",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      },
      {
        "id": "5",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      },
      {
        "id": "6",
        "name": "string",
        "price": 123.456,
        "reviewCount": 123
      }
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

