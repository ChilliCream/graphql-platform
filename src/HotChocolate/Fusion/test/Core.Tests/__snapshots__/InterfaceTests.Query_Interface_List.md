# Query_Interface_List

## Result

```json
{
  "data": {
    "appointments": {
      "nodes": [
        {
          "patient": {
            "id": "UGF0aWVudDE6MQ=="
          }
        },
        {
          "patient": {
            "id": "UGF0aWVudDI6Mg=="
          }
        }
      ]
    }
  }
}
```

## Request

```graphql
query Appointments {
  appointments {
    nodes {
      patient {
        id
      }
    }
  }
}
```

## QueryPlan Hash

```text
3097238732BE7F08C83A6417B0AE8AE1E716E16D
```

## QueryPlan

```json
{
  "document": "query Appointments { appointments { nodes { patient { id } } } }",
  "operation": "Appointments",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Appointment",
        "document": "query Appointments_1 { appointments { nodes { patient { __typename ... on Patient2 { id } ... on Patient1 { id } } } } }",
        "selectionSetId": 0
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

