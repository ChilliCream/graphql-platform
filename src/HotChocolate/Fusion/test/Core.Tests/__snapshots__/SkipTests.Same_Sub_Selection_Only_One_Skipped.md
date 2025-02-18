# Same_Sub_Selection_Only_One_Skipped

## Result

```json
{
  "data": {
    "product": {
      "brand": {
        "id": "1"
      }
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product {
    brand @skip(if: $skip) {
      name
    }
    brand {
      id
    }
  }
}
```

## QueryPlan Hash

```text
464C363B5DA6DE9DECACBC57867F8467C5F6EBDC
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { brand @skip(if: $skip) { name } brand { id } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { product { brand @skip(if: $skip) { name id } brand { name id } } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "skip"
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

