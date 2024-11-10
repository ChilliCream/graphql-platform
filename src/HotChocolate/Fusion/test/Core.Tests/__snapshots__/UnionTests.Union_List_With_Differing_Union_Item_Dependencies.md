# Union_List_With_Differing_Union_Item_Dependencies

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 15,
          "column": 9
        }
      ],
      "path": [
        "listOfUnion",
        1,
        "product",
        "name"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 15,
          "column": 9
        }
      ],
      "path": [
        "listOfUnion",
        0,
        "product",
        "name"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": null
}
```

## Request

```graphql
{
  listOfUnion {
    __typename
    ... on Item1 {
      something
      product {
        id
        name
      }
    }
    ... on Item2 {
      other
      product {
        id
        name
      }
    }
    ... on Item3 {
      another
      review {
        id
        score
      }
    }
  }
}
```

## QueryPlan Hash

```text
51CA519135EDC9C49C71D22D7EF0562D417753EA
```

## QueryPlan

```json
{
  "document": "{ listOfUnion { __typename ... on Item1 { something product { id name } } ... on Item2 { other product { id name } } ... on Item3 { another review { id score } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_listOfUnion_1 { listOfUnion { __typename ... on Item3 { __typename another review { id __fusion_exports__1: id } } ... on Item2 { __typename other product { id __fusion_exports__2: id } } ... on Item1 { __typename something product { id __fusion_exports__2: id } } } }",
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
            "document": "query fetch_listOfUnion_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on Review { score __fusion_exports__1: id } } }",
            "selectionSetId": 4,
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
            "document": "query fetch_listOfUnion_3($__fusion_exports__2: [ID!]!) { nodes(ids: $__fusion_exports__2) { ... on Product { name __fusion_exports__2: id } } }",
            "selectionSetId": 5,
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
            "type": "ResolveByKeyBatch",
            "subgraph": "Subgraph_2",
            "document": "query fetch_listOfUnion_4($__fusion_exports__2: [ID!]!) { nodes(ids: $__fusion_exports__2) { ... on Product { name __fusion_exports__2: id } } }",
            "selectionSetId": 5,
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
          4,
          5
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Review_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

