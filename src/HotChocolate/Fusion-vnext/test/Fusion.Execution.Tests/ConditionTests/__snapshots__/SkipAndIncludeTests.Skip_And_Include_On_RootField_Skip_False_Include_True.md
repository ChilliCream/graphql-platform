# Skip_And_Include_On_RootField_Skip_False_Include_True

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) @skip(if: false) @include(if: true) {
    name
  }
  products {
    nodes {
      name
    }
  }
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      query($slug: String!) {
        productBySlug(slug: $slug) {
          name
        }
        products {
          nodes {
            name
          }
        }
      }

```

