# Execute_ConnectionQuery_ReturnsExpectedResult

## Schema

```text
type Query {
    examples1(first: Int, after: String, last: Int, before: String): Examples1Connection
    @listSize(slicingArguments: ["first", "last"], sizedFields: ["edges", "nodes"])
}

type Example1 {
    field1: Boolean!
    field2(first: Int, after: String, last: Int, before: String): Examples2Connection
        @listSize(slicingArguments: ["first", "last"], sizedFields: ["edges"])
}

type Example2 {
    field1: Boolean!
    field2: Int!
}

type Examples1Connection {
    pageInfo: PageInfo!
    edges: [Examples1Edge!]
    nodes: [Example1!]
}

type Examples2Connection {
    pageInfo: PageInfo!
    edges: [Examples2Edge!]
    nodes: [Example2!]
}

type Examples1Edge {
    cursor: String!
    node: Example1!
}

type Examples2Edge {
    cursor: String!
    node: Example2!
}

type PageInfo {
    hasNextPage: Boolean!
    hasPreviousPage: Boolean!
    startCursor: String
    endCursor: String
}
```

## Query

```text
query {
    examples1(first: 10) {          # Examples1Connection x1
    pageInfo {                  # PageInfo x1
        hasNextPage             # Boolean x1
    }
    edges {                     # Examples1Edge x10
        node {                  # Example1 x10
            field1              # Boolean x10
            field2(first: 10) { # Examples2Connection x10
                pageInfo {      # PageInfo x10
                    hasNextPage # Boolean x10
                }
                edges {         # Examples2Edge x(10x10)
                    node {      # Example2 x(10x10)
                        field1  # Boolean x(10x10)
                        field2  # Int x(10x10)
                    }
                }
            }
        }
    }
    nodes {                     # Example1 x10
        field1                  # Boolean x10
    }
}

    __cost {
        requestCosts {
            fieldCounts { name, value }
            typeCounts { name, value }
            inputTypeCounts { name, value }
            inputFieldCounts { name, value }
            argumentCounts { name, value }
            directiveCounts { name, value }
        }
    }
}
```

## Result Result:

```text
{
  "errors": [
    {
      "message": "The field `__cost` does not exist on the type `Query`.",
      "locations": [
        {
          "line": 27,
          "column": 5
        }
      ],
      "extensions": {
        "type": "Query",
        "field": "__cost",
        "responseName": "__cost",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types"
      }
    }
  ]
}
```

