# ApolloEntityBatch_Should_SendOneOperationBatch_When_CapabilitiesUndeclared

## HTTP Request 1 to 'left'

```json
{
  "query": "query Op_88f73202_1 {\n  parent {\n    a: child {\n      id\n    }\n    b: child {\n      id\n    }\n  }\n}"
}
```

## HTTP Request 2 to 'right'

```json
[
  {
    "query": "query($representations: [_Any!]!) {\n  _entities(representations: $representations) {\n    ... on Child {\n      b: value(suffix: \"!\")\n    }\n  }\n}",
    "variables": {
      "representations": [
        {
          "__typename": "Child",
          "id": "1"
        }
      ]
    }
  },
  {
    "query": "query($representations: [_Any!]!) {\n  _entities(representations: $representations) {\n    ... on Child {\n      a: value\n    }\n  }\n}",
    "variables": {
      "representations": [
        {
          "__typename": "Child",
          "id": "1"
        }
      ]
    }
  }
]
```

## Gateway Result

```json
{
  "data": {
    "parent": {
      "a": {
        "a": "child-1"
      },
      "b": {
        "b": "child-1!"
      }
    }
  }
}
```
