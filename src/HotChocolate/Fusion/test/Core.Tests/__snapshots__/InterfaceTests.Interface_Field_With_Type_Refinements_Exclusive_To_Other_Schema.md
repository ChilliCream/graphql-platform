# Interface_Field_With_Type_Refinements_Exclusive_To_Other_Schema

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
    ... on ConcreteTypeB {
      specificToB
    }
  }
}
```

## QueryPlan Hash

```text
7B275C9B935F1E19D5E6E559DE791F73D9E28311
```

## QueryPlan

```json
{
  "document": "{ someField { value ... on ConcreteTypeA { specificToA } ... on ConcreteTypeB { specificToB } } }",
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

