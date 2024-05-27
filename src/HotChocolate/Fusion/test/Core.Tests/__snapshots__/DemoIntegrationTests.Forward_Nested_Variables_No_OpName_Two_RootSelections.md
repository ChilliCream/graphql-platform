# Forward_Nested_Variables_No_OpName_Two_RootSelections

## Result

```json
{
  "data": {
    "a": {
      "id": "UHJvZHVjdDox",
      "repeat": 1
    },
    "b": {
      "id": "UHJvZHVjdDox",
      "repeat": 1
    }
  }
}
```

## Request

```graphql
query($id: ID!, $first: Int!) {
  a: productById(id: $id) {
    id
    repeat(num: $first)
  }
  b: productById(id: $id) {
    id
    repeat(num: $first)
  }
}
```

## QueryPlan Hash

```text
0B9DDBAE987F3EF0FF2163307F2744D756A81213
```

## QueryPlan

```json
{
  "document": "query($id: ID!, $first: Int!) { a: productById(id: $id) { id repeat(num: $first) } b: productById(id: $id) { id repeat(num: $first) } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query fetch_a_b_1($first: Int!, $id: ID!) { a: productById(id: $id) { id repeat(num: $first) } b: productById(id: $id) { id repeat(num: $first) } }",
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

