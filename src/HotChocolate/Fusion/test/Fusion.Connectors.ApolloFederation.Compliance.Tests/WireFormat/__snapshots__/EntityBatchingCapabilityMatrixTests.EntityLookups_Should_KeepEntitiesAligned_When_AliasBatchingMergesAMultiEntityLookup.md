# EntityLookups_Should_KeepEntitiesAligned_When_AliasBatchingMergesAMultiEntityLookup

## HTTP Request 1 to 'left'

```json
{
  "query": "query Op_5328c9ea_1 {\n  parents {\n    a: child {\n      id\n    }\n  }\n  parent {\n    b: child {\n      id\n    }\n  }\n}"
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
      },
      {
        "__typename": "Child",
        "id": "2"
      }
    ]
  }
}
```

## Gateway Result

```json
{
  "data": {
    "parents": [
      {
        "a": {
          "a": "child-1"
        }
      },
      {
        "a": {
          "a": "child-2"
        }
      }
    ],
    "parent": {
      "b": {
        "b": "child-1!"
      }
    }
  }
}
```
