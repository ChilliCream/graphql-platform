# Plan_Simple_Operation_3_Source_Schema

## Request

```graphql
{
  productById(id: 1) {
    ... ProductCard
  }
}

fragment ProductCard on Product {
  name
  reviews(first: 10) {
    nodes {
      ... ReviewCard
    }
  }
}

fragment ReviewCard on Review {
  body
  stars
  author {
    ... AuthorCard
  }
}

fragment AuthorCard on UserProfile {
  displayName
}
```

## Plan

```json
{
  "kind": "Root",
  "nodes": [
    {
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "{ productById(id: 1) { name } }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "{ productById { reviews(first: 10) { nodes { body stars author } } } }",
          "nodes": [
            {
              "kind": "Operation",
              "schema": "ACCOUNTS",
              "document": "{ userById { displayName } }"
            }
          ]
        }
      ]
    }
  ]
}
```

