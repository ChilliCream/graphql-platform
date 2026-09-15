# BatchResolver_Should_NotInvoke_When_ParentCollectionContainsOnlyNulls

## Null parents

```json
{
  "data": {
    "nullCollection": null,
    "allNullParents": [
      null,
      null
    ]
  }
}
```

## Invocations for null parents

```json
0
```

## Non-null control

```json
{
  "data": {
    "users": [
      {
        "value": "Alice"
      },
      {
        "value": "Bob"
      }
    ]
  }
}
```

## Invocations after control

```json
1
```
