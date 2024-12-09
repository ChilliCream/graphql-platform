# Skipped_Sub_Selection_From_Different_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    averageRating @skip(if: true)
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
          __typename
        }
      }

```

