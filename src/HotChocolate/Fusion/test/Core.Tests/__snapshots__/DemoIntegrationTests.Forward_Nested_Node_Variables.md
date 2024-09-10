# Forward_Nested_Node_Variables

## Result

```json
{
  "data": {
    "node": {
      "id": "UHJvZHVjdDox",
      "repeat": 1
    }
  }
}
```

## Request

```graphql
query ProductReviews($id: ID!, $first: Int!) {
  node(id: $id) {
    ... on Product {
      id
      repeat(num: $first)
    }
  }
}
```

## QueryPlan Hash

```text
3EB74A019DB95A4FA68CF9569951EA91659E7186
```

## QueryPlan

```json
{
  "document": "query ProductReviews($id: ID!, $first: Int!) { node(id: $id) { ... on Product { id repeat(num: $first) } } }",
  "operation": "ProductReviews",
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
              "subgraph": "Accounts",
              "document": "query ProductReviews_1($id: ID!) { node(id: $id) { ... on User { __typename } } }",
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
              "document": "query ProductReviews_2($id: ID!) { node(id: $id) { ... on Review { __typename } } }",
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
              "document": "query ProductReviews_3($id: ID!) { node(id: $id) { ... on ProductConfiguration { __typename } } }",
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
              "document": "query ProductReviews_4($id: ID!) { node(id: $id) { ... on ProductBookmark { __typename } } }",
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
              "document": "query ProductReviews_5($first: Int!, $id: ID!) { node(id: $id) { ... on Product { id repeat(num: $first) __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "first"
                },
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
      }
    ]
  }
}
```

