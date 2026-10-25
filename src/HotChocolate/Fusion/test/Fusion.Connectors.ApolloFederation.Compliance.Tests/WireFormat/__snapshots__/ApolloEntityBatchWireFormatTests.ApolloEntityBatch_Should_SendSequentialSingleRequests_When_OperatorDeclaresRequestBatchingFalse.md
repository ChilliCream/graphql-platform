# ApolloEntityBatch_Should_SendSequentialSingleRequests_When_OperatorDeclaresRequestBatchingFalse

## HTTP Request 1 to 'left'

```json
{
  "query": "query Op_88f73202_1 { parent { a: child { id } b: child { id } } }"
}
```

## HTTP Request 2 to 'right'

```json
{
  "query": "query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Child { b: value(suffix: \"!\") } } }",
  "variables": {
    "representations": [
      {
        "__typename": "Child",
        "id": "1"
      }
    ]
  }
}
```

## HTTP Request 3 to 'right'

```json
{
  "query": "query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Child { a: value } } }",
  "variables": {
    "representations": [
      {
        "__typename": "Child",
        "id": "1"
      }
    ]
  }
}
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
