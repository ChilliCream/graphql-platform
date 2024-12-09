# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) @include(if: $include) {
    name
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
      }
    skipIf: "skip"
    includeIf: "include"

```

