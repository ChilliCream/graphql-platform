# UseBatch_Should_ActivateFieldMiddleware_When_RegisteredThroughSupportedPaths

## First request

```json
{
  "data": {
    "value": "field(value)"
  }
}
```

## Second request

```json
{
  "data": {
    "value": "field(value)"
  }
}
```

## Activations

```json
1
```

## Middleware order

```json
[
  "field:before",
  "field:after",
  "field:before",
  "field:after"
]
```
