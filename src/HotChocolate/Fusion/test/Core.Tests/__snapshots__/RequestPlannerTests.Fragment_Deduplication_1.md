# Test1

## UserRequest

```graphql
query test {
  entry {
    id
    string
    other {
      id
      number
    }
    ...frag1
    ...frag2
    ...frag3
  }
}

fragment frag1 on SomeObject {
  id
  string
}

fragment frag2 on SomeObject {
  id
  other {
    id
    number
  }
}

fragment frag3 on SomeObject {
  id
  other {
    __typename
  }
}
```

## QueryPlan

```json
{
  "document": "query test { entry { id string other { id number } ... frag1 ... frag2 ... frag3 } } fragment frag1 on SomeObject { id string } fragment frag2 on SomeObject { id other { id number } } fragment frag3 on SomeObject { id other { __typename } }",
  "operation": "test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query test_1 { entry { id string other { id number __typename } } }",
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

