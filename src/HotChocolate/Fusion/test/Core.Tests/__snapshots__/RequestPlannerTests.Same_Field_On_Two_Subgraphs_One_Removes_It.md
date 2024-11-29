# Same_Field_On_Two_Subgraphs_One_Removes_It

## UserRequest

```graphql
{
  userBySlug(slug: "me") {
    ... likedAuthors
  }
}

fragment likedAuthors on User {
  someField
  otherField
  anotherField
  followedBlogAuthors(first: 3) {
    fullName
  }
}
```

## QueryPlan

```json
{
  "document": "{ userBySlug(slug: \u0022me\u0022) { ... likedAuthors } } fragment likedAuthors on User { someField otherField anotherField followedBlogAuthors(first: 3) { fullName } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_3",
        "document": "query fetch_userBySlug_1 { userBySlug(slug: \u0022me\u0022) { __fusion_exports__1: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query fetch_userBySlug_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on User { someField otherField anotherField } } }",
            "selectionSetId": 1,
            "path": [
              "node"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_userBySlug_3($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on User { followedBlogAuthors(first: 3) { fullName } } } }",
            "selectionSetId": 1,
            "path": [
              "node"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

