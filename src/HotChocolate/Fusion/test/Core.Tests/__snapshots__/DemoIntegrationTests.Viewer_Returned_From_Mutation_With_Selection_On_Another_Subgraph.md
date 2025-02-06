# Viewer_Returned_From_Mutation_With_Selection_On_Another_Subgraph

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
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

