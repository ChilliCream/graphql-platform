# Skip_And_Include_On_RootField_Skip_False

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) @include(if: $include) @skip(if: false) {
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
      query($include: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) @include(if: $include) {
          name
        }
        products {
          nodes {
            name
          }
        }
      }

```

