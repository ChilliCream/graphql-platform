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
934EF521F28696C6C6BD91A930219DA09867A536
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
            "type": "Product",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query ProductReviews_1($first: Int!, $id: ID!) { node(id: $id) { ... on Product { id repeat(num: $first) __typename } } }",
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
          },
          {
            "type": "ProductBookmark",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query ProductReviews_2($id: ID!) { node(id: $id) { ... on ProductBookmark { __typename } } }",
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
            "type": "Review",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query ProductReviews_4($id: ID!) { node(id: $id) { ... on Review { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "User",
            "node": {
              "type": "Resolve",
              "subgraph": "Accounts",
              "document": "query ProductReviews_5($id: ID!) { node(id: $id) { ... on User { __typename } } }",
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
      }
    ]
  }
}
```

