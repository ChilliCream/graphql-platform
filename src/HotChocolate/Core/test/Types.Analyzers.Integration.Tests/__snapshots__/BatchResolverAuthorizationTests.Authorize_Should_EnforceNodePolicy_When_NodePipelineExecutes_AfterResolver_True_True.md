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
  "AfterResolver:node",
  "AfterResolver:node"
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
  "AfterResolver:nodes[0]",
  "AfterResolver:nodes[1]",
  "AfterResolver:nodes[0]",
  "AfterResolver:nodes[1]"
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
