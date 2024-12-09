# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_With_Same_Variable

## Request

```graphql
query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
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
    skipIf: "skipOrInclude"
    includeIf: "skipOrInclude"

```

