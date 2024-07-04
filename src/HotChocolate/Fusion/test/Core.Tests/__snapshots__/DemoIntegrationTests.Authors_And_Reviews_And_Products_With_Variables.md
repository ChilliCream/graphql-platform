# Authors_And_Reviews_And_Products_With_Variables

## Result

```json
{
  "data": {
    "topProducts": [
      {
        "id": "UHJvZHVjdDox"
      },
      {
        "id": "UHJvZHVjdDoy"
      }
    ]
  }
}
```

## Request

```graphql
query TopProducts($first: Int!) {
  topProducts(first: $first) {
    id
  }
}
```

## QueryPlan Hash

```text
D95EB3DB7C743969F7A95EBD409217BF5E1BB5D8
```

## QueryPlan

```json
{
  "document": "query TopProducts($first: Int!) { topProducts(first: $first) { id } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query TopProducts_1($first: Int!) { topProducts(first: $first) { id } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "first"
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

