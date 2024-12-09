# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip)
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
      query($skip: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) {
          name @skip(if: $skip)
          description
        }
      }

```

