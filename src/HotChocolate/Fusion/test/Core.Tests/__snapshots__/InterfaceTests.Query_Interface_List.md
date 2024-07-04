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
7FA915AFBA06ABAAF57A31CE4888B161285111C3
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
        "document": "query Appointments_1 { appointments { nodes { patient { __typename ... on Patient1 { id } ... on Patient2 { id } } } } }",
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

