# Query_Plan_06_Introspection

## UserRequest

```graphql
query Introspect {
  __schema {
    types {
      name
      kind
      fields {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query Introspect { __schema { types { name kind fields { name type { name kind } } } } }",
  "operation": "Introspect",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Introspect",
        "document": "{ __schema { types { name kind fields { name type { name kind } } } } }"
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

