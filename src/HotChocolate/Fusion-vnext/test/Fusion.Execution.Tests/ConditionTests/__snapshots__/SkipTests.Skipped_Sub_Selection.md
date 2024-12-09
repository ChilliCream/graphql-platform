# Skipped_Sub_Selection

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip)
  }
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      query($skip: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) {
          name @skip(if: $skip)
        }
      }

```

