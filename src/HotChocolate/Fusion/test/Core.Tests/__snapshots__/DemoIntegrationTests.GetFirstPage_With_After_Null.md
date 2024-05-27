# GetFirstPage_With_After_Null

## Result

```json
{
  "data": {
    "appointments": {
      "nodes": [
        {
          "id": "QXBwb2ludG1lbnQ6MQ=="
        },
        {
          "id": "QXBwb2ludG1lbnQ6Mg=="
        }
      ]
    }
  }
}
```

## Request

```graphql
query AfterNull($after: String) {
  appointments(after: $after) {
    nodes {
      id
    }
  }
}
```

## QueryPlan Hash

```text
C601EB39A2F136D152B59B30853A0073588356FE
```

## QueryPlan

```json
{
  "document": "query AfterNull($after: String) { appointments(after: $after) { nodes { id } } }",
  "operation": "AfterNull",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Appointment",
        "document": "query AfterNull_1($after: String) { appointments(after: $after) { nodes { id } } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "after"
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

