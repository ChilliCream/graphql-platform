# Query_Plan_22_Interfaces

## UserRequest

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

