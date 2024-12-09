# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true)
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

