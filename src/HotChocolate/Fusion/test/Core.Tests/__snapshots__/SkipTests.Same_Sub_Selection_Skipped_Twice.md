# Same_Sub_Selection_Skipped_Twice

## Result

```json
{
  "errors": [
    {
      "message": "Only one of each directive is allowed per location.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "product"
      ],
      "extensions": {
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Directives-Are-Unique-Per-Location"
      }
    }
  ],
  "data": {
    "product": null
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
    brand @skip(if: $skip) {
      id
    }
  }
}
```

## QueryPlan Hash

```text
9BFCF93360016E8648F2E5FE8AB694B3CD50AC97
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { brand @skip(if: $skip) { name } brand @skip(if: $skip) { id } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { product { brand @skip(if: $skip) @skip(if: $skip) { name id } } }",
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

