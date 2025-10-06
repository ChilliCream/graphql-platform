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
BF4A5244798FF9AB04AF0EB6141674EB32DFDD75
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

