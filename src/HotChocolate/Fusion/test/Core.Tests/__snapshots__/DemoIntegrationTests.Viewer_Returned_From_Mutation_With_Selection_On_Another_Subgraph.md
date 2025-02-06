# Viewer_Returned_From_Mutation_With_Selection_On_Another_Subgraph

## Result

```json
{
  "data": {
    "doSomething": {
      "something": 123,
      "viewer": {
        "subgraphA": "string",
        "subgraphB": "string"
      }
    }
  }
}
```

## Request

```graphql
mutation {
  doSomething {
    something
    viewer {
      subgraphA
      subgraphB
    }
  }
}
```

## QueryPlan Hash

```text
3BAD760CBB742706BF2DEDB9CA40F8DA880B22CC
```

## QueryPlan

```json
{
  "document": "mutation { doSomething { something viewer { subgraphA subgraphB } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "mutation fetch_doSomething_1 { doSomething { something viewer { subgraphA } } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Subgraph_2",
        "document": "query fetch_doSomething_2 { viewer { subgraphB } }",
        "selectionSetId": 2,
        "path": [
          "viewer"
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      }
    ]
  }
}
```

