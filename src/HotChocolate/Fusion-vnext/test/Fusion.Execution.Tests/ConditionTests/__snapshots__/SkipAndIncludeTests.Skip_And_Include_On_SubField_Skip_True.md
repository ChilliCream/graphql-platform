# Skip_And_Include_On_SubField_Skip_True

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) {
    name @include(if: $include) @skip(if: true)
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

