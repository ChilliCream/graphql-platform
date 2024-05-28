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
802664ED0C849F707F3AA4098DE07E5D1D9E80DB
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
            "type": "User",
            "node": {
              "type": "Resolve",
              "subgraph": "Accounts",
              "document": "query ProductReviews_3($id: ID!) { node(id: $id) { ... on User { __typename } } }",
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

