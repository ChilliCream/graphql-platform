# EntityLookups_Should_UseTheProtocolTheCapabilitiesSelect

## HTTP Request 1 to 'left'

```json
{
  "query": "query Op_88f73202_1 {\n  parent {\n    a: child {\n      id\n    }\n    b: child {\n      id\n    }\n  }\n}"
}
```

## HTTP Request 2 to 'right'

```json
{
  "query": "query Batch_86274262454d197e($_0_representations:[_Any!]!,$_1_representations:[_Any!]!){_0__entities:_entities(representations:$_0_representations){...on Child{b:value(suffix:\"!\")}} _1__entities:_entities(representations:$_1_representations){...on Child{a:value}}}",
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
