# Query_Plan_16_Two_Node_Fields_Aliased

## UserRequest

```graphql
query FetchNode($a: ID!, $b: ID!) {
  a: node(id: $a) {
    ... on User {
      id
    }
  }
  b: node(id: $b) {
    ... on User {
      id
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query FetchNode($a: ID!, $b: ID!) { a: node(id: $a) { ... on User { id } } b: node(id: $b) { ... on User { id } } }",
  "operation": "FetchNode",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "ResolveNode",
            "selectionId": 0,
            "responseName": "a",
            "branches": [
              {
                "type": "User",
                "node": {
                  "type": "Resolve",
                  "subgraph": "Reviews2",
                  "document": "query FetchNode_1($a: ID!) { a: node(id: $a) { ... on User { id __typename } } }",
                  "selectionSetId": 0,
                  "forwardedVariables": [
                    {
                      "variable": "a"
                    }
                  ]
                }
              },
              {
                "type": "Review",
                "node": {
                  "type": "Resolve",
                  "subgraph": "Reviews2",
                  "document": "query FetchNode_2($a: ID!) { a: node(id: $a) { ... on Review { __typename } } }",
                  "selectionSetId": 0,
                  "forwardedVariables": [
                    {
                      "variable": "a"
                    }
                  ]
                }
              }
            ]
          },
          {
            "type": "ResolveNode",
            "selectionId": 1,
            "responseName": "b",
            "branches": [
              {
                "type": "User",
                "node": {
                  "type": "Resolve",
                  "subgraph": "Reviews2",
                  "document": "query FetchNode_3($b: ID!) { b: node(id: $b) { ... on User { id __typename } } }",
                  "selectionSetId": 0,
                  "forwardedVariables": [
                    {
                      "variable": "b"
                    }
                  ]
                }
              },
              {
                "type": "Review",
                "node": {
                  "type": "Resolve",
                  "subgraph": "Reviews2",
                  "document": "query FetchNode_4($b: ID!) { b: node(id: $b) { ... on Review { __typename } } }",
                  "selectionSetId": 0,
                  "forwardedVariables": [
                    {
                      "variable": "b"
                    }
                  ]
                }
              }
            ]
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

