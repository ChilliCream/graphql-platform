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
        "nodes"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": null
}
```

## Nodes authorization calls

```json
[
  "AfterResolver:nodes"
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
