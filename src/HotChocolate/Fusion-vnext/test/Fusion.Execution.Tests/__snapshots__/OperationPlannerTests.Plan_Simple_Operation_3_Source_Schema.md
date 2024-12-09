# Plan_Simple_Operation_3_Source_Schema

## Request

```graphql
{
  productBySlug(slug: "1") {
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

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      {
        productBySlug(slug: "1") {
          name
          id
        }
      }
  - id: 2
    schema: "REVIEWS"
    operation: >-
      query($__fusion_requirement_2: ID!) {
        productById(id: $__fusion_requirement_2) {
          reviews(first: 10) {
            nodes {
              body
              stars
              author {
                id
              }
            }
          }
        }
      }
    requirements:
      - name: "__fusion_requirement_2"
        dependsOn: "1"
        selectionSet: "productBySlug"
        field: "id"
        type: "ID!"
  - id: 3
    schema: "ACCOUNTS"
    operation: >-
      query($__fusion_requirement_1: ID!) {
        userById(id: $__fusion_requirement_1) {
          displayName
        }
      }
    requirements:
      - name: "__fusion_requirement_1"
        dependsOn: "2"
        selectionSet: "productBySlug.reviews.nodes.author"
        field: "id"
        type: "ID!"

```

