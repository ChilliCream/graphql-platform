# Execute_ConnectionQuery_ReturnsExpectedResult

## Query

```graphql
{
  examples1(first: 10) {
    pageInfo {
      hasNextPage
    }
    edges {
      node {
        field1
        field2(first: 10) {
          pageInfo {
            hasNextPage
          }
          edges {
            node {
              field1
              field2
            }
          }
        }
      }
    }
    nodes {
      field1
    }
  }
}
```

## Result

```text
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
}
```

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

