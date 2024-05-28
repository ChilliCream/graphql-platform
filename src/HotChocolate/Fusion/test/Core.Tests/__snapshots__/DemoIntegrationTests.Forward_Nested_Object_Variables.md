# Forward_Nested_Object_Variables

## Result

```json
{
  "data": {
    "productById": {
      "id": "UHJvZHVjdDox",
      "repeatData": {
        "data": {
          "num": 1
        }
      }
    }
  }
}
```

## Request

```graphql
query ProductReviews($id: ID!, $first: Int!) {
  productById(id: $id) {
    id
    repeatData(data: { data: { num: $first } }) {
      data {
        num
      }
    }
  }
}
```

## QueryPlan Hash

```text
8ECD591E2162E9499A4E7DAC6CA05988445547E6
```

## QueryPlan

```json
{
  "document": "query ProductReviews($id: ID!, $first: Int!) { productById(id: $id) { id repeatData(data: { data: { num: $first } }) { data { num } } } }",
  "operation": "ProductReviews",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query ProductReviews_1($first: Int!, $id: ID!) { productById(id: $id) { id repeatData(data: { data: { num: $first } }) { data { num } } } }",
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

