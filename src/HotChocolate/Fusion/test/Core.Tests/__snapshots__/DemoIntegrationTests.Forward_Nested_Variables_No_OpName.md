# Forward_Nested_Variables_No_OpName

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
query($id: ID!, $first: Int!) {
  productById(id: $id) {
    id
    repeat(num: $first)
  }
}
```

## QueryPlan Hash

```text
361C327571AE2F6013116F6DBD44EBDB241DA154
```

## QueryPlan

```json
{
  "document": "query($id: ID!, $first: Int!) { productById(id: $id) { id repeat(num: $first) } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query fetch_productById_1($first: Int!, $id: ID!) { productById(id: $id) { id repeat(num: $first) } }",
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

