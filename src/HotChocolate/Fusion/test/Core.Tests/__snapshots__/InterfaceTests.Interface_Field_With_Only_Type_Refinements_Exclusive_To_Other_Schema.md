# Interface_Field_With_Only_Type_Refinements_Exclusive_To_Other_Schema

## Result

```json
{
  "errors": [
    {
      "message": "Field \"someField\" of type \"SomeInterface\" must have a selection of subfields. Did you mean \"someField { ... }\"?",
      "locations": [
        {
          "line": 1,
          "column": 27
        }
      ],
      "extensions": {
        "declaringType": "Query",
        "field": "someField",
        "type": "SomeInterface",
        "responseName": "someField",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types"
      }
    }
  ],
  "data": {
    "someField": null
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
FAE12506C6604D14892FE4B185078A93A6CDC794
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
        "document": "query fetch_someField_1 { someField {  } }",
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

