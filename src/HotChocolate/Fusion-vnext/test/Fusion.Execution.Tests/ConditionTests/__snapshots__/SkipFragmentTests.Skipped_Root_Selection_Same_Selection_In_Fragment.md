# Skipped_Root_Selection_Same_Selection_In_Fragment

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
    name
  }
  ... QueryFragment
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) @skip(if: $skip) {
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

```

