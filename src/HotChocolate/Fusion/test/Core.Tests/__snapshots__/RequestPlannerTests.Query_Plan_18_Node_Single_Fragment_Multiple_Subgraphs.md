# Query_Plan_18_Node_Single_Fragment_Multiple_Subgraphs

## UserRequest

```graphql
query FetchNode($id: ID!) {
  node(id: $id) {
    ... on User {
      birthdate
      reviews {
        body
      }
    }
    ... on Review {
      body
      author {
        birthdate
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query FetchNode($id: ID!) { node(id: $id) { ... on User { birthdate reviews { body } } ... on Review { body author { birthdate } } } }",
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
            "type": "User",
            "node": {
              "type": "Resolve",
              "subgraph": "Reviews2",
              "document": "query FetchNode_1($id: ID!) { node(id: $id) { ... on User { reviews { body } __fusion_exports__1: id __typename } } }",
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
              "document": "query FetchNode_2($id: ID!) { node(id: $id) { ... on Review { body author { __fusion_exports__2: id } __typename } } }",
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
      },
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query FetchNode_3($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { birthdate } }",
            "selectionSetId": 1,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query FetchNode_4($__fusion_exports__2: ID!) { userById(id: $__fusion_exports__2) { birthdate } }",
            "selectionSetId": 4,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1,
          4
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id"
  }
}
```

