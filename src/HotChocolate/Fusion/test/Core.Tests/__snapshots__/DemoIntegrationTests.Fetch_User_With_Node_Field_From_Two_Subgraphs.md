# Fetch_User_With_Node_Field_From_Two_Subgraphs

## Result

```json
{
  "data": {
    "node": {
      "birthdate": "1815-12-10",
      "reviews": [
        {
          "body": "Love it!"
        },
        {
          "body": "Could be better."
        }
      ]
    }
  }
}
```

## Request

```graphql
query FetchNode($id: ID!) {
  node(id: $id) {
    ... on User {
      birthdate
      reviews {
        body
      }
    }
  }
}
```

## QueryPlan Hash

```text
5D785409A20DB1C3613ACAA7F705728C826E966E
```

## QueryPlan

```json
{
  "document": "query FetchNode($id: ID!) { node(id: $id) { ... on User { birthdate reviews { body } } } }",
  "operation": "FetchNode",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "node",
        "branches": [
          {
            "type": "User",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query FetchNode_1($id: ID!) { node(id: $id) { ... on User { reviews { body } __fusion_exports__1: id __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Review",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query FetchNode_2($id: ID!) { node(id: $id) { ... on Review { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "ProductConfiguration",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_3($id: ID!) { node(id: $id) { ... on ProductConfiguration { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "ProductBookmark",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_4($id: ID!) { node(id: $id) { ... on ProductBookmark { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Product",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_5($id: ID!) { node(id: $id) { ... on Product { __typename } } }",
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
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query FetchNode_6($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { birthdate } }",
        "selectionSetId": 1,
        "path": [
          "userById"
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
    "__fusion_exports__1": "User_id"
  }
}
```

