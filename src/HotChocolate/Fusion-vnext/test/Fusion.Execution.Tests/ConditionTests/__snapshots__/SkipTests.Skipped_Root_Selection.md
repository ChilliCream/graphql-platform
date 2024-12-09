# Skipped_Root_Selection

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
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

