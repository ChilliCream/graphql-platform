# Fragment_Deduplication_1

## UserRequest

```graphql
{
  entry {
    id
    string
    other {
      __typename
      ... frag4
    }
    ... frag1
    ... frag2
    ... frag3
  }
}

fragment frag1 on SomeObject {
  id
  string
  other {
    number
  }
}

fragment frag2 on SomeObject {
  id
  other {
    id
  }
}

fragment frag3 on SomeObject {
  id
  other {
    __typename
  }
}

fragment frag4 on AnotherObject {
  id
  number
}
```

## QueryPlan

```json
{
  "document": "{ entry { id string other { __typename ... frag4 } ... frag1 ... frag2 ... frag3 } } fragment frag1 on SomeObject { id string other { number } } fragment frag2 on SomeObject { id other { id } } fragment frag3 on SomeObject { id other { __typename } } fragment frag4 on AnotherObject { id number }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_entry_1 { entry { id string other { __typename id number } } }",
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

