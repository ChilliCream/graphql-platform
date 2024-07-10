# Fetch_User_With_Node_Field_Pass_In_Review_Id

## Result

```json
{
  "data": {
    "node": {}
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
BF6F48F7F9CFB1049588D6F68B31736F8631A502
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
            "type": "ProductBookmark",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_2($id: ID!) { node(id: $id) { ... on ProductBookmark { __typename } } }",
              "selectionSetId": 0,
              "forwardedVariables": [
                {
                  "variable": "id"
                }
              ]
            }
          },
          {
            "type": "ProductConfiguration",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query FetchNode_3($id: ID!) { node(id: $id) { ... on ProductConfiguration { __typename } } }",
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
              "document": "query FetchNode_4($id: ID!) { node(id: $id) { ... on Review { __typename } } }",
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
              "document": "query FetchNode_5($id: ID!) { node(id: $id) { ... on User { id __typename } } }",
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

