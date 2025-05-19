# Selections_On_Interface_And_Concrete_Type_On_Node_Field_Interface_Selection_Has_Dependency

## Result

```json
{
  "data": {
    "node": {
      "__typename": "Item2",
      "id": "SXRlbTI6MQ==",
      "products": [
        {
          "id": "2",
          "name": "string"
        },
        {
          "id": "3",
          "name": "string"
        },
        {
          "id": "4",
          "name": "string"
        }
      ],
      "singularProduct": {
        "name": "string"
      }
    }
  }
}
```

## Request

```graphql
query testQuery($id: ID!) {
  node(id: $id) {
    __typename
    id
    ... on ProductList {
      products {
        id
        name
      }
    }
    ... on Item2 {
      singularProduct {
        name
      }
    }
  }
}
```

## QueryPlan Hash

```text
D7ECED31E24122116A43B641BB4FE1FFDC849F00
```

## QueryPlan

```json
{
  "document": "query testQuery($id: ID!) { node(id: $id) { __typename id ... on ProductList { products { id name } } ... on Item2 { singularProduct { name } } } }",
  "operation": "testQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "node",
        "branches": [
          {
            "type": "Product",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query testQuery_1($id: ID!) { node(id: $id) { ... on Product { __typename id } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Item2",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query testQuery_2($id: ID!) { node(id: $id) { ... on Item2 { __typename id products { id __fusion_exports__1: id } singularProduct { __fusion_exports__2: id } } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Item1",
            "node": {
              "type": "Resolve",
              "subgraph": "Subgraph_1",
              "document": "query testQuery_3($id: ID!) { node(id: $id) { ... on Item1 { __typename id products { id } } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
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
            "document": "query testQuery_4($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on Product { name __fusion_exports__1: id } } }",
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
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query testQuery_5($__fusion_exports__2: ID!) { node(id: $__fusion_exports__2) { ... on Product { name } } }",
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
    "__fusion_exports__1": "Product_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

