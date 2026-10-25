# Execute_Should_HideInaccessibleImplementor_When_InterfaceObjectStandsInForInterface

## interface-object

```text
{
  "data": {
    "items": [
      {
        "id": "v1",
        "common": "catalog-v1",
        "extra": "extra-v1"
      },
      null,
      {
        "id": "v2",
        "common": "catalog-v2",
        "extra": "extra-v2"
      }
    ],
    "alive": "catalog-alive"
  }
}
```

## common-only

```text
{
  "data": {
    "items": [
      {
        "id": "v1",
        "common": "catalog-v1"
      },
      null,
      {
        "id": "v2",
        "common": "catalog-v2"
      }
    ],
    "alive": "catalog-alive"
  }
}
```

## opaque-entry

```text
{
  "data": {
    "extraItems": [
      {
        "id": "v1",
        "extra": "extra-v1"
      },
      {
        "id": "p1",
        "extra": "extra-p1"
      },
      {
        "id": "v2",
        "extra": "extra-v2"
      }
    ],
    "alive": "catalog-alive"
  }
}
```

## opaque-entry-typename

```text
{
  "data": {
    "extraItems": [
      {
        "__typename": "Visible",
        "id": "v1",
        "extra": "extra-v1"
      },
      null,
      {
        "__typename": "Visible",
        "id": "v2",
        "extra": "extra-v2"
      }
    ],
    "alive": "catalog-alive"
  }
}
```
