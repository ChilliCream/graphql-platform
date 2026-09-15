# Authorize_Should_EnforceNodePolicy_When_NodePipelineExecutes

## Node result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "node"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "node": null
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
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "nodes",
        0
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "nodes",
        1
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "nodes": [
      null,
      null
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
