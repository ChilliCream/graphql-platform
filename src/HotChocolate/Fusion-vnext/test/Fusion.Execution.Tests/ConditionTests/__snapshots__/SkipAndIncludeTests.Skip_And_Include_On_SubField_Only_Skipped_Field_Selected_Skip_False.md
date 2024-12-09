# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) {
    name @include(if: $include) @skip(if: false)
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
        productBySlug(slug: $slug) {
          name @include(if: $include)
        }
      }

```

