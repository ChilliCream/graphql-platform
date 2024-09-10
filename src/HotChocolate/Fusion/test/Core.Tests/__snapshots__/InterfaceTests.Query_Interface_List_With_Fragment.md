# Query_Interface_List_With_Fragment

## Result

```json
{
  "data": {
    "appointments": {
      "nodes": [
        {
          "patient": {
            "id": "UGF0aWVudDE6MQ==",
            "name": "Karl Kokoloko"
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
        ... on Patient1 {
          name
        }
      }
    }
  }
}
```

## QueryPlan Hash

```text
5F5BF5AEEC7BF9B75984AF29704E92CE9B486168
```

## QueryPlan

```json
{
  "document": "query Appointments { appointments { nodes { patient { id ... on Patient1 { name } } } } }",
  "operation": "Appointments",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Appointment",
        "document": "query Appointments_1 { appointments { nodes { patient { __typename ... on Patient2 { id } ... on Patient1 { id __fusion_exports__1: id } } } } }",
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
        "type": "ResolveByKeyBatch",
        "subgraph": "Patient1",
        "document": "query Appointments_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on Patient1 { name __fusion_exports__1: id } } }",
        "selectionSetId": 4,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          4
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Patient1_id"
  }
}
```

