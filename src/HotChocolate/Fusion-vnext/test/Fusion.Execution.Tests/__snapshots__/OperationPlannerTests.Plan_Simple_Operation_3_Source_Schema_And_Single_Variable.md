# Plan_Simple_Operation_3_Source_Schema_And_Single_Variable

## Request

```graphql
query GetProduct($slug: String!, $first: Int! = 10) {
  productBySlug(slug: $slug) {
    ... ProductCard
  }
}

fragment ProductCard on Product {
  name
  reviews(first: $first) {
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "query($first: Int! = 10) { productById { reviews(first: $first) { nodes { body stars author } } } }",
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

