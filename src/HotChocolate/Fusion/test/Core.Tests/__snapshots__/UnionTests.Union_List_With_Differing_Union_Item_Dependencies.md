# Union_List_With_Differing_Union_Item_Dependencies

## Result

```json
{
  "data": {
    "listOfUnion": [
      {
        "__typename": "Item1",
        "something": "Something",
        "product": {
          "id": "UHJvZHVjdDox",
          "name": "Product_1"
        }
      },
      {
        "__typename": "Item2",
        "other": 123,
        "product": {
          "name": "Product_2"
        }
      },
      {
        "__typename": "Item3",
        "another": true,
        "review": {
          "id": "UmV2aWV3OjM=",
          "score": 3
        }
      },
      {
        "__typename": "Item1",
        "something": "Something",
        "product": {
          "id": "UHJvZHVjdDo0",
          "name": "Product_4"
        }
      },
      {
        "__typename": "Item2",
        "other": 123,
        "product": {
          "name": "Product_5"
        }
      },
      {
        "__typename": "Item3",
        "another": true,
        "review": {
          "id": "UmV2aWV3OjY=",
          "score": 1
        }
      }
    ]
  }
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
6F3D15770F165F5A7166C5598F4B1A7D6910A88D
```

## QueryPlan

```json
{
  "document": "{ listOfUnion { __typename ... on Item1 { something product { id name } } ... on Item2 { other product { name } } ... on Item3 { another review { id score } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_listOfUnion_1 { listOfUnion { __typename ... on Item3 { __typename another review { id __fusion_exports__1: id } } ... on Item2 { __typename other product { __fusion_exports__2: id } } ... on Item1 { __typename something product { id __fusion_exports__3: id } } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
          },
          {
            "variable": "__fusion_exports__3"
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
            "document": "query fetch_listOfUnion_4($__fusion_exports__3: [ID!]!) { nodes(ids: $__fusion_exports__3) { ... on Product { name __fusion_exports__3: id } } }",
            "selectionSetId": 6,
            "path": [
              "nodes"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__3"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          4,
          5,
          6
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Review_id",
    "__fusion_exports__2": "Product_id",
    "__fusion_exports__3": "Product_id"
  }
}
```

