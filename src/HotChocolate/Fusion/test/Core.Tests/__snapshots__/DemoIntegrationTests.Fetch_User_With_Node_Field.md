# Fetch_User_With_Node_Field

## Result

```json
{
  "data": {
    "node": {
      "id": "VXNlcjox"
    }
  }
}
```

## Request

```graphql
query FetchNode($id: ID!) {
  node(id: $id) {
    ... on User {
      id
    }
  }
}
```

## QueryPlan Hash

```text
1A21CA422439C6540C53FC5028089D965C5F8A03
```

## QueryPlan

```json
{
  "document": "query FetchNode($id: ID!) { node(id: $id) { ... on User { id } } }",
  "operation": "FetchNode",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "node",
        "branches": [
          {
            "type": "Product",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_1($id: ID!) { node(id: $id) { ... on Product { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "Review",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query FetchNode_2($id: ID!) { node(id: $id) { ... on Review { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "User",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query FetchNode_3($id: ID!) { node(id: $id) { ... on User { id __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
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

