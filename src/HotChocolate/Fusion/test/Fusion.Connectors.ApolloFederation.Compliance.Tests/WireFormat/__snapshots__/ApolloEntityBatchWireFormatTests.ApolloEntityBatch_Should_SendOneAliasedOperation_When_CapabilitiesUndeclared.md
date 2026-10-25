# ApolloEntityBatch_Should_SendOneAliasedOperation_When_CapabilitiesUndeclared

## HTTP Request 1 to 'left'

```json
{
  "query": "query Op_88f73202_1 { parent { a: child { id } b: child { id } } }"
}
```

## HTTP Request 2 to 'right'

```json
{
  "query": "query Op_88f73202_Batch_a189d37d1227784b($_0_representations:[_Any!]!,$_1_representations:[_Any!]!){_0__entities:_entities(representations:$_0_representations){...on Child{b:value(suffix:\"!\")}} _1__entities:_entities(representations:$_1_representations){...on Child{a:value}}}",
  "variables": {
    "_0_representations": [
      {
        "__typename": "Child",
        "id": "1"
      }
    ],
    "_1_representations": [
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
