# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False_Include_True

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: false) @include(if: true)
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

```

