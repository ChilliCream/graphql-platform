# Forward_Nested_Variables

## Result

```json
{
  "data": {
    "productById": {
      "id": "UHJvZHVjdDox",
      "repeat": 1
    }
  }
}
```

## Request

```graphql
query ProductReviews($id: ID!, $first: Int!) {
  productById(id: $id) {
    id
    repeat(num: $first)
  }
}
```

## QueryPlan Hash

```text
CAA1FE80B0D6C630C75664DB34FD91624B849F27
```

## QueryPlan

```json
{
  "document": "query ProductReviews($id: ID!, $first: Int!) { productById(id: $id) { id repeat(num: $first) } }",
  "operation": "ProductReviews",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query ProductReviews_1($first: Int!, $id: ID!) { productById(id: $id) { id repeat(num: $first) } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "first"
          },
          {
            "variable": "id"
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

