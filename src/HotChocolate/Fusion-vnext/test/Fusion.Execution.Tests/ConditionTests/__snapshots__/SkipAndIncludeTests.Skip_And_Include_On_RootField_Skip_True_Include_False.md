# Skip_And_Include_On_RootField_Skip_True_Include_False

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) @skip(if: true) @include(if: false) {
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
      {
        products {
          nodes {
            name
          }
        }
      }

```

