# Skip_And_Include_On_SubField_Skip_True_Include_True

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true) @include(if: true)
    description
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
          description
        }
      }

```

