# Union_Two_Branches_With_Differing_Resolve_Nodes_Item1

## Result

```json
{
  "data": {
    "union": {
      "something": "Something",
      "product": {
        "id": "UHJvZHVjdDox",
        "name": "Product_1"
      }
    }
  }
}
```

## Request

```graphql
{
  union(item: 1) {
    ... on Item1 {
      something
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
6162CA1B5815FE006F5BEC9B4A2F51035B1990DC
```

## QueryPlan

```json
{
  "document": "{ union(item: 1) { ... on Item1 { something product { id name } } ... on Item3 { another review { id score } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_union_1 { union(item: 1) { __typename ... on Item3 { another review { id __fusion_exports__1: id } } ... on Item2 {  } ... on Item1 { something product { id __fusion_exports__2: id } } } }",
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
            "type": "Resolve",
            "subgraph": "Subgraph_3",
            "document": "query fetch_union_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on Review { score } } }",
            "selectionSetId": 4,
            "path": [
              "node"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_union_3($__fusion_exports__2: ID!) { node(id: $__fusion_exports__2) { ... on Product { name } } }",
            "selectionSetId": 5,
            "path": [
              "node"
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

