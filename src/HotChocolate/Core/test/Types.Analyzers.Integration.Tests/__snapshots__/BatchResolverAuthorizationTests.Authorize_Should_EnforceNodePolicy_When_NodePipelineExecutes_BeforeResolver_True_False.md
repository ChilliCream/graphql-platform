# Authorize_Should_EnforceNodePolicy_When_NodePipelineExecutes

## Node result

```json
{
  "data": {
    "node": {
      "__typename": "BatchAuthorizationNode"
    }
  }
}
```

## Node authorization calls

```json
[
  "BeforeResolver:node"
]
```

## Node resolver batches

```json
[
  [
    "1"
  ]
]
```

## Nodes result

```json
{
  "data": {
    "nodes": [
      {
        "__typename": "BatchAuthorizationNode"
      },
      {
        "__typename": "BatchAuthorizationNode"
      }
    ]
  }
}
```

## Nodes authorization calls

```json
[
  "BeforeResolver:nodes"
]
```

## Nodes resolver batches

```json
[
  [
    "1",
    "2"
  ]
]
```
