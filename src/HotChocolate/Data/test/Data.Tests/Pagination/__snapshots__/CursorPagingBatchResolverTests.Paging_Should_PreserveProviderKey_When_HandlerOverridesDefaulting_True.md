# Paging_Should_PreserveProviderKey_When_HandlerOverridesDefaulting

## Omitted custom default

```json
{
  "variableIndex": 0,
  "data": {
    "parents": [
      {
        "products": {
          "items": [
            1
          ]
        }
      }
    ]
  }
}
```

## Explicit custom default

```json
{
  "variableIndex": 1,
  "data": {
    "parents": [
      {
        "products": {
          "items": [
            2
          ]
        }
      }
    ]
  }
}
```

## Different size

```json
{
  "variableIndex": 2,
  "data": {
    "parents": [
      {
        "products": {
          "items": [
            3
          ]
        }
      }
    ]
  }
}
```

## Resolver batches

```json
[
  [
    1,
    2
  ],
  [
    3
  ]
]
```

## Handler page sizes

```json
[
  7,
  7,
  10
]
```
