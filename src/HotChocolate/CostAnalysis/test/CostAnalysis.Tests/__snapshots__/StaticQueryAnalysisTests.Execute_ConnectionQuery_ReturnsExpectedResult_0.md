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

## Result

```text
{
  "data": {
    "examples1": {
      "pageInfo": {
        "hasNextPage": true
      },
      "edges": [
        {
          "node": {
            "field1": true,
            "field2": {
              "pageInfo": {
                "hasNextPage": true
              },
              "edges": [
                {
                  "node": {
                    "field1": true,
                    "field2": 1
                  }
                }
              ]
            }
          }
        }
      ],
      "nodes": [
        {
          "field1": true
        }
      ]
    },
    "__cost": {
      "requestCosts": {
        "fieldCounts": [
          {
            "name": "Query.examples1",
            "value": 1
          },
          {
            "name": "Examples1Connection.pageInfo",
            "value": 1
          },
          {
            "name": "PageInfo.hasNextPage",
            "value": 11
          },
          {
            "name": "Examples1Connection.edges",
            "value": 1
          },
          {
            "name": "Examples1Edge.node",
            "value": 10
          },
          {
            "name": "Example1.field1",
            "value": 20
          },
          {
            "name": "Example1.field2",
            "value": 10
          },
          {
            "name": "Examples2Connection.pageInfo",
            "value": 10
          },
          {
            "name": "Examples2Connection.edges",
            "value": 10
          },
          {
            "name": "Examples2Edge.node",
            "value": 100
          },
          {
            "name": "Example2.field1",
            "value": 100
          },
          {
            "name": "Example2.field2",
            "value": 100
          },
          {
            "name": "Examples1Connection.nodes",
            "value": 1
          }
        ],
        "typeCounts": [
          {
            "name": "Query",
            "value": 1
          },
          {
            "name": "Examples1Connection",
            "value": 1
          },
          {
            "name": "PageInfo",
            "value": 11
          },
          {
            "name": "Boolean",
            "value": 131
          },
          {
            "name": "Examples1Edge",
            "value": 10
          },
          {
            "name": "Example1",
            "value": 20
          },
          {
            "name": "Examples2Connection",
            "value": 10
          },
          {
            "name": "Examples2Edge",
            "value": 100
          },
          {
            "name": "Example2",
            "value": 100
          },
          {
            "name": "Int",
            "value": 100
          }
        ],
        "inputTypeCounts": [],
        "inputFieldCounts": [],
        "argumentCounts": [
          {
            "name": "Query.examples1(first:)",
            "value": 1
          },
          {
            "name": "Example1.field2(first:)",
            "value": 10
          }
        ],
        "directiveCounts": []
      }
    }
  }
}
```

