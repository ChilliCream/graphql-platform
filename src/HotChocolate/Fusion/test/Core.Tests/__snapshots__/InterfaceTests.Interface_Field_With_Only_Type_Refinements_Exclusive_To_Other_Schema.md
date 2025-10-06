# Interface_Field_With_Only_Type_Refinements_Exclusive_To_Other_Schema

## Result

```json
{
  "data": {
    "someField": {}
  }
}
```

## Request

```graphql
{
  someField {
    ... on ConcreteTypeB {
      specificToB
    }
  }
}
```

## QueryPlan Hash

```text
D517577711CC066E36DD6785CA938437DAA08994
```

## QueryPlan

```json
{
  "document": "{ someField { ... on ConcreteTypeB { specificToB } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_someField_1 { someField { __typename } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

