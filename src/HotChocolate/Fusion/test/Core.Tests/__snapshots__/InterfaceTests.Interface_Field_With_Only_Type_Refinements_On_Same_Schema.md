# Interface_Field_With_Only_Type_Refinements_On_Same_Schema

## Result

```json
{
  "data": {
    "someField": {
      "value": "string",
      "specificToA": "string"
    }
  }
}
```

## Request

```graphql
{
  someField {
    value
    ... on ConcreteTypeA {
      specificToA
    }
  }
}
```

## QueryPlan Hash

```text
FB5D43AB280C472A5C17E5F21578B602602D41C3
```

## QueryPlan

```json
{
  "document": "{ someField { value ... on ConcreteTypeA { specificToA } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_someField_1 { someField { __typename ... on ConcreteTypeA { value specificToA } } }",
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

