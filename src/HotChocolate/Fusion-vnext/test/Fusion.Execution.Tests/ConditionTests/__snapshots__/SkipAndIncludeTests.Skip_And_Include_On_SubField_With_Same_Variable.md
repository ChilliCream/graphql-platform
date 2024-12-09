# Skip_And_Include_On_SubField_With_Same_Variable

## Request

```graphql
query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
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
      query($skipOrInclude: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) {
          name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
          description
        }
      }

```

