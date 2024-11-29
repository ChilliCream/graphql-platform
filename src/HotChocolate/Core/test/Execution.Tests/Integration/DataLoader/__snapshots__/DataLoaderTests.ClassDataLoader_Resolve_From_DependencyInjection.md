# ClassDataLoader_Resolve_From_DependencyInjection

## Result 1

```json
{
  "data": {
    "a": "a",
    "b": "b"
  },
  "extensions": {
    "loads": [
      [
        "a",
        "b"
      ]
    ]
  }
}
```

## Result 2

```json
{
  "data": {
    "a": "a"
  },
  "extensions": {
    "loads": [
      [
        "a"
      ]
    ]
  }
}
```

## Result 3

```json
{
  "data": {
    "c": "c"
  },
  "extensions": {
    "loads": [
      [
        "c"
      ]
    ]
  }
}
```

