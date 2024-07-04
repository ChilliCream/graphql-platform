# Query_Plan_25_Variables_Are_Passed_Through

## UserRequest

```graphql
query Appointments($first: Int!) {
  patientById(patientId: 1) {
    name
    appointments(first: $first) {
      nodes {
        id
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query Appointments($first: Int!) { patientById(patientId: 1) { name appointments(first: $first) { nodes { id } } } }",
  "operation": "Appointments",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Patient1",
        "document": "query Appointments_1 { patientById(patientId: 1) { name __fusion_exports__1: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Appointment",
        "document": "query Appointments_2($__fusion_exports__1: ID!, $first: Int!) { node(id: $__fusion_exports__1) { ... on Patient1 { appointments(first: $first) { nodes { id } } } } }",
        "selectionSetId": 1,
        "path": [
          "node"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "forwardedVariables": [
          {
            "variable": "first"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Patient1_id"
  }
}
```

